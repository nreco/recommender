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
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;

namespace NReco.CF.Taste.Impl.Eval {

/// <summary>
/// Simple helper class for running load on a Recommender.
/// </summary>
public sealed class LoadEvaluator {
  
  private LoadEvaluator() { }

  public static LoadStatistics runLoad(IRecommender recommender) {
    return runLoad(recommender, 10);
  }
  
  public static LoadStatistics runLoad(IRecommender recommender, int howMany) {
    IDataModel dataModel = recommender.GetDataModel();
    int numUsers = dataModel.GetNumUsers();
    double sampleRate = 1000.0 / numUsers;
    var userSampler =
        SamplinglongPrimitiveIterator.MaybeWrapIterator(dataModel.GetUserIDs(), sampleRate);
    
	if (userSampler.MoveNext()) 
		recommender.Recommend(userSampler.Current, howMany); // Warm up
    
	var callables = new List<Action>();
    while (userSampler.MoveNext()) {
      callables.Add(new LoadCallable(recommender, userSampler.Current).call);
    }
    AtomicInteger noEstimateCounter = new AtomicInteger();
    IRunningAverageAndStdDev timing = new FullRunningAverageAndStdDev();
    AbstractDifferenceRecommenderEvaluator.execute(callables, noEstimateCounter, timing);
    return new LoadStatistics(timing);
  }

}

}