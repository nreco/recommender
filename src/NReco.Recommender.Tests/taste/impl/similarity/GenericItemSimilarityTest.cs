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
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Similarity;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Similarity {


/// <p>Tests {@link GenericItemSimilarity}.</p> 
public sealed class GenericItemSimilarityTest : SimilarityTestCase {

  [Test]
  public void testSimple() {
    List<GenericItemSimilarity.ItemItemSimilarity> similarities = new List<GenericItemSimilarity.ItemItemSimilarity>();
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(1, 2, 0.5));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(2, 1, 0.6));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(1, 1, 0.5));
    similarities.Add(new GenericItemSimilarity.ItemItemSimilarity(1, 3, 0.3));
    GenericItemSimilarity itemCorrelation = new GenericItemSimilarity(similarities);
    Assert.AreEqual(1.0, itemCorrelation.ItemSimilarity(1, 1), EPSILON);
    Assert.AreEqual(0.6, itemCorrelation.ItemSimilarity(1, 2), EPSILON);
    Assert.AreEqual(0.6, itemCorrelation.ItemSimilarity(2, 1), EPSILON);
    Assert.AreEqual(0.3, itemCorrelation.ItemSimilarity(1, 3), EPSILON);
    Assert.True(Double.IsNaN(itemCorrelation.ItemSimilarity(3, 4)));
  }

  [Test]
  public void testFromCorrelation() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2, 3},
            new Double?[][] {
                   new double?[] {1.0, 2.0},
                   new double?[] {2.0, 5.0},
                   new double?[] {3.0, 6.0},
            });
    IItemSimilarity otherSimilarity = new PearsonCorrelationSimilarity(dataModel);
    IItemSimilarity itemSimilarity = new GenericItemSimilarity(otherSimilarity, dataModel);
    assertCorrelationEquals(1.0, itemSimilarity.ItemSimilarity(0, 0));
    assertCorrelationEquals(0.960768922830523, itemSimilarity.ItemSimilarity(0, 1));
  }

  [Test]
  public void testAllSimilaritiesWithoutIndex() {

    List<GenericItemSimilarity.ItemItemSimilarity> itemItemSimilarities =
        new List<GenericItemSimilarity.ItemItemSimilarity>() {new GenericItemSimilarity.ItemItemSimilarity(1L, 2L, 0.2),
                      new GenericItemSimilarity.ItemItemSimilarity(1L, 3L, 0.2),
                      new GenericItemSimilarity.ItemItemSimilarity(2L, 1L, 0.2),
                      new GenericItemSimilarity.ItemItemSimilarity(3L, 5L, 0.2),
                      new GenericItemSimilarity.ItemItemSimilarity(3L, 4L, 0.2)};

    IItemSimilarity similarity = new GenericItemSimilarity(itemItemSimilarities);

    Assert.True(containsExactly(similarity.AllSimilarItemIDs(1L), 2L, 3L));
    Assert.True(containsExactly(similarity.AllSimilarItemIDs(2L), 1L));
    Assert.True(containsExactly(similarity.AllSimilarItemIDs(3L), 1L, 5L, 4L));
    Assert.True(containsExactly(similarity.AllSimilarItemIDs(4L), 3L));
    Assert.True(containsExactly(similarity.AllSimilarItemIDs(5L), 3L));
  }

  [Test]
  public void testAllSimilaritiesWithIndex() {

    List<GenericItemSimilarity.ItemItemSimilarity> itemItemSimilarities =
		new List<GenericItemSimilarity.ItemItemSimilarity>(){new GenericItemSimilarity.ItemItemSimilarity(1L, 2L, 0.2),
                      new GenericItemSimilarity.ItemItemSimilarity(1L, 3L, 0.2),
                      new GenericItemSimilarity.ItemItemSimilarity(2L, 1L, 0.2),
                      new GenericItemSimilarity.ItemItemSimilarity(3L, 5L, 0.2),
                      new GenericItemSimilarity.ItemItemSimilarity(3L, 4L, 0.2)};

    IItemSimilarity similarity = new GenericItemSimilarity(itemItemSimilarities);

    Assert.True(containsExactly(similarity.AllSimilarItemIDs(1L), 2L, 3L));
    Assert.True(containsExactly(similarity.AllSimilarItemIDs(2L), 1L));
    Assert.True(containsExactly(similarity.AllSimilarItemIDs(3L), 1L, 5L, 4L));
    Assert.True(containsExactly(similarity.AllSimilarItemIDs(4L), 3L));
    Assert.True(containsExactly(similarity.AllSimilarItemIDs(5L), 3L));
  }

  private static bool containsExactly(long[] allIDs, params long[] shouldContainID) {
    return new FastIDSet(allIDs).IntersectionSize(new FastIDSet(shouldContainID)) == shouldContainID.Length;
  }

}

}