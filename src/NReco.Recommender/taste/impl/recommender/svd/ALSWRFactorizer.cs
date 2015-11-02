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
using System.Threading.Tasks;

using NReco.CF.Taste.Common;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Model;
using NReco.CF;
using NReco.Math3;
using NReco.Math3.Als;

//using NReco.Math3.map;


namespace NReco.CF.Taste.Impl.Recommender.SVD {
	
/// <summary>
/// Factorizes the rating matrix using "Alternating-Least-Squares with Weighted-λ-Regularization" as described in
/// <a href="http://www.hpl.hp.com/personal/Robert_Schreiber/papers/2008%20AAIM%20Netflix/netflix_aaim08(submitted).pdf">
/// "Large-scale Collaborative Filtering for the Netflix Prize"</a>
///
///  also supports the implicit feedback variant of this approach as described in "Collaborative Filtering for Implicit
///  Feedback Datasets" available at http://research.yahoo.com/pub/2433
/// </summary>
public class ALSWRFactorizer : AbstractFactorizer {

  private IDataModel dataModel;

  /// number of features used to compute this factorization 
  private int numFeatures;
  /// parameter to control the regularization 
  private double lambda;
  /// number of iterations 
  private int numIterations;

  private bool usesImplicitFeedback;
  /// confidence weighting parameter, only necessary when working with implicit feedback 
  private double alpha;

  private int numTrainingThreads;

  private static double DEFAULT_ALPHA = 40;

  private static Logger log = LoggerFactory.GetLogger(typeof(ALSWRFactorizer));

  public ALSWRFactorizer(IDataModel dataModel, int numFeatures, double lambda, int numIterations,
      bool usesImplicitFeedback, double alpha, int numTrainingThreads) : base(dataModel) {
    this.dataModel = dataModel;
    this.numFeatures = numFeatures;
    this.lambda = lambda;
    this.numIterations = numIterations;
    this.usesImplicitFeedback = usesImplicitFeedback;
    this.alpha = alpha;
    this.numTrainingThreads = numTrainingThreads;
  }

  public ALSWRFactorizer(IDataModel dataModel, int numFeatures, double lambda, int numIterations,
                         bool usesImplicitFeedback, double alpha) :
	this(dataModel, numFeatures, lambda, numIterations, usesImplicitFeedback, alpha,
        Environment.ProcessorCount) {
    
  }

  public ALSWRFactorizer(IDataModel dataModel, int numFeatures, double lambda, int numIterations) :
	 this(dataModel, numFeatures, lambda, numIterations, false, DEFAULT_ALPHA) {
  }

  public class Features {

    private IDataModel dataModel;
    private int numFeatures;

    private double[][] M;
    private double[][] U;

    public Features(ALSWRFactorizer factorizer) {
      dataModel = factorizer.dataModel;
      numFeatures = factorizer.numFeatures;
      var random = RandomUtils.getRandom();
      M = new double[dataModel.GetNumItems()][]; //numFeatures
      var itemIDsIterator = dataModel.GetItemIDs();
      while (itemIDsIterator.MoveNext()) {
        long itemID = itemIDsIterator.Current;
        int itemIDIndex = factorizer.itemIndex(itemID);
		  M[itemIDIndex] = new double[numFeatures];
        M[itemIDIndex][0] = averateRating(itemID);
        for (int feature = 1; feature < numFeatures; feature++) {
          M[itemIDIndex][feature] = random.nextDouble() * 0.1;
        }
      }

      U = new double[dataModel.GetNumUsers()][]; //numFeatures
	  for (int i=0; i<U.Length; i++)
		  U[i] = new double[numFeatures];
    }

    public double[][] getM() {
      return M;
    }

    public double[][] getU() {
      return U;
    }

    public double[] getUserFeatureColumn(int index) {
      return U[index]; //new DenseVector(
    }

    public double[] getItemFeatureColumn(int index) {
      return M[index];
    }

    public void setFeatureColumnInU(int idIndex, double[] vector) {
      setFeatureColumn(U, idIndex, vector);
    }

    public void setFeatureColumnInM(int idIndex, double[] vector) {
      setFeatureColumn(M, idIndex, vector);
    }

    protected void setFeatureColumn(double[][] matrix, int idIndex, double[] vector) {
      for (int feature = 0; feature < numFeatures; feature++) {
        matrix[idIndex][feature] = vector[feature];
      }
    }

    public double averateRating(long itemID) {
      IPreferenceArray prefs = dataModel.GetPreferencesForItem(itemID);
      IRunningAverage avg = new FullRunningAverage();
      foreach (IPreference pref in prefs) {
        avg.AddDatum(pref.GetValue());
      }
      return avg.GetAverage();
    }
  }

