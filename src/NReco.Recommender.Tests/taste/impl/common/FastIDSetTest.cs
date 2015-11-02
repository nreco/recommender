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
using NReco.CF;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Common {


/// <p>Tests {@link FastIDSet}.</p> 
public sealed class FastIDSetTest : TasteTestCase {

  [Test]
  public void testContainsAndAdd() {
    FastIDSet set = new FastIDSet();
    Assert.False(set.Contains(1));
    set.Add(1);
    Assert.True(set.Contains(1));
  }

  [Test]
  public void testRemove() {
    FastIDSet set = new FastIDSet();
    set.Add(1);
    set.Remove(1);
    Assert.AreEqual(0, set.Count());
    Assert.True(set.IsEmpty());
    Assert.False(set.Contains(1));
  }

  [Test]
  public void testClear() {
    FastIDSet set = new FastIDSet();
    set.Add(1);
    set.Clear();
	Assert.AreEqual(0, set.Count());
    Assert.True(set.IsEmpty());
    Assert.False(set.Contains(1));
  }

  [Test]
  public void testSizeEmpty() {
    FastIDSet set = new FastIDSet();
	Assert.AreEqual(0, set.Count());
    Assert.True(set.IsEmpty());
    set.Add(1);
	Assert.AreEqual(1, set.Count());
    Assert.False(set.IsEmpty());
    set.Remove(1);
	Assert.AreEqual(0, set.Count());
    Assert.True(set.IsEmpty());
  }

  [Test]
  public void testContains() {
    FastIDSet set = buildTestFastSet();
    Assert.True(set.Contains(1));
    Assert.True(set.Contains(2));
    Assert.True(set.Contains(3));
    Assert.False(set.Contains(4));
  }

  [Test]
  public void testReservedValues() {
    FastIDSet set = new FastIDSet();
    try {
      set.Add(Int64.MinValue);
      Assert.Fail("Should have thrown IllegalArgumentException");
	} catch (ArgumentException iae) { //IllegalArgumentException
      // good
    }
    Assert.False(set.Contains(Int64.MinValue));
    try {
      set.Add(long.MaxValue);
      Assert.Fail("Should have thrown IllegalArgumentException");
    } catch (ArgumentException iae) {
      // good
    }
    Assert.False(set.Contains(long.MaxValue));
  }

  [Test]
  public void testRehash() {
    FastIDSet set = buildTestFastSet();
    set.Remove(1);
    set.Rehash();
    Assert.False(set.Contains(1));
  }

  [Test]
  public void testGrow() {
    FastIDSet set = new FastIDSet(1);
    set.Add(1);
    set.Add(2);
    Assert.True(set.Contains(1));
    Assert.True(set.Contains(2));
  }

  [Test]
  public void testIterator() {
    FastIDSet set = buildTestFastSet();
    var expected = new List<long>(3);
    expected.Add(1L);
    expected.Add(2L);
    expected.Add(3L);
    var it = set.GetEnumerator();
    while (it.MoveNext()) {
      expected.Remove(it.Current);
    }
    Assert.True(expected.Count == 0);
  }

  [Test]
  public void testVersusHashSet() {
    FastIDSet actual = new FastIDSet(1);
	var expected = new HashSet<int>(); //1000000
    var r = RandomUtils.getRandom();
    for (int i = 0; i < 1000000; i++) {
      double d = r.nextDouble();
      var key = r.nextInt(100);
      if (d < 0.4) {
        Assert.AreEqual(expected.Contains(key), actual.Contains(key));
      } else {
        if (d < 0.7) {
          Assert.AreEqual(expected.Add(key), actual.Add(key));
        } else {
          Assert.AreEqual(expected.Remove(key), actual.Remove(key));
        }
        Assert.AreEqual(expected.Count, actual.Count() );
        Assert.AreEqual(expected.Count==0, actual.IsEmpty());
      }
    }
  }

  private static FastIDSet buildTestFastSet() {
    FastIDSet set = new FastIDSet();
    set.Add(1);
    set.Add(2);
    set.Add(3);
    return set;
  }


}
}