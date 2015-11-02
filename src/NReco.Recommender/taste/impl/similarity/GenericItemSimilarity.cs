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
using NReco.CF.Taste.Impl.Recommender;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Similarity;
using NReco.CF;


namespace NReco.CF.Taste.Impl.Similarity {

 /// <summary>
 /// A "generic" <see cref="IItemSimilarity"/> which takes a static list of precomputed item similarities and bases its
 /// responses on that alone. The values may have been precomputed offline by another process, stored in a file,
 /// and then read and fed into an instance of this class.
 /// </summary>
 /// <remarks>
 /// This is perhaps the best <see cref="IItemSimilarity"/> to use with
 /// <see cref="NReco.CF.Taste.Impl.Recommender.GenericItemBasedRecommender"/>, for now, since the point
 /// of item-based recommenders is that they can take advantage of the fact that item similarity is relatively
 /// static, can be precomputed, and then used in computation to gain a significant performance advantage.
 /// </remarks>
public sealed class GenericItemSimilarity : IItemSimilarity {

  private static long[] NO_IDS = new long[0];
  
  private FastByIDMap<FastByIDMap<double?>> similarityMaps = new FastByIDMap<FastByIDMap<double?>>();
  private FastByIDMap<FastIDSet> similarItemIDsIndex = new FastByIDMap<FastIDSet>();

   /// <p>
   /// Creates a {@link GenericItemSimilarity} from a precomputed list of {@link ItemItemSimilarity}s. Each
   /// represents the similarity between two distinct items. Since similarity is assumed to be symmetric, it is
   /// not necessary to specify similarity between item1 and item2, and item2 and item1. Both are the same. It
   /// is also not necessary to specify a similarity between any item and itself; these are assumed to be 1.0.
   /// </p>
   ///
   /// <p>
   /// Note that specifying a similarity between two items twice is not an error, but, the later value will win.
   /// </p>
   ///
   /// @param similarities
   ///          set of {@link ItemItemSimilarity}s on which to base this instance
  public GenericItemSimilarity(IEnumerable<ItemItemSimilarity> similarities) {
    initSimilarityMaps(similarities.GetEnumerator());
  }

   /// <p>
   /// Like {@link #GenericItemSimilarity(Iterable)}, but will only keep the specified number of similarities
   /// from the given {@link Iterable} of similarities. It will keep those with the highest similarity -- those
   /// that are therefore most important.
   /// </p>
   /// 
   /// <p>
   /// Thanks to tsmorton for suggesting this and providing part of the implementation.
   /// </p>
   /// 
   /// @param similarities
   ///          set of {@link ItemItemSimilarity}s on which to base this instance
   /// @param maxToKeep
   ///          maximum number of similarities to keep
  public GenericItemSimilarity(IEnumerable<ItemItemSimilarity> similarities, int maxToKeep) {
    var keptSimilarities =
        TopItems.GetTopItemItemSimilarities(maxToKeep, similarities.GetEnumerator());
    initSimilarityMaps(keptSimilarities.GetEnumerator());
  }

   /// <p>
   /// Builds a list of item-item similarities given an {@link ItemSimilarity} implementation and a
   /// {@link DataModel}, rather than a list of {@link ItemItemSimilarity}s.
   /// </p>
   /// 
   /// <p>
   /// It's valid to build a {@link GenericItemSimilarity} this way, but perhaps missing some of the point of an
   /// item-based recommender. Item-based recommenders use the assumption that item-item similarities are
   /// relatively fixed, and might be known already independent of user preferences. Hence it is useful to
   /// inject that information, using {@link #GenericItemSimilarity(Iterable)}.
   /// </p>
   /// 
   /// @param otherSimilarity
   ///          other {@link ItemSimilarity} to get similarities from
   /// @param dataModel
   ///          data model to get items from
   /// @throws TasteException
   ///           if an error occurs while accessing the {@link DataModel} items
  public GenericItemSimilarity(IItemSimilarity otherSimilarity, IDataModel dataModel) {
    long[] itemIDs = GenericUserSimilarity.longIteratorToList(dataModel.GetItemIDs());
    initSimilarityMaps(new DataModelSimilaritiesIterator(otherSimilarity, itemIDs));
  }

