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
using NReco.Math3.Stats;

namespace NReco.CF.Taste.Impl.Similarity {


/// <summary>
/// Similarity test is based on the likelihood ratio, which expresses how many times more likely the data are under one model than the other.
/// </summary>
/// <remarks>
/// See <a href="http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.14.5962">
/// http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.14.5962</a> and
/// <a href="http://tdunning.blogspot.com/2008/03/surprise-and-coincidence.html">
/// http://tdunning.blogspot.com/2008/03/surprise-and-coincidence.html</a>.
/// </remarks>
public sealed class LogLikelihoodSimilarity : AbstractItemSimilarity, IUserSimilarity {

  public LogLikelihoodSimilarity(IDataModel dataModel) : base(dataModel) {
    
  }
  
   /// @throws NotSupportedException
  public void SetPreferenceInferrer(IPreferenceInferrer inferrer) {
    throw new NotSupportedException();
  }
  
  public double UserSimilarity(long userID1, long userID2) {

    IDataModel dataModel = getDataModel();
    FastIDSet prefs1 = dataModel.GetItemIDsFromUser(userID1);
    FastIDSet prefs2 = dataModel.GetItemIDsFromUser(userID2);
    
    long prefs1Size = prefs1.Count();
    long prefs2Size = prefs2.Count();
    long intersectionSize =
        prefs1Size < prefs2Size ? prefs2.IntersectionSize(prefs1) : prefs1.IntersectionSize(prefs2);
    if (intersectionSize == 0) {
      return Double.NaN;
    }
    long numItems = dataModel.GetNumItems();
    double logLikelihood =
        LogLikelihood.logLikelihoodRatio(intersectionSize,
                                         prefs2Size - intersectionSize,
                                         prefs1Size - intersectionSize,
                                         numItems - prefs1Size - prefs2Size + intersectionSize);
    return 1.0 - 1.0 / (1.0 + logLikelihood);
  }
  
  public override double ItemSimilarity(long itemID1, long itemID2) {
    IDataModel dataModel = getDataModel();
    long preferring1 = dataModel.GetNumUsersWithPreferenceFor(itemID1);
    long numUsers = dataModel.GetNumUsers();
    return doItemSimilarity(itemID1, itemID2, preferring1, numUsers);
  }

  public override double[] ItemSimilarities(long itemID1, long[] itemID2s) {
    IDataModel dataModel = getDataModel();
    long preferring1 = dataModel.GetNumUsersWithPreferenceFor(itemID1);
    long numUsers = dataModel.GetNumUsers();
    int length = itemID2s.Length;
    double[] result = new double[length];
    for (int i = 0; i < length; i++) {
      result[i] = doItemSimilarity(itemID1, itemID2s[i], preferring1, numUsers);
    }
    return result;
  }

  private double doItemSimilarity(long itemID1, long itemID2, long preferring1, long numUsers) {
    IDataModel dataModel = getDataModel();
    long preferring1and2 = dataModel.GetNumUsersWithPreferenceFor(itemID1, itemID2);
    if (preferring1and2 == 0) {
      return Double.NaN;
    }
    long preferring2 = dataModel.GetNumUsersWithPreferenceFor(itemID2);
    double logLikelihood =
        LogLikelihood.logLikelihoodRatio(preferring1and2,
                                         preferring2 - preferring1and2,
                                         preferring1 - preferring1and2,
                                         numUsers - preferring1 - preferring2 + preferring1and2);
    return 1.0 - 1.0 / (1.0 + logLikelihood);
  }

  public void Refresh(IList<IRefreshable> alreadyRefreshed) {
    alreadyRefreshed = RefreshHelper.BuildRefreshed(alreadyRefreshed);
    RefreshHelper.MaybeRefresh(alreadyRefreshed, getDataModel());
  }
  
  public override string ToString() {
    return "LogLikelihoodSimilarity[dataModel:" + getDataModel() + ']';
  }
  
}

}