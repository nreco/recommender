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
using NReco.CF.Taste.Neighborhood;
using NReco.CF.Taste.Similarity;

namespace NReco.CF.Taste.Impl.Recommender {

/// <summary>
/// A variant on <see cref="GenericUserBasedRecommender"/> which is appropriate for use when no notion of preference
/// value exists in the data.
/// </summary>
public sealed class GenericBooleanPrefUserBasedRecommender : GenericUserBasedRecommender {
  
  public GenericBooleanPrefUserBasedRecommender(IDataModel dataModel,
                                                IUserNeighborhood neighborhood,
												IUserSimilarity similarity)
		: base(dataModel, neighborhood, similarity) {
  }
  
   /// This computation is in a technical sense, wrong, since in the domain of "bool preference users" where
   /// all preference values are 1, this method should only ever return 1.0 or NaN. This isn't terribly useful
   /// however since it means results can't be ranked by preference value (all are 1). So instead this returns a
   /// sum of similarities to any other user in the neighborhood who has also rated the item.
  protected override float doEstimatePreference(long theUserID, long[] theNeighborhood, long itemID) {
    if (theNeighborhood.Length == 0) {
      return float.NaN;
    }
    IDataModel dataModel = GetDataModel();
    IUserSimilarity similarity = getSimilarity();
    float totalSimilarity = 0.0f;
    bool foundAPref = false;
    foreach (long userID in theNeighborhood) {
      // See GenericItemBasedRecommender.doEstimatePreference() too
      if (userID != theUserID && dataModel.GetPreferenceValue(userID, itemID) != null) {
        foundAPref = true;
        totalSimilarity += (float) similarity.UserSimilarity(theUserID, userID);
      }
    }
    return foundAPref ? totalSimilarity : float.NaN;
  }
  
  protected FastIDSet getAllOtherItems(long[] theNeighborhood, long theUserID) {
    IDataModel dataModel = GetDataModel();
    FastIDSet possibleItemIDs = new FastIDSet();
    foreach (long userID in theNeighborhood) {
      possibleItemIDs.AddAll(dataModel.GetItemIDsFromUser(userID));
    }
    possibleItemIDs.RemoveAll(dataModel.GetItemIDsFromUser(theUserID));
    return possibleItemIDs;
  }
  
  public override string ToString() {
    return "GenericBooleanPrefUserBasedRecommender";
  }
  
}

}