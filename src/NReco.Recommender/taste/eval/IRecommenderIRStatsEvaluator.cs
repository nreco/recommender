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
using NReco.CF.Taste.Recommender;

namespace NReco.CF.Taste.Eval {

/// <summary>
/// Implementations collect information retrieval-related statistics on a
/// <see cref="NReco.CF.Taste.Recommender.IRecommender"/>'s performance, including precision, recall and
/// f-measure.
/// </summary>
/// 
/// <remarks>
/// See <a href="http://en.wikipedia.org/wiki/Information_retrieval">Information retrieval</a>.
/// </remarks>
public interface IRecommenderIRStatsEvaluator {
  
   /// @param recommenderBuilder
   ///          object that can build a {@link NReco.CF.Taste.Recommender.Recommender} to test
   /// @param dataModelBuilder
   ///          {@link DataModelBuilder} to use, or if null, a default {@link DataModel} implementation will be
   ///          used
   /// @param dataModel
   ///          dataset to test on
   /// @param rescorer
   ///          if any, to use when computing recommendations
   /// @param at
   ///          as in, "precision at 5". The number of recommendations to consider when evaluating precision,
   ///          etc.
   /// @param relevanceThreshold
   ///          items whose preference value is at least this value are considered "relevant" for the purposes
   ///          of computations
   /// @return {@link IRStatistics} with resulting precision, recall, etc.
   /// @throws TasteException
   ///           if an error occurs while accessing the {@link DataModel}
  IRStatistics Evaluate(IRecommenderBuilder recommenderBuilder,
                        IDataModelBuilder dataModelBuilder,
                        IDataModel dataModel,
                        IDRescorer rescorer,
                        int at,
                        double relevanceThreshold,
                        double evaluationPercentage) ;
  
}

}