   /// <p>
   /// Like {@link #GenericItemSimilarity(ItemSimilarity, DataModel)} )}, but will only keep the specified
   /// number of similarities from the given {@link DataModel}. It will keep those with the highest similarity
   /// -- those that are therefore most important.
   /// </p>
   /// 
   /// <p>
   /// Thanks to tsmorton for suggesting this and providing part of the implementation.
   /// </p>
   /// 
   /// @param otherSimilarity
   ///          other {@link ItemSimilarity} to get similarities from
   /// @param dataModel
   ///          data model to get items from
   /// @param maxToKeep
   ///          maximum number of similarities to keep
   /// @throws TasteException
   ///           if an error occurs while accessing the {@link DataModel} items
  public GenericItemSimilarity(IItemSimilarity otherSimilarity,
                               IDataModel dataModel,
                               int maxToKeep) {
    long[] itemIDs = GenericUserSimilarity.longIteratorToList(dataModel.GetItemIDs());
    var it = new DataModelSimilaritiesIterator(otherSimilarity, itemIDs);
    var keptSimilarities = TopItems.GetTopItemItemSimilarities(maxToKeep, it);
    initSimilarityMaps(keptSimilarities.GetEnumerator() );
  }

  private void initSimilarityMaps(IEnumerator<ItemItemSimilarity> similarities) {
    while (similarities.MoveNext()) {
      ItemItemSimilarity iic = similarities.Current;
      long similarityItemID1 = iic.getItemID1();
      long similarityItemID2 = iic.getItemID2();
      if (similarityItemID1 != similarityItemID2) {
        // Order them -- first key should be the "smaller" one
        long itemID1;
        long itemID2;
        if (similarityItemID1 < similarityItemID2) {
          itemID1 = similarityItemID1;
          itemID2 = similarityItemID2;
        } else {
          itemID1 = similarityItemID2;
          itemID2 = similarityItemID1;
        }
        FastByIDMap<double?> map = similarityMaps.Get(itemID1);
        if (map == null) {
          map = new FastByIDMap<double?>();
          similarityMaps.Put(itemID1, map);
        }
        map.Put(itemID2, iic.getValue());

        doIndex(itemID1, itemID2);
        doIndex(itemID2, itemID1);
      }
      // else similarity between item and itself already assumed to be 1.0
    }
  }

  private void doIndex(long fromItemID, long toItemID) {
    FastIDSet similarItemIDs = similarItemIDsIndex.Get(fromItemID);
    if (similarItemIDs == null) {
      similarItemIDs = new FastIDSet();
      similarItemIDsIndex.Put(fromItemID, similarItemIDs);
    }
    similarItemIDs.Add(toItemID);
  }

   /// <p>
   /// Returns the similarity between two items. Note that similarity is assumed to be symmetric, that
   /// {@code itemSimilarity(item1, item2) == itemSimilarity(item2, item1)}, and that
   /// {@code itemSimilarity(item1,item1) == 1.0} for all items.
   /// </p>
   ///
   /// @param itemID1
   ///          first item
   /// @param itemID2
   ///          second item
   /// @return similarity between the two
  public double ItemSimilarity(long itemID1, long itemID2) {
    if (itemID1 == itemID2) {
      return 1.0;
    }
    long firstID;
    long secondID;
    if (itemID1 < itemID2) {
      firstID = itemID1;
      secondID = itemID2;
    } else {
      firstID = itemID2;
      secondID = itemID1;
    }
    FastByIDMap<double?> nextMap = similarityMaps.Get(firstID);
    if (nextMap == null) {
      return Double.NaN;
    }
    double? similarity = nextMap.Get(secondID);
    return !similarity.HasValue ? Double.NaN : similarity.Value;
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
    FastIDSet similarItemIDs = similarItemIDsIndex.Get(itemID);
    return similarItemIDs != null ? similarItemIDs.ToArray() : NO_IDS;
  }
  
