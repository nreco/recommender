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
using NReco.CF.Taste.Common;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;
using NReco.CF.Taste.Similarity;

namespace NReco.CF.Taste.Impl.Recommender {

/// <summary>
/// A variant on <see cref="GenericItemBasedRecommender"/> which is appropriate for use when no notion of preference
/// value exists in the data.
/// </summary>
/// <seealso cref="NReco.CF.Taste.Impl.Recommender.GenericBooleanPrefUserBasedRecommender"/>
public sealed class GenericBooleanPrefItemBasedRecommender : GenericItemBasedRecommender {

	public GenericBooleanPrefItemBasedRecommender(IDataModel dataModel, IItemSimilarity similarity)
		: base(dataModel, similarity) {
  }

  public GenericBooleanPrefItemBasedRecommender(IDataModel dataModel, IItemSimilarity similarity,
      ICandidateItemsStrategy candidateItemsStrategy, IMostSimilarItemsCandidateItemsStrategy
	  mostSimilarItemsCandidateItemsStrategy)
		: base(dataModel, similarity, candidateItemsStrategy, mostSimilarItemsCandidateItemsStrategy) {
    
  }
  
   /// This computation is in a technical sense, wrong, since in the domain of "bool preference users" where
   /// all preference values are 1, this method should only ever return 1.0 or NaN. This isn't terribly useful
   /// however since it means results can't be ranked by preference value (all are 1). So instead this returns a
   /// sum of similarities.
  protected override float doEstimatePreference(long userID, IPreferenceArray preferencesFromUser, long itemID)
    {
    double[] similarities = getSimilarity().ItemSimilarities(itemID, preferencesFromUser.GetIDs());
    bool foundAPref = false;
    double totalSimilarity = 0.0;
    foreach (double theSimilarity in similarities) {
      if (!Double.IsNaN(theSimilarity)) {
        foundAPref = true;
        totalSimilarity += theSimilarity;
      }
    }
    return foundAPref ? (float) totalSimilarity : float.NaN;
  }
  
  public override string ToString() {
    return "GenericBooleanPrefItemBasedRecommender";
  }
  
}

}