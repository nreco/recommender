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

namespace NReco.CF.Taste.Recommender {

/// <summary>
/// A IRescorer simply assigns a new "score" to a thing like an ID of an item or user which a
/// {@link Recommender} is considering returning as a top recommendation. It may be used to arbitrarily re-rank
/// the results according to application-specific logic before returning recommendations. For example, an
/// application may want to boost the score of items in a certain category just for one request.
/// <para>
/// A <see cref="IRescorer"/> can also exclude a thing from consideration entirely by returning <code>true</code> from
/// <see cref="IRescorer.isFiltered"/>.
/// </para>
/// </summary>
public interface IRescorer<T> {
  
	/// <summary>
	/// Calculate new score for given thing and its original score
	/// </summary>
	/// <param name="thing">thing to rescore</param>
	/// <param name="originalScore">original score</param>
	/// <returns>modified score, or {@link Double#NaN} to indicate that this should be excluded entirely</returns>
	double Rescore(T thing, double originalScore);


	/// <summary>
	/// Returns <code>true</code> to exclude the given thing.
	/// </summary>
	/// <param name="thing">the thing to filter</param>
	/// <returns><code>true</code> to exclude, <code>false</code> otherwise</returns>
	bool IsFiltered(T thing);

}

}