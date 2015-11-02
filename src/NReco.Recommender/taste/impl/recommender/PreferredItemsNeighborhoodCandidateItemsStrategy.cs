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

namespace NReco.CF.Taste.Impl.Recommender {

/// <summary>
/// Returns all items that have not been rated by the user and that were preferred by another user
/// that has preferred at least one item that the current user has preferred too.
/// </summary>
public sealed class PreferredItemsNeighborhoodCandidateItemsStrategy : AbstractCandidateItemsStrategy {

  protected override FastIDSet doGetCandidateItems(long[] preferredItemIDs, IDataModel dataModel) {
    FastIDSet possibleItemsIDs = new FastIDSet();
    foreach (long itemID in preferredItemIDs) {
      IPreferenceArray itemPreferences = dataModel.GetPreferencesForItem(itemID);
      int numUsersPreferringItem = itemPreferences.Length();
      for (int index = 0; index < numUsersPreferringItem; index++) {
        possibleItemsIDs.AddAll(dataModel.GetItemIDsFromUser(itemPreferences.GetUserID(index)));
      }
    }
    possibleItemsIDs.RemoveAll(preferredItemIDs);
    return possibleItemsIDs;
  }

}

}