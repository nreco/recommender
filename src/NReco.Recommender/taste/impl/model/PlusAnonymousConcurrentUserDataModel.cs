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
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

using NReco.CF.Taste.Common;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Model;

namespace NReco.CF.Taste.Impl.Model {

 /// <summary>
 /// <para>
 /// This is a special thread-safe version of <see cref="PlusAnonymousUserDataModel"/>
 /// which allow multiple concurrent anonymous requests.
 /// </para>
 ///
 /// <para>
 /// To use it, you have to estimate the number of concurrent anonymous users of your application.
 /// The pool of users with the given size will be created. For each anonymous recommendations request,
 /// a user has to be taken from the pool and returned back immediately afterwards.
 /// </para>
 ///
 /// <para>
 /// If no more users are available in the pool, anonymous recommendations cannot be produced.
 /// </para>
 /// </summary>
 /// <example>
/// Setup:
/// <code>
/// int concurrentUsers = 100;
/// IDataModel realModel = ..
/// PlusAnonymousConcurrentUserDataModel plusModel =
///   new PlusAnonymousConcurrentUserDataModel(realModel, concurrentUsers);
/// IRecommender recommender = ...;
/// </code>
///
/// Real-time recommendation:
/// <code>
/// PlusAnonymousConcurrentUserDataModel plusModel =
///   (PlusAnonymousConcurrentUserDataModel) recommender.GetDataModel();
///
/// // Take the next available anonymous user from the pool
/// long anonymousUserID = plusModel.TakeAvailableUser();
///
/// IPreferenceArray tempPrefs = ..
/// tempPrefs.SetUserID(0, anonymousUserID);
/// tempPrefs.SetItemID(0, itemID);
/// plusModel.SetTempPrefs(tempPrefs, anonymousUserID);
///
/// // Produce recommendations
/// recommender.Recommend(anonymousUserID, howMany);
///
/// // It is very IMPORTANT to release user back to the pool
/// plusModel.ReleaseUser(anonymousUserID);
/// </code>
 /// </example>
public sealed class PlusAnonymousConcurrentUserDataModel : PlusAnonymousUserDataModel {

  /// Preferences for all anonymous users 
  private IDictionary<long,IPreferenceArray> tempPrefs;
  /// Item IDs set for all anonymous users 
  private IDictionary<long,FastIDSet> prefItemIDs;
  /// Pool of the users (FIFO) 
  private ConcurrentQueue<long> usersPool;

  private static Logger log = LoggerFactory.GetLogger(typeof(PlusAnonymousUserDataModel));

   /// @param delegate Real model where anonymous users will be added to
   /// @param maxConcurrentUsers Maximum allowed number of concurrent anonymous users
  public PlusAnonymousConcurrentUserDataModel(IDataModel _delegate, int maxConcurrentUsers) : base(_delegate) {

    tempPrefs = new ConcurrentDictionary<long, IPreferenceArray>();
	prefItemIDs = new ConcurrentDictionary<long, FastIDSet>();

    initializeUsersPools(maxConcurrentUsers);
  }

   /// Initialize the pool of concurrent anonymous users.
   ///
   /// @param usersPoolSize Maximum allowed number of concurrent anonymous user. Depends on the consumer system.
  private void initializeUsersPools(int usersPoolSize) {
    usersPool = new ConcurrentQueue<long>();
    for (int i = 0; i < usersPoolSize; i++) {
      usersPool.Enqueue(TEMP_USER_ID + i);
    }
  }

   /// Take the next available concurrent anonymous users from the pool.
   ///
   /// @return User ID or null if no more users are available
  public long? TakeAvailableUser() {
    long takenUserID;
	if (usersPool.TryDequeue(out takenUserID)) {
		// Initialize the preferences array to indicate that the user is taken.
		tempPrefs[takenUserID] = new GenericUserPreferenceArray(0);
		return takenUserID;
	}
    return null;
  }

   /// Release previously taken anonymous user and return it to the pool.
   ///
   /// @param userID ID of a previously taken anonymous user
   /// @return true if the user was previously taken, false otherwise
  public bool ReleaseUser(long userID) {
    if (tempPrefs.ContainsKey(userID)) {
      this.ClearTempPrefs(userID);
      // Return previously taken user to the pool
      usersPool.Enqueue(userID);
      return true;
    }
    return false;
  }

   /// Checks whether a given user is a valid previously acquired anonymous user.
  private bool isAnonymousUser(long userID) {
    return tempPrefs.ContainsKey(userID);
  }

   /// Sets temporary preferences for a given anonymous user.
  public void SetTempPrefs(IPreferenceArray prefs, long anonymousUserID) {
    //Preconditions.checkArgument(prefs != null && prefs.Length() > 0, "prefs is null or empty");

    this.tempPrefs[anonymousUserID] = prefs;
    FastIDSet userPrefItemIDs = new FastIDSet();

    for (int i = 0; i < prefs.Length(); i++) {
      userPrefItemIDs.Add(prefs.GetItemID(i));
    }

    this.prefItemIDs[anonymousUserID] = userPrefItemIDs;
  }

   /// Clears temporary preferences for a given anonymous user.
  public void ClearTempPrefs(long anonymousUserID) {
    this.tempPrefs.Remove(anonymousUserID);
    this.prefItemIDs.Remove(anonymousUserID);
  }

  public override IEnumerator<long> GetUserIDs() {
    // Anonymous users have short lifetime and should not be included into the neighbohoods of the real users.
    // Thus exclude them from the universe.
    return getDelegate().GetUserIDs();
  }

