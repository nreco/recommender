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

/// <summary>Caches the results from an underlying <see cref="IItemSimilarity"/> implementation.</summary>
public sealed class CachingItemSimilarity : IItemSimilarity {

  private IItemSimilarity similarity;
  private Cache<Tuple<long,long>,Double> similarityCache;
  private RefreshHelper refreshHelper;

   /// Creates this on top of the given {@link ItemSimilarity}.
   /// The cache is sized according to properties of the given {@link DataModel}.
  public CachingItemSimilarity(IItemSimilarity similarity, IDataModel dataModel) : this(similarity, dataModel.GetNumItems()) {
    ;
  }

   /// Creates this on top of the given {@link ItemSimilarity}.
   /// The cache size is capped by the given size.
  public CachingItemSimilarity(IItemSimilarity similarity, int maxCacheSize) {
    //Preconditions.checkArgument(similarity != null, "similarity is null");
    this.similarity = similarity;
    this.similarityCache = new Cache<Tuple<long,long>,Double>(new SimilarityRetriever(similarity), maxCacheSize);
    this.refreshHelper = new RefreshHelper( () => {
        similarityCache.Clear();
    });
    refreshHelper.AddDependency(similarity);
  }
  
  public double ItemSimilarity(long itemID1, long itemID2) {
    Tuple<long,long> key = itemID1 < itemID2 ? new Tuple<long,long>(itemID1, itemID2) : new Tuple<long,long>(itemID2, itemID1);
    return similarityCache.Get(key);
  }

  public double[] ItemSimilarities(long itemID1, long[] itemID2s) {
    int length = itemID2s.Length;
    double[] result = new double[length];
    for (int i = 0; i < length; i++) {
      result[i] = ItemSimilarity(itemID1, itemID2s[i]);
    }
    return result;
  }

  public long[] AllSimilarItemIDs(long itemID) {
    return similarity.AllSimilarItemIDs(itemID);
  }

  public void Refresh(IList<IRefreshable> alreadyRefreshed) {
    refreshHelper.Refresh(alreadyRefreshed);
  }

  public void clearCacheForItem(long itemID) {
    similarityCache.RemoveKeysMatching( new LongPairMatchPredicate(itemID).Matches);
  }

  private sealed class SimilarityRetriever : IRetriever<Tuple<long, long>, Double> {
    private IItemSimilarity similarity;
    
    internal SimilarityRetriever(IItemSimilarity similarity) {
      this.similarity = similarity;
    }

	public Double Get(Tuple<long, long> key) {
      return similarity.ItemSimilarity(key.Item1, key.Item2);
    }
  }

}

}