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

using NReco.CF;


namespace NReco.CF.Taste.Impl.Common {

 /// @see FastMap
 /// @see FastIDSet
[Serializable]
public sealed class FastByIDMap<V> /*: ICloneable*/ {
  
  public const int NO_MAX_SIZE = Int32.MaxValue;
  private static float DEFAULT_LOAD_FACTOR = 1.5f;
  
  /// Dummy object used to represent a key that has been removed. 
  private static long REMOVED = Int64.MaxValue;
  private static long NULL = Int64.MinValue;
  
  private long[] keys;
  private V[] values;
  private float loadFactor;
  private int numEntries;
  private int numSlotsUsed;
  private int maxSize;
  private BitSet recentlyAccessed;
  private bool countingAccesses;
  
  /// Creates a new {@link FastByIDMap} with default capacity. 
  public FastByIDMap() : this(2, NO_MAX_SIZE) {
  }
  
  public FastByIDMap(int size) : this(size, NO_MAX_SIZE) {
  }

  public FastByIDMap(int size, float loadFactor) : this(size, NO_MAX_SIZE, loadFactor) {
  }

  public FastByIDMap(int size, int maxSize) : this(size, maxSize, DEFAULT_LOAD_FACTOR) {

  }

   /// Creates a new {@link FastByIDMap} whose capacity can accommodate the given number of entries without rehash.
   /// 
   /// @param size desired capacity
   /// @param maxSize max capacity
   /// @param loadFactor ratio of internal hash table size to current size
   /// @throws IllegalArgumentException if size is less than 0, maxSize is less than 1
   ///  or at least half of {@link RandomUtils#MAX_INT_SMALLER_TWIN_PRIME}, or
   ///  loadFactor is less than 1
  public FastByIDMap(int size, int maxSize, float loadFactor) {
    //Preconditions.checkArgument(size >= 0, "size must be at least 0");
    //Preconditions.checkArgument(loadFactor >= 1.0f, "loadFactor must be at least 1.0");
    this.loadFactor = loadFactor;
    int max = (int) (RandomUtils.MAX_INT_SMALLER_TWIN_PRIME / loadFactor);
    //Preconditions.checkArgument(size < max, "size must be less than " + max);
    //Preconditions.checkArgument(maxSize >= 1, "maxSize must be at least 1");
    int hashSize = RandomUtils.nextTwinPrime((int) (loadFactor * size));
    keys = new long[hashSize];
    
	ArrayFill(keys,NULL);
    values = new V[hashSize];
    this.maxSize = maxSize;
    this.countingAccesses = maxSize != Int32.MaxValue;
    this.recentlyAccessed = countingAccesses ? new BitSet( (uint)hashSize) : null;
  }
  
	void ArrayFill<T>(T[] a, T val) {
		for (int i=0;i<a.Length;i++)
			a[i] = val; 
	}

  private int find(long key) {
    int theHashCode = (int) key & 0x7FFFFFFF; // make sure it's positive
    long[] keys = this.keys;
    int hashSize = keys.Length;
    int jump = 1 + theHashCode % (hashSize - 2);
    int index = theHashCode % hashSize;
    long currentKey = keys[index];
    while (currentKey != NULL && key != currentKey) {
      index -= index < jump ? jump - hashSize : jump;
      currentKey = keys[index];
    }
    return index;
  }

  private int findForAdd(long key) {
    int theHashCode = (int) key & 0x7FFFFFFF; // make sure it's positive
    long[] keys = this.keys;
    int hashSize = keys.Length;
    int jump = 1 + theHashCode % (hashSize - 2);
    int index = theHashCode % hashSize;
    long currentKey = keys[index];
    while (currentKey != NULL && currentKey != REMOVED && key != currentKey) {
      index -= index < jump ? jump - hashSize : jump;
      currentKey = keys[index];
    }
    if (currentKey != REMOVED) {
      return index;
    }
    // If we're adding, it's here, but, the key might have a value already later
    int addIndex = index;
    while (currentKey != NULL && key != currentKey) {
      index -= index < jump ? jump - hashSize : jump;
      currentKey = keys[index];
    }
    return key == currentKey ? index : addIndex;
  }
  
