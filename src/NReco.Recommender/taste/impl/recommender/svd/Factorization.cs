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

namespace NReco.CF.Taste.Impl.Recommender.SVD {

/// <summary>
/// A factorization of the rating matrix
/// </summary>
public class Factorization {

  /// used to find the rows in the user features matrix by userID 
  private FastByIDMap<int?> userIDMapping;
  /// used to find the rows in the item features matrix by itemID 
  private FastByIDMap<int?> itemIDMapping;

  /// user features matrix 
  private double[][] userFeatures;
  /// item features matrix 
  private double[][] itemFeatures;

  public Factorization(FastByIDMap<int?> userIDMapping, FastByIDMap<int?> itemIDMapping, double[][] userFeatures,
      double[][] itemFeatures) {
    this.userIDMapping = userIDMapping; //Preconditions.checkNotNull(
	this.itemIDMapping = itemIDMapping; //Preconditions.checkNotNull();
    this.userFeatures = userFeatures;
    this.itemFeatures = itemFeatures;
  }

  public double[][] allUserFeatures() {
    return userFeatures;
  }

  public virtual double[] getUserFeatures(long userID) {
    int? index = userIDMapping.Get(userID);
    if (index == null) {
      throw new NoSuchUserException(userID);
    }
    return userFeatures[index.Value];
  }

  public double[][] allItemFeatures() {
    return itemFeatures;
  }

  public virtual double[] getItemFeatures(long itemID) {
    int? index = itemIDMapping.Get(itemID);
    if (index == null) {
      throw new NoSuchItemException(itemID);
    }
    return itemFeatures[index.Value];
  }

  public int userIndex(long userID) {
    int? index = userIDMapping.Get(userID);
    if (index == null) {
      throw new NoSuchUserException(userID);
    }
    return index.Value;
  }

  public IEnumerable<KeyValuePair<long,int?>> getUserIDMappings() {
    return userIDMapping.EntrySet();
  }

  public IEnumerator<long> getUserIDMappingKeys() {
	  return userIDMapping.Keys.GetEnumerator();
  }


  public int itemIndex(long itemID) {
    int? index = itemIDMapping.Get(itemID);
    if (index == null) {
      throw new NoSuchItemException(itemID);
    }
    return index.Value;
  }

  public IEnumerable<KeyValuePair<long, int?>> getItemIDMappings() {
    return itemIDMapping.EntrySet();
  }

  public IEnumerator<long> getItemIDMappingKeys() {
	  return itemIDMapping.Keys.GetEnumerator();
  }

  public int numFeatures() {
    return userFeatures.Length > 0 ? userFeatures[0].Length : 0;
  }

  public int numUsers() {
    return userIDMapping.Count();
  }

  public int numItems() {
    return itemIDMapping.Count();
  }

  public override bool Equals(object o) {
    if (o is Factorization) {
      Factorization other = (Factorization) o;
      return userIDMapping.Equals(other.userIDMapping) && itemIDMapping.Equals(other.itemIDMapping)
		  && Utils.ArrayDeepEquals(userFeatures, other.userFeatures) && Utils.ArrayDeepEquals(itemFeatures, other.itemFeatures);
    }
    return false;
  }

  public override int GetHashCode() {
    int hashCode = 31 * userIDMapping.GetHashCode() + itemIDMapping.GetHashCode();
    hashCode = 31 * hashCode + Utils.GetArrayDeepHashCode(userFeatures);
    hashCode = 31 * hashCode + Utils.GetArrayDeepHashCode(itemFeatures);
    return hashCode;
  }
}

}