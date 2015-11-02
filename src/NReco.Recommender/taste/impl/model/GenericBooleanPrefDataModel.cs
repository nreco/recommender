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

namespace NReco.CF.Taste.Impl.Model {

/// <summary>
/// A simple <see cref="IDataModel"/> which uses given user data as its data source. This implementation
/// is mostly useful for small experiments and is not recommended for contexts where performance is important.
/// </summary>
public sealed class GenericBooleanPrefDataModel : AbstractDataModel {
  
	private long[] userIDs;
	private FastByIDMap<FastIDSet> preferenceFromUsers;
	private long[] itemIDs;
	private FastByIDMap<FastIDSet> preferenceForItems;
	private FastByIDMap<FastByIDMap<DateTime?>> timestamps;
  
	/// <summary>
	/// Creates a new <see cref="GenericBooleanPrefDataModel"/> from the given users (and their preferences). This
	/// <see cref="IDataModel"/> retains all this information in memory and is effectively immutable.
	/// </summary>
	public GenericBooleanPrefDataModel(FastByIDMap<FastIDSet> userData)
		: this(userData, null) {
    
	}

   /// <p>
   /// Creates a new {@link GenericDataModel} from the given users (and their preferences). This
   /// {@link DataModel} retains all this information in memory and is effectively immutable.
   /// </p>
   ///
   /// @param userData users to include
   /// @param timestamps optionally, provided timestamps of preferences as milliseconds since the epoch.
   ///  User IDs are mapped to maps of item IDs to long timestamps.
  public GenericBooleanPrefDataModel(FastByIDMap<FastIDSet> userData, FastByIDMap<FastByIDMap<DateTime?>> timestamps) {
    //Preconditions.checkArgument(userData != null, "userData is null");

    this.preferenceFromUsers = userData;
    this.preferenceForItems = new FastByIDMap<FastIDSet>();
    FastIDSet itemIDSet = new FastIDSet();
    foreach (var entry in preferenceFromUsers.EntrySet()) {
      long userID = entry.Key;
      FastIDSet itemIDs1 = entry.Value;
      itemIDSet.AddAll(itemIDs1);
      var it = itemIDs1.GetEnumerator();
      while (it.MoveNext()) {
        long itemID = it.Current;
        FastIDSet userIDs1 = preferenceForItems.Get(itemID);
        if (userIDs1 == null) {
          userIDs1 = new FastIDSet(2);
          preferenceForItems.Put(itemID, userIDs1);
        }
        userIDs1.Add(userID);
      }
    }

    this.itemIDs = itemIDSet.ToArray();
    itemIDSet = null; // Might help GC -- this is big
    Array.Sort(itemIDs);

    this.userIDs = new long[userData.Count()];
    int i = 0;
    var it1 = userData.Keys.GetEnumerator();
    while (it1.MoveNext()) {
      userIDs[i++] = it1.Current;
    }
    Array.Sort(userIDs);

    this.timestamps = timestamps;
  }
  
   /// <p>
   /// Creates a new {@link GenericDataModel} containing an immutable copy of the data from another given
   /// {@link DataModel}.
   /// </p>
   /// 
   /// @param dataModel
   ///          {@link DataModel} to copy
   /// @throws TasteException
   ///           if an error occurs while retrieving the other {@link DataModel}'s users
   /// @deprecated without direct replacement.
   ///  Consider {@link #toDataMap(DataModel)} with {@link #GenericBooleanPrefDataModel(FastByIDMap)}
[Obsolete]
  public GenericBooleanPrefDataModel(IDataModel dataModel)
	  : this(toDataMap(dataModel)) {
 
  }

   /// Exports the simple user IDs and associated item IDs in the data model.
   ///
   /// @return a {@link FastByIDMap} mapping user IDs to {@link FastIDSet}s representing
   ///  that user's associated items
  public static FastByIDMap<FastIDSet> toDataMap(IDataModel dataModel) {
    FastByIDMap<FastIDSet> data = new FastByIDMap<FastIDSet>(dataModel.GetNumUsers());
    var it = dataModel.GetUserIDs();
    while (it.MoveNext()) {
      long userID = it.Current;
      data.Put(userID, dataModel.GetItemIDsFromUser(userID));
    }
    return data;
  }

  public static FastByIDMap<FastIDSet> toDataMap(FastByIDMap<IPreferenceArray> data) {
    var res = new FastByIDMap<FastIDSet>( data.Count() );
	foreach (var entry in data.EntrySet()) {
      IPreferenceArray prefArray = entry.Value;
      int size = prefArray.Length();
      FastIDSet itemIDs = new FastIDSet(size);
      for (int i = 0; i < size; i++) {
        itemIDs.Add(prefArray.GetItemID(i));
      }
	 
	  res.Put( entry.Key, itemIDs );
    }
	return res;
  }
  
