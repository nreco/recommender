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
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS 
 *  OF ANY KIND, either express or implied.
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NReco.CF.Taste.Common;
using NReco.CF.Taste.Model;

namespace NReco.CF.Taste.Eval {

/// <summary>
/// Implementations of this interface evaluate the quality of a
/// <see cref="NReco.CF.Taste.Recommender.IRecommender"/>'s recommendations.
/// </summary>
public interface IRecommenderEvaluator {
  
	/// <summary>
	/// Evaluates the quality of a <see cref="NReco.CF.Taste.Recommender.IRecommender"/>'s recommendations.
	/// The range of values that may be returned depends on the implementation, but <em>lower</em> values must
	/// mean better recommendations, with 0 being the lowest / best possible evaluation, meaning a perfect match.
	/// This method does not accept a <see cref="NReco.CF.Taste.Recommender.IRecommender"/> directly, but
	/// rather a <see cref="NReco.CF.Taste.Recommender.IRecommenderBuilder"/> which can build the
	/// <see cref="NReco.CF.Taste.Recommender.IRecommender"/> to test on top of a given <see cref="IDataModel"/>.
	/// 
	/// <para>
	/// Implementations will take a certain percentage of the preferences supplied by the given <see cref="IDataModel"/>
	/// as "training data". This is typically most of the data, like 90%. This data is used to produce
	/// recommendations, and the rest of the data is compared against estimated preference values to see how much
	/// the <see cref="NReco.CF.Taste.Recommender.IRecommender"/>'s predicted preferences match the user's
	/// real preferences. Specifically, for each user, this percentage of the user's ratings are used to produce
	/// recommendations, and for each user, the remaining preferences are compared against the user's real
	/// preferences.
	/// </para>
	///
	/// <para>
	/// For large datasets, it may be desirable to only evaluate based on a small percentage of the data.
	/// <code>evaluationPercentage</code> controls how many of the <see cref="IDataModel"/>'s users are used in
	/// evaluation.
	/// </para>
	///
	/// <para>
	/// To be clear, <code>trainingPercentage</code> and <code>evaluationPercentage</code> are not related. They
	/// do not need to add up to 1.0, for example.
	/// </para>
	/// </summary>
	/// <param name="recommenderBuilder">object that can build a <see cref="NReco.CF.Taste.Recommender.IRecommender"/> to test</param>
	/// <param name="dataModelBuilder"><see cref="IDataModelBuilder"/> to use, or if null, a default <see cref="IDataModel"/> implementation will be used</param>     
	/// <param name="dataModel">dataset to test on</param>  
	/// <param name="trainingPercentage">
	/// percentage of each user's preferences to use to produce recommendations; the rest are compared
	/// to estimated preference values to evaluate
	/// <see cref="NReco.CF.Taste.Recommender.IRecommender"/> performance
	/// </param>
	/// <param name="evaluationPercentage">
	/// percentage of users to use in evaluation
	/// </param>
	/// <returns>
	/// a "score" representing how well the <see cref="NReco.CF.Taste.Recommender.IRecommender"/>'s
	/// estimated preferences match real values; <em>lower</em> scores mean a better match and 0 is a perfect match
	/// </returns>
	double Evaluate(IRecommenderBuilder recommenderBuilder,
					IDataModelBuilder dataModelBuilder,
					IDataModel dataModel,
					double trainingPercentage,
					double evaluationPercentage);

   /// @deprecated see {@link DataModel#getMaxPreference()}
	[Obsolete]
	float GetMaxPreference();

	[Obsolete]
	void SetMaxPreference(float maxPreference);

	/// @deprecated see {@link DataModel#getMinPreference()}
	[Obsolete]
	float GetMinPreference();

	[Obsolete]
	void SetMinPreference(float minPreference);
  
}

}