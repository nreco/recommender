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
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;

namespace NReco.CF.Taste.Impl.Recommender {



public sealed class MockRecommender : IRecommender {

  public int recommendCount;

  public MockRecommender(int recommendCount) {
    this.recommendCount = recommendCount;
  }

  public IList<IRecommendedItem> Recommend(long userID, int howMany) {
	  lock (this) {
		  recommendCount++;
	  }
	return new List<IRecommendedItem>() {
        new GenericRecommendedItem(1, 1.0f) };
  }

  public IList<IRecommendedItem> Recommend(long userID, int howMany, IDRescorer rescorer) {
    return Recommend(userID, howMany);
  }

  public float EstimatePreference(long userID, long itemID) {
	  lock (this) {
		  recommendCount++;
	  }
    return 0.0f;
  }

  public void SetPreference(long userID, long itemID, float value) {
    // do nothing
  }

  public void RemovePreference(long userID, long itemID) {
    // do nothing
  }

  public IDataModel GetDataModel() {
    return TasteTestCase.getDataModel(
            new long[] {1, 2, 3},
            new Double?[][]{
				new double?[]{1.0},
				new double?[]{2.0},
				new double?[]{3.0}
			});
  }

  public void Refresh(IList<IRefreshable> alreadyRefreshed) {
    // do nothing
  }

}

}