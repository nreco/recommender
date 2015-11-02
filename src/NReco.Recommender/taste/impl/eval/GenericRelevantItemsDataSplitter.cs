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
using NReco.CF.Taste.Eval;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Impl.Model;
using NReco.CF.Taste.Model;

namespace NReco.CF.Taste.Impl.Eval {

/// <summary>
/// Picks relevant items to be those with the strongest preference, and
/// includes the other users' preferences in full.
/// </summary>
public sealed class GenericRelevantItemsDataSplitter : IRelevantItemsDataSplitter {

  public FastIDSet GetRelevantItemsIDs(long userID,
                                       int at,
                                       double relevanceThreshold,
                                       IDataModel dataModel) {
    IPreferenceArray prefs = dataModel.GetPreferencesFromUser(userID);
    FastIDSet relevantItemIDs = new FastIDSet(at);
    prefs.SortByValueReversed();
    for (int i = 0; i < prefs.Length() && relevantItemIDs.Count() < at; i++) {
      if (prefs.GetValue(i) >= relevanceThreshold) {
        relevantItemIDs.Add(prefs.GetItemID(i));
      }
    }
    return relevantItemIDs;
  }

  public void ProcessOtherUser(long userID,
                               FastIDSet relevantItemIDs,
                               FastByIDMap<IPreferenceArray> trainingUsers,
                               long otherUserID,
                               IDataModel dataModel) {
    IPreferenceArray prefs2Array = dataModel.GetPreferencesFromUser(otherUserID);
    // If we're dealing with the very user that we're evaluating for precision/recall,
    if (userID == otherUserID) {
      // then must remove all the test IDs, the "relevant" item IDs
      List<IPreference> prefs2 = new List<IPreference>(prefs2Array.Length());
      foreach (IPreference pref in prefs2Array) {
		  if (!relevantItemIDs.Contains(pref.GetItemID())) {
			  prefs2.Add(pref);
		  }
      }

      if (prefs2.Count>0) {
        trainingUsers.Put(otherUserID, new GenericUserPreferenceArray(prefs2));
      }
    } else {
      // otherwise just add all those other user's prefs
      trainingUsers.Put(otherUserID, prefs2Array);
    }
  }
}

}