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

namespace NReco.CF.Taste.Model {

	/// <summary>
	/// A <see cref="IPreference"/> encapsulates an item and a preference value, which indicates the strength of the
	/// preference for it. <see cref="IPreference"/>s are associated to users.
	/// </summary>
	public interface IPreference {
  
		/// <summary>
		/// ID of user who prefers the item 
		/// </summary>
		long GetUserID();
  
		/// <summary>
		/// Item ID that is preferred 
		/// </summary>
		long GetItemID();

		/// <summary>
		/// Strength of the preference for that item. 
		/// </summary>
		/// <remarks>Zero should indicate "no preference either way"; positive values indicate preference and negative values indicate dislike.</remarks>
		float GetValue();

		/// <summary>
		/// Sets the strength of the preference for this item
		/// </summary>
		/// <param name="value">new preference</param>
		void SetValue(float value);
  
	}

}