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
using NReco.CF.Taste.Recommender;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Recommender {

/// <p>Tests {@link CachingRecommender}.</p> 
public sealed class CachingRecommenderTest : TasteTestCase {

  [Test]
  public void testRecommender() {
    var mockRecommender = new MockRecommender(0);

    IRecommender cachingRecommender = new CachingRecommender(mockRecommender);
    cachingRecommender.Recommend(1, 1);
    Assert.AreEqual(1, mockRecommender.recommendCount);
    cachingRecommender.Recommend(2, 1);
	Assert.AreEqual(2, mockRecommender.recommendCount);
    cachingRecommender.Recommend(1, 1);
	Assert.AreEqual(2, mockRecommender.recommendCount);
    cachingRecommender.Recommend(2, 1);
	Assert.AreEqual(2, mockRecommender.recommendCount);
    cachingRecommender.Refresh(null);
    cachingRecommender.Recommend(1, 1);
	Assert.AreEqual(3, mockRecommender.recommendCount);
    cachingRecommender.Recommend(2, 1);
	Assert.AreEqual(4, mockRecommender.recommendCount);
    cachingRecommender.Recommend(3, 1);
	Assert.AreEqual(5, mockRecommender.recommendCount);

    // Results from this recommend() method can be cached...
    IDRescorer rescorer = NullRescorer.getItemInstance();
    cachingRecommender.Refresh(null);
    cachingRecommender.Recommend(1, 1, rescorer);
	Assert.AreEqual(6, mockRecommender.recommendCount);
    cachingRecommender.Recommend(2, 1, rescorer);
	Assert.AreEqual(7, mockRecommender.recommendCount);
    cachingRecommender.Recommend(1, 1, rescorer);
	Assert.AreEqual(7, mockRecommender.recommendCount);
    cachingRecommender.Recommend(2, 1, rescorer);
	Assert.AreEqual(7, mockRecommender.recommendCount);

    // until you switch Rescorers
    cachingRecommender.Recommend(1, 1, null);
	Assert.AreEqual(8, mockRecommender.recommendCount);
    cachingRecommender.Recommend(2, 1, null);
	Assert.AreEqual(9, mockRecommender.recommendCount);

    cachingRecommender.Refresh(null);
    cachingRecommender.EstimatePreference(1, 1);
	Assert.AreEqual(10, mockRecommender.recommendCount);
    cachingRecommender.EstimatePreference(1, 2);
	Assert.AreEqual(11, mockRecommender.recommendCount);
    cachingRecommender.EstimatePreference(1, 2);
	Assert.AreEqual(11, mockRecommender.recommendCount);
  }

}

}