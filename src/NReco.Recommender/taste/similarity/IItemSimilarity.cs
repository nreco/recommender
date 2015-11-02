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

namespace NReco.CF.Taste.Similarity {

/// <summary>
/// Implementations of this interface define a notion of similarity between two items. Implementations should
/// return values in the range -1.0 to 1.0, with 1.0 representing perfect similarity.
/// </summary>
/// <see cref="IUserSimilarity"/>
public interface IItemSimilarity : IRefreshable {
  
	/// <summary>
	/// Returns the degree of similarity, of two items, based on the preferences that users have expressed for
	/// the items.
	/// </summary>
	/// <param name="itemID1">first item ID</param>
	/// <param name="itemID2">second item ID</param>
	/// <returns>similarity between the items, in [-1,1] or {@link Double#NaN} similarity is unknown</returns>
	/// <remarks>
	/// Throws NReco.CF.Taste.Common.NoSuchItemException if either item is known to be non-existent in the data.
	/// Throws TasteException if an error occurs while accessing the data.
	/// </remarks>
	double ItemSimilarity(long itemID1, long itemID2) ;

	/// <summary>
	/// A bulk-get version of <see cref="ItemSimilarity(long, long)"/>.
	/// </summary>
	/// <param name="itemID1">first item ID</param>
	/// <param name="itemID2s">second item IDs to compute similarity with</param>
	/// <returns>similarity between itemID1 and other items</returns>
	double[] ItemSimilarities(long itemID1, long[] itemID2s);

	/// <summary>
	/// Return all similar item IDs
	/// </summary>
	/// <returns>all IDs of similar items, in no particular order</returns>
	long[] AllSimilarItemIDs(long itemID) ;


}

}