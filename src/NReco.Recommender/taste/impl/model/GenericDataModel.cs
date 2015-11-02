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
/// A simple <see cref="IDataModel"/> which uses a given list of users as its data source. This implementation
/// is mostly useful for small experiments and is not recommended for contexts where performance is important.
/// </summary>
[Serializable]
public sealed class GenericDataModel : AbstractDataModel {
  
	private static Logger log = LoggerFactory.GetLogger(typeof(GenericDataModel));
  
	private long[] userIDs;
	private FastByIDMap<IPreferenceArray> preferenceFromUsers;
	private long[] itemIDs;
	private FastByIDMap<IPreferenceArray> preferenceForItems;
	private FastByIDMap<FastByIDMap<DateTime?>> timestamps;
  
	/// <summary>
	/// Creates a new <see cref="GenericDataModel"/> from the given users (and their preferences). This
	/// <see cref="IDataModel"/> retains all this information in memory and is effectively immutable.
	/// </summary>
	/// <param name="userData">userData users to include; (see also <see cref="GenericDataModel.ToDataMap(FastByIDMap, bool)"/>)</param>
	public GenericDataModel(FastByIDMap<IPreferenceArray> userData) : this(userData, null) {
    
	}

	/// <summary>
	/// Creates a new <see cref="GenericDataModel"/> from the given users (and their preferences). This
	/// <see cref="IDataModel"/> retains all this information in memory and is effectively immutable.
	/// </summary>
	/// <param name="userData">users to include; (see also <see cref="GenericDataModel.ToDataMap(FastByIDMap, bool)"/>)</param>
	/// <param name="timestamps">timestamps optionally, provided timestamps of preferences as milliseconds since the epoch. User IDs are mapped to maps of item IDs to long timestamps.</param>
	public GenericDataModel(FastByIDMap<IPreferenceArray> userData, FastByIDMap<FastByIDMap<DateTime?>> timestamps) {
		//Preconditions.checkArgument(userData != null, "userData is null");

		this.preferenceFromUsers = userData;
		FastByIDMap<IList<IPreference>> prefsForItems = new FastByIDMap<IList<IPreference>>();
		FastIDSet itemIDSet = new FastIDSet();
		int currentCount = 0;
		float maxPrefValue = float.NegativeInfinity;
		float minPrefValue = float.PositiveInfinity;
		foreach (var entry in preferenceFromUsers.EntrySet()) {
			IPreferenceArray prefs = entry.Value;
			prefs.SortByItem();
			foreach (IPreference preference in prefs) {
			long itemID = preference.GetItemID();
			itemIDSet.Add(itemID);
			var prefsForItem = prefsForItems.Get(itemID);
			if (prefsForItem == null) {
				prefsForItem = new List<IPreference>(2);
				prefsForItems.Put(itemID, prefsForItem);
			}
			prefsForItem.Add(preference);
			float value = preference.GetValue();
			if (value > maxPrefValue) {
				maxPrefValue = value;
			}
			if (value < minPrefValue) {
				minPrefValue = value;
			}
			}
			if (++currentCount % 10000 == 0) {
			log.Info("Processed {0} users", currentCount);
			}
		}
		log.Info("Processed {0} users", currentCount);

		setMinPreference(minPrefValue);
		setMaxPreference(maxPrefValue);

		this.itemIDs = itemIDSet.ToArray();
		itemIDSet = null; // Might help GC -- this is big
		Array.Sort(itemIDs);

		this.preferenceForItems = ToDataMap(prefsForItems, false);

		foreach (var entry in preferenceForItems.EntrySet()) {
			entry.Value.SortByUser();
	}

	this.userIDs = new long[userData.Count()];
	int i = 0;
	foreach (var v in userData.Keys) {
		userIDs[i++] = v;
	}
	Array.Sort(userIDs);

	this.timestamps = timestamps;
	}

	/// <summary>
	/// Creates a new <see cref="GenericDataModel"/> containing an immutable copy of the data from another given
	/// <see cref="IDataModel"/>.
	/// </summary>
	/// <param name="dataModel">dataModel <see cref="IDataModel"/> to copy</param>
	public GenericDataModel(IDataModel dataModel) : this(ToDataMap(dataModel)) {
	}

