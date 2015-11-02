 /// Licensed to the Apache Software Foundation (ASF) under one or more
 /// contributor license agreements.  See the NOTICE file distributed with
 /// this work for additional information regarding copyright ownership.
 /// The ASF licenses this file to You under the Apache License, Version 2.0
 /// (the "License"); you may not use this file except in compliance with
 /// the License.  You may obtain a copy of the License at
 ///
 ///     http://www.apache.org/licenses/LICENSE-2.0
 ///
 /// Unless required by applicable law or agreed to in writing, software
 /// distributed under the License is distributed on an "AS IS" BASIS,
 /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 /// See the License for the specific language governing permissions and
 /// limitations under the License.


using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using org.apache.mahout.cf.taste.impl;
using org.apache.mahout.common;
using NUnit.Framework;

namespace org.apache.mahout.cf.taste.impl.common {


/// <p>Tests {@link FastMap}.</p> 
public sealed class FastMapTest : TasteTestCase {

  [Test]
  public void testPutAndGet() {
    IDictionary<string, string> map = new FastMap<String, String>();
    Assert.IsNull(map.get("foo"));
    map.put("foo", "bar");
    Assert.AreEqual("bar", map.get("foo"));
  }

  [Test]
  public void testRemove() {
    IDictionary<String, String> map = new FastMap<String, String>();
    map.put("foo", "bar");
    map.remove("foo");
    Assert.AreEqual(0, map.Count);
    Assert.True(map.isEmpty());
    Assert.IsNull(map.get("foo"));
  }

  [Test]
  public void testClear() {
    IDictionary<String, String> map = new FastMap<String, String>();
    map.put("foo", "bar");
    map.clear();
    Assert.AreEqual(0, map.Count);
    Assert.True(map.isEmpty());
    Assert.IsNull(map.get("foo"));
  }

  [Test]
  public void testSizeEmpty() {
    IDictionary<String, String> map = new FastMap<String, String>();
    Assert.AreEqual(0, map.Count);
    Assert.True(map.isEmpty());
    map.put("foo", "bar");
    Assert.AreEqual(1, map.Count);
    Assert.False(map.isEmpty());
    map.remove("foo");
    Assert.AreEqual(0, map.Count);
    Assert.True(map.isEmpty());
  }

  [Test]
  public void testContains() {
    FastMap<String, String> map = buildTestFastMap();
    Assert.True(map.containsKey("foo"));
    Assert.True(map.containsKey("baz"));
    Assert.True(map.containsKey("alpha"));
    Assert.True(map.containsValue("bar"));
    Assert.True(map.containsValue("bang"));
    Assert.True(map.containsValue("beta"));
    Assert.False(map.containsKey("something"));
    Assert.False(map.containsValue("something"));
  }

  [Test](expected = NullPointerException.class)
  public void testNull1() {
    IDictionary<String, String> map = new FastMap<String, String>();
    Assert.IsNull(map.get(null));
    map.put(null, "bar");
  }

  [Test](expected = NullPointerException.class)
  public void testNull2() {
    IDictionary<String, String> map = new FastMap<String, String>();
    map.put("foo", null);
  }

  [Test]
  public void testRehash() {
    FastMap<String, String> map = buildTestFastMap();
    map.remove("foo");
    map.rehash();
    Assert.IsNull(map.get("foo"));
    Assert.AreEqual("bang", map.get("baz"));
  }

  [Test]
  public void testGrow() {
    IDictionary<String, String> map = new FastMap<String, String>(1, FastMap.NO_MAX_SIZE);
    map.put("foo", "bar");
    map.put("baz", "bang");
    Assert.AreEqual("bar", map.get("foo"));
    Assert.AreEqual("bang", map.get("baz"));
  }

  [Test]
  public void testKeySet() {
    FastMap<String, String> map = buildTestFastMap();
    IEnumerable<String> expected = Sets.newHashSetWithExpectedSize(3);
    expected.add("foo");
    expected.add("baz");
    expected.add("alpha");
    Set<String> actual = map.keySet();
    Assert.True(expected.containsAll(actual));
    Assert.True(actual.containsAll(expected));
    IEnumerable<String> it = actual.iterator();
    while (it.hasNext()) {
      String value = it.next();
      if (!"baz".Equals(value)) {
        it.remove();
      }
    }
    Assert.True(map.containsKey("baz"));
    Assert.False(map.containsKey("foo"));
    Assert.False(map.containsKey("alpha"));
  }

  [Test]
  public void testValues() {
    FastMap<String, String> map = buildTestFastMap();
    IEnumerable<String> expected = Sets.newHashSetWithExpectedSize(3);
    expected.add("bar");
    expected.add("bang");
    expected.add("beta");
    IEnumerable<String> actual = map.values();
    Assert.True(expected.containsAll(actual));
    Assert.True(actual.containsAll(expected));
    IEnumerable<String> it = actual.iterator();
    while (it.hasNext()) {
      String value = it.next();
      if (!"bang".Equals(value)) {
        it.remove();
      }
    }
    Assert.True(map.containsValue("bang"));
    Assert.False(map.containsValue("bar"));
    Assert.False(map.containsValue("beta"));
  }

  [Test]
  public void testEntrySet() {
    FastMap<String, String> map = buildTestFastMap();
    Set<Map.Entry<String, String>> actual = map.entrySet();
    IEnumerable<String> expectedKeys = Sets.newHashSetWithExpectedSize(3);
    expectedKeys.add("foo");
    expectedKeys.add("baz");
    expectedKeys.add("alpha");
    IEnumerable<String> expectedValues = Sets.newHashSetWithExpectedSize(3);
    expectedValues.add("bar");
    expectedValues.add("bang");
    expectedValues.add("beta");
    Assert.AreEqual(3, actual.Count);
    for (Map.Entry<String, String> entry : actual) {
      expectedKeys.remove(entry.getKey());
      expectedValues.remove(entry.getValue());
    }
    Assert.AreEqual(0, expectedKeys.Count);
    Assert.AreEqual(0, expectedValues.Count);
  }

  [Test]
  public void testVersusHashMap() {
    IDictionary<Integer, String> actual = new FastMap<Integer, String>(1, 1000000);
    IDictionary<Integer, String> expected = Maps.newHashMapWithExpectedSize(1000000);
    Random r = RandomUtils.getRandom();
    for (int i = 0; i < 1000000; i++) {
      double d = r.nextDouble();
      Integer key = r.nextInt(100);
      if (d < 0.4) {
        Assert.AreEqual(expected.get(key), actual.get(key));
      } else {
        if (d < 0.7) {
          Assert.AreEqual(expected.put(key, "foo"), actual.put(key, "foo"));
        } else {
          Assert.AreEqual(expected.remove(key), actual.remove(key));
        }
        Assert.AreEqual(expected.Count, actual.Count);
        Assert.AreEqual(expected.isEmpty(), actual.isEmpty());
      }
    }
  }

  [Test]
  public void testMaxSize() {
    IDictionary<String, String> map = new FastMap<String, String>(1, 1);
    map.put("foo", "bar");
    Assert.AreEqual(1, map.Count);
    map.put("baz", "bang");
    Assert.AreEqual(1, map.Count);
    Assert.IsNull(map.get("foo"));
    map.put("baz", "buzz");
    Assert.AreEqual(1, map.Count);
    Assert.AreEqual("buzz", map.get("baz"));
  }

  private static FastMap<String, String> buildTestFastMap() {
    FastMap<String, String> map = new FastMap<String, String>();
    map.put("foo", "bar");
    map.put("baz", "bang");
    map.put("alpha", "beta");
    return map;
  }

}

}