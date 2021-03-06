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


public abstract class BatchItemSimilarities {

  private IItemBasedRecommender recommender;
  private int similarItemsPerItem;

   /// @param recommender recommender to use
   /// @param similarItemsPerItem number of similar items to compute per item
  protected BatchItemSimilarities(IItemBasedRecommender recommender, int similarItemsPerItem) {
    this.recommender = recommender;
    this.similarItemsPerItem = similarItemsPerItem;
  }

  protected IItemBasedRecommender getRecommender() {
    return recommender;
  }

  protected int getSimilarItemsPerItem() {
    return similarItemsPerItem;
  }

   /// @param degreeOfParallelism number of threads to use for the computation
   /// @param maxDurationInHours  maximum duration of the computation
   /// @param writer  {@link SimilarItemsWriter} used to persist the results
   /// @return  the number of similarities precomputed
   /// @throws IOException
   /// @throws RuntimeException if the computation takes longer than maxDurationInHours
  public abstract int computeItemSimilarities(int degreeOfParallelism, int maxDurationInHours,
      ISimilarItemsWriter writer) ;
}

}