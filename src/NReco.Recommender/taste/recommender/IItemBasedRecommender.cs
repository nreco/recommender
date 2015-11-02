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
using NReco.CF;

namespace NReco.CF.Taste.Recommender {

	/// <summary>
	/// Interface implemented by "item-based" recommenders.
	/// </summary>
	public interface IItemBasedRecommender : IRecommender {
  
		/// <summary>
		/// Get list of most similar items
		/// </summary>
		/// <param name="itemID">ID of item for which to find most similar other items</param>
		/// <param name="howMany">desired number of most similar items to find</param>
		/// <returns>items most similar to the given item, ordered from most similar to least</returns>
		List<IRecommendedItem> MostSimilarItems(long itemID, int howMany);


		/// <summary>
		/// Get list of most similar items
		/// </summary>
		/// <param name="itemID">ID of item for which to find most similar other items</param>
		/// <param name="howMany">desired number of most similar items to find</param>
		/// <param name="rescorer"><see cref="IRescorer"/> which can adjust item-item similarity estimates used to determine most similar items</param>
		/// <returns>items most similar to the given item, ordered from most similar to least</returns>
		List<IRecommendedItem> MostSimilarItems(long itemID, int howMany, IRescorer<Tuple<long,long>> rescorer);

		/// <summary>
		/// Get list of most similar items
		/// </summary>
		/// <param name="itemIDs">IDs of item for which to find most similar other items</param>
		/// <param name="howMany">desired number of most similar items to find estimates used to determine most similar items</param>
		/// <returns>items most similar to the given items, ordered from most similar to least</returns>
		List<IRecommendedItem> MostSimilarItems(long[] itemIDs, int howMany) ;


		/// <summary>
		/// Get list of most similar items
		/// </summary>
		/// <param name="itemIDs">IDs of item for which to find most similar other items</param>
		/// <param name="howMany">desired number of most similar items to find</param>
		/// <param name="rescorer"><see cref="IRescorer"/> which can adjust item-item similarity estimates used to determine most similar items</param>
		/// <returns>items most similar to the given items, ordered from most similar to least</returns>
		List<IRecommendedItem> MostSimilarItems(long[] itemIDs,
												int howMany,
												IRescorer<Tuple<long,long>> rescorer);

		/// <summary>
		/// Get list of most similar items
		/// </summary>
		/// <param name="itemIDs">IDs of item for which to find most similar other items</param>
		/// <param name="howMany">desired number of most similar items to find</param>
		/// <param name="excludeItemIfNotSimilarToAll">exclude an item if it is not similar to each of the input items</param>
		/// <returns>items most similar to the given items, ordered from most similar to least</returns>
		List<IRecommendedItem> MostSimilarItems(long[] itemIDs,
												int howMany,
												bool excludeItemIfNotSimilarToAll);

		/// <summary>
		/// Get list of most similar items
		/// </summary>
		/// <param name="itemIDs">IDs of item for which to find most similar other items</param>
		/// <param name="howMany">desired number of most similar items to find</param>
		/// <param name="rescorer">{@link Rescorer} which can adjust item-item similarity estimates used to determine most similar items</param>
		/// <param name="excludeItemIfNotSimilarToAll">exclude an item if it is not similar to each of the input items</param>
		/// <returns>items most similar to the given items, ordered from most similar to least</returns>
		List<IRecommendedItem> MostSimilarItems(long[] itemIDs,
												int howMany,
												IRescorer<Tuple<long,long>> rescorer,
												bool excludeItemIfNotSimilarToAll);

		/// <summary>
		/// Lists the items that were most influential in recommending a given item to a given user. Exactly how this
		/// is determined is left to the implementation, but, generally this will return items that the user prefers
		/// and that are similar to the given item.
		/// <para>
		/// This returns a <see cref="List"/> of <see cref="IRecommendedItem"/> which is a little misleading since it's returning
		/// recommend<strong>ing</strong> items, but, I thought it more natural to just reuse this class since it
		/// encapsulates an item and value. The value here does not necessarily have a consistent interpretation or
		/// expected range; it will be higher the more influential the item was in the recommendation. 
		/// </para>
		/// </summary>
		/// <param name="userID">ID of user who was recommended the item</param>
		/// <param name="itemID">ID of item that was recommended</param>
		/// <param name="howMany">maximum number of items to return</param>
		/// <returns><see cref="List"/> of <see cref="IRecommendedItem"/>, ordered from most influential in recommended the given item to least</returns>
		List<IRecommendedItem> RecommendedBecause(long userID, long itemID, int howMany);

	}

}