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
using NReco.CF.Taste.Model;

namespace NReco.CF.Taste.Recommender {

	/// <summary>
	/// Implementations of this interface can recommend items for a user. Implementations will likely take
	/// advantage of several classes in other packages here to compute this.
	/// </summary>
	public interface IRecommender : IRefreshable {
  
		/// <summary>
		/// Recommend desired number of items for given user ID
		/// </summary>
		/// <param name="userID">user for which recommendations are to be computed</param>
		/// <param name="howMany">desired number of recommendations</param>
		/// <returns><see cref="ICollection"/> of recommended <see cref="IRecommendedItem"/>s, ordered from most strongly recommend to least</returns>
		IList<IRecommendedItem> Recommend(long userID, int howMany);       

		/// <summary>
		/// Recommend desired number of items for given user ID and rescorer
		/// </summary>
		/// <param name="userID">user for which recommendations are to be computed</param>
		/// <param name="howMany">desired number of recommendations</param>
		/// <param name="rescorer">rescoring function to apply before final list of recommendations is determined</param>
		/// <returns><see cref="List"/> of recommended <see cref="IRecommendedItem"/>s, ordered from most strongly recommend to least</returns>
		IList<IRecommendedItem> Recommend(long userID, int howMany, IDRescorer rescorer);


		/// <summary>
		/// Estimate preference for given user ID and item ID
		/// </summary>
		/// <param name="userID">user ID whose preference is to be estimated</param>
		/// <param name="itemID">item ID to estimate preference for</param>
		/// <returns>
		/// an estimated preference if the user has not expressed a preference for the item, or else the
		/// user's actual preference for the item. If a preference cannot be estimated, returns Double.NaN
		/// </returns>
		float EstimatePreference(long userID, long itemID);

		/// <summary>
		/// Set preference value for given user ID and item ID
		/// </summary>
		/// <param name="userID">user to set preference for</param>
		/// <param name="itemID">item to set preference for</param>
		/// <param name="value">preference value</param>
		void SetPreference(long userID, long itemID, float value);


		/// <summary>
		/// Remove preferense for given user ID and item ID
		/// </summary>
		/// <param name="userID">user from which to remove preference</param>
		/// <param name="itemID">item for which to remove preference</param>
		void RemovePreference(long userID, long itemID);

		/// <summary>
		/// Get underlying data model instance
		/// </summary>
		/// <returns>underlying <see cref="IDataModel"/> used by this <see cref="IRecommender"/> implementation</returns>
		IDataModel GetDataModel();

	}

}