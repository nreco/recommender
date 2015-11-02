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


using NReco.CF.Taste.Model;
using NReco.CF.Taste.Common;
//using NReco.CF.iterator;

namespace NReco.CF.Taste.Impl.Model {

/// <summary>
/// Like <see cref="GenericUserPreferenceArray"/> but stores, conceptually, <see cref="BooleanPreference"/> objects which
/// have no associated preference value.
/// </summary>
/// <seealso cref="BooleanPreference"/>
/// <seealso cref="BooleanItemPreferenceArray"/>
/// <seealso cref="GenericUserPreferenceArray"/>
public sealed class BooleanUserPreferenceArray : IPreferenceArray {
  
  private long[] ids;
  private long id;
  
  public BooleanUserPreferenceArray(int size) {
    this.ids = new long[size];
    this.id = Int64.MinValue; // as a sort of 'unspecified' value
  }
  
  public BooleanUserPreferenceArray(List<IPreference> prefs) : this(prefs.Count)  {
    int size = prefs.Count;
    for (int i = 0; i < size; i++) {
      IPreference pref = prefs[i];
      ids[i] = pref.GetItemID();
    }
    if (size > 0) {
      id = prefs[0].GetUserID();
    }
  }
  
   /// This is a private copy constructor for clone().
  private BooleanUserPreferenceArray(long[] ids, long id) {
    this.ids = ids;
    this.id = id;
  }
  
  public int Length() {
    return ids.Length;
  }
  
  public IPreference Get(int i) {
    return new PreferenceView(this,i);
  }
  
  public void Set(int i, IPreference pref) {
    id = pref.GetUserID();
    ids[i] = pref.GetItemID();
  }
  
  public long GetUserID(int i) {
    return id;
  }
  
   /// {@inheritDoc}
   /// 
   /// Note that this method will actually set the user ID for <em>all</em> preferences.
  public void SetUserID(int i, long userID) {
    id = userID;
  }
  
  public long GetItemID(int i) {
    return ids[i];
  }
  
  public void SetItemID(int i, long itemID) {
    ids[i] = itemID;
  }

   /// @return all item IDs
  public long[] GetIDs() {
    return ids;
  }
  
  public float GetValue(int i) {
    return 1.0f;
  }
  
  public void SetValue(int i, float value) {
    throw new NotSupportedException();
  }
  
  public void SortByUser() { }
  
  public void SortByItem() {
    Array.Sort(ids);
  }
  
  public void SortByValue() { }
  
  public void SortByValueReversed() { }
  
  public bool HasPrefWithUserID(long userID) {
    return id == userID;
  }
  
  public bool HasPrefWithItemID(long itemID) {
    foreach (long id in ids) {
      if (itemID == id) {
        return true;
      }
    }
    return false;
  }
  
  public IPreferenceArray Clone() {
    return new BooleanUserPreferenceArray( (long[])ids.Clone(), id);
  }

  public override int GetHashCode() {
    return (int) (id >> 32) ^ (int) id ^ Utils.GetArrayHashCode(ids);
  }

  public override bool Equals(Object other) {
    if (!(other is BooleanUserPreferenceArray)) {
      return false;
    }
    var otherArray = (BooleanUserPreferenceArray) other;
    return id == otherArray.id && Enumerable.SequenceEqual(ids, otherArray.ids);
  }
  
  public IEnumerator<IPreference> GetEnumerator() {
    for (int i=0; i<Length(); i++)
		yield return new PreferenceView(this,i);
  }

  IEnumerator IEnumerable.GetEnumerator() {
	  return GetEnumerator();
  }

  public override string ToString() {
    var result = new System.Text.StringBuilder(10 * ids.Length);
    result.Append("BooleanUserPreferenceArray[userID:");
    result.Append(id);
    result.Append(",{");
    for (int i = 0; i < ids.Length; i++) {
      if (i > 0) {
        result.Append(',');
      }
      result.Append(ids[i]);
    }
    result.Append("}]");
    return result.ToString();
  }

  private sealed class PreferenceView : IPreference {
    
    private int i;
	BooleanUserPreferenceArray arr;
    
    internal PreferenceView(BooleanUserPreferenceArray arr, int i) {
      this.i = i;
	  this.arr = arr;
    }
    
    public long GetUserID() {
      return arr.GetUserID(i);
    }
    
    public long GetItemID() {
      return arr.GetItemID(i);
    }
    
    public float GetValue() {
      return 1.0f;
    }
    
    public void SetValue(float value) {
      throw new NotSupportedException();
    }
    
  }
  
}

}