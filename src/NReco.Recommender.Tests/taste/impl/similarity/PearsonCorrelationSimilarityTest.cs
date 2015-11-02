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
using NReco.CF.Taste.Similarity;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Similarity {

/// <p>Tests {@link PearsonCorrelationSimilarity}.</p> 
public sealed class PearsonCorrelationSimilarityTest : SimilarityTestCase {

  [Test]
  public void testFullCorrelation1() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{3.0, -2.0},
                    new double?[]{3.0, -2.0},
            });
    double correlation = new PearsonCorrelationSimilarity(dataModel).UserSimilarity(1, 2);
    assertCorrelationEquals(1.0, correlation);
  }

  [Test]
  public void testFullCorrelation1Weighted() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{3.0, -2.0},
                    new double?[]{3.0, -2.0},
            });
    double correlation = new PearsonCorrelationSimilarity(dataModel, Weighting.WEIGHTED).UserSimilarity(1, 2);
    assertCorrelationEquals(1.0, correlation);
  }

  [Test]
  public void testFullCorrelation2() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{3.0, 3.0},
                    new double?[]{3.0, 3.0},
            });
    double correlation = new PearsonCorrelationSimilarity(dataModel).UserSimilarity(1, 2);
    // Yeah, undefined in this case
    Assert.True(Double.IsNaN(correlation));
  }

  [Test]
  public void testNoCorrelation1() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{3.0, -2.0},
                    new double?[]{-3.0, 2.0},
            });
    double correlation = new PearsonCorrelationSimilarity(dataModel).UserSimilarity(1, 2);
    assertCorrelationEquals(-1.0, correlation);
  }

  [Test]
  public void testNoCorrelation1Weighted() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{3.0, -2.0},
                    new double?[]{-3.0, 2.0},
            });
    double correlation = new PearsonCorrelationSimilarity(dataModel, Weighting.WEIGHTED).UserSimilarity(1, 2);
    assertCorrelationEquals(-1.0, correlation);
  }

  [Test]
  public void testNoCorrelation2() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{null, 1.0, null},
                    new double?[]{null, null, 1.0},
            });
    double correlation = new PearsonCorrelationSimilarity(dataModel).UserSimilarity(1, 2);
    Assert.True(Double.IsNaN(correlation));
  }

  [Test]
  public void testNoCorrelation3() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{90.0, 80.0, 70.0},
                    new double?[]{70.0, 80.0, 90.0},
            });
    double correlation = new PearsonCorrelationSimilarity(dataModel).UserSimilarity(1, 2);
    assertCorrelationEquals(-1.0, correlation);
  }

  [Test]
  public void testSimple() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{1.0, 2.0, 3.0},
                    new double?[]{2.0, 5.0, 6.0},
            });
    double correlation = new PearsonCorrelationSimilarity(dataModel).UserSimilarity(1, 2);
    assertCorrelationEquals(0.9607689228305227, correlation);
  }

  [Test]
  public void testSimpleWeighted() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{1.0, 2.0, 3.0},
                    new double?[]{2.0, 5.0, 6.0},
            });
    double correlation = new PearsonCorrelationSimilarity(dataModel, Weighting.WEIGHTED).UserSimilarity(1, 2);
    assertCorrelationEquals(0.9901922307076306, correlation);
  }

  [Test]
  public void testFullItemCorrelation1() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{3.0, 3.0},
                    new double?[]{-2.0, -2.0},
            });
    double correlation =
        new PearsonCorrelationSimilarity(dataModel).ItemSimilarity(0, 1);
    assertCorrelationEquals(1.0, correlation);
  }

  [Test]
  public void testFullItemCorrelation2() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{3.0, 3.0},
                    new double?[]{3.0, 3.0},
            });
    double correlation =
        new PearsonCorrelationSimilarity(dataModel).ItemSimilarity(0, 1);
    // Yeah, undefined in this case
    Assert.True(Double.IsNaN(correlation));
  }

  [Test]
  public void testNoItemCorrelation1() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{3.0, -3.0},
                    new double?[]{2.0, -2.0},
            });
    double correlation =
        new PearsonCorrelationSimilarity(dataModel).ItemSimilarity(0, 1);
    assertCorrelationEquals(-1.0, correlation);
  }

  [Test]
  public void testNoItemCorrelation2() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2},
            new Double?[][] {
                    new double?[]{null, 1.0, null},
                    new double?[]{null, null, 1.0},
            });
    double correlation =
        new PearsonCorrelationSimilarity(dataModel).ItemSimilarity(1, 2);
    Assert.True(Double.IsNaN(correlation));
  }

  [Test]
  public void testNoItemCorrelation3() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2, 3},
            new Double?[][] {
                    new double?[]{90.0, 70.0},
                    new double?[]{80.0, 80.0},
                    new double?[]{70.0, 90.0},
            });
    double correlation =
        new PearsonCorrelationSimilarity(dataModel).ItemSimilarity(0, 1);
    assertCorrelationEquals(-1.0, correlation);
  }

  [Test]
  public void testSimpleItem() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2, 3},
            new Double?[][] {
                    new double?[]{1.0, 2.0},
                    new double?[]{2.0, 5.0},
                    new double?[]{3.0, 6.0},
            });
    double correlation =
        new PearsonCorrelationSimilarity(dataModel).ItemSimilarity(0, 1);
    assertCorrelationEquals(0.9607689228305227, correlation);
  }

  [Test]
  public void testSimpleItemWeighted() {
    IDataModel dataModel = getDataModel(
            new long[] {1, 2, 3},
            new Double?[][] {
                    new double?[]{1.0, 2.0},
                    new double?[]{2.0, 5.0},
                    new double?[]{3.0, 6.0},
            });
    IItemSimilarity itemSimilarity = new PearsonCorrelationSimilarity(dataModel, Weighting.WEIGHTED);
    double correlation = itemSimilarity.ItemSimilarity(0, 1);
    assertCorrelationEquals(0.9901922307076306, correlation);
  }

  [Test]
  public void testRefresh() {
    // Make sure this doesn't throw an exception
    new PearsonCorrelationSimilarity(getDataModel()).Refresh(null);
  }

  [Test]
  public void testInferrer() {
    IDataModel dataModel = getDataModel(
      new long[] {1, 2},
      new Double?[][] {
              new double?[]{null, 1.0, 2.0,  null, null, 6.0},
              new double?[]{1.0, 8.0, null, 3.0,  4.0,  null},
      });
    IUserSimilarity similarity = new PearsonCorrelationSimilarity(dataModel);
	similarity.SetPreferenceInferrer(new TestPreferenceInferrer());

    Assert.AreEqual(-0.435285750066007, similarity.UserSimilarity(1L, 2L), EPSILON);
  }

	public class TestPreferenceInferrer : IPreferenceInferrer {
		public float InferPreference(long userID, long itemID) {
			return 1.0f;
		}
		public void Refresh(IList<IRefreshable> alreadyRefreshed) {
		}
	}

}

}