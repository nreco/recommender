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
using NReco.CF;


namespace NReco.CF.Taste.Impl.Recommender {

/// <summary>
/// A simple implementation of <see cref="IRecommendedItem"/>.
/// </summary>
[Serializable]
public sealed class GenericRecommendedItem : IRecommendedItem {
  
  private long itemID;
  private float value;
  
   /// @throws IllegalArgumentException
   ///           if item is null or value is NaN
  public GenericRecommendedItem(long itemID, float value) {
    //Preconditions.checkArgument(!Float.isNaN(value), "value is NaN");
    this.itemID = itemID;
    this.value = value;
  }
  
  public long GetItemID() {
    return itemID;
  }
  
  public float GetValue() {
    return value;
  }

  public override string ToString() {
    return "RecommendedItem[item:" + itemID + ", value:" + value + ']';
  }
  
  public override int GetHashCode() {
    return (int) itemID ^ RandomUtils.hashFloat(value);
  }
  
  public override bool Equals(object o) {
    if (!(o is GenericRecommendedItem)) {
      return false;
    }
    IRecommendedItem other = (IRecommendedItem) o;
    return itemID == other.GetItemID() && value == other.GetValue();
  }
  
}

}