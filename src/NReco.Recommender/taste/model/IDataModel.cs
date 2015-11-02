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

namespace NReco.CF.Taste.Model {

	/// <summary>
	/// Implementations represent a repository of information about users and their associated <see cref="IPreference"/>s
	/// for items.
	/// </summary>
	public interface IDataModel : IRefreshable {
  
		/// <summary>
		/// All user IDs in the model, in order
		/// </summary>
		IEnumerator<long> GetUserIDs() ;
  
		/// <summary>
		/// Get preferences for specified user ID
		/// </summary>
		/// <param name="userID">ID of user to get prefs for</param>
		/// <returns>user's preferences, ordered by item ID</returns>
		/// <remarks>Throws NReco.CF.Taste.Common.NoSuchUserException if the user does not exist</remarks>
		IPreferenceArray GetPreferencesFromUser(long userID);


		/// <summary>
		/// Get list of item IDs for specified user ID
		/// </summary>
		/// <param name="userID">ID of user to get prefs for</param>
		/// <returns>IDs of items user expresses a preference for</returns>
		FastIDSet GetItemIDsFromUser(long userID);

		/// <summary>
		/// Get all item IDs in the model
		/// </summary>
		/// <returns><see cref="IEnumerator"/> of all item IDs in the model, in order</returns>
		IEnumerator<long> GetItemIDs();


		/// <summary>
		/// Get all existing preferences by specified item ID
		/// </summary>
		/// <param name="itemID">item ID</param>
		/// <returns>all existing <see cref="IPreference"/>s expressed for that item, ordered by user ID, as an array</returns>
		IPreferenceArray GetPreferencesForItem(long itemID);

		/// <summary>
		/// Retrieves the preference value for a single user and item.
		/// </summary>
		/// <param name="userID">user ID to get pref value from</param>
		/// <param name="itemID">item ID to get pref value for</param>
		/// <returns>preference value from the given user for the given item or null if none exists</returns>
		float? GetPreferenceValue(long userID, long itemID);

		/// <summary>
		/// Retrieves the time at which a preference value from a user and item was set, if known.
		/// </summary>
		/// <param name="userID">user ID for preference in question</param>
		/// <param name="itemID">item ID for preference in question</param>
		/// <returns>time at which preference was set or null if no preference exists or its time is not known</returns>
		DateTime? GetPreferenceTime(long userID, long itemID);
  
		/// <summary>
		/// Get total number of items in the model
		/// </summary>
		/// <returns>total number of items known to the model. This is generally the union of all items preferred by at least one user but could include more.</returns>
		int GetNumItems();

		/// <summary>
		/// Get total number of users in the model
		/// </summary>
		/// <returns>total number of users known to the model.</returns>
		int GetNumUsers();

		/// <summary>
		/// Ger number of users that prefer specified item ID
		/// </summary>
		/// <param name="itemID">item ID to check for</param>
		/// <returns>the number of users who have expressed a preference for the item</returns>
		int GetNumUsersWithPreferenceFor(long itemID);

		/// <summary>
		/// Ger number of users that prefer both specified item IDs
		/// </summary>
		/// <param name="itemID1">first item ID to check for</param>
		/// <param name="itemID2">second item ID to check for</param>
		/// <returns>the number of users who have expressed a preference for the items</returns>
		int GetNumUsersWithPreferenceFor(long itemID1, long itemID2);
  
		/// <summary>
		/// Sets a particular preference (item plus rating) for a user.
		/// </summary>
		/// <param name="userID">user to set preference for</param>
		/// <param name="itemID">item to set preference for</param>
		/// <param name="value">preference value</param>
		/// <remarks>
		/// throws NReco.CF.Taste.Common.NoSuchItemException if the item does not exist.
		/// throws NReco.CF.Taste.Common.NoSuchUserException if the user does not exist.
		/// </remarks>
		void SetPreference(long userID, long itemID, float value);

  
		/// <summary>
		/// Removes a particular preference for a user.
		/// </summary>
		/// <param name="userID">user from which to remove preference</param>
		/// <param name="itemID">item to remove preference for</param>
		/// <remarks>
		/// Throws NReco.CF.Taste.Common.NoSuchItemException if the item does not exist.
		/// Throws NReco.CF.Taste.Common.NoSuchUserException if the user does not exist
		/// </remarks>
		void RemovePreference(long userID, long itemID);

		/// <summary>
		/// Check if data model has distinct preference values
		/// </summary>
		/// <returns>true if this implementation actually stores and returns distinct preference values that is, if it is not a 'bool' DataModel</returns>
		bool HasPreferenceValues();

		/// <summary>
		/// Get maximum preference value that is possible in the current problem domain being evaluated. 
		/// </summary>
		/// <remarks>
		/// For example, if the domain is movie ratings on a scale of 1 to 5, this should be 5. While a
		/// <see cref="NReco.CF.Taste.Recommender.IRecommender"/> may estimate a preference value above 5.0, it
		/// isn't "fair" to consider that the system is actually suggesting an impossible rating of, say, 5.4 stars.
		/// In practice the application would cap this estimate to 5.0. Since evaluators evaluate
		/// the difference between estimated and actual value, this at least prevents this effect from unfairly
		/// penalizing a <see cref="NReco.CF.Taste.Recommender.IRecommender"/>.
		/// </remarks>
		float GetMaxPreference();

		/// <summary>
		/// Get minimum preference value that is possible in the current problem domain being evaluated.
		/// </summary>
		float GetMinPreference();
  
	}

}