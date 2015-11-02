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
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Similarity;
using NReco.CF;


namespace NReco.CF.Taste.Impl.Similarity {

/// <summary>Caches the results from an underlying <see cref="IUserSimilarity"/> implementation.</summary>
public sealed class CachingUserSimilarity : IUserSimilarity {
  
  private IUserSimilarity similarity;
  private Cache<Tuple<long,long>,Double> similarityCache;
  private RefreshHelper refreshHelper;

   /// Creates this on top of the given {@link UserSimilarity}.
   /// The cache is sized according to properties of the given {@link DataModel}.
  public CachingUserSimilarity(IUserSimilarity similarity, IDataModel dataModel) : this(similarity, dataModel.GetNumUsers()) {
  }

   /// Creates this on top of the given {@link UserSimilarity}.
   /// The cache size is capped by the given size.
  public CachingUserSimilarity(IUserSimilarity similarity, int maxCacheSize) {
    //Preconditions.checkArgument(similarity != null, "similarity is null");
    this.similarity = similarity;
	this.similarityCache = new Cache<Tuple<long, long>, Double>(new SimilarityRetriever(similarity), maxCacheSize);
    this.refreshHelper = new RefreshHelper( () => {
        similarityCache.Clear();
    });
    refreshHelper.AddDependency(similarity);
  }
  
  public double UserSimilarity(long userID1, long userID2) {
	  Tuple<long, long> key = userID1 < userID2 ? new Tuple<long, long>(userID1, userID2) : new Tuple<long, long>(userID2, userID1);
    return similarityCache.Get(key);
  }
  
  public void SetPreferenceInferrer(IPreferenceInferrer inferrer) {
    similarityCache.Clear();
    similarity.SetPreferenceInferrer(inferrer);
  }
 

  public void clearCacheForUser(long userID) {
    similarityCache.RemoveKeysMatching(new LongPairMatchPredicate(userID).Matches);
  }
  
  public void Refresh(IList<IRefreshable> alreadyRefreshed) {
    refreshHelper.Refresh(alreadyRefreshed);
  }

  private sealed class SimilarityRetriever : IRetriever<Tuple<long, long>, Double> {
    private IUserSimilarity similarity;
    
    internal SimilarityRetriever(IUserSimilarity similarity) {
      this.similarity = similarity;
    }

	public Double Get(Tuple<long, long> key) {
      return similarity.UserSimilarity(key.Item1, key.Item2);
    }
  }
  
}

}