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
using NReco.CF.Taste.Recommender;

namespace NReco.CF.Taste.Similarity.Precompute {

/// <summary>
/// Compact representation of all similar items for an item
/// </summary>
public class SimilarItems {

  private long itemID;
  private long[] similarItemIDs;
  private double[] similarities;

  public SimilarItems(long itemID, List<IRecommendedItem> similarItems) {
    this.itemID = itemID;

    int numSimilarItems = similarItems.Count;
    similarItemIDs = new long[numSimilarItems];
    similarities = new double[numSimilarItems];

    for (int n = 0; n < numSimilarItems; n++) {
      similarItemIDs[n] = similarItems[n].GetItemID();
      similarities[n] = similarItems[n].GetValue();
    }
  }

  public long getItemID() {
    return itemID;
  }

  public int numSimilarItems() {
    return similarItemIDs.Length;
  }

  public IEnumerable<SimilarItem> getSimilarItems() {
	  for (int index=0; index < similarItemIDs.Length; index++)
		  yield return new SimilarItem(similarItemIDs[index], similarities[index]);
  }

}

}