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
using NReco.CF.Taste.Common;
using NReco.CF.Taste.Impl;
using NReco.CF;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Common {


public sealed class CacheTest : TasteTestCase {

  [Test]
  public void testLotsOfGets() {
    IRetriever<Object,Object> retriever = new IdentityRetriever();
    Cache<Object,Object> cache = new Cache<Object,Object>(retriever, 1000);
    for (int i = 0; i < 1000000; i++) {
      Assert.AreEqual(i, cache.Get(i));
    }
  }

  [Test]
  public void testMixedUsage() {
    var random = RandomUtils.getRandom();
    IRetriever<Object,Object> retriever = new IdentityRetriever();
    Cache<Object,Object> cache = new Cache<Object,Object>(retriever, 1000);
    for (int i = 0; i < 1000000; i++) {
      double r = random.nextDouble();
      if (r < 0.01) {
        cache.Clear();
      } else if (r < 0.1) {
        cache.Remove(r - 100);
      } else {
        Assert.AreEqual(i, cache.Get(i));
      }
    }
  }
  
  private class IdentityRetriever : IRetriever<Object,Object> {
    public Object Get(Object key) {
      return key;
    }
  }
}
}