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

namespace NReco.CF.Taste.Eval {

	/// <summary>
	/// Implementations encapsulate information retrieval-related statistics about a
	/// <see cref="NReco.CF.Taste.Recommender.IRecommender"/>'s recommendations.
	/// </summary>
	/// <remarks>See <a href="http://en.wikipedia.org/wiki/Information_retrieval">Information retrieval</a>.</remarks>
	public interface IRStatistics {

		/// <summary>
		/// See <a href="http://en.wikipedia.org/wiki/Information_retrieval#Precision">Precision</a>.
		/// </summary>
		double GetPrecision();

		/// <summary>
		/// See <a href="http://en.wikipedia.org/wiki/Information_retrieval#Recall">Recall</a>.
		/// </summary>
		double GetRecall();

		/// <summary>
		/// See <a href="http://en.wikipedia.org/wiki/Information_retrieval#Fall-Out">Fall-Out</a>.
		/// </summary>
		double GetFallOut();

		/// <summary>
		/// See <a href="http://en.wikipedia.org/wiki/Information_retrieval#F-measure">F-measure</a>.
		/// </summary>
		double GetF1Measure();

		/// <summary>
		/// See <a href="http://en.wikipedia.org/wiki/Information_retrieval#F-measure">F-measure</a>.
		/// </summary>
		double GetFNMeasure(double n);

		/// <summary>
		/// See <a href="http://en.wikipedia.org/wiki/Discounted_cumulative_gain#Normalized_DCG">
		/// Normalized Discounted Cumulative Gain</a>.
		/// </summary>
		double GetNormalizedDiscountedCumulativeGain();
  
		/// <summary>
		/// The fraction of all users for whom recommendations could be produced
		/// </summary>
		double GetReach();
  
	}

}