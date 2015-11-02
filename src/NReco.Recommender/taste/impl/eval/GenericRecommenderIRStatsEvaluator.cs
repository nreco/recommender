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

using NReco.CF.Taste.Common;
using NReco.CF.Taste.Eval;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Impl.Model;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;
using NReco.CF;
using NReco.Math3.Util;

namespace NReco.CF.Taste.Impl.Eval {

/// <summary>
/// For each user, these implementation determine the top <code>n</code> preferences, then evaluate the IR
/// statistics based on a <see cref="IDataModel"/> that does not have these values. This number <code>n</code> is the
/// "at" value, as in "precision at 5". For example, this would mean precision evaluated by removing the top 5
/// preferences for a user and then finding the percentage of those 5 items included in the top 5
/// recommendations for that user.
/// </summary>
public sealed class GenericRecommenderIRStatsEvaluator : IRecommenderIRStatsEvaluator {

  private static Logger log = LoggerFactory.GetLogger(typeof(GenericRecommenderIRStatsEvaluator));

  private static double LOG2 = MathUtil.Log(2.0);

   /// Pass as "relevanceThreshold" argument to
   /// {@link #evaluate(RecommenderBuilder, DataModelBuilder, DataModel, IDRescorer, int, double, double)} to
   /// have it attempt to compute a reasonable threshold. Note that this will impact performance.
  public const double CHOOSE_THRESHOLD = Double.NaN;

  private RandomWrapper random;
  private IRelevantItemsDataSplitter dataSplitter;

  public GenericRecommenderIRStatsEvaluator()
	  : this(new GenericRelevantItemsDataSplitter()) {
  }

  public GenericRecommenderIRStatsEvaluator(IRelevantItemsDataSplitter dataSplitter) {
    //Preconditions.checkNotNull(dataSplitter);
    random = RandomUtils.getRandom();
    this.dataSplitter = dataSplitter;
  }