	/// <summary>Swaps, in-place, <see cref="IList<T>"/>s for arrays in map values.</summary>
	/// <returns>input value</returns>
	public static FastByIDMap<IPreferenceArray> ToDataMap(FastByIDMap<IList<IPreference>> data, bool byUser) {
		var newData = new FastByIDMap<IPreferenceArray>( data.Count() );
		foreach (var entry in data.EntrySet()) {
			var prefList = entry.Value;
			newData.Put( entry.Key, 
				byUser ? (IPreferenceArray) new GenericUserPreferenceArray(prefList) : new GenericItemPreferenceArray(prefList) );
		}
		return newData;
	}

	/// <summary>Exports the simple user IDs and preferences in the data model.</summary>
	/// <returns>a <see cref="FastByIDMap"/> mapping user IDs to <see cref="IPreferenceArray"/>s representing that user's preferences</returns>
	public static FastByIDMap<IPreferenceArray> ToDataMap(IDataModel dataModel) {
		FastByIDMap<IPreferenceArray> data = new FastByIDMap<IPreferenceArray>(dataModel.GetNumUsers());
		var it = dataModel.GetUserIDs();
		while (it.MoveNext()) {
			long userID = it.Current;
			data.Put(userID, dataModel.GetPreferencesFromUser(userID));
		}
		return data;
	}

	/// <summary>This is used mostly internally to the framework, and shouldn't be relied upon otherwise.</summary>
	public FastByIDMap<IPreferenceArray> GetRawUserData() {
		return this.preferenceFromUsers;
	}

	/// <summary>This is used mostly internally to the framework, and shouldn't be relied upon otherwise.</summary>
	public FastByIDMap<IPreferenceArray> GetRawItemData() {
		return this.preferenceForItems;
	}

  public override IEnumerator<long> GetUserIDs() {
    return ((IEnumerable<long>)userIDs).GetEnumerator();
  }
  
   /// @throws NoSuchUserException
   ///           if there is no such user
  public override IPreferenceArray GetPreferencesFromUser(long userID) {
    IPreferenceArray prefs = preferenceFromUsers.Get(userID);
    if (prefs == null) {
      throw new NoSuchUserException(userID);
    }
    return prefs;
  }

  public override FastIDSet GetItemIDsFromUser(long userID) {
    IPreferenceArray prefs = GetPreferencesFromUser(userID);
    int size = prefs.Length();
    FastIDSet result = new FastIDSet(size);
    for (int i = 0; i < size; i++) {
      result.Add(prefs.GetItemID(i));
    }
    return result;
  }

  public override IEnumerator<long> GetItemIDs() {
    return ((IEnumerable<long>)itemIDs).GetEnumerator();
  }

  public override IPreferenceArray GetPreferencesForItem(long itemID) {
    IPreferenceArray prefs = preferenceForItems.Get(itemID);
    if (prefs == null) {
      throw new NoSuchItemException(itemID);
    }
    return prefs;
  }

  public override float? GetPreferenceValue(long userID, long itemID) {
    IPreferenceArray prefs = GetPreferencesFromUser(userID);
    int size = prefs.Length();
    for (int i = 0; i < size; i++) {
      if (prefs.GetItemID(i) == itemID) {
        return prefs.GetValue(i);
      }
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
    IPreferenceArray prefs1 = preferenceForItems.Get(itemID);
    return prefs1 == null ? 0 : prefs1.Length();
  }

  public override int GetNumUsersWithPreferenceFor(long itemID1, long itemID2) {
    IPreferenceArray prefs1 = preferenceForItems.Get(itemID1);
    if (prefs1 == null) {
      return 0;
    }
    IPreferenceArray prefs2 = preferenceForItems.Get(itemID2);
    if (prefs2 == null) {
      return 0;
    }

    int size1 = prefs1.Length();
    int size2 = prefs2.Length();
    int count = 0;
    int i = 0;
    int j = 0;
    long userID1 = prefs1.GetUserID(0);
    long userID2 = prefs2.GetUserID(0);
    while (true) {
      if (userID1 < userID2) {
        if (++i == size1) {
          break;
        }
        userID1 = prefs1.GetUserID(i);
      } else if (userID1 > userID2) {
        if (++j == size2) {
          break;
        }
        userID2 = prefs2.GetUserID(j);
      } else {
        count++;
        if (++i == size1 || ++j == size2) {
          break;
        }
        userID1 = prefs1.GetUserID(i);
        userID2 = prefs2.GetUserID(j);
      }
    }
    return count;
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
    return true;
  }
  
  public override string ToString() {
    var result = new System.Text.StringBuilder(200);
    result.Append("GenericDataModel[users:");
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