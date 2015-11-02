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

/// <summary>
/// Defines ordering on <see cref="IRecommendedItem"/> by the rescored value of the recommendations' estimated
/// preference value, from high to low.
/// </summary>
public sealed class ByRescoreComparator : IComparer<IRecommendedItem> {
  
  private IDRescorer rescorer;
  
  public ByRescoreComparator(IDRescorer rescorer) {
    this.rescorer = rescorer;
  }
  
  public int Compare(IRecommendedItem o1, IRecommendedItem o2) {
    double rescored1;
    double rescored2;
    if (rescorer == null) {
      rescored1 = o1.GetValue();
      rescored2 = o2.GetValue();
    } else {
      rescored1 = rescorer.rescore(o1.GetItemID(), o1.GetValue());
      rescored2 = rescorer.rescore(o2.GetItemID(), o2.GetValue());
    }
    if (rescored1 < rescored2) {
      return 1;
    } else if (rescored1 > rescored2) {
      return -1;
    } else {
      return 0;
    }
  }
  
  public override string ToString() {
    return "ByRescoreComparator[rescorer:" + rescorer + ']';
  }
  
}

}