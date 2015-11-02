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
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS 
 *  OF ANY KIND, either express or implied.
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NReco.CF.Taste.Impl;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Common {

public sealed class BitSetTest : TasteTestCase {

  private static int NUM_BITS = 100;

  [Test]
  public void testGetSet() {
    BitSet bitSet = new BitSet(NUM_BITS);
    for (int i = 0; i < NUM_BITS; i++) {
      Assert.False(bitSet.Get(i));

    }
    bitSet.Set(0);
    bitSet.Set(NUM_BITS-1);
    Assert.True(bitSet.Get(0));
    Assert.True(bitSet.Get(NUM_BITS-1));
  }

  //[Test](expected = ArrayIndexOutOfBoundsException.class)
	[Test]
	[ExpectedException]
  public void testBounds1() {
    BitSet bitSet = new BitSet(NUM_BITS);
    bitSet.Set(1000);
  }

  //[Test](expected = ArrayIndexOutOfBoundsException.class)\
	[Test]
	[ExpectedException]
  public void testBounds2() {
    BitSet bitSet = new BitSet(NUM_BITS);
    bitSet.Set(-1);
  }

	  [Test]
  public void testClear() {
    BitSet bitSet = new BitSet(NUM_BITS);
    for (int i = 0; i < NUM_BITS; i++) {
      bitSet.Set(i);
    }
    for (int i = 0; i < NUM_BITS; i++) {
      Assert.True(bitSet.Get(i));
    }
    bitSet.Clear();
    for (int i = 0; i < NUM_BITS; i++) {
      Assert.False(bitSet.Get(i));
    }
  }

  [Test]
  public void testClone() {
    BitSet bitSet = new BitSet(NUM_BITS);
    bitSet.Set(NUM_BITS-1);
    bitSet = (BitSet)bitSet.Clone();
    Assert.True(bitSet.Get(NUM_BITS-1));
  }

}
}