  public V Get(long key) {
    if (key == NULL) {
      return default(V);
    }
    int index = find(key);
    if (countingAccesses) {
      recentlyAccessed.Set(index);
    }
    return values[index];
  }
  
  public int Count() {
    return numEntries;
  }
  
  public bool IsEmpty() {
    return numEntries == 0;
  }
  
  public bool ContainsKey(long key) {
    return key != NULL && key != REMOVED && keys[find(key)] != NULL;
  }
  
  public bool ContainsValue(Object value) {
    if (value == null) {
      return false;
    }
    foreach (var theValue in values) {
      if (theValue != null && value.Equals(theValue)) {
        return true;
      }
    }
    return false;
  }
  
  public V Put(long key, V value) {
    //Preconditions.checkArgument(key != NULL && key != REMOVED);
    //Preconditions.checkNotNull(value);
    // If less than half the slots are open, let's clear it up
    if (numSlotsUsed * loadFactor >= keys.Length) {
      // If over half the slots used are actual entries, let's grow
      if (numEntries * loadFactor >= numSlotsUsed) {
        GrowAndRehash();
      } else {
        // Otherwise just rehash to clear REMOVED entries and don't grow
        Rehash();
      }
    }
    // Here we may later consider implementing Brent's variation described on page 532
    int index = findForAdd(key);
    long keyIndex = keys[index];
    if (keyIndex == key) {
      V oldValue = values[index];
      values[index] = value;
      return oldValue;
    }
    // If size is limited,
    if (countingAccesses && numEntries >= maxSize) {
      // and we're too large, clear some old-ish entry
      clearStaleEntry(index);
    }
    keys[index] = key;
    values[index] = value;
    numEntries++;
    if (keyIndex == NULL) {
      numSlotsUsed++;
    }
    return default(V);
  }
  
  private void clearStaleEntry(int index) {
    while (true) {
      long currentKey;
      do {
        if (index == 0) {
          index = keys.Length - 1;
        } else {
          index--;
        }
        currentKey = keys[index];
      } while (currentKey == NULL || currentKey == REMOVED);
      if (recentlyAccessed.Get(index)) {
        recentlyAccessed.Clear(index);
      } else {
        break;
      }
    }
    // Delete the entry
    keys[index] = REMOVED;
    numEntries--;
    values[index] = default(V);
  }
  
  public V Remove(long key) {
    if (key == NULL || key == REMOVED) {
      return default(V);
    }
    int index = find(key);
    if (keys[index] == NULL) {
      return default(V);
    } else {
      keys[index] = REMOVED;
      numEntries--;
      V oldValue = values[index];
      values[index] = default(V);
      // don't decrement numSlotsUsed
      return oldValue;
    }
    // Could un-set recentlyAccessed's bit but doesn't matter
  }
  
  public void Clear() {
    numEntries = 0;
    numSlotsUsed = 0;
    ArrayFill(keys, NULL);
    ArrayFill(values, default(V) );
    if (countingAccesses) {
      recentlyAccessed.Clear();
    }
  }


  public IEnumerable<KeyValuePair<long,V>> EntrySet() {
	  for (int i = 0; i < keys.Length && i < values.Length; i++)
		  if (values[i]!=null)
			yield return new KeyValuePair<long,V>(keys[i], values[i]);
  }

  public IEnumerable<long> Keys {
	  get {
		  for (int i = 0; i < keys.Length && i < values.Length; i++)
			  if (values[i]!=null)
				yield return keys[i];
	  }
  }

  public IEnumerable<V> Values {
	  get {
		  for (int i = 0; i < values.Length; i++)
			  if (values[i] != null)
				yield return values[i];
	  }
  }

