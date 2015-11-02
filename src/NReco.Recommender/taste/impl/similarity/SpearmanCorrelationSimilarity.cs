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
using NReco.CF.Taste.Similarity;

namespace NReco.CF.Taste.Impl.Similarity {

/// <summary>
/// Like <see cref="PearsonCorrelationSimilarity"/>, but compares relative ranking of preference values instead of
/// preference values themselves. That is, each user's preferences are sorted and then assign a rank as their
/// preference value, with 1 being assigned to the least preferred item.
/// </summary>
public sealed class SpearmanCorrelationSimilarity : IUserSimilarity {
  
  private IDataModel dataModel;
  
  public SpearmanCorrelationSimilarity(IDataModel dataModel) {
	  this.dataModel = dataModel; //Preconditions.checkNotNull(dataModel);
  }
  
  public double UserSimilarity(long userID1, long userID2) {
    IPreferenceArray xPrefs = dataModel.GetPreferencesFromUser(userID1);
    IPreferenceArray yPrefs = dataModel.GetPreferencesFromUser(userID2);
    int xLength = xPrefs.Length();
    int yLength = yPrefs.Length();
    
    if (xLength <= 1 || yLength <= 1) {
      return Double.NaN;
    }
    
    // Copy prefs since we need to modify pref values to ranks
    xPrefs = xPrefs.Clone();
    yPrefs = yPrefs.Clone();
    
    // First sort by values from low to high
    xPrefs.SortByValue();
    yPrefs.SortByValue();
    
    // Assign ranks from low to high
    float nextRank = 1.0f;
    for (int i = 0; i < xLength; i++) {
      // ... but only for items that are common to both pref arrays
      if (yPrefs.HasPrefWithItemID(xPrefs.GetItemID(i))) {
        xPrefs.SetValue(i, nextRank);
        nextRank += 1.0f;
      }
      // Other values are bogus but don't matter
    }
    nextRank = 1.0f;
    for (int i = 0; i < yLength; i++) {
      if (xPrefs.HasPrefWithItemID(yPrefs.GetItemID(i))) {
        yPrefs.SetValue(i, nextRank);
        nextRank += 1.0f;
      }
    }
    
    xPrefs.SortByItem();
    yPrefs.SortByItem();
    
    long xIndex = xPrefs.GetItemID(0);
    long yIndex = yPrefs.GetItemID(0);
    int xPrefIndex = 0;
    int yPrefIndex = 0;
    
    double sumXYRankDiff2 = 0.0;
    int count = 0;
    
    while (true) {
      int compare = xIndex < yIndex ? -1 : xIndex > yIndex ? 1 : 0;
      if (compare == 0) {
        double diff = xPrefs.GetValue(xPrefIndex) - yPrefs.GetValue(yPrefIndex);
        sumXYRankDiff2 += diff * diff;
        count++;
      }
      if (compare <= 0) {
        if (++xPrefIndex >= xLength) {
          break;
        }
        xIndex = xPrefs.GetItemID(xPrefIndex);
      }
      if (compare >= 0) {
        if (++yPrefIndex >= yLength) {
          break;
        }
        yIndex = yPrefs.GetItemID(yPrefIndex);
      }
    }
    
    if (count <= 1) {
      return Double.NaN;
    }
    
    // When ranks are unique, this formula actually gives the Pearson correlation
    return 1.0 - 6.0 * sumXYRankDiff2 / (count * (count * count - 1));
  }
  
  public void SetPreferenceInferrer(IPreferenceInferrer inferrer) {
    throw new NotSupportedException();
  }
  
  public void Refresh(IList<IRefreshable> alreadyRefreshed) {
    alreadyRefreshed = RefreshHelper.BuildRefreshed(alreadyRefreshed);
    RefreshHelper.MaybeRefresh(alreadyRefreshed, dataModel);
  }
  
}

}