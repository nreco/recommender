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
using NReco.CF.Taste.Eval;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Impl.Model;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;
using NReco.CF;

namespace NReco.CF.Taste.Impl.Eval {

 /// Abstract superclass of a couple implementations, providing shared functionality.
public abstract class AbstractDifferenceRecommenderEvaluator : IRecommenderEvaluator {
  
  private static Logger log = LoggerFactory.GetLogger(typeof(AbstractDifferenceRecommenderEvaluator));
  
  private RandomWrapper random;
  private float maxPreference;
  private float minPreference;
  
  protected AbstractDifferenceRecommenderEvaluator() {
    random = RandomUtils.getRandom();
    maxPreference = float.NaN;
    minPreference = float.NaN;
  }
  
  public virtual float GetMaxPreference() {
    return maxPreference;
  }
  
  public virtual void SetMaxPreference(float maxPreference) {
    this.maxPreference = maxPreference;
  }
  
  public virtual float GetMinPreference() {
    return minPreference;
  }
  
  public virtual void SetMinPreference(float minPreference) {
    this.minPreference = minPreference;
  }
  
  public virtual double Evaluate(IRecommenderBuilder recommenderBuilder,
                         IDataModelBuilder dataModelBuilder,
                         IDataModel dataModel,
                         double trainingPercentage,
                         double evaluationPercentage) {
    //Preconditions.checkNotNull(recommenderBuilder);
    //Preconditions.checkNotNull(dataModel);
    //Preconditions.checkArgument(trainingPercentage >= 0.0 && trainingPercentage <= 1.0,
    //  "Invalid trainingPercentage: " + trainingPercentage + ". Must be: 0.0 <= trainingPercentage <= 1.0");
    //Preconditions.checkArgument(evaluationPercentage >= 0.0 && evaluationPercentage <= 1.0,
    //  "Invalid evaluationPercentage: " + evaluationPercentage + ". Must be: 0.0 <= evaluationPercentage <= 1.0");

    log.Info("Beginning evaluation using {} of {}", trainingPercentage, dataModel);
    
    int numUsers = dataModel.GetNumUsers();
    FastByIDMap<IPreferenceArray> trainingPrefs = new FastByIDMap<IPreferenceArray>(
        1 + (int) (evaluationPercentage * numUsers));
    FastByIDMap<IPreferenceArray> testPrefs = new FastByIDMap<IPreferenceArray>(
        1 + (int) (evaluationPercentage * numUsers));
    
    var it = dataModel.GetUserIDs();
    while (it.MoveNext()) {
      long userID = it.Current;
      if (random.nextDouble() < evaluationPercentage) {
        splitOneUsersPrefs(trainingPercentage, trainingPrefs, testPrefs, userID, dataModel);
      }
    }
    
    IDataModel trainingModel = dataModelBuilder == null ? new GenericDataModel(trainingPrefs)
        : dataModelBuilder.BuildDataModel(trainingPrefs);
    
    IRecommender recommender = recommenderBuilder.BuildRecommender(trainingModel);
    
    double result = getEvaluation(testPrefs, recommender);
    log.Info("Evaluation result: {}", result);
    return result;
  }
  
  private void splitOneUsersPrefs(double trainingPercentage,
                                  FastByIDMap<IPreferenceArray> trainingPrefs,
                                  FastByIDMap<IPreferenceArray> testPrefs,
                                  long userID,
                                  IDataModel dataModel) {
    List<IPreference> oneUserTrainingPrefs = null;
    List<IPreference> oneUserTestPrefs = null;
    IPreferenceArray prefs = dataModel.GetPreferencesFromUser(userID);
    int size = prefs.Length();
    for (int i = 0; i < size; i++) {
      IPreference newPref = new GenericPreference(userID, prefs.GetItemID(i), prefs.GetValue(i));
      if (random.nextDouble() < trainingPercentage) {
        if (oneUserTrainingPrefs == null) {
          oneUserTrainingPrefs = new List<IPreference>(3);
        }
        oneUserTrainingPrefs.Add(newPref);
      } else {
        if (oneUserTestPrefs == null) {
          oneUserTestPrefs = new List<IPreference>(3);
        }
        oneUserTestPrefs.Add(newPref);
      }
    }
    if (oneUserTrainingPrefs != null) {
      trainingPrefs.Put(userID, new GenericUserPreferenceArray(oneUserTrainingPrefs));
      if (oneUserTestPrefs != null) {
        testPrefs.Put(userID, new GenericUserPreferenceArray(oneUserTestPrefs));
      }
    }
  }

  private float capEstimatedPreference(float estimate) {
    if (estimate > maxPreference) {
      return maxPreference;
    }
    if (estimate < minPreference) {
      return minPreference;
    }
    return estimate;
  }