  public override IPreferenceArray GetPreferencesFromUser(long userID) {
    if (isAnonymousUser(userID)) {
      return tempPrefs[userID];
    }
    return getDelegate().GetPreferencesFromUser(userID);
  }

  public override FastIDSet GetItemIDsFromUser(long userID) {
    if (isAnonymousUser(userID)) {
      return prefItemIDs[userID];
    }
    return getDelegate().GetItemIDsFromUser(userID);
  }

  public override IPreferenceArray GetPreferencesForItem(long itemID) {
    if (tempPrefs.Count==0) {
      return getDelegate().GetPreferencesForItem(itemID);
    }

    IPreferenceArray delegatePrefs = null;

    try {
      delegatePrefs = getDelegate().GetPreferencesForItem(itemID);
    } catch (NoSuchItemException nsie) {
      // OK. Probably an item that only the anonymous user has
      //if (log.isDebugEnabled()) {
      //  log.debug("Item {} unknown", itemID);
      //}
    }

    List<IPreference> anonymousPreferences =  new List<IPreference>();

    foreach (var prefsMap in tempPrefs) {
      IPreferenceArray singleUserTempPrefs = prefsMap.Value;
      for (int i = 0; i < singleUserTempPrefs.Length(); i++) {
        if (singleUserTempPrefs.GetItemID(i) == itemID) {
          anonymousPreferences.Add(singleUserTempPrefs.Get(i));
        }
      }
    }

    int delegateLength = delegatePrefs == null ? 0 : delegatePrefs.Length();
    int anonymousPrefsLength = anonymousPreferences.Count;
    int prefsCounter = 0;

    // Merge the delegate and anonymous preferences into a single array
    IPreferenceArray newPreferenceArray = new GenericItemPreferenceArray(delegateLength + anonymousPrefsLength);

    for (int i = 0; i < delegateLength; i++) {
      newPreferenceArray.Set(prefsCounter++, delegatePrefs.Get(i));
    }

    foreach (IPreference anonymousPreference in anonymousPreferences) {
      newPreferenceArray.Set(prefsCounter++, anonymousPreference);
    }

    if (newPreferenceArray.Length() == 0) {
      // No, didn't find it among the anonymous user prefs
      throw new NoSuchItemException(itemID);
    }

    return newPreferenceArray;
  }

  public override float? GetPreferenceValue(long userID, long itemID) {
    if (isAnonymousUser(userID)) {
      IPreferenceArray singleUserTempPrefs = tempPrefs[userID];
      for (int i = 0; i < singleUserTempPrefs.Length(); i++) {
        if (singleUserTempPrefs.GetItemID(i) == itemID) {
          return singleUserTempPrefs.GetValue(i);
        }
      }
      return null;
    }
    return getDelegate().GetPreferenceValue(userID, itemID);
  }

  public override DateTime? GetPreferenceTime(long userID, long itemID) {
    if (isAnonymousUser(userID)) {
      // Timestamps are not saved for anonymous preferences
      return null;
    }
    return getDelegate().GetPreferenceTime(userID, itemID);
  }

  public override int GetNumUsers() {
    // Anonymous users have short lifetime and should not be included into the neighbohoods of the real users.
    // Thus exclude them from the universe.
    return getDelegate().GetNumUsers();
  }

  public override int GetNumUsersWithPreferenceFor(long itemID) {
    if (tempPrefs.Count==0) {
      return getDelegate().GetNumUsersWithPreferenceFor(itemID);
    }

    int countAnonymousUsersWithPreferenceFor = 0;

    foreach (var singleUserTempPrefs in tempPrefs) {
      for (int i = 0; i < singleUserTempPrefs.Value.Length(); i++) {
        if (singleUserTempPrefs.Value.GetItemID(i) == itemID) {
          countAnonymousUsersWithPreferenceFor++;
          break;
        }
      }
    }
    return getDelegate().GetNumUsersWithPreferenceFor(itemID) + countAnonymousUsersWithPreferenceFor;
  }

  public override int GetNumUsersWithPreferenceFor(long itemID1, long itemID2) {
    if (tempPrefs.Count==0) {
      return getDelegate().GetNumUsersWithPreferenceFor(itemID1, itemID2);
    }

    int countAnonymousUsersWithPreferenceFor = 0;

    foreach (var singleUserTempPrefs in tempPrefs) {
      bool found1 = false;
      bool found2 = false;
      for (int i = 0; i < singleUserTempPrefs.Value.Length() && !(found1 && found2); i++) {
        long itemID = singleUserTempPrefs.Value.GetItemID(i);
        if (itemID == itemID1) {
          found1 = true;
        }
        if (itemID == itemID2) {
          found2 = true;
        }
      }

      if (found1 && found2) {
        countAnonymousUsersWithPreferenceFor++;
      }
    }

    return getDelegate().GetNumUsersWithPreferenceFor(itemID1, itemID2) + countAnonymousUsersWithPreferenceFor;
  }

  public override void SetPreference(long userID, long itemID, float value) {
    if (isAnonymousUser(userID)) {
      throw new NotSupportedException();
    }
    getDelegate().SetPreference(userID, itemID, value);
  }

  public override void RemovePreference(long userID, long itemID) {
    if (isAnonymousUser(userID)) {
      throw new NotSupportedException();
    }
    getDelegate().RemovePreference(userID, itemID);
  }
}

}