  public void Refresh(IList<IRefreshable> alreadyRefreshed) {
  // Do nothing
  }
  
  /// Encapsulates a similarity between two items. Similarity must be in the range [-1.0,1.0]. 
  public class ItemItemSimilarity : IComparable<ItemItemSimilarity> {
    
    private long itemID1;
    private long itemID2;
    private double value;
    
     /// @param itemID1
     ///          first item
     /// @param itemID2
     ///          second item
     /// @param value
     ///          similarity between the two
     /// @throws IllegalArgumentException
     ///           if value is NaN, less than -1.0 or greater than 1.0
    public ItemItemSimilarity(long itemID1, long itemID2, double value) {
      //Preconditions.checkArgument(value >= -1.0 && value <= 1.0, "Illegal value: " + value + ". Must be: -1.0 <= value <= 1.0");
      this.itemID1 = itemID1;
      this.itemID2 = itemID2;
      this.value = value;
    }
    
    public long getItemID1() {
      return itemID1;
    }
    
    public long getItemID2() {
      return itemID2;
    }
    
    public double getValue() {
      return value;
    }
    
    public override string ToString() {
      return "ItemItemSimilarity[" + itemID1 + ',' + itemID2 + ':' + value + ']';
    }
    
    /// Defines an ordering from highest similarity to lowest. 
    public int CompareTo(ItemItemSimilarity other) {
      double otherValue = other.getValue();
      return value > otherValue ? -1 : value < otherValue ? 1 : 0;
    }
    
    public override bool Equals(Object other) {
      if (!(other is ItemItemSimilarity)) {
        return false;
      }
      ItemItemSimilarity otherSimilarity = (ItemItemSimilarity) other;
      return otherSimilarity.getItemID1() == itemID1
          && otherSimilarity.getItemID2() == itemID2
          && otherSimilarity.getValue() == value;
    }
    
    public override int GetHashCode() {
      return (int) itemID1 ^ (int) itemID2 ^ RandomUtils.hashDouble(value);
    }
    
  }
  
  private sealed class DataModelSimilaritiesIterator : IEnumerator<ItemItemSimilarity> {
    
    private IItemSimilarity otherSimilarity;
    private long[] itemIDs;
    private int i;
    private long itemID1;
    private int j;

    internal DataModelSimilaritiesIterator(IItemSimilarity otherSimilarity, long[] itemIDs) {
      this.otherSimilarity = otherSimilarity;
      this.itemIDs = itemIDs;
      i = 0;
      itemID1 = itemIDs[0];
      j = 1;
    }

    protected ItemItemSimilarity computeNext() {
      int size = itemIDs.Length;
      ItemItemSimilarity result = null;
      while (result == null && i < size - 1) {
        long itemID2 = itemIDs[j];
        double similarity;
        try {
          similarity = otherSimilarity.ItemSimilarity(itemID1, itemID2);
        } catch (TasteException te) {
          // ugly:
          throw new InvalidOperationException(te.Message, te);
        }
        if (!Double.IsNaN(similarity)) {
          result = new ItemItemSimilarity(itemID1, itemID2, similarity);
        }
        if (++j == size) {
          itemID1 = itemIDs[++i];
          j = i + 1;
        }
      }
      return result;
    }

	ItemItemSimilarity _Current;

	public ItemItemSimilarity Current {
		get {
			if (_Current == null)
				throw new InvalidOperationException();
			return _Current;
		}
	}

	public void Dispose() {
		
	}

	object IEnumerator.Current {
		get { return Current; }
	}

	public bool MoveNext() {
		_Current = computeNext();
		return _Current != null;
	}

	public void Reset() {
		_Current = null;
		i = 0;
		j = 1;
	}
  }
  
}

}