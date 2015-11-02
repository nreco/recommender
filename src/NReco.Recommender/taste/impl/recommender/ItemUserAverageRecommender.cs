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
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;

namespace NReco.CF.Taste.Impl.Recommender {

/// <summary>
/// Like <see cref="ItemAverageRecommender"/>, except that estimated preferences are adjusted for the users' average
/// preference value. For example, say user X has not rated item Y. Item Y's average preference value is 3.5.
/// User X's average preference value is 4.2, and the average over all preference values is 4.0. User X prefers
/// items 0.2 higher on average, so, the estimated preference for user X, item Y is 3.5 + 0.2 = 3.7.
/// </summary>
public sealed class ItemUserAverageRecommender : AbstractRecommender {
  
  private static Logger log = LoggerFactory.GetLogger(typeof(ItemUserAverageRecommender));
  
  private FastByIDMap<IRunningAverage> itemAverages;
  private FastByIDMap<IRunningAverage> userAverages;
  private IRunningAverage overallAveragePrefValue;
  //private ReadWriteLock buildAveragesLock;
  private RefreshHelper refreshHelper;
  
  public ItemUserAverageRecommender(IDataModel dataModel) : base(dataModel) {
    this.itemAverages = new FastByIDMap<IRunningAverage>();
    this.userAverages = new FastByIDMap<IRunningAverage>();
    this.overallAveragePrefValue = new FullRunningAverage();
    //this.buildAveragesLock = new ReentrantReadWriteLock();
    this.refreshHelper = new RefreshHelper( () => {
        buildAverageDiffs();
      });
    refreshHelper.AddDependency(dataModel);
    buildAverageDiffs();
  }
  
  public override IList<IRecommendedItem> Recommend(long userID, int howMany, IDRescorer rescorer) {
    //Preconditions.checkArgument(howMany >= 1, "howMany must be at least 1");
    log.Debug("Recommending items for user ID '{}'", userID);

    IPreferenceArray preferencesFromUser = GetDataModel().GetPreferencesFromUser(userID);
    FastIDSet possibleItemIDs = GetAllOtherItems(userID, preferencesFromUser);

    TopItems.IEstimator<long> estimator = new Estimator(this,userID);

    List<IRecommendedItem> topItems = TopItems.GetTopItems(howMany, possibleItemIDs.GetEnumerator(), rescorer,
      estimator);

    log.Debug("Recommendations are: {}", topItems);
    return topItems;
  }
  
  public override float EstimatePreference(long userID, long itemID) {
    IDataModel dataModel = GetDataModel();
    float? actualPref = dataModel.GetPreferenceValue(userID, itemID);
    if (actualPref.HasValue) {
      return actualPref.Value;
    }
    return doEstimatePreference(userID, itemID);
  }
  
  private float doEstimatePreference(long userID, long itemID) {
    //buildAveragesLock.readLock().lock();
    lock (this) {
      IRunningAverage itemAverage = itemAverages.Get(itemID);
      if (itemAverage == null) {
        return float.NaN;
      }
      IRunningAverage userAverage = userAverages.Get(userID);
      if (userAverage == null) {
        return float.NaN;
      }
      double userDiff = userAverage.GetAverage() - overallAveragePrefValue.GetAverage();
      return (float) (itemAverage.GetAverage() + userDiff);
    } /*finally {
      buildAveragesLock.readLock().unlock();
    }*/
  }

  private void buildAverageDiffs() {
    lock (this) {
      //buildAveragesLock.writeLock().lock();
      IDataModel dataModel = GetDataModel();
      var it = dataModel.GetUserIDs();
      while (it.MoveNext()) {
        long userID = it.Current;
        IPreferenceArray prefs = dataModel.GetPreferencesFromUser(userID);
        int size = prefs.Length();
        for (int i = 0; i < size; i++) {
          long itemID = prefs.GetItemID(i);
          float value = prefs.GetValue(i);
          addDatumAndCreateIfNeeded(itemID, value, itemAverages);
          addDatumAndCreateIfNeeded(userID, value, userAverages);
          overallAveragePrefValue.AddDatum(value);
        }
      }
    } /*finally {
      buildAveragesLock.writeLock().unlock();
    }*/
  }
  
  private static void addDatumAndCreateIfNeeded(long itemID, float value, FastByIDMap<IRunningAverage> averages) {
    IRunningAverage itemAverage = averages.Get(itemID);
    if (itemAverage == null) {
      itemAverage = new FullRunningAverage();
      averages.Put(itemID, itemAverage);
    }
    itemAverage.AddDatum(value);
  }
  
  public override void SetPreference(long userID, long itemID, float value) {
    IDataModel dataModel = GetDataModel();
    double prefDelta;
    try {
      float? oldPref = dataModel.GetPreferenceValue(userID, itemID);
      prefDelta = !oldPref.HasValue ? value : value - oldPref.Value;
    } catch (NoSuchUserException nsee) {
      prefDelta = value;
    }
    base.SetPreference(userID, itemID, value);
    lock (this) {
      //buildAveragesLock.writeLock().lock();
      IRunningAverage itemAverage = itemAverages.Get(itemID);
      if (itemAverage == null) {
        IRunningAverage newItemAverage = new FullRunningAverage();
        newItemAverage.AddDatum(prefDelta);
        itemAverages.Put(itemID, newItemAverage);
      } else {
        itemAverage.ChangeDatum(prefDelta);
      }
      IRunningAverage userAverage = userAverages.Get(userID);
      if (userAverage == null) {
        IRunningAverage newUserAveragae = new FullRunningAverage();
        newUserAveragae.AddDatum(prefDelta);
        userAverages.Put(userID, newUserAveragae);
      } else {
        userAverage.ChangeDatum(prefDelta);
      }
      overallAveragePrefValue.ChangeDatum(prefDelta);
    } /*finally {
      buildAveragesLock.writeLock().unlock();
    }*/
  }
  
  public override void RemovePreference(long userID, long itemID) {
    IDataModel dataModel = GetDataModel();
    float? oldPref = dataModel.GetPreferenceValue(userID, itemID);
    base.RemovePreference(userID, itemID);
    if (oldPref.HasValue) {
      lock(this) {
        //buildAveragesLock.writeLock().lock();
        IRunningAverage itemAverage = itemAverages.Get(itemID);
        if (itemAverage == null) {
          throw new InvalidOperationException("No preferences exist for item ID: " + itemID);
        }
        itemAverage.RemoveDatum(oldPref.Value);
        IRunningAverage userAverage = userAverages.Get(userID);
        if (userAverage == null) {
          throw new InvalidOperationException("No preferences exist for user ID: " + userID);
        }
        userAverage.RemoveDatum(oldPref.Value);
        overallAveragePrefValue.RemoveDatum(oldPref.Value);
      }/* finally {
        buildAveragesLock.writeLock().unlock();
      }*/
    }
  }
  
  public override void Refresh(IList<IRefreshable> alreadyRefreshed) {
    refreshHelper.Refresh(alreadyRefreshed);
  }
  
  public override string ToString() {
    return "ItemUserAverageRecommender";
  }
  
  private sealed class Estimator : TopItems.IEstimator<long> {
    
    private long userID;
	ItemUserAverageRecommender itemUserAverageRecommender;
    
    internal Estimator(ItemUserAverageRecommender itemUserAverageRecommender, long userID) {
      this.userID = userID;
	  this.itemUserAverageRecommender = itemUserAverageRecommender;
    }
    
    public double Estimate(long itemID) {
		return itemUserAverageRecommender.doEstimatePreference(userID, itemID);
    }
  }
  
}

}