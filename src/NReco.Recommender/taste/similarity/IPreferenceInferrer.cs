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
	/// Implementations of this interface compute an inferred preference for a user and an item that the user has
	/// not expressed any preference for. This might be an average of other preferences scores from that user, for
	/// example. This technique is sometimes called "default voting".
	/// </summary>
	public interface IPreferenceInferrer : IRefreshable {
  
		/// <summary>
		/// Infers the given user's preference value for an item.
		/// </summary>
		/// <param name="userID">ID of user to infer preference for</param>
		/// <param name="itemID">item ID to infer preference for</param>
		/// <returns>inferred preference</returns>
		float InferPreference(long userID, long itemID);
  
	}

}