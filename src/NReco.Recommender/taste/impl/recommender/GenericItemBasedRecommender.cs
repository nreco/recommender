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
using NReco.CF.Taste.Recommender;

using NReco.CF.Taste.Common;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Similarity;
using NReco.CF;

namespace NReco.CF.Taste.Impl.Recommender {

/// <summary>
/// <para>
/// A simple <see cref="NReco.CF.Taste.Recommender.IRecommender"/> which uses a given
/// <see cref="NReco.CF.Taste.Model.IDataModel"/> and
/// <see cref="NReco.CF.Taste.Similarity.IItemSimilarity"/> to produce recommendations. This class
/// represents Taste's support for item-based recommenders.
/// </para>
/// 
/// <para>
/// The <see cref="NReco.CF.Taste.Similarity.IItemSimilarity"/> is the most important point to discuss
/// here. Item-based recommenders are useful because they can take advantage of something to be very fast: they
/// base their computations on item similarity, not user similarity, and item similarity is relatively static.
/// It can be precomputed, instead of re-computed in real time.
/// </para>
/// 
/// <para>
/// Thus it's strongly recommended that you use
/// <see cref="NReco.CF.Taste.Impl.Similarity.GenericItemSimilarity"/> with pre-computed similarities if
/// you're going to use this class. You can use
/// <see cref="NReco.CF.Taste.Impl.Similarity.PearsonCorrelationSimilarity"/> too, which computes
/// similarities in real-time, but will probably find this painfully slow for large amounts of data.
/// </para>
/// </summary>
public class GenericItemBasedRecommender : AbstractRecommender, IItemBasedRecommender {
  
  private static Logger log = LoggerFactory.GetLogger(typeof(GenericItemBasedRecommender));
  
  private IItemSimilarity similarity;
  private IMostSimilarItemsCandidateItemsStrategy mostSimilarItemsCandidateItemsStrategy;
  private RefreshHelper refreshHelper;
  private EstimatedPreferenceCapper capper;

  private static bool EXCLUDE_ITEM_IF_NOT_SIMILAR_TO_ALL_BY_DEFAULT = true;

  public GenericItemBasedRecommender(IDataModel dataModel,
                                     IItemSimilarity similarity,
                                     ICandidateItemsStrategy candidateItemsStrategy,
                                     IMostSimilarItemsCandidateItemsStrategy mostSimilarItemsCandidateItemsStrategy) :
	  base(dataModel, candidateItemsStrategy) {
    //Preconditions.checkArgument(similarity != null, "similarity is null");
    this.similarity = similarity;
    //Preconditions.checkArgument(mostSimilarItemsCandidateItemsStrategy != null,
    //    "mostSimilarItemsCandidateItemsStrategy is null");
    this.mostSimilarItemsCandidateItemsStrategy = mostSimilarItemsCandidateItemsStrategy;
    this.refreshHelper = new RefreshHelper( () => {
        capper = buildCapper();
    });
    refreshHelper.AddDependency(dataModel);
    refreshHelper.AddDependency(similarity);
    refreshHelper.AddDependency(candidateItemsStrategy);
    refreshHelper.AddDependency(mostSimilarItemsCandidateItemsStrategy);
    capper = buildCapper();
  }

  public GenericItemBasedRecommender(IDataModel dataModel, IItemSimilarity similarity) :
	  this(dataModel,
		 similarity,
		 AbstractRecommender.GetDefaultCandidateItemsStrategy(),
		 getDefaultMostSimilarItemsCandidateItemsStrategy()) {
  }

  protected static IMostSimilarItemsCandidateItemsStrategy getDefaultMostSimilarItemsCandidateItemsStrategy() {
    return new PreferredItemsNeighborhoodCandidateItemsStrategy();
  }

  public IItemSimilarity getSimilarity() {
    return similarity;
  }
  
  public override IList<IRecommendedItem> Recommend(long userID, int howMany, IDRescorer rescorer) {
    //Preconditions.checkArgument(howMany >= 1, "howMany must be at least 1");
    log.Debug("Recommending items for user ID '{}'", userID);

    IPreferenceArray preferencesFromUser = GetDataModel().GetPreferencesFromUser(userID);
    if (preferencesFromUser.Length() == 0) {
      return new List<IRecommendedItem>();
    }

    FastIDSet possibleItemIDs = GetAllOtherItems(userID, preferencesFromUser);

    TopItems.IEstimator<long> estimator = new Estimator(this, userID, preferencesFromUser);

    List<IRecommendedItem> topItems = TopItems.GetTopItems(howMany, possibleItemIDs.GetEnumerator(), rescorer,
      estimator);

    log.Debug("Recommendations are: {}", topItems);
    return topItems;
  }
  
