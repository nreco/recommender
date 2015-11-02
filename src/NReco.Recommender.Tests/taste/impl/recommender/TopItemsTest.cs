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
using NReco.CF.Taste.Impl.Similarity;
using NReco.CF.Taste.Recommender;
using NReco.CF;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Recommender {


 /// Tests for {@link TopItems}.
public sealed class TopItemsTest : TasteTestCase {

  [Test]
  public void testTopItems() {
    long[] ids = new long[100];
    for (int i = 0; i < 100; i++) {
      ids[i] = i;
    }
    var possibleItemIds = ((IEnumerable<long>) ids).GetEnumerator();
    TopItems.IEstimator<long> estimator = new TestTopItemsEstimator();
    List<IRecommendedItem> topItems = TopItems.GetTopItems(10, possibleItemIds, null, estimator);
	int gold = 99;
    foreach (IRecommendedItem topItem in topItems) {
      Assert.AreEqual(gold, topItem.GetItemID());
      Assert.AreEqual(gold--, topItem.GetValue(), 0.01);
    }
  }

  public class TestTopItemsEstimator : TopItems.IEstimator<long> {
      public double Estimate(long thing) {
        return thing;
      }
 }


  [Test]
  public void testTopItemsRandom() {
    long[] ids = new long[100];
    for (int i = 0; i < 100; i++) {
      ids[i] = i;
    }
    var possibleItemIds = ((IEnumerable<long>) ids).GetEnumerator();
   
    TopItems.IEstimator<long> estimator = new TestRndTopItemsEstimator();
    List<IRecommendedItem> topItems = TopItems.GetTopItems(10, possibleItemIds, null, estimator);
    Assert.AreEqual(10, topItems.Count);
    double last = 2.0;
    foreach (IRecommendedItem topItem in topItems) {
      Assert.True(topItem.GetValue() <= last);
      last = topItem.GetItemID();
    }
  }

   public class TestRndTopItemsEstimator : TopItems.IEstimator<long> {
	    RandomWrapper random = RandomUtils.getRandom();
      public double Estimate(long thing) {
         return random.nextDouble();
      }
 } 


  [Test]
  public void testTopUsers() {
    long[] ids = new long[100];
    for (int i = 0; i < 100; i++) {
      ids[i] = i;
    }
    var possibleItemIds = ((IEnumerable<long>) ids).GetEnumerator();
    TopItems.IEstimator<long> estimator = new TestTopItemsEstimator();

    long[] topItems = TopItems.GetTopUsers(10, possibleItemIds, null, estimator);
    int gold = 99;
    foreach (long topItem in topItems) {
      Assert.AreEqual(gold--, topItem);
    }
  }

  [Test]
  public void testTopItemItem() {
    List<GenericItemSimilarity.ItemItemSimilarity> sims = new List<GenericItemSimilarity.ItemItemSimilarity>();
    for (int i = 0; i < 99; i++) {
      sims.Add(new GenericItemSimilarity.ItemItemSimilarity(i, i + 1, i / 99.0));
    }

    List<GenericItemSimilarity.ItemItemSimilarity> res = TopItems.GetTopItemItemSimilarities(10, sims.GetEnumerator());
    int gold = 99;
    foreach (GenericItemSimilarity.ItemItemSimilarity re in res) {
      Assert.AreEqual(gold--, re.getItemID2()); //the second id should be equal to 99 to start
    }
  }

  [Test]
  public void testTopItemItemAlt() {
    List<GenericItemSimilarity.ItemItemSimilarity> sims = new List<GenericItemSimilarity.ItemItemSimilarity>();
    for (int i = 0; i < 99; i++) {
      sims.Add(new GenericItemSimilarity.ItemItemSimilarity(i, i + 1, 1 - (i / 99.0)));
    }

    List<GenericItemSimilarity.ItemItemSimilarity> res = TopItems.GetTopItemItemSimilarities(10, sims.GetEnumerator());
    int gold = 0;
    foreach (GenericItemSimilarity.ItemItemSimilarity re in res) {
      Assert.AreEqual(gold++, re.getItemID1()); //the second id should be equal to 99 to start
    }
  }

  [Test]
  public void testTopUserUser() {
    List<GenericUserSimilarity.UserUserSimilarity> sims = new List<GenericUserSimilarity.UserUserSimilarity>();
    for (int i = 0; i < 99; i++) {
      sims.Add(new GenericUserSimilarity.UserUserSimilarity(i, i + 1, i / 99.0));
    }

    List<GenericUserSimilarity.UserUserSimilarity> res = TopItems.GetTopUserUserSimilarities(10, sims.GetEnumerator());
    int gold = 99;
    foreach (GenericUserSimilarity.UserUserSimilarity re in res) {
      Assert.AreEqual(gold--, re.getUserID2()); //the second id should be equal to 99 to start
    }
  }

  [Test]
  public void testTopUserUserAlt() {
    List<GenericUserSimilarity.UserUserSimilarity> sims = new List<GenericUserSimilarity.UserUserSimilarity>();
    for (int i = 0; i < 99; i++) {
      sims.Add(new GenericUserSimilarity.UserUserSimilarity(i, i + 1, 1 - (i / 99.0)));
    }

    List<GenericUserSimilarity.UserUserSimilarity> res = TopItems.GetTopUserUserSimilarities(10, sims.GetEnumerator());
    int gold = 0;
    foreach (GenericUserSimilarity.UserUserSimilarity re in res) {
      Assert.AreEqual(gold++, re.getUserID1()); //the first id should be equal to 0 to start
    }
  }

}

}