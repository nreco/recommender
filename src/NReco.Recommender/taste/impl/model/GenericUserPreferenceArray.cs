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

namespace NReco.CF.Taste.Impl.Model {

/// <summary>
/// Like <see cref="GenericItemPreferenceArray"/> but stores preferences for one user (all user IDs the same) rather
/// than one item.
/// <para>
/// This implementation maintains two parallel arrays, of item IDs and values. The idea is to save allocating
/// <see cref="IPreference"/> objects themselves. This saves the overhead of <see cref="IPreference"/> objects but also
/// duplicating the user ID value.
/// </para>
/// </summary>
/// @see BooleanUserPreferenceArray
/// @see GenericItemPreferenceArray
/// @see GenericPreference
[Serializable]
public sealed class GenericUserPreferenceArray : IPreferenceArray {

  private const int ITEM = 1;
  private const int VALUE = 2;
  private const int VALUE_REVERSED = 3;

  private long[] ids;
  private long id;
  private float[] values;

  public GenericUserPreferenceArray(int size) {
    this.ids = new long[size];
    values = new float[size];
    this.id = Int64.MinValue; // as a sort of 'unspecified' value
  }

  public GenericUserPreferenceArray(IList<IPreference> prefs) : this(prefs.Count) {
    int size = prefs.Count;
    long userID = Int64.MinValue;
    for (int i = 0; i < size; i++) {
      IPreference pref = prefs[i];
      if (i == 0) {
        userID = pref.GetUserID();
      } else {
        if (userID != pref.GetUserID()) {
          throw new ArgumentException("Not all user IDs are the same");
        }
      }
      ids[i] = pref.GetItemID();
      values[i] = pref.GetValue();
    }
    id = userID;
  }

   /// This is a private copy constructor for clone().
  private GenericUserPreferenceArray(long[] ids, long id, float[] values) {
    this.ids = ids;
    this.id = id;
    this.values = values;
  }

  public int Length() {
    return ids.Length;
  }

  public IPreference Get(int i) {
    return new PreferenceView(this, i);
  }

  public void Set(int i, IPreference pref) {
    id = pref.GetUserID();
    ids[i] = pref.GetItemID();
    values[i] = pref.GetValue();
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
    return values[i];
  }

  public void SetValue(int i, float value) {
    values[i] = value;
  }

  public void SortByUser() { }

  public void SortByItem() {
    lateralSort(ITEM);
  }

  public void SortByValue() {
    lateralSort(VALUE);
  }

  public void SortByValueReversed() {
    lateralSort(VALUE_REVERSED);
  }

  public bool HasPrefWithUserID(long userID) {
    return id == userID;
  }

  public bool HasPrefWithItemID(long itemID) {
    foreach (var id in ids) {
      if (itemID == id) {
        return true;
      }
    }
    return false;
  }

  private void lateralSort(int type) {
    //Comb sort: http://en.wikipedia.org/wiki/Comb_sort
    int len = Length();
    int gap = len;
    bool swapped = false;
    while (gap > 1 || swapped) {
      if (gap > 1) {
        gap = (int) ( gap / 1.247330950103979 ); // = 1 / (1 - 1/e^phi)
      }
      swapped = false;
      int max = len - gap;
      for (int i = 0; i < max; i++) {
        int other = i + gap;
        if (isLess(other, i, type)) {
          swap(i, other);
          swapped = true;
        }
      }
    }
  }

  private bool isLess(int i, int j, int type) {
    switch (type) {
      case ITEM:
        return ids[i] < ids[j];
      case VALUE:
        return values[i] < values[j];
      case VALUE_REVERSED:
        return values[i] > values[j];
      default:
        throw new InvalidOperationException();
    }
  }

  private void swap(int i, int j) {
    long temp1 = ids[i];
    float temp2 = values[i];
    ids[i] = ids[j];
    values[i] = values[j];
    ids[j] = temp1;
    values[j] = temp2;
  }

  public IPreferenceArray Clone() {
    return new GenericUserPreferenceArray( (long[])ids.Clone(), id, (float[])values.Clone());
  }

  public override int GetHashCode() {
	  return (int)(id >> 32) ^ (int)id ^ Utils.GetArrayHashCode(ids) ^ Utils.GetArrayHashCode(values);
  }

  public override bool Equals(object other) {
    if (!(other is GenericUserPreferenceArray)) {
      return false;
    }
    GenericUserPreferenceArray otherArray = (GenericUserPreferenceArray) other;
	return id == otherArray.id && Enumerable.SequenceEqual(ids, otherArray.ids) && Enumerable.SequenceEqual(values, otherArray.values);
  }

  public IEnumerator<IPreference> GetEnumerator() {
    for (int i=0; i<Length(); i++)
		yield return new PreferenceView(this,i);	
  }

  IEnumerator IEnumerable.GetEnumerator() {
	return GetEnumerator();
  }


  public override string ToString() {
    if (ids == null || ids.Length == 0) {
      return "GenericUserPreferenceArray[{}]";
    }
    var result = new System.Text.StringBuilder(20 * ids.Length);
    result.Append("GenericUserPreferenceArray[userID:");
    result.Append(id);
    result.Append(",{");
    for (int i = 0; i < ids.Length; i++) {
      if (i > 0) {
        result.Append(',');
      }
      result.Append(ids[i]);
      result.Append('=');
      result.Append(values[i]);
    }
    result.Append("}]");
    return result.ToString();
  }

  private sealed class PreferenceView : IPreference {

    private int i;
	GenericUserPreferenceArray arr;

    internal PreferenceView(GenericUserPreferenceArray arr, int i) {
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
      return arr.values[i];
    }

    public void SetValue(float value) {
      arr.values[i] = value;
    }

  }

}

}