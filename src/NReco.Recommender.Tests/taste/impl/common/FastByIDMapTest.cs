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


/// <p>Tests {@link FastByIDMap}.</p> 
public sealed class FastByIDMapTest : TasteTestCase {

  [Test]
  public void testPutAndGet() {
    FastByIDMap<long?> map = new FastByIDMap<long?>();
    Assert.IsNull(map.Get(500000L));
    map.Put(500000L, 2L);
    Assert.AreEqual(2L, (long) map.Get(500000L));
  }
  
  [Test]
  public void testRemove() {
    FastByIDMap<long?> map = new FastByIDMap<long?>();
    map.Put(500000L, 2L);
    map.Remove(500000L);
    Assert.AreEqual(0, map.Count());
    Assert.True(map.IsEmpty());
    Assert.IsNull(map.Get(500000L));
  }
  
  [Test]
  public void testClear() {
    FastByIDMap<long?> map = new FastByIDMap<long?>();
    map.Put(500000L, 2L);
    map.Clear();
    Assert.AreEqual(0, map.Count());
    Assert.True(map.IsEmpty());
    Assert.IsNull(map.Get(500000L));
  }
  
  [Test]
  public void testSizeEmpty() {
    FastByIDMap<long> map = new FastByIDMap<long>();
    Assert.AreEqual(0, map.Count());
    Assert.True(map.IsEmpty());
    map.Put(500000L, 2L);
	Assert.AreEqual(1, map.Count());
    Assert.False(map.IsEmpty());
    map.Remove(500000L);
	Assert.AreEqual(0, map.Count());
    Assert.True(map.IsEmpty());
  }
  
  [Test]
  public void testContains() {
    FastByIDMap<String> map = buildTestFastMap();
    Assert.True(map.ContainsKey(500000L));
    Assert.True(map.ContainsKey(47L));
    Assert.True(map.ContainsKey(2L));
    Assert.True(map.ContainsValue("alpha"));
    Assert.True(map.ContainsValue("bang"));
    Assert.True(map.ContainsValue("beta"));
    Assert.False(map.ContainsKey(999));
    Assert.False(map.ContainsValue("something"));
  }

  [Test]
  public void testRehash() {
    FastByIDMap<String> map = buildTestFastMap();
    map.Remove(500000L);
    map.Rehash();
    Assert.IsNull(map.Get(500000L));
    Assert.AreEqual("bang", map.Get(47L));
  }
  
  [Test]
  public void testGrow() {
    FastByIDMap<String> map = new FastByIDMap<String>(1,1);
    map.Put(500000L, "alpha");
    map.Put(47L, "bang");
    Assert.IsNull(map.Get(500000L));
    Assert.AreEqual("bang", map.Get(47L));
  }
   
  [Test]
  public void testVersusHashMap() {
    FastByIDMap<String> actual = new FastByIDMap<String>();
    IDictionary<long, string> expected = new Dictionary<long,string>(1000000);
    var r = RandomUtils.getRandom();
    for (int i = 0; i < 1000000; i++) {
      double d = r.nextDouble();
      long key = (long) r.nextInt(100);
      if (d < 0.4) {
        Assert.AreEqual( expected.ContainsKey(key)?expected[key]:null, actual.Get(key));
      } else {
        if (d < 0.7) {
			var expectedOldVal = expected.ContainsKey(key) ? expected[key] : null;
			expected[key] = "bang";
			Assert.AreEqual(expectedOldVal, actual.Put(key, "bang"));
        } else {
			var expectedOldVal = expected.ContainsKey(key) ? expected[key] : null;
			expected.Remove(key);
			Assert.AreEqual(expectedOldVal, actual.Remove(key));
        }
        Assert.AreEqual(expected.Count, actual.Count());
        Assert.AreEqual(expected.Count==0, actual.IsEmpty());
      }
    }
  }
  
  [Test]
  public void testMaxSize() {
    FastByIDMap<String> map = new FastByIDMap<String>();
    map.Put(4, "bang");
    Assert.AreEqual(1, map.Count());
    map.Put(47L, "bang");
	Assert.AreEqual(2, map.Count());
    Assert.IsNull(map.Get(500000L));
    map.Put(47L, "buzz");
	Assert.AreEqual(2, map.Count());
    Assert.AreEqual("buzz", map.Get(47L));
  }
  
  
  private static FastByIDMap<String> buildTestFastMap() {
    FastByIDMap<String> map = new FastByIDMap<String>();
    map.Put(500000L, "alpha");
    map.Put(47L, "bang");
    map.Put(2L, "beta");
    return map;
  }
  
}

}