  public override float EstimatePreference(long userID, long itemID) {
    IPreferenceArray preferencesFromUser = GetDataModel().GetPreferencesFromUser(userID);
    float? actualPref = getPreferenceForItem(preferencesFromUser, itemID);
    if (actualPref.HasValue) {
      return actualPref.Value;
    }
    return doEstimatePreference(userID, preferencesFromUser, itemID);
  }

  private static float? getPreferenceForItem(IPreferenceArray preferencesFromUser, long itemID) {
    int size = preferencesFromUser.Length();
    for (int i = 0; i < size; i++) {
      if (preferencesFromUser.GetItemID(i) == itemID) {
        return preferencesFromUser.GetValue(i);
      }
    }
    return null;
  }

  public List<IRecommendedItem> MostSimilarItems(long itemID, int howMany) {
    return MostSimilarItems(itemID, howMany, null);
  }
  
  public List<IRecommendedItem> MostSimilarItems(long itemID, int howMany,
                                                IRescorer<Tuple<long,long>> rescorer) {
    TopItems.IEstimator<long> estimator = new MostSimilarEstimator(itemID, similarity, rescorer);
    return doMostSimilarItems(new long[] {itemID}, howMany, estimator);
  }
  
  public List<IRecommendedItem> MostSimilarItems(long[] itemIDs, int howMany) {
    TopItems.IEstimator<long> estimator = new MultiMostSimilarEstimator(itemIDs, similarity, null,
        EXCLUDE_ITEM_IF_NOT_SIMILAR_TO_ALL_BY_DEFAULT);
    return doMostSimilarItems(itemIDs, howMany, estimator);
  }
  
  public List<IRecommendedItem> MostSimilarItems(long[] itemIDs, int howMany,
                                                IRescorer<Tuple<long,long>> rescorer) {
    TopItems.IEstimator<long> estimator = new MultiMostSimilarEstimator(itemIDs, similarity, rescorer,
        EXCLUDE_ITEM_IF_NOT_SIMILAR_TO_ALL_BY_DEFAULT);
    return doMostSimilarItems(itemIDs, howMany, estimator);
  }

  public List<IRecommendedItem> MostSimilarItems(long[] itemIDs,
                                                int howMany,
                                                bool excludeItemIfNotSimilarToAll) {
    TopItems.IEstimator<long> estimator = new MultiMostSimilarEstimator(itemIDs, similarity, null,
        excludeItemIfNotSimilarToAll);
    return doMostSimilarItems(itemIDs, howMany, estimator);
  }

  public List<IRecommendedItem> MostSimilarItems(long[] itemIDs, int howMany,
                                                IRescorer<Tuple<long,long>> rescorer,
                                                bool excludeItemIfNotSimilarToAll) {
    TopItems.IEstimator<long> estimator = new MultiMostSimilarEstimator(itemIDs, similarity, rescorer,
        excludeItemIfNotSimilarToAll);
    return doMostSimilarItems(itemIDs, howMany, estimator);
  }

  public List<IRecommendedItem> RecommendedBecause(long userID, long itemID, int howMany) {
    //Preconditions.checkArgument(howMany >= 1, "howMany must be at least 1");

    IDataModel model = GetDataModel();
    TopItems.IEstimator<long> estimator = new RecommendedBecauseEstimator(this, userID, itemID);

    IPreferenceArray prefs = model.GetPreferencesFromUser(userID);
    int size = prefs.Length();
    FastIDSet allUserItems = new FastIDSet(size);
    for (int i = 0; i < size; i++) {
      allUserItems.Add(prefs.GetItemID(i));
    }
    allUserItems.Remove(itemID);

    return TopItems.GetTopItems(howMany, allUserItems.GetEnumerator(), null, estimator);
  }
  
  private List<IRecommendedItem> doMostSimilarItems(long[] itemIDs,
                                                   int howMany,
                                                   TopItems.IEstimator<long> estimator) {
    FastIDSet possibleItemIDs = mostSimilarItemsCandidateItemsStrategy.GetCandidateItems(itemIDs, GetDataModel());
    return TopItems.GetTopItems(howMany, possibleItemIDs.GetEnumerator(), null, estimator);
  }
  
  protected virtual float doEstimatePreference(long userID, IPreferenceArray preferencesFromUser, long itemID)
    {
    double preference = 0.0;
    double totalSimilarity = 0.0;
    int count = 0;
    double[] similarities = similarity.ItemSimilarities(itemID, preferencesFromUser.GetIDs());
    for (int i = 0; i < similarities.Length; i++) {
      double theSimilarity = similarities[i];
      if (!Double.IsNaN(theSimilarity)) {
        // Weights can be negative!
        preference += theSimilarity * preferencesFromUser.GetValue(i);
        totalSimilarity += theSimilarity;
        count++;
      }
    }
    // Throw out the estimate if it was based on no data points, of course, but also if based on
    // just one. This is a bit of a band-aid on the 'stock' item-based algorithm for the moment.
    // The reason is that in this case the estimate is, simply, the user's rating for one item
    // that happened to have a defined similarity. The similarity score doesn't matter, and that
    // seems like a bad situation.
    if (count <= 1) {
      return float.NaN;
    }
    float estimate = (float) (preference / totalSimilarity);
    if (capper != null) {
      estimate = capper.capEstimate(estimate);
    }
    return estimate;
  }