  public IRStatistics Evaluate(IRecommenderBuilder recommenderBuilder,
                               IDataModelBuilder dataModelBuilder,
                               IDataModel dataModel,
                               IDRescorer rescorer,
                               int at,
                               double relevanceThreshold,
                               double evaluationPercentage) {

    //Preconditions.checkArgument(recommenderBuilder != null, "recommenderBuilder is null");
    //Preconditions.checkArgument(dataModel != null, "dataModel is null");
    //Preconditions.checkArgument(at >= 1, "at must be at least 1");
    //Preconditions.checkArgument(evaluationPercentage > 0.0 && evaluationPercentage <= 1.0,
    //    "Invalid evaluationPercentage: " + evaluationPercentage + ". Must be: 0.0 < evaluationPercentage <= 1.0");

    int numItems = dataModel.GetNumItems();
    IRunningAverage precision = new FullRunningAverage();
    IRunningAverage recall = new FullRunningAverage();
    IRunningAverage fallOut = new FullRunningAverage();
    IRunningAverage nDCG = new FullRunningAverage();
    int numUsersRecommendedFor = 0;
    int numUsersWithRecommendations = 0;

    var it = dataModel.GetUserIDs();
    while (it.MoveNext()) {

      long userID = it.Current;

      if (random.nextDouble() >= evaluationPercentage) {
        // Skipped
        continue;
      }

	  var stopWatch = new System.Diagnostics.Stopwatch();
	  stopWatch.Start();

      IPreferenceArray prefs = dataModel.GetPreferencesFromUser(userID);

      // List some most-preferred items that would count as (most) "relevant" results
      double theRelevanceThreshold = Double.IsNaN(relevanceThreshold) ? computeThreshold(prefs) : relevanceThreshold;
      FastIDSet relevantItemIDs = dataSplitter.GetRelevantItemsIDs(userID, at, theRelevanceThreshold, dataModel);

      int numRelevantItems = relevantItemIDs.Count();
      if (numRelevantItems <= 0) {
        continue;
      }

      FastByIDMap<IPreferenceArray> trainingUsers = new FastByIDMap<IPreferenceArray>(dataModel.GetNumUsers());
      var it2 = dataModel.GetUserIDs();
      while (it2.MoveNext()) {
        dataSplitter.ProcessOtherUser(userID, relevantItemIDs, trainingUsers, it2.Current, dataModel);
      }

      IDataModel trainingModel = dataModelBuilder == null ? new GenericDataModel(trainingUsers)
          : dataModelBuilder.BuildDataModel(trainingUsers);
      try {
        trainingModel.GetPreferencesFromUser(userID);
      } catch (NoSuchUserException nsee) {
        continue; // Oops we excluded all prefs for the user -- just move on
      }

      int size = numRelevantItems + trainingModel.GetItemIDsFromUser(userID).Count();
      if (size < 2 * at) {
        // Really not enough prefs to meaningfully evaluate this user
        continue;
      }

      IRecommender recommender = recommenderBuilder.BuildRecommender(trainingModel);

      int intersectionSize = 0;
      var recommendedItems = recommender.Recommend(userID, at, rescorer);
      foreach (IRecommendedItem recommendedItem in recommendedItems) {
        if (relevantItemIDs.Contains(recommendedItem.GetItemID())) {
          intersectionSize++;
        }
      }

      int numRecommendedItems = recommendedItems.Count;

      // Precision
      if (numRecommendedItems > 0) {
        precision.AddDatum((double) intersectionSize / (double) numRecommendedItems);
      }

      // Recall
      recall.AddDatum((double) intersectionSize / (double) numRelevantItems);

      // Fall-out
      if (numRelevantItems < size) {
        fallOut.AddDatum((double) (numRecommendedItems - intersectionSize)
                         / (double) (numItems - numRelevantItems));
      }

      // nDCG
      // In computing, assume relevant IDs have relevance 1 and others 0
      double cumulativeGain = 0.0;
      double idealizedGain = 0.0;
      for (int i = 0; i < numRecommendedItems; i++) {
        IRecommendedItem item = recommendedItems[i];
        double discount = 1.0 / log2(i + 2.0); // Classical formulation says log(i+1), but i is 0-based here
        if (relevantItemIDs.Contains(item.GetItemID())) {
          cumulativeGain += discount;
        }
        // otherwise we're multiplying discount by relevance 0 so it doesn't do anything

        // Ideally results would be ordered with all relevant ones first, so this theoretical
        // ideal list starts with number of relevant items equal to the total number of relevant items
        if (i < numRelevantItems) {
          idealizedGain += discount;
        }
      }
      if (idealizedGain > 0.0) {
        nDCG.AddDatum(cumulativeGain / idealizedGain);
      }

      // Reach
      numUsersRecommendedFor++;
      if (numRecommendedItems > 0) {
        numUsersWithRecommendations++;
      }

	  stopWatch.Stop();

      log.Info("Evaluated with user {} in {}ms", userID, stopWatch.ElapsedMilliseconds);
      log.Info("Precision/recall/fall-out/nDCG/reach: {} / {} / {} / {} / {}",
               precision.GetAverage(), recall.GetAverage(), fallOut.GetAverage(), nDCG.GetAverage(),
               (double) numUsersWithRecommendations / (double) numUsersRecommendedFor);
    }

    return new IRStatisticsImpl(
        precision.GetAverage(),
        recall.GetAverage(),
        fallOut.GetAverage(),
        nDCG.GetAverage(),
        (double) numUsersWithRecommendations / (double) numUsersRecommendedFor);
  }

  private static double computeThreshold(IPreferenceArray prefs) {
    if (prefs.Length() < 2) {
      // Not enough data points -- return a threshold that allows everything
      return Double.NegativeInfinity;
    }
    IRunningAverageAndStdDev stdDev = new FullRunningAverageAndStdDev();
    int size = prefs.Length();
    for (int i = 0; i < size; i++) {
      stdDev.AddDatum(prefs.GetValue(i));
    }
    return stdDev.GetAverage() + stdDev.GetStandardDeviation();
  }

  private static double log2(double value) {
    return MathUtil.Log(value) / LOG2;
  }

}

}