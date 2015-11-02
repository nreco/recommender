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

namespace NReco.CF.Taste.Impl.Common {

/// <summary>
/// A simplified and streamlined version of BitSet
/// </summary>
[Serializable]
public sealed class BitSet : ICloneable {
  
  private long[] bits;

  public BitSet(int numBits) : this( (uint)numBits ) {
	}
  
  public BitSet(uint numBits) {
    uint numlongs = numBits >> 6;
    if ((numBits & 0x3F) != 0) {
      numlongs++;
    }
    bits = new long[numlongs];
  }
  
  private BitSet(long[] bits) {
    this.bits = bits;
  }
  
  public bool Get(int index) {
    // skipping range check for speed
    return (bits[index >> 6] & 1L << (int)( index & 0x3F)) != 0L;
  }
  
  public void Set(int index) {
    // skipping range check for speed
    bits[index >> 6] |= 1L << (int)(index & 0x3F);
  }
  
  public void Clear(int index) {
    // skipping range check for speed
    bits[ (uint)index >> 6] &= ~(1L << (index & 0x3F));
  }
  
  public void Clear() {
    int length = bits.Length;
    for (int i = 0; i < length; i++) {
      bits[i] = 0L;
    }
  }
  
  public object Clone() {
    return new BitSet( (long[])bits.Clone());
  }

  public override int GetHashCode() {
	  return Utils.GetArrayHashCode(bits);
  }

  public override bool Equals(Object o) {
    if (!(o is BitSet)) {
      return false;
    }
    BitSet other = (BitSet) o;
	return Enumerable.SequenceEqual(bits, other.bits);
  }
  
  public override string ToString() {
    var result = new System.Text.StringBuilder(64 * bits.Length);
    foreach (long l in bits) {
      for (int j = 0; j < 64; j++) {
        result.Append((l & 1L << j) == 0 ? '0' : '1');
      }
      result.Append(' ');
    }
    return result.ToString();
  }
  
}

}