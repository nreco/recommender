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
using NReco.CF.Taste.Impl;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;
using NUnit.Mocks;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Recommender.SVD {


public class SVDRecommenderTest : TasteTestCase {

  [Test]
  public void estimatePreference() {
    var dataModelMock = new DynamicMock( typeof( IDataModel) );
    var factorizerMock = new DynamicMock( typeof(IFactorizer) );
    var factorization = new Factorization_estimatePreference_TestMock();

	factorizerMock.ExpectAndReturn("Factorize", factorization);
    
	 //EasyMock.replay(dataModel, factorizer, factorization);

    SVDRecommender svdRecommender = new SVDRecommender( (IDataModel)dataModelMock.MockInstance, (IFactorizer)factorizerMock.MockInstance);

    float estimate = svdRecommender.EstimatePreference(1L, 5L);
    Assert.AreEqual(1, estimate, EPSILON);

	factorizerMock.Verify();
	Assert.AreEqual(1, factorization.getItemFeaturesCallCount );
	Assert.AreEqual(1, factorization.getUserFeaturesCallCount);
		//EasyMock.verify(dataModel, factorizer, factorization);
  }


  public class Factorization_estimatePreference_TestMock : Factorization {
	  public int getItemFeaturesCallCount = 0;
	  public int getUserFeaturesCallCount = 0;

	  public Factorization_estimatePreference_TestMock() : base( new FastByIDMap<int?>(0), new FastByIDMap<int?>(0),
		null,null) {

		}

	  public override double[] getItemFeatures(long itemID) {
		  getItemFeaturesCallCount++;
		  if (itemID == 5L) 
			return new double[] { 1, 0.3 };
		  throw new Exception();
	  }
	  public override double[] getUserFeatures(long userID) {
		  getUserFeaturesCallCount++;
		  if (userID == 1L)
			  return new double[] { 0.4, 2 };
		  throw new Exception();
	  }
  }

  [Test]
  public void recommend() {
    var dataModelMock = new DynamicMock( typeof(IDataModel) );
    var preferencesFromUserMock = new DynamicMock( typeof(IPreferenceArray) );
    var candidateItemsStrategyMock = new DynamicMock( typeof(ICandidateItemsStrategy) );
    var factorizerMock = new DynamicMock( typeof(IFactorizer) );
    var factorization = new Factorization_recommend_TestMock();

    FastIDSet candidateItems = new FastIDSet();
    candidateItems.Add(5L);
    candidateItems.Add(3L);

    factorizerMock.ExpectAndReturn("Factorize", factorization);

	dataModelMock.ExpectAndReturn("GetPreferencesFromUser", preferencesFromUserMock.MockInstance, (1L));

	candidateItemsStrategyMock.ExpectAndReturn("GetCandidateItems", candidateItems, 
		1L, preferencesFromUserMock.MockInstance, dataModelMock.MockInstance);

    //EasyMock.replay(dataModel, candidateItemsStrategy, factorizer, factorization);

    SVDRecommender svdRecommender = new SVDRecommender( 
		(IDataModel)dataModelMock.MockInstance, 
		(IFactorizer)factorizerMock.MockInstance, 
		(ICandidateItemsStrategy)candidateItemsStrategyMock.MockInstance);

    IList<IRecommendedItem> recommendedItems = svdRecommender.Recommend(1L, 5);
    Assert.AreEqual(2, recommendedItems.Count);
    Assert.AreEqual(3L, recommendedItems[0].GetItemID());
    Assert.AreEqual(2.0f, recommendedItems[0].GetValue(), EPSILON);
    Assert.AreEqual(5L, recommendedItems[1].GetItemID());
    Assert.AreEqual(1.0f, recommendedItems[1].GetValue(), EPSILON);

	dataModelMock.Verify();
	candidateItemsStrategyMock.Verify();
	factorizerMock.Verify();

	Assert.AreEqual(2, factorization.getItemFeaturesCallCount);
	Assert.AreEqual(2, factorization.getUserFeaturesCallCount);
    //EasyMock.verify(dataModel, candidateItemsStrategy, factorizer, factorization);
  }


  public class Factorization_recommend_TestMock : Factorization {
	  public int getItemFeaturesCallCount = 0;
	  public int getUserFeaturesCallCount = 0;

	  public Factorization_recommend_TestMock()
		  : base(new FastByIDMap<int?>(0), new FastByIDMap<int?>(0),
			  null, null) {

	  }

	  public override double[] getItemFeatures(long itemID) {
		  getItemFeaturesCallCount++;
		  if (itemID == 5L)
			  return new double[] { 1, 0.3 };
		  if (itemID == 3L)
			  return new double[] { 2, 0.6 };
		  throw new Exception();
	  }
	  public override double[] getUserFeatures(long userID) {
		  getUserFeaturesCallCount++;
		  if (userID == 1L)
			  return new double[] { 0.4, 2 };
		  throw new Exception();
	  }
  }



}

}