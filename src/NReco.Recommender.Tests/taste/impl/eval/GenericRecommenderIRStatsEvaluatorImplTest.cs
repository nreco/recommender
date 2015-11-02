/*
 *  Copyright 2013-2015 Vitalii Fedorchenko (nrecosite.com)
 *
 *	This program is free software; you can redistribute it and/or modify
 *	it under the terms of the GNU General Public License as published by
 *	the Free Software Foundation; either version 2 of the License, or
 *	(at your option) any later version.
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
using NReco.CF.Taste.Eval;
using NReco.CF.Taste.Impl;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Impl.Model;
using NReco.CF.Taste.Impl.Recommender;
using NReco.CF.Taste.Impl.Similarity;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Eval {

public sealed class GenericRecommenderIRStatsEvaluatorImplTest : TasteTestCase {

  [Test]
  public void testBoolean() {
    IDataModel model = getBooleanDataModel();
    IRecommenderBuilder builder = new TestRecommenderBuilder();

	IDataModelBuilder dataModelBuilder = new TestDataModelBuilder();
    IRecommenderIRStatsEvaluator evaluator = new GenericRecommenderIRStatsEvaluator();
    IRStatistics stats = evaluator.Evaluate(
        builder, dataModelBuilder, model, null, 1, GenericRecommenderIRStatsEvaluator.CHOOSE_THRESHOLD, 1.0);

    Assert.NotNull(stats);
    Assert.AreEqual(0.666666666, stats.GetPrecision(), EPSILON);
    Assert.AreEqual(0.666666666, stats.GetRecall(), EPSILON);
    Assert.AreEqual(0.666666666, stats.GetF1Measure(), EPSILON);
    Assert.AreEqual(0.666666666, stats.GetFNMeasure(2.0), EPSILON);
    Assert.AreEqual(0.666666666, stats.GetNormalizedDiscountedCumulativeGain(), EPSILON);
  }

  public class TestRecommenderBuilder : IRecommenderBuilder {
      public IRecommender BuildRecommender(IDataModel dataModel) {
        return new GenericBooleanPrefItemBasedRecommender(dataModel, new LogLikelihoodSimilarity(dataModel));
      }
    };

  public class TestDataModelBuilder : IDataModelBuilder {
	  public IDataModel BuildDataModel(FastByIDMap<IPreferenceArray> trainingData) {
		  return new GenericBooleanPrefDataModel(GenericBooleanPrefDataModel.toDataMap(trainingData));
	  }
  };
  

  [Test]
  public void testIRStats() {
    IRStatistics stats = new IRStatisticsImpl(0.3, 0.1, 0.2, 0.05, 0.15);
    Assert.AreEqual(0.3, stats.GetPrecision(), EPSILON);
    Assert.AreEqual(0.1, stats.GetRecall(), EPSILON);
    Assert.AreEqual(0.15, stats.GetF1Measure(), EPSILON);
    Assert.AreEqual(0.11538461538462, stats.GetFNMeasure(2.0), EPSILON);
    Assert.AreEqual(0.05, stats.GetNormalizedDiscountedCumulativeGain(), EPSILON);
  }

}
}