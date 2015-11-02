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
	/// Implementations of this interface define a notion of similarity between two users. Implementations should
	/// return values in the range -1.0 to 1.0, with 1.0 representing perfect similarity.
	/// </summary>
	/// <see cref="IItemSimilarity"/>
	public interface IUserSimilarity : IRefreshable {
  
		/// <summary>
		/// Returns the degree of similarity, of two users, based on the their preferences.
		/// </summary>
		/// <param name="userID1">first user ID</param>
		/// <param name="userID2">second user ID</param>
		/// <returns>similarity between the users, in [-1,1] or Double.NaN if similarity is unknown</returns>
		/// <remarks>
		/// Throws NReco.CF.Taste.Common.NoSuchUserException if either user is known to be non-existent in the data.
		/// Throws TasteException if an error occurs while accessing the data.
		/// </remarks>
		double UserSimilarity(long userID1, long userID2) ;


		// Should we implement userSimilarities() like ItemSimilarity.itemSimilarities()?
  
		/// <summary>
		/// Attaches a <see cref="IPreferenceInferrer"/> to the <see cref="IUserSimilarity"/> implementation.
		/// </summary>
		/// <param name="inferrer">inferrer to set</param>
		void SetPreferenceInferrer(IPreferenceInferrer inferrer);


	}

}