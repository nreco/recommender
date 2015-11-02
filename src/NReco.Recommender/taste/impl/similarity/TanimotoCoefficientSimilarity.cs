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
/// An implementation of a "similarity" based on the <a
/// href="http://en.wikipedia.org/wiki/Jaccard_index#Tanimoto_coefficient_.28extended_Jaccard_coefficient.29">
/// Tanimoto coefficient</a>, or extended <a href="http://en.wikipedia.org/wiki/Jaccard_index">Jaccard
/// coefficient</a>.
/// </summary>
/// <remarks>
/// <para>
/// This is intended for "binary" data sets where a user either expresses a generic "yes" preference for an
/// item or has no preference. The actual preference values do not matter here, only their presence or absence.
/// </para>
/// 
/// <para>
/// The value returned is in [0,1].
/// </para>
/// </remarks>
public sealed class TanimotoCoefficientSimilarity : AbstractItemSimilarity, IUserSimilarity {

  public TanimotoCoefficientSimilarity(IDataModel dataModel) : base(dataModel) {
  }
  
   /// @throws NotSupportedException
  public void SetPreferenceInferrer(IPreferenceInferrer inferrer) {
    throw new NotSupportedException();
  }
  
  public double UserSimilarity(long userID1, long userID2) {

    IDataModel dataModel = getDataModel();
    FastIDSet xPrefs = dataModel.GetItemIDsFromUser(userID1);
    FastIDSet yPrefs = dataModel.GetItemIDsFromUser(userID2);

    int xPrefsSize = xPrefs.Count();
    int yPrefsSize = yPrefs.Count();
    if (xPrefsSize == 0 && yPrefsSize == 0) {
      return Double.NaN;
    }
    if (xPrefsSize == 0 || yPrefsSize == 0) {
      return 0.0;
    }
    
    int intersectionSize =
        xPrefsSize < yPrefsSize ? yPrefs.IntersectionSize(xPrefs) : xPrefs.IntersectionSize(yPrefs);
    if (intersectionSize == 0) {
      return Double.NaN;
    }
    
    int unionSize = xPrefsSize + yPrefsSize - intersectionSize;
    
    return (double) intersectionSize / (double) unionSize;
  }
  
  public override double ItemSimilarity(long itemID1, long itemID2) {
    int preferring1 = getDataModel().GetNumUsersWithPreferenceFor(itemID1);
    return doItemSimilarity(itemID1, itemID2, preferring1);
  }

  public override double[] ItemSimilarities(long itemID1, long[] itemID2s) {
    int preferring1 = getDataModel().GetNumUsersWithPreferenceFor(itemID1);
    int length = itemID2s.Length;
    double[] result = new double[length];
    for (int i = 0; i < length; i++) {
      result[i] = doItemSimilarity(itemID1, itemID2s[i], preferring1);
    }
    return result;
  }

  private double doItemSimilarity(long itemID1, long itemID2, int preferring1) {
    IDataModel dataModel = getDataModel();
    int preferring1and2 = dataModel.GetNumUsersWithPreferenceFor(itemID1, itemID2);
    if (preferring1and2 == 0) {
      return Double.NaN;
    }
    int preferring2 = dataModel.GetNumUsersWithPreferenceFor(itemID2);
    return (double) preferring1and2 / (double) (preferring1 + preferring2 - preferring1and2);
  }
  
  public void Refresh(IList<IRefreshable> alreadyRefreshed) {
    alreadyRefreshed = RefreshHelper.BuildRefreshed(alreadyRefreshed);
    RefreshHelper.MaybeRefresh(alreadyRefreshed, getDataModel());
  }
  
  public override string ToString() {
    return "TanimotoCoefficientSimilarity[dataModel:" + getDataModel() + ']';
  }
  
}

}