  private double getEvaluation(FastByIDMap<IPreferenceArray> testPrefs, IRecommender recommender)
  {
    reset();
    var estimateCallables = new List<Action>();
    AtomicInteger noEstimateCounter = new AtomicInteger();
    foreach (var entry in testPrefs.EntrySet()) {
      estimateCallables.Add( () => {
		  var testUserID = entry.Key;
		  var prefs = entry.Value;

		  foreach (IPreference realPref in prefs) {
			float estimatedPreference = float.NaN;
			try {
			  estimatedPreference = recommender.EstimatePreference(testUserID, realPref.GetItemID());
			} catch (NoSuchUserException nsue) {
			  // It's possible that an item exists in the test data but not training data in which case
			  // NSEE will be thrown. Just ignore it and move on.
			  log.Info("User exists in test data but not training data: {}", testUserID);
			} catch (NoSuchItemException nsie) {
			  log.Info("Item exists in test data but not training data: {}", realPref.GetItemID());
			}
			if (float.IsNaN(estimatedPreference)) {
			  noEstimateCounter.incrementAndGet();
			} else {
			  estimatedPreference = capEstimatedPreference(estimatedPreference);
			  processOneEstimate(estimatedPreference, realPref);
			}
		  }


	  });
         // new PreferenceEstimateCallable(recommender, entry.Key, entry.Value, noEstimateCounter));
    }
    log.Info("Beginning evaluation of {} users", estimateCallables.Count);
    IRunningAverageAndStdDev timing = new FullRunningAverageAndStdDev();
    execute(estimateCallables, noEstimateCounter, timing);
    return computeFinalEvaluation();
  }
  
  public static void execute(List<Action> callables,
                                AtomicInteger noEstimateCounter,
                                IRunningAverageAndStdDev timing) {

    List<Action> wrappedCallables = wrapWithStatsCallables(callables, noEstimateCounter, timing);
    int numProcessors = Environment.ProcessorCount;
    var tasks = new Task[wrappedCallables.Count];
    log.Info("Starting timing of {} tasks in {} threads", wrappedCallables.Count, numProcessors);
    try {
		for (int i=0; i<tasks.Length; i++)
			tasks[i] = Task.Factory.StartNew( wrappedCallables[i] );
		Task.WaitAll( tasks, 10000 ); // 10 sec
      /*List<Future<Void>> futures = executor.invokeAll(wrappedCallables);
      // Go look for exceptions here, really
      for (Future<Void> future : futures) {
        future.get();
      }*/

    } catch (Exception e) {
      throw new TasteException(e.Message, e);
    }
    
    /*executor.shutdown();
    try {
      executor.awaitTermination(10, TimeUnit.SECONDS);
    } catch (InterruptedException e) {
      throw new TasteException(e.getCause());
    }*/
  }
  
  private static List<Action> wrapWithStatsCallables(List<Action> callables,
                                                                   AtomicInteger noEstimateCounter,
                                                                   IRunningAverageAndStdDev timing) {
    List<Action> wrapped = new List<Action>();
    for (int count=0; count<callables.Count; count++) {
      bool logStats = count % 1000 == 0; // log every 1000 or so iterations
	  wrapped.Add(new StatsCallable(callables[count], logStats, timing, noEstimateCounter).call);
    }
    return wrapped;
  }
  
  protected abstract void reset();
  
  protected abstract void processOneEstimate(float estimatedPreference, IPreference realPref);
  
  protected abstract double computeFinalEvaluation();

 /* public sealed class PreferenceEstimateCallable {

    private Recommender recommender;
    private long testUserID;
    private PreferenceArray prefs;
    private AtomicInteger noEstimateCounter;

    public PreferenceEstimateCallable(Recommender recommender,
                                      long testUserID,
                                      PreferenceArray prefs,
                                      AtomicInteger noEstimateCounter) {
      this.recommender = recommender;
      this.testUserID = testUserID;
      this.prefs = prefs;
      this.noEstimateCounter = noEstimateCounter;
    }

    public void call() {
      foreach (Preference realPref in prefs) {
        float estimatedPreference = float.NaN;
        try {
          estimatedPreference = recommender.estimatePreference(testUserID, realPref.getItemID());
        } catch (NoSuchUserException nsue) {
          // It's possible that an item exists in the test data but not training data in which case
          // NSEE will be thrown. Just ignore it and move on.
          log.info("User exists in test data but not training data: {}", testUserID);
        } catch (NoSuchItemException nsie) {
          log.info("Item exists in test data but not training data: {}", realPref.getItemID());
        }
        if (float.IsNaN(estimatedPreference)) {
          noEstimateCounter.incrementAndGet();
        } else {
          estimatedPreference = capEstimatedPreference(estimatedPreference);
          processOneEstimate(estimatedPreference, realPref);
        }
      }
    }

  }*/

}

}