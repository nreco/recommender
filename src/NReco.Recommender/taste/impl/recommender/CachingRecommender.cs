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

using NReco.CF.Taste.Common;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Impl.Model;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;
using NReco.CF;

namespace NReco.CF.Taste.Impl.Recommender {

 /// <summary>
 /// A <see cref="IRecommender"/> which caches the results from another <see cref="IRecommender"/> in memory.
 /// </summary>
public sealed class CachingRecommender : IRecommender {
  
  private static Logger log = LoggerFactory.GetLogger(typeof(CachingRecommender));
  
  private IRecommender recommender;
  private int[] maxHowMany;
  private IRetriever<long,Recommendations> recommendationsRetriever;
  private Cache<long,Recommendations> recommendationCache;
  private Cache<Tuple<long,long>,float> estimatedPrefCache;
  private RefreshHelper refreshHelper;
  private IDRescorer currentRescorer;
  
  public CachingRecommender(IRecommender recommender) {
    //Preconditions.checkArgument(recommender != null, "recommender is null");
    this.recommender = recommender;
    maxHowMany = new int[]{1};
    // Use "num users" as an upper limit on cache size. Rough guess.
    int numUsers = recommender.GetDataModel().GetNumUsers();
    recommendationsRetriever = new RecommendationRetriever(this);
    recommendationCache = new Cache<long, Recommendations>(recommendationsRetriever, numUsers);
	estimatedPrefCache = new Cache<Tuple<long, long>, float>(new EstimatedPrefRetriever(this), numUsers);
    refreshHelper = new RefreshHelper( () => {
        clear();
    });
    refreshHelper.AddDependency(recommender);
  }
  
  private void setCurrentRescorer(IDRescorer rescorer) {
    if (rescorer == null) {
      if (currentRescorer != null) {
        currentRescorer = null;
        clear();
      }
    } else {
      if (!rescorer.Equals(currentRescorer)) {
        currentRescorer = rescorer;
        clear();
      }
    }
  }
  
  public IList<IRecommendedItem> Recommend(long userID, int howMany) {
    return Recommend(userID, howMany, null);
  }

  public IList<IRecommendedItem> Recommend(long userID, int howMany, IDRescorer rescorer) {
    //Preconditions.checkArgument(howMany >= 1, "howMany must be at least 1");
    lock (maxHowMany) {
      if (howMany > maxHowMany[0]) {
        maxHowMany[0] = howMany;
      }
    }

    // Special case, avoid caching an anonymous user
    if (userID == PlusAnonymousUserDataModel.TEMP_USER_ID) {
      return recommendationsRetriever.Get(PlusAnonymousUserDataModel.TEMP_USER_ID).getItems();
    }

    setCurrentRescorer(rescorer);

    Recommendations recommendations = recommendationCache.Get(userID);
    if (recommendations.getItems().Count < howMany && !recommendations.isNoMoreRecommendableItems()) {
      clear(userID);
      recommendations = recommendationCache.Get(userID);
      if (recommendations.getItems().Count < howMany) {
        recommendations.setNoMoreRecommendableItems(true);
      }
    }

    List<IRecommendedItem> recommendedItems = recommendations.getItems();
    return recommendedItems.Count > howMany ? recommendedItems.GetRange(0, howMany) : recommendedItems;
  }
  
  public float EstimatePreference(long userID, long itemID) {
    return estimatedPrefCache.Get(new Tuple<long,long>(userID, itemID));
  }
  
  public void SetPreference(long userID, long itemID, float value) {
    recommender.SetPreference(userID, itemID, value);
    clear(userID);
  }
  
  public void RemovePreference(long userID, long itemID) {
    recommender.RemovePreference(userID, itemID);
    clear(userID);
  }
  
  public IDataModel GetDataModel() {
    return recommender.GetDataModel();
  }
  
  public void Refresh(IList<IRefreshable> alreadyRefreshed) {
    refreshHelper.Refresh(alreadyRefreshed);
  }
  
   /// <p>
   /// Clears cached recommendations for the given user.
   /// </p>
   /// 
   /// @param userID
   ///          clear cached data associated with this user ID
  public void clear(long userID) {
    log.Debug("Clearing recommendations for user ID '{}'", userID);
    recommendationCache.Remove(userID);
    estimatedPrefCache.RemoveKeysMatching( (Tuple<long,long> userItemPair) => {
        return userItemPair.Item1 == userID;
      });
  }
  
   /// <p>
   /// Clears all cached recommendations.
   /// </p>
  public void clear() {
    log.Debug("Clearing all recommendations...");
    recommendationCache.Clear();
    estimatedPrefCache.Clear();
  }
  
  public override string ToString() {
    return "CachingRecommender[recommender:" + recommender + ']';
  }
  
  private sealed class RecommendationRetriever : IRetriever<long,Recommendations> {

	  CachingRecommender p;

	  internal RecommendationRetriever(CachingRecommender parent) {
		  p = parent;
	  }

	  public Recommendations Get(long key) {
      log.Debug("Retrieving new recommendations for user ID '{}'", key);
      int howMany = p.maxHowMany[0];
      IDRescorer rescorer = p.currentRescorer;
      var recommendations =
          rescorer == null ? p.recommender.Recommend(key, howMany) : p.recommender.Recommend(key, howMany, rescorer);
      return new Recommendations( new List<IRecommendedItem>(recommendations) );
    }
  }
  
  private sealed class EstimatedPrefRetriever : IRetriever<Tuple<long,long>,float> {

	  CachingRecommender p;
	  internal EstimatedPrefRetriever(CachingRecommender parent) {
		  p = parent;
	  }

	  public float Get(Tuple<long, long> key) {
      long userID = key.Item1;
      long itemID = key.Item2;
      log.Debug("Retrieving estimated preference for user ID '{}' and item ID '{}'", userID, itemID);
      return p.recommender.EstimatePreference(userID, itemID);
    }
  }
  
  private sealed class Recommendations {
    
    private List<IRecommendedItem> items;
    private bool noMoreRecommendableItems;
    
    internal Recommendations(List<IRecommendedItem> items) {
      this.items = items;
    }
    
    public List<IRecommendedItem> getItems() {
      return items;
    }
    
    public bool isNoMoreRecommendableItems() {
      return noMoreRecommendableItems;
    }
    
    public void setNoMoreRecommendableItems(bool noMoreRecommendableItems) {
      this.noMoreRecommendableItems = noMoreRecommendableItems;
    }
  }
  
}

}