  public override Factorization Factorize() {
    log.Info("starting to compute the factorization...");
    Features features = new Features(this);

    /// feature maps necessary for solving for implicit feedback 
    IDictionary<int, double[] > userY = null;
    IDictionary<int, double[] > itemY = null;

    if (usesImplicitFeedback) {
      userY = userFeaturesMapping(dataModel.GetUserIDs(), dataModel.GetNumUsers(), features.getU());
      itemY = itemFeaturesMapping(dataModel.GetItemIDs(), dataModel.GetNumItems(), features.getM());
    }

    IList<Task> tasks;

    for (int iteration = 0; iteration < numIterations; iteration++) {
      log.Info("iteration {0}", iteration);

      /// fix M - compute U 
      tasks = new List<Task>();
      var userIDsIterator = dataModel.GetUserIDs();
      try {

        ImplicitFeedbackAlternatingLeastSquaresSolver implicitFeedbackSolver = usesImplicitFeedback
            ? new ImplicitFeedbackAlternatingLeastSquaresSolver(numFeatures, lambda, alpha, itemY) : null;

        while (userIDsIterator.MoveNext()) {
          long userID = userIDsIterator.Current;
          var itemIDsFromUser = dataModel.GetItemIDsFromUser(userID).GetEnumerator();
          IPreferenceArray userPrefs = dataModel.GetPreferencesFromUser(userID);
          
			tasks.Add(  Task.Factory.StartNew( () => {
				  List<double[]> featureVectors = new List<double[]>();
				  while (itemIDsFromUser.MoveNext()) {
					long itemID = itemIDsFromUser.Current;
					featureVectors.Add(features.getItemFeatureColumn(itemIndex(itemID)));
				  }

				  var userFeatures = usesImplicitFeedback
					  ? implicitFeedbackSolver.solve(sparseUserRatingVector(userPrefs))
					  : AlternatingLeastSquaresSolver.solve(featureVectors, ratingVector(userPrefs), lambda, numFeatures);

				  features.setFeatureColumnInU(userIndex(userID), userFeatures);
				}
			  ) );
        }
      } finally {
		 
       // queue.shutdown();
        try {
           Task.WaitAll(tasks.ToArray(), 1000*dataModel.GetNumUsers() );
        } catch (AggregateException e) {
          log.Warn("Error when computing user features", e);
		  throw e;
        }
      }

      /// fix U - compute M 
      //queue = createQueue();
	  tasks = new List<Task>();

      var itemIDsIterator = dataModel.GetItemIDs();
      try {

        ImplicitFeedbackAlternatingLeastSquaresSolver implicitFeedbackSolver = usesImplicitFeedback
            ? new ImplicitFeedbackAlternatingLeastSquaresSolver(numFeatures, lambda, alpha, userY) : null;

        while (itemIDsIterator.MoveNext()) {
          long itemID = itemIDsIterator.Current;
          IPreferenceArray itemPrefs = dataModel.GetPreferencesForItem(itemID);

          tasks.Add(  Task.Factory.StartNew( () => {

              var featureVectors = new List<double[]>();
              foreach (IPreference pref in itemPrefs) {
                long userID = pref.GetUserID();
                featureVectors.Add(features.getUserFeatureColumn(userIndex(userID)));
              }

              var itemFeatures = usesImplicitFeedback
                  ? implicitFeedbackSolver.solve(sparseItemRatingVector(itemPrefs))
                  : AlternatingLeastSquaresSolver.solve(featureVectors, ratingVector(itemPrefs), lambda, numFeatures);

              features.setFeatureColumnInM(itemIndex(itemID), itemFeatures);
           }) );
        }
      } finally {
        
        try {
			Task.WaitAll(tasks.ToArray(), 1000 * dataModel.GetNumItems());
          //queue.awaitTermination(dataModel.getNumItems(), TimeUnit.SECONDS);
		} catch (AggregateException e) {
          log.Warn("Error when computing item features", e);
		  throw e;
        }
      }
    }

    log.Info("finished computation of the factorization...");
    return createFactorization(features.getU(), features.getM());
  }


  public static double[] ratingVector(IPreferenceArray prefs) {
    double[] ratings = new double[prefs.Length()];
    for (int n = 0; n < prefs.Length(); n++) {
      ratings[n] = prefs.Get(n).GetValue();
    }
    return ratings; //, true); new DenseVector(
  }

  //TODO find a way to get rid of the object overhead here
  protected IDictionary<int,double[]> itemFeaturesMapping(IEnumerator<long> itemIDs, int numItems,
      double[][] featureMatrix) {
    var mapping = new Dictionary<int,double[]>(numItems);
    while (itemIDs.MoveNext()) {
      long itemID = itemIDs.Current;
      mapping[ (int) itemID ] = featureMatrix[itemIndex(itemID)];
    }

    return mapping;
  }

  protected IDictionary<int,double[]> userFeaturesMapping(IEnumerator<long> userIDs, int numUsers,
      double[][] featureMatrix) {
		  var mapping = new Dictionary<int, double[]>(numUsers);

    while (userIDs.MoveNext()) {
      long userID = userIDs.Current;
	  mapping[(int)userID] = featureMatrix[userIndex(userID)];
    }

    return mapping;
  }

  protected IList<Tuple<int, double>> sparseItemRatingVector(IPreferenceArray prefs) {
	  var ratings = new List<Tuple<int, double>>(prefs.Length());
    foreach (IPreference preference in prefs) {
		ratings.Add(new Tuple<int, double>((int)preference.GetUserID(), preference.GetValue()));
    }
    return ratings;
  }

  protected IList<Tuple<int,double>> sparseUserRatingVector(IPreferenceArray prefs) {
	 var ratings = new List<Tuple<int, double>>(prefs.Length());
    foreach (IPreference preference in prefs) {
		ratings.Add(new Tuple<int, double>((int)preference.GetItemID(), preference.GetValue()));
    }
    return ratings;
  }
}

}