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
using NReco.CF;

namespace NReco.CF.Taste.Impl.Recommender {

/// <summary>
/// Produces random recommendations and preference estimates. This is likely only useful as a novelty and for benchmarking.
/// </summary>
public sealed class RandomRecommender : AbstractRecommender {
  
  private RandomWrapper random = RandomUtils.getRandom();
  private float minPref;
  private float maxPref;
  
  public RandomRecommender(IDataModel dataModel) : base(dataModel) {
    float maxPref = float.NegativeInfinity;
    float minPref = float.PositiveInfinity;
    var userIterator = dataModel.GetUserIDs();
    while (userIterator.MoveNext()) {
      long userID = userIterator.Current;
      IPreferenceArray prefs = dataModel.GetPreferencesFromUser(userID);
      for (int i = 0; i < prefs.Length(); i++) {
        float prefValue = prefs.GetValue(i);
        if (prefValue < minPref) {
          minPref = prefValue;
        }
        if (prefValue > maxPref) {
          maxPref = prefValue;
        }
      }
    }
    this.minPref = minPref;
    this.maxPref = maxPref;
  }
  
  public override IList<IRecommendedItem> Recommend(long userID, int howMany, IDRescorer rescorer) {
    IDataModel dataModel = GetDataModel();
    int numItems = dataModel.GetNumItems();
	List<IRecommendedItem> result = new List<IRecommendedItem>(howMany);
    while (result.Count < howMany) {
      var it = dataModel.GetItemIDs();
	  it.MoveNext();

	  var skipNum = random.nextInt(numItems);
	  for (int i=0; i<skipNum; i++)
		if (!it.MoveNext()) { break; }  // skip() ??
      
	  long itemID = it.Current;
      if (dataModel.GetPreferenceValue(userID, itemID) == null) {
        result.Add(new GenericRecommendedItem(itemID, randomPref()));
      }
    }
    return result;
  }
  
  public  override float EstimatePreference(long userID, long itemID) {
    return randomPref();
  }
  
  private float randomPref() {
    return minPref + (float)random.nextDouble() * (maxPref - minPref);
  }
  
  public override void Refresh(IList<IRefreshable> alreadyRefreshed) {
    GetDataModel().Refresh(alreadyRefreshed);
  }
  
}

}