   /// This is used mostly internally to the framework, and shouldn't be relied upon otherwise.
  public FastByIDMap<FastIDSet> getRawUserData() {
    return this.preferenceFromUsers;
  }

   /// This is used mostly internally to the framework, and shouldn't be relied upon otherwise.
  public FastByIDMap<FastIDSet> getRawItemData() {
    return this.preferenceForItems;
  }
  
  public override IEnumerator<long> GetUserIDs() {
    return ((IEnumerable<long>)userIDs).GetEnumerator();
  }
  
   /// @throws NoSuchUserException
   ///           if there is no such user
  public override IPreferenceArray GetPreferencesFromUser(long userID) {
    FastIDSet itemIDs = preferenceFromUsers.Get(userID);
    if (itemIDs == null) {
      throw new NoSuchUserException(userID);
    }
    IPreferenceArray prefArray = new BooleanUserPreferenceArray(itemIDs.Count() );
    int i = 0;
    var it = itemIDs.GetEnumerator();
    while (it.MoveNext()) {
      prefArray.SetUserID(i, userID);
      prefArray.SetItemID(i, it.Current);
      i++;
    }
    return prefArray;
  }
  
  public override FastIDSet GetItemIDsFromUser(long userID) {
    FastIDSet itemIDs = preferenceFromUsers.Get(userID);
    if (itemIDs == null) {
      throw new NoSuchUserException(userID);
    }
    return itemIDs;
  }
  
  public override IEnumerator<long> GetItemIDs() {
    return ((IEnumerable<long>)itemIDs).GetEnumerator();
  }
  
  public override IPreferenceArray GetPreferencesForItem(long itemID) {
    FastIDSet userIDs = preferenceForItems.Get(itemID);
    if (userIDs == null) {
      throw new NoSuchItemException(itemID);
    }
    IPreferenceArray prefArray = new BooleanItemPreferenceArray(userIDs.Count());
    int i = 0;
    var it = userIDs.GetEnumerator();
    while (it.MoveNext()) {
      prefArray.SetUserID(i, it.Current);
      prefArray.SetItemID(i, itemID);
      i++;
    }
    return prefArray;
  }
  
  public override float? GetPreferenceValue(long userID, long itemID) {
    FastIDSet itemIDs = preferenceFromUsers.Get(userID);
    if (itemIDs == null) {
      throw new NoSuchUserException(userID);
    }
    if (itemIDs.Contains(itemID)) {
      return 1.0f;
    }
    return null;
  }

  public override DateTime? GetPreferenceTime(long userID, long itemID) {
    if (timestamps == null) {
      return null;
    }
    var itemTimestamps = timestamps.Get(userID);
    if (itemTimestamps == null) {
      throw new NoSuchUserException(userID);
    }
    return itemTimestamps.Get(itemID);
  }
  
  public override int GetNumItems() {
    return itemIDs.Length;
  }

  public override int GetNumUsers() {
    return userIDs.Length;
  }

  public override int GetNumUsersWithPreferenceFor(long itemID) {
    FastIDSet userIDs1 = preferenceForItems.Get(itemID);
    return userIDs1 == null ? 0 : userIDs1.Count();
  }

  public override int GetNumUsersWithPreferenceFor(long itemID1, long itemID2) {
    FastIDSet userIDs1 = preferenceForItems.Get(itemID1);
    if (userIDs1 == null) {
      return 0;
    }
    FastIDSet userIDs2 = preferenceForItems.Get(itemID2);
    if (userIDs2 == null) {
      return 0;
    }
    return userIDs1.Count() < userIDs2.Count()
        ? userIDs2.IntersectionSize(userIDs1)
        : userIDs1.IntersectionSize(userIDs2);
  }

  public override void RemovePreference(long userID, long itemID) {
    throw new NotSupportedException();
  }

  public override void SetPreference(long userID, long itemID, float value) {
    throw new NotSupportedException();
  }
  
  public override void Refresh(IList<IRefreshable> alreadyRefreshed) {
  // Does nothing
  }

  public override bool HasPreferenceValues() {
    return false;
  }
  
  public override string ToString() {
    var result = new System.Text.StringBuilder(200);
    result.Append("GenericBooleanPrefDataModel[users:");
    for (int i = 0; i < Math.Min(3, userIDs.Length); i++) {
      if (i > 0) {
        result.Append(',');
      }
      result.Append(userIDs[i]);
    }
    if (userIDs.Length > 3) {
      result.Append("...");
    }
    result.Append(']');
    return result.ToString();
  }
  
}

}