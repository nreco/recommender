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

namespace NReco.CF.Taste.Impl.Recommender {

/// <summary>Defines a natural ordering from most-preferred item (highest value) to least-preferred.</summary>
public sealed class ByValueRecommendedItemComparator : IComparer<IRecommendedItem> {

	private static IComparer<IRecommendedItem> INSTANCE = new ByValueRecommendedItemComparator();

	public static IComparer<IRecommendedItem> getInstance() {
	 return INSTANCE;
  }

	public static IComparer<IRecommendedItem> getReverseInstance() {
		return new ReverseComparer<IRecommendedItem>( INSTANCE );
	}

  public int Compare(IRecommendedItem o1, IRecommendedItem o2) {
    float value1 = o1.GetValue();
    float value2 = o2.GetValue();
    return value1 > value2 ? -1 : value1 < value2 ? 1 : (o1.GetItemID().CompareTo(o2.GetItemID())); // SortedSet uses IComparer to find identical elements
  }

  internal class ReverseComparer<T> : IComparer<T> {
		IComparer<T> comparer;
		internal ReverseComparer(IComparer<T> comparer) {
			this.comparer = comparer;
		}
	  public int Compare(T obj1, T obj2) {
		  return -comparer.Compare(obj1, obj2);
	  }
  }

}




}