  public override void Refresh(IList<IRefreshable> alreadyRefreshed) {
    refreshHelper.Refresh(alreadyRefreshed);
  }
  
  public override string ToString() {
    return "GenericItemBasedRecommender[similarity:" + similarity + ']';
  }

  private EstimatedPreferenceCapper buildCapper() {
    IDataModel dataModel = GetDataModel();
    if (float.IsNaN(dataModel.GetMinPreference()) && float.IsNaN(dataModel.GetMaxPreference())) {
      return null;
    } else {
      return new EstimatedPreferenceCapper(dataModel);
    }
  }
  
  public class MostSimilarEstimator : TopItems.IEstimator<long> {
    
    private long toItemID;
    private IItemSimilarity similarity;
    private IRescorer<Tuple<long,long>> rescorer;

	public MostSimilarEstimator(long toItemID, IItemSimilarity similarity, IRescorer<Tuple<long, long>> rescorer) {
      this.toItemID = toItemID;
      this.similarity = similarity;
      this.rescorer = rescorer;
    }
    
    public double Estimate(long itemID) {
		Tuple<long, long> pair = new Tuple<long, long>(toItemID, itemID);
      if (rescorer != null && rescorer.IsFiltered(pair)) {
        return Double.NaN;
      }
      double originalEstimate = similarity.ItemSimilarity(toItemID, itemID);
      return rescorer == null ? originalEstimate : rescorer.Rescore(pair, originalEstimate);
    }
  }
  
  private class Estimator : TopItems.IEstimator<long> {
    
    private long userID;
    private IPreferenceArray preferencesFromUser;
	GenericItemBasedRecommender recommender;
    
    internal Estimator(GenericItemBasedRecommender recommender, long userID, IPreferenceArray preferencesFromUser) {
		this.recommender = recommender;
	  this.userID = userID;
      this.preferencesFromUser = preferencesFromUser;
    }
    
    public double Estimate(long itemID) {
      return recommender.doEstimatePreference(userID, preferencesFromUser, itemID);
    }
  }
  
  private sealed class MultiMostSimilarEstimator : TopItems.IEstimator<long> {
    
    private long[] toItemIDs;
    private IItemSimilarity similarity;
    private IRescorer<Tuple<long,long>> rescorer;
    private bool excludeItemIfNotSimilarToAll;
    
    internal MultiMostSimilarEstimator(long[] toItemIDs, IItemSimilarity similarity, IRescorer<Tuple<long,long>> rescorer,
        bool excludeItemIfNotSimilarToAll) {
      this.toItemIDs = toItemIDs;
      this.similarity = similarity;
      this.rescorer = rescorer;
      this.excludeItemIfNotSimilarToAll = excludeItemIfNotSimilarToAll;
    }
    
    public double Estimate(long itemID) {
      IRunningAverage average = new FullRunningAverage();
      double[] similarities = similarity.ItemSimilarities(itemID, toItemIDs);
      for (int i = 0; i < toItemIDs.Length; i++) {
        long toItemID = toItemIDs[i];
        Tuple<long,long> pair = new Tuple<long,long>(toItemID, itemID);
        if (rescorer != null && rescorer.IsFiltered(pair)) {
          continue;
        }
        double estimate = similarities[i];
        if (rescorer != null) {
          estimate = rescorer.Rescore(pair, estimate);
        }
        if (excludeItemIfNotSimilarToAll || !Double.IsNaN(estimate)) {
          average.AddDatum(estimate);
        }
      }
      double averageEstimate = average.GetAverage();
      return averageEstimate == 0 ? Double.NaN : averageEstimate;
    }
  }
  
  private sealed class RecommendedBecauseEstimator : TopItems.IEstimator<long> {
    
    private long userID;
    private long recommendedItemID;
	GenericItemBasedRecommender r;

    internal RecommendedBecauseEstimator(GenericItemBasedRecommender r, long userID, long recommendedItemID) {
      this.r = r;
	  this.userID = userID;
      this.recommendedItemID = recommendedItemID;
    }
    
    public double Estimate(long itemID) {
      float? pref = r.GetDataModel().GetPreferenceValue(userID, itemID);
      if (!pref.HasValue) {
        return float.NaN;
      }
      double similarityValue = r.similarity.ItemSimilarity(recommendedItemID, itemID);
	  return (1.0 + similarityValue) * pref.Value;
    }
  }
  
}

}