/*
 *  Copyright 2013-2015 Vitalii Fedorchenko (nrecosite.com)
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License version 3
 *  as published by the Free Software Foundation
 *  You can be released from the requirements of the license by purchasing
 *  a commercial license. Buying such a license is mandatory as soon as you
 *  develop commercial activities involving the NReco Recommender software without
 *  disclosing the source code of your own applications.
 *  These activities include: offering paid services to customers as an ASP,
 *  making recommendations in a web application, shipping NReco Recommender with a closed
 *  source product.
 *
 *  For more information, please contact: support@nrecosite.com 
 *  
 *  Parts of this code are based on Apache Mahout ("Taste") that was licensed under the
 *  Apache 2.0 License (see http://www.apache.org/licenses/LICENSE-2.0).
 *
 *  Unless required by applicable law or agreed to in writing, software distributed on an
 *  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using NReco.CF.Taste.Common;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Model;
using NReco.CF;

namespace NReco.CF.Taste.Impl.Recommender.SVD {

/// <summary>
/// Minimalistic implementation of Parallel SGD factorizer based on
/// <a href="http://www.sze.hu/~gtakacs/download/jmlr_2009.pdf">
/// "Scalable Collaborative Filtering Approaches for Large Recommender Systems"</a>
/// and
/// <a href="hwww.cs.wisc.edu/~brecht/papers/hogwildTR.pdf">
/// "Hogwild!: A Lock-Free Approach to Parallelizing Stochastic Gradient Descent"</a> 
/// </summary>
public class ParallelSGDFactorizer : AbstractFactorizer {

  private IDataModel dataModel;
  /// Parameter used to prevent overfitting. 
  private double lambda;
  /// Number of features used to compute this factorization 
  private int rank;
  /// Number of iterations 
  private int numEpochs;

  private int numThreads;

  // these next two control decayFactor^steps exponential type of annealing learning rate and decay factor
  private double mu0 = 0.01;
  private double decayFactor = 1;
  // these next two control 1/steps^forget type annealing
  private int stepOffset = 0;
  // -1 equals even weighting of all examples, 0 means only use exponential annealing
  private double forgettingExponent = 0;

  // The following two should be inversely proportional :)
  private double biasMuRatio = 0.5;
  private double biasLambdaRatio = 0.1;

  /// TODO: this is not safe as += is not atomic on many processors, can be replaced with AtomicDoubleArray
   /// but it works just fine right now  
  /// user features 
  protected volatile double[][] userVectors;
  /// item features 
  protected volatile double[][] itemVectors;

  private PreferenceShuffler shuffler;

  private int epoch = 1;
  /// place in user vector where the bias is stored 
  private static int USER_BIAS_INDEX = 1;
  /// place in item vector where the bias is stored 
  private static int ITEM_BIAS_INDEX = 2;
  private static int FEATURE_OFFSET = 3;
  /// Standard deviation for random initialization of features 
  private static double NOISE = 0.02;

  private static Logger logger = LoggerFactory.GetLogger(typeof(ParallelSGDFactorizer));

  public class PreferenceShuffler {

    private IPreference[] preferences;
    private IPreference[] unstagedPreferences;

    protected RandomWrapper random = RandomUtils.getRandom();

    public PreferenceShuffler(IDataModel dataModel) {
      cachePreferences(dataModel);
      shuffle();
      stage();
    }

    private int countPreferences(IDataModel dataModel) {
      int numPreferences = 0;
      var userIDs = dataModel.GetUserIDs();
      while (userIDs.MoveNext()) {
        IPreferenceArray preferencesFromUser = dataModel.GetPreferencesFromUser(userIDs.Current);
        numPreferences += preferencesFromUser.Length();
      }
      return numPreferences;
    }

    private void cachePreferences(IDataModel dataModel) {
      int numPreferences = countPreferences(dataModel);
      preferences = new IPreference[numPreferences];

      var userIDs = dataModel.GetUserIDs();
      int index = 0;
      while (userIDs.MoveNext()) {
        long userID = userIDs.Current;
        IPreferenceArray preferencesFromUser = dataModel.GetPreferencesFromUser(userID);
        foreach (IPreference preference in preferencesFromUser) {
          preferences[index++] = preference;
        }
      }
    }

    public void shuffle() {
      unstagedPreferences = (IPreference[]) preferences.Clone();
      /// Durstenfeld shuffle 
      for (int i = unstagedPreferences.Length - 1; i > 0; i--) {
        int rand = random.nextInt(i + 1);
        swapCachedPreferences(i, rand);
      }
    }

    //merge this part into shuffle() will make compiler-optimizer do some real absurd stuff, test on OpenJDK7
    private void swapCachedPreferences(int x, int y) {
      IPreference p = unstagedPreferences[x];

      unstagedPreferences[x] = unstagedPreferences[y];
      unstagedPreferences[y] = p;
    }

    public void stage() {
      preferences = unstagedPreferences;
    }

    public IPreference get(int i) {
      return preferences[i];
    }

    public int size() {
      return preferences.Length;
    }

  }

  public ParallelSGDFactorizer(IDataModel dataModel, int numFeatures, double lambda, int numEpochs) : base(dataModel)
    {
    this.dataModel = dataModel;
    this.rank = numFeatures + FEATURE_OFFSET;
    this.lambda = lambda;
    this.numEpochs = numEpochs;

    shuffler = new PreferenceShuffler(dataModel);

    //max thread num set to n^0.25 as suggested by hogwild! paper
    numThreads = Math.Min(Environment.ProcessorCount, (int) Math.Pow((double) shuffler.size(), 0.25));
  }

  public ParallelSGDFactorizer(IDataModel dataModel, int numFeatures, double lambda, int numIterations,
      double mu0, double decayFactor, int stepOffset, double forgettingExponent) : this(dataModel, numFeatures, lambda, numIterations) {

    this.mu0 = mu0;
    this.decayFactor = decayFactor;
    this.stepOffset = stepOffset;
    this.forgettingExponent = forgettingExponent;
  }

  public ParallelSGDFactorizer(IDataModel dataModel, int numFeatures, double lambda, int numIterations,
      double mu0, double decayFactor, int stepOffset, double forgettingExponent, int numThreads) :
	  this(dataModel, numFeatures, lambda, numIterations, mu0, decayFactor, stepOffset, forgettingExponent)
	{

    this.numThreads = numThreads;
  }

  public ParallelSGDFactorizer(IDataModel dataModel, int numFeatures, double lambda, int numIterations,
      double mu0, double decayFactor, int stepOffset, double forgettingExponent,
      double biasMuRatio, double biasLambdaRatio) :
	this(dataModel, numFeatures, lambda, numIterations, mu0, decayFactor, stepOffset, forgettingExponent) {

    this.biasMuRatio = biasMuRatio;
    this.biasLambdaRatio = biasLambdaRatio;
  }

  public ParallelSGDFactorizer(IDataModel dataModel, int numFeatures, double lambda, int numIterations,
      double mu0, double decayFactor, int stepOffset, double forgettingExponent,
      double biasMuRatio, double biasLambdaRatio, int numThreads) :
	this(dataModel, numFeatures, lambda, numIterations, mu0, decayFactor, stepOffset, forgettingExponent, biasMuRatio,
         biasLambdaRatio)
  {

    this.numThreads = numThreads;
  }

  protected void initialize() {
    RandomWrapper random = RandomUtils.getRandom();
    userVectors = new double[dataModel.GetNumUsers()][];
    itemVectors = new double[dataModel.GetNumItems()][];

    double globalAverage = getAveragePreference();
    for (int userIndex = 0; userIndex < userVectors.Length; userIndex++) {
		userVectors[userIndex] = new double[rank];

      userVectors[userIndex][0] = globalAverage;
      userVectors[userIndex][USER_BIAS_INDEX] = 0; // will store user bias
      userVectors[userIndex][ITEM_BIAS_INDEX] = 1; // corresponding item feature contains item bias
      for (int feature = FEATURE_OFFSET; feature < rank; feature++) {
        userVectors[userIndex][feature] = random.nextGaussian() * NOISE;
      }
    }
    for (int itemIndex = 0; itemIndex < itemVectors.Length; itemIndex++) {
		itemVectors[itemIndex] = new double[rank];

      itemVectors[itemIndex][0] = 1; // corresponding user feature contains global average
      itemVectors[itemIndex][USER_BIAS_INDEX] = 1; // corresponding user feature contains user bias
      itemVectors[itemIndex][ITEM_BIAS_INDEX] = 0; // will store item bias
      for (int feature = FEATURE_OFFSET; feature < rank; feature++) {
        itemVectors[itemIndex][feature] = random.nextGaussian() * NOISE;
      }
    }
  }

  //TODO: needs optimization
  private double getMu(int i) {
    return mu0 * Math.Pow(decayFactor, i - 1) * Math.Pow(i + stepOffset, forgettingExponent);
  }

  public override Factorization Factorize() {
    initialize();

    logger.Info("starting to compute the factorization...");
    for (epoch = 1; epoch <= numEpochs; epoch++) {
      shuffler.stage();

      double mu = getMu(epoch);
      int subSize = shuffler.size() / numThreads + 1;

	  Task[] tasks = new Task[numThreads];

      try {
        for (int t = 0; t < numThreads; t++) {
          int iStart = t * subSize;
          int iEnd = Math.Min((t + 1) * subSize, shuffler.size());
		  
          tasks[t] = Task.Factory.StartNew( () => {
              for (int i = iStart; i < iEnd; i++) {
                update(shuffler.get(i), mu);
              }
		  });

        }
      } finally {
		  Task.WaitAll(tasks, numEpochs * shuffler.size());
        shuffler.shuffle();

        /*try {
          bool terminated = executor.awaitTermination(numEpochs * shuffler.size(), TimeUnit.MICROSECONDS);
          if (!terminated) {
            logger.error("subtasks takes forever, return anyway");
          }
        } catch (InterruptedException e) {
          throw new TasteException("waiting fof termination interrupted", e);
        }*/
      }

    }

    return createFactorization(userVectors, itemVectors);
  }

  double getAveragePreference() {
    IRunningAverage average = new FullRunningAverage();
    var it = dataModel.GetUserIDs();
    while (it.MoveNext()) {
      foreach (IPreference pref in dataModel.GetPreferencesFromUser(it.Current)) {
        average.AddDatum(pref.GetValue());
      }
    }
    return average.GetAverage();
  }

  /// TODO: this is the vanilla sgd by Tacaks 2009, I speculate that using scaling technique proposed in:
   /// Towards Optimal One Pass Large Scale Learning with Averaged Stochastic Gradient Descent section 5, page 6
   /// can be beneficial in term s of both speed and accuracy.
   ///
   /// Tacaks' method doesn't calculate gradient of regularization correctly, which has non-zero elements everywhere of
   /// the matrix. While Tacaks' method can only updates a single row/column, if one user has a lot of recommendation,
   /// her vector will be more affected by regularization using an isolated scaling factor for both user vectors and
   /// item vectors can remove this issue without inducing more update cost it even reduces it a bit by only performing
   /// one addition and one multiplication.
   ///
   /// BAD SIDE1: the scaling factor decreases fast, it has to be scaled up from time to time before dropped to zero or
   ///            caused roundoff error
   /// BAD SIDE2: no body experiment on it before, and people generally use very small lambda
   ///            so it's impact on accuracy may still be unknown.
   /// BAD SIDE3: don't know how to make it work for L1-regularization or
   ///            "pseudorank?" (sum of singular values)-regularization 
  protected void update(IPreference preference, double mu) {
    int userIdx = userIndex(preference.GetUserID());
    int itemIdx = itemIndex(preference.GetItemID());

    double[] userVector = userVectors[userIdx];
    double[] itemVector = itemVectors[itemIdx];

    double prediction = dot(userVector, itemVector);
    double err = preference.GetValue() - prediction;

    // adjust features
    for (int k = FEATURE_OFFSET; k < rank; k++) {
      double userFeature = userVector[k];
      double itemFeature = itemVector[k];

      userVector[k] += mu * (err * itemFeature - lambda * userFeature);
      itemVector[k] += mu * (err * userFeature - lambda * itemFeature);
    }

    // adjust user and item bias
    userVector[USER_BIAS_INDEX] += biasMuRatio * mu * (err - biasLambdaRatio * lambda * userVector[USER_BIAS_INDEX]);
    itemVector[ITEM_BIAS_INDEX] += biasMuRatio * mu * (err - biasLambdaRatio * lambda * itemVector[ITEM_BIAS_INDEX]);
  }

  private double dot(double[] userVector, double[] itemVector) {
    double sum = 0;
    for (int k = 0; k < rank; k++) {
      sum += userVector[k] * itemVector[k];
    }
    return sum;
  }
}
}