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
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Common {


/// Tests {@link RefreshHelper} 
public sealed class RefreshHelperTest : TasteTestCase {

  [Test]
  public void testCallable() {
    MockRefreshable mock = new MockRefreshable();
    IRefreshable helper = new RefreshHelper(mock.call);
    helper.Refresh(null);
    Assert.AreEqual(1, mock.getCallCount());
  }

  [Test]
  public void testNoCallable() {
    IRefreshable helper = new RefreshHelper(null);
    helper.Refresh(null);
  }

  [Test]
  public void testDependencies() {
    RefreshHelper helper = new RefreshHelper(null);
    MockRefreshable mock1 = new MockRefreshable();
    MockRefreshable mock2 = new MockRefreshable();
    helper.AddDependency(mock1);
    helper.AddDependency(mock2);
    helper.Refresh(null);
    Assert.AreEqual(1, mock1.getCallCount());
    Assert.AreEqual(1, mock2.getCallCount());
  }

  [Test]
  public void testAlreadyRefreshed() {
    RefreshHelper helper = new RefreshHelper(null);
    MockRefreshable mock1 = new MockRefreshable();
    MockRefreshable mock2 = new MockRefreshable();
    helper.AddDependency(mock1);
    helper.AddDependency(mock2);
    IList<IRefreshable> alreadyRefreshed = new List<IRefreshable>(1);
    alreadyRefreshed.Add(mock1);
    helper.Refresh(alreadyRefreshed);
    Assert.AreEqual(0, mock1.getCallCount());
    Assert.AreEqual(1, mock2.getCallCount());
  }

}

}