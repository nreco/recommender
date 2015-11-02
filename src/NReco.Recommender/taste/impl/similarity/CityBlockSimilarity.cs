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
/// Implementation of City Block distance (also known as Manhattan distance) - the absolute value of the difference of
/// each direction is summed.  The resulting unbounded distance is then mapped between 0 and 1.
/// </summary>
public sealed class CityBlockSimilarity : AbstractItemSimilarity, IUserSimilarity {

  public CityBlockSimilarity(IDataModel dataModel) : base(dataModel) {
  }

   /// @throws NotSupportedException
  public void SetPreferenceInferrer(IPreferenceInferrer inferrer) {
    throw new NotSupportedException();
  }

  public void Refresh(IList<IRefreshable> alreadyRefreshed) {
    var refreshed = RefreshHelper.BuildRefreshed(alreadyRefreshed);
    RefreshHelper.MaybeRefresh(refreshed, getDataModel());
  }

  public override double ItemSimilarity(long itemID1, long itemID2) {
    IDataModel dataModel = getDataModel();
    int preferring1 = dataModel.GetNumUsersWithPreferenceFor(itemID1);
    int preferring2 = dataModel.GetNumUsersWithPreferenceFor(itemID2);
    int intersection = dataModel.GetNumUsersWithPreferenceFor(itemID1, itemID2);
    return doSimilarity(preferring1, preferring2, intersection);
  }

  public override double[] ItemSimilarities(long itemID1, long[] itemID2s) {
    IDataModel dataModel = getDataModel();
    int preferring1 = dataModel.GetNumUsersWithPreferenceFor(itemID1);
    double[] distance = new double[itemID2s.Length];
    for (int i = 0; i < itemID2s.Length; ++i) {
      int preferring2 = dataModel.GetNumUsersWithPreferenceFor(itemID2s[i]);
      int intersection = dataModel.GetNumUsersWithPreferenceFor(itemID1, itemID2s[i]);
      distance[i] = doSimilarity(preferring1, preferring2, intersection);
    }
    return distance;
  }

  public double UserSimilarity(long userID1, long userID2) {
    IDataModel dataModel = getDataModel();
    FastIDSet prefs1 = dataModel.GetItemIDsFromUser(userID1);
    FastIDSet prefs2 = dataModel.GetItemIDsFromUser(userID2);
    int prefs1Size = prefs1.Count();
    int prefs2Size = prefs2.Count();
    int intersectionSize = prefs1Size < prefs2Size ? prefs2.IntersectionSize(prefs1) : prefs1.IntersectionSize(prefs2);
    return doSimilarity(prefs1Size, prefs2Size, intersectionSize);
  }

   /// Calculate City Block Distance from total non-zero values and intersections and map to a similarity value.
   ///
   /// @param pref1        number of non-zero values in left vector
   /// @param pref2        number of non-zero values in right vector
   /// @param intersection number of overlapping non-zero values
  private static double doSimilarity(int pref1, int pref2, int intersection) {
    int distance = pref1 + pref2 - 2 * intersection;
    return 1.0 / (1.0 + distance);
  }

}

}