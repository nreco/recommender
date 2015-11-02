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
using NReco.CF.Taste.Impl.Model;
using NReco.CF.Taste.Impl.Similarity;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;
using NReco.CF.Taste.Similarity;
using NUnit.Mocks;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Recommender {


/// <p>Tests {@link GenericItemBasedRecommender}.</p> 
public sealed class GenericItemBasedRecommenderTest : TasteTestCase {

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
    recommended = recommender.Recommend(1, 1);
    firstRecommended = recommended[0];    
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

    var similarities = new List<GenericItemSimilarity.ItemItemSimilarity>();
    for (int i = 0; i < 6; i++) {
      for (int j = i + 1; j < 6; j++) {
        similarities.Add(
            new GenericItemSimilarity.ItemItemSimilarity(i, j, 1.0 / (1.0 + i + j)));
      }
    }
    IItemSimilarity similarity = new GenericItemSimilarity(similarities);
    IRecommender recommender = new GenericItemBasedRecommender(dataModel, similarity);
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
                    new double?[]{0.4, 0.4, 0.5, 0.9},
            });

    var similarities = new List<GenericItemSimilarity.ItemItemSimilarity>();
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(0, 1, 1.0));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(0, 2, 0.5));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(0, 3, 0.2));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(1, 2, 0.7));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(1, 3, 0.5));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(2, 3, 0.9));
    IItemSimilarity similarity = new GenericItemSimilarity(similarities);
    IRecommender recommender = new GenericItemBasedRecommender(dataModel, similarity);
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

   /// Contributed test case that verifies fix for bug
   /// <a href="http://sourceforge.net/tracker/index.php?func=detail&amp;aid=1396128&amp;group_id=138771&amp;atid=741665">
   /// 1396128</a>.
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
    IItemBasedRecommender recommender = buildRecommender();
    List<IRecommendedItem> similar = recommender.MostSimilarItems(0, 2);
    Assert.NotNull(similar);
    Assert.AreEqual(2, similar.Count);
    IRecommendedItem first = similar[0];
    IRecommendedItem second = similar[1];
    Assert.AreEqual(1, first.GetItemID());
    Assert.AreEqual(1.0f, first.GetValue(), EPSILON);
    Assert.AreEqual(2, second.GetItemID());
    Assert.AreEqual(0.5f, second.GetValue(), EPSILON);
  }

  [Test]
  public void testMostSimilarToMultiple() {
    IItemBasedRecommender recommender = buildRecommender2();
    List<IRecommendedItem> similar = recommender.MostSimilarItems(new long[] {0, 1}, 2);
    Assert.NotNull(similar);
    Assert.AreEqual(2, similar.Count);
    IRecommendedItem first = similar[0];
    IRecommendedItem second = similar[1];
    Assert.AreEqual(2, first.GetItemID());
    Assert.AreEqual(0.85f, first.GetValue(), EPSILON);
    Assert.AreEqual(3, second.GetItemID());
    Assert.AreEqual(-0.3f, second.GetValue(), EPSILON);
  }

  [Test]
  public void testMostSimilarToMultipleExcludeIfNotSimilarToAll() {
    IItemBasedRecommender recommender = buildRecommender2();
    List<IRecommendedItem> similar = recommender.MostSimilarItems(new long[] {3, 4}, 2);
    Assert.NotNull(similar);
    Assert.AreEqual(1, similar.Count);
    IRecommendedItem first = similar[0];
    Assert.AreEqual(0, first.GetItemID());
    Assert.AreEqual(0.2f, first.GetValue(), EPSILON);
  }

  [Test]
  public void testMostSimilarToMultipleDontExcludeIfNotSimilarToAll() {
    IItemBasedRecommender recommender = buildRecommender2();
    List<IRecommendedItem> similar = recommender.MostSimilarItems(new long[] {1, 2, 4}, 10, false);
    Assert.NotNull(similar);
    Assert.AreEqual(2, similar.Count);
    IRecommendedItem first = similar[0];
    IRecommendedItem second = similar[1];
    Assert.AreEqual(0, first.GetItemID());
    Assert.AreEqual(0.933333333f, first.GetValue(), EPSILON);
    Assert.AreEqual(3, second.GetItemID());
    Assert.AreEqual(-0.2f, second.GetValue(), EPSILON);
  }


  [Test]
  public void testRecommendedBecause() {
    IItemBasedRecommender recommender = buildRecommender2();
    List<IRecommendedItem> recommendedBecause = recommender.RecommendedBecause(1, 4, 3);
    Assert.NotNull(recommendedBecause);
    Assert.AreEqual(3, recommendedBecause.Count);
    IRecommendedItem first = recommendedBecause[0];
    IRecommendedItem second = recommendedBecause[1];
    IRecommendedItem third = recommendedBecause[2];
    Assert.AreEqual(2, first.GetItemID());
    Assert.AreEqual(0.99f, first.GetValue(), EPSILON);
    Assert.AreEqual(3, second.GetItemID());
    Assert.AreEqual(0.4f, second.GetValue(), EPSILON);
    Assert.AreEqual(0, third.GetItemID());
    Assert.AreEqual(0.2f, third.GetValue(), EPSILON);
  }

  private static IItemBasedRecommender buildRecommender() {
    IDataModel dataModel = getDataModel();
    var similarities = new List<GenericItemSimilarity.ItemItemSimilarity>();
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(0, 1, 1.0));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(0, 2, 0.5));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(1, 2, 0.0));
    IItemSimilarity similarity = new GenericItemSimilarity(similarities);
    return new GenericItemBasedRecommender(dataModel, similarity);
  }

  private static IItemBasedRecommender buildRecommender2() {

    IDataModel dataModel = getDataModel(
        new long[] {1, 2, 3, 4},
        new Double?[][] {
                new double?[]{0.1, 0.3, 0.9, 0.8},
                new double?[]{0.2, 0.3, 0.3, 0.4},
                new double?[]{0.4, 0.3, 0.5, 0.1, 0.1},
                new double?[]{0.7, 0.3, 0.8, 0.5, 0.6},
        });

    var similarities = new List<GenericItemSimilarity.ItemItemSimilarity>();
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(0, 1, 1.0));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(0, 2, 0.8));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(0, 3, -0.6));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(0, 4, 1.0));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(1, 2, 0.9));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(1, 3, 0.0));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(1, 1, 1.0));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(2, 3, -0.1));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(2, 4, 0.1));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(3, 4, -0.5));
    IItemSimilarity similarity = new GenericItemSimilarity(similarities);
    return new GenericItemBasedRecommender(dataModel, similarity);
  }


   /// we're making sure that a user's preferences are fetched only once from the {@link DataModel} for one call to
   /// {@link GenericItemBasedRecommender#recommend(long, int)}
   ///
   /// @throws Exception
  [Test]
  public void preferencesFetchedOnlyOnce() {

    var dataModelMock = new DynamicMock( typeof( IDataModel) );

    var itemSimilarityMock = new DynamicMock( typeof(IItemSimilarity) );
    var candidateItemsStrategyMock = new DynamicMock( typeof (ICandidateItemsStrategy) );
    var mostSimilarItemsCandidateItemsStrategyMock =
        new DynamicMock( typeof(IMostSimilarItemsCandidateItemsStrategy) );

    IPreferenceArray preferencesFromUser = new GenericUserPreferenceArray(
        new List<IPreference>() {new GenericPreference(1L, 1L, 5.0f), new GenericPreference(1L, 2L, 4.0f)});

	dataModelMock.ExpectAndReturn("GetMinPreference", float.NaN);
	dataModelMock.ExpectAndReturn("GetMaxPreference", float.NaN);
	dataModelMock.ExpectAndReturn("GetPreferencesFromUser", preferencesFromUser, 1L);
	var dataModel = (IDataModel)dataModelMock.MockInstance;

	candidateItemsStrategyMock.ExpectAndReturn("GetCandidateItems", new FastIDSet(new long[] { 3L, 4L }),
		1L, preferencesFromUser, dataModel);

	itemSimilarityMock.ExpectAndReturn("ItemSimilarities", new double[] { 0.5, 0.3 },
		3L, preferencesFromUser.GetIDs());
	itemSimilarityMock.ExpectAndReturn("ItemSimilarities", new double[] { 0.4, 0.1 },
		4L, preferencesFromUser.GetIDs());



    //EasyMock.replay(dataModel, itemSimilarity, candidateItemsStrategy, mostSimilarItemsCandidateItemsStrategy);

	IRecommender recommender = new GenericItemBasedRecommender((IDataModel)dataModel,
		(IItemSimilarity)itemSimilarityMock.MockInstance,
        (ICandidateItemsStrategy)candidateItemsStrategyMock.MockInstance, 
		(IMostSimilarItemsCandidateItemsStrategy)mostSimilarItemsCandidateItemsStrategyMock.MockInstance);

    recommender.Recommend(1L, 3);

	dataModelMock.Verify();
	itemSimilarityMock.Verify();
	candidateItemsStrategyMock.Verify();
	mostSimilarItemsCandidateItemsStrategyMock.Verify();
    //EasyMock.verify(dataModel, itemSimilarity, candidateItemsStrategy, mostSimilarItemsCandidateItemsStrategy);
  }
}

}