  /*public longPrimitiveIterator keySetIterator() {
    return new KeyIterator();
  }

  
  public IEnumerable<V> allValues() {
    return new ValueCollection();
  }*/
  
  public void Rehash() {
    rehash(RandomUtils.nextTwinPrime((int) (loadFactor * numEntries)));
  }
  
  private void GrowAndRehash() {
    if (keys.Length * loadFactor >= RandomUtils.MAX_INT_SMALLER_TWIN_PRIME) {
      throw new InvalidOperationException("Can't grow any more");
    }
    rehash(RandomUtils.nextTwinPrime((int) (loadFactor * keys.Length)));
  }
  
  private void rehash(int newHashSize) {
    long[] oldKeys = keys;
    V[] oldValues = values;
    numEntries = 0;
    numSlotsUsed = 0;
    if (countingAccesses) {
      recentlyAccessed = new BitSet(newHashSize);
    }
    keys = new long[newHashSize];
    ArrayFill(keys, NULL);
    values = new V[newHashSize];
    int length = oldKeys.Length;
    for (int i = 0; i < length; i++) {
      long key = oldKeys[i];
      if (key != NULL && key != REMOVED) {
        Put(key, oldValues[i]);
      }
    }
  }
  
  void iteratorRemove(int lastNext) {
    if (lastNext >= values.Length) {
      throw new InvalidOperationException(); //NoSuchElementException
    }
    if (lastNext < 0) {
      throw new InvalidOperationException();
    }
    values[lastNext] = default(V);
    keys[lastNext] = REMOVED;
    numEntries--;
  }
  
  /*public override FastByIDMap<V> clone() {
    FastByIDMap<V> clone = new FastByIDMap<V>(;
    try {
      clone = (FastByIDMap<V>) super.clone();
    } catch (CloneNotSupportedException cnse) {
      throw new AssertionError();
    }
    clone.keys = keys.clone();
    clone.values = values.clone();
    clone.recentlyAccessed = countingAccesses ? new BitSet(keys.Length) : null;
    return clone;
  }*/
  
  public override string ToString() {
    if (IsEmpty()) {
      return "{}";
    }
    var result = new System.Text.StringBuilder();
    result.Append('{');
    for (int i = 0; i < keys.Length; i++) {
      long key = keys[i];
      if (key != NULL && key != REMOVED) {
        result.Append(key).Append('=').Append( values[i].ToString() ).Append(',');
      }
    }
    result[ result.Length - 1 ] = '}';
    return result.ToString();
  }

  public override int GetHashCode() {
    int hash = 0;
    long[] keys = this.keys;
    int max = keys.Length;
    for (int i = 0; i < max; i++) {
      long key = keys[i];
      if (key != NULL && key != REMOVED) {
        hash = 31 * hash + ((int) (key >> 32) ^ (int) key);
        hash = 31 * hash + values[i].GetHashCode();
      }
    }
    return hash;
  }

  public override bool Equals(object other) {
    if (!(other is FastByIDMap<V>)) {
      return false;
    }
    FastByIDMap<V> otherMap = (FastByIDMap<V>) other;
    long[] otherKeys = otherMap.keys;
    V[] otherValues = otherMap.values;
    int length = keys.Length;
    int otherLength = otherKeys.Length;
    int max = Math.Min(length, otherLength);

    int i = 0;
    while (i < max) {
      long key = keys[i];
      long otherKey = otherKeys[i];
      if (key == NULL || key == REMOVED) {
        if (otherKey != NULL && otherKey != REMOVED) {
          return false;
        }
      } else {
        if (key != otherKey || !values[i].Equals(otherValues[i])) {
          return false;
        }
      }
      i++;
    }
    while (i < length) {
      long key = keys[i];
      if (key != NULL && key != REMOVED) {
        return false;
      }
      i++;
    }
    while (i < otherLength) {
      long key = otherKeys[i];
      if (key != NULL && key != REMOVED) {
        return false;
      }
      i++;
    }
    return true;
  }
  

    
  }
}

