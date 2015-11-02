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
using NReco.CF.Taste.Impl;
using NReco.CF.Taste.Impl.Neighborhood;
using NReco.CF.Taste.Impl.Similarity;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Neighborhood;
using NReco.CF.Taste.Recommender;
using NReco.CF.Taste.Similarity;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Recommender {


/// <p>Tests {@link GenericUserBasedRecommender}.</p> 
public sealed class GenericUserBasedRecommenderTest : TasteTestCase {

  [Test]
  public void testRecommender() {
    IRecommender recommender = buildRecommender();
    IList<IRecommendedItem> recommended = recommender.Recommend(1, 1);
    Assert.NotNull(recommended);
    Assert.AreEqual(1, recommended.Count);
    IRecommendedItem firstRecommended = recommended[0];
    Assert.AreEqual(2, firstRecommended.GetItemID());
    Assert.AreEqual(0.1f, firstRecommended.GetValue(), EPSILON);
    recommender.Refresh(null);
    Assert.AreEqual(2, firstRecommended.GetItemID());
    Assert.AreEqual(0.1f, firstRecommended.GetValue(), EPSILON);
  }

  [Test]
  public void testHowMany() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2, 3, 4, 5},
            new Double?[][] {
                    new double?[]{0.1, 0.2},
                    new double?[]{0.2, 0.3, 0.3, 0.6},
                    new double?[]{0.4, 0.4, 0.5, 0.9},
                    new double?[]{0.1, 0.4, 0.5, 0.8, 0.9, 1.0},
                    new double?[]{0.2, 0.3, 0.6, 0.7, 0.1, 0.2},
            });
    IUserSimilarity similarity = new PearsonCorrelationSimilarity(dataModel);
    IUserNeighborhood neighborhood = new NearestNUserNeighborhood(2, similarity, dataModel);
    IRecommender recommender = new GenericUserBasedRecommender(dataModel, neighborhood, similarity);
    IList<IRecommendedItem> fewRecommended = recommender.Recommend(1, 2);
    IList<IRecommendedItem> moreRecommended = recommender.Recommend(1, 4);
    for (int i = 0; i < fewRecommended.Count; i++) {
      Assert.AreEqual(fewRecommended[i].GetItemID(), moreRecommended[i].GetItemID());
    }
    recommender.Refresh(null);
    for (int i = 0; i < fewRecommended.Count; i++) {
      Assert.AreEqual(fewRecommended[i].GetItemID(), moreRecommended[i].GetItemID());
    }
  }

  [Test]
  public void testRescorer() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2, 3},
            new Double?[][] {
                    new double?[]{0.1, 0.2},
                    new double?[]{0.2, 0.3, 0.3, 0.6},
                    new double?[]{0.4, 0.5, 0.5, 0.9},
            });
    IUserSimilarity similarity = new PearsonCorrelationSimilarity(dataModel);
    IUserNeighborhood neighborhood = new NearestNUserNeighborhood(2, similarity, dataModel);
    IRecommender recommender = new GenericUserBasedRecommender(dataModel, neighborhood, similarity);
    IList<IRecommendedItem> originalRecommended = recommender.Recommend(1, 2);
    IList<IRecommendedItem> rescoredRecommended =
        recommender.Recommend(1, 2, new ReversingRescorer<long>());
    Assert.NotNull(originalRecommended);
    Assert.NotNull(rescoredRecommended);
    Assert.AreEqual(2, originalRecommended.Count);
    Assert.AreEqual(2, rescoredRecommended.Count);
    Assert.AreEqual(originalRecommended[0].GetItemID(), rescoredRecommended[1].GetItemID());
    Assert.AreEqual(originalRecommended[1].GetItemID(), rescoredRecommended[0].GetItemID());
  }

  [Test]
  public void testEstimatePref() {
    IRecommender recommender = buildRecommender();
    Assert.AreEqual(0.1f, recommender.EstimatePreference(1, 2), EPSILON);
  }

  [Test]
  public void testBestRating() {
    IRecommender recommender = buildRecommender();
    IList<IRecommendedItem> recommended = recommender.Recommend(1, 1);
    Assert.NotNull(recommended);
    Assert.AreEqual(1, recommended.Count);
    IRecommendedItem firstRecommended = recommended[0];
    // item one should be recommended because it has a greater rating/score
    Assert.AreEqual(2, firstRecommended.GetItemID());
    Assert.AreEqual(0.1f, firstRecommended.GetValue(), EPSILON);
  }

  [Test]
  public void testMostSimilar() {
    IUserBasedRecommender recommender = buildRecommender();
    long[] similar = recommender.MostSimilarUserIDs(1, 2);
    Assert.NotNull(similar);
    Assert.AreEqual(2, similar.Length);
    Assert.AreEqual(2, similar[0]);
    Assert.AreEqual(3, similar[1]);
  }

  [Test]
  public void testIsolatedUser() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2, 3, 4},
            new Double?[][] {
                    new double?[]{0.1, 0.2},
                    new double?[]{0.2, 0.3, 0.3, 0.6},
                    new double?[]{0.4, 0.4, 0.5, 0.9},
                    new double?[]{null, null, null, null, 1.0},
            });
    IUserSimilarity similarity = new PearsonCorrelationSimilarity(dataModel);
    IUserNeighborhood neighborhood = new NearestNUserNeighborhood(3, similarity, dataModel);
    IUserBasedRecommender recommender = new GenericUserBasedRecommender(dataModel, neighborhood, similarity);
    long[] mostSimilar = recommender.MostSimilarUserIDs(4, 3);
    Assert.NotNull(mostSimilar);
    Assert.AreEqual(0, mostSimilar.Length);
  }

  private static IUserBasedRecommender buildRecommender() {
    IDataModel dataModel = getDataModel();
    IUserSimilarity similarity = new PearsonCorrelationSimilarity(dataModel);
    IUserNeighborhood neighborhood = new NearestNUserNeighborhood(2, similarity, dataModel);
    return new GenericUserBasedRecommender(dataModel, neighborhood, similarity);
  }

}

}