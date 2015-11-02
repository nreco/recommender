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


public sealed class RunningAverageAndStdDevTest : TasteTestCase {

  private static double SMALL_EPSILON = 1.0;

  [Test]
  public void testFull() {
    IRunningAverageAndStdDev average = new FullRunningAverageAndStdDev();

    Assert.AreEqual(0, average.GetCount());
    Assert.True(Double.IsNaN(average.GetAverage()));
    Assert.True(Double.IsNaN(average.GetStandardDeviation()));

    average.AddDatum(6.0);
    Assert.AreEqual(1, average.GetCount());
    Assert.AreEqual(6.0, average.GetAverage(), EPSILON);
    Assert.True(Double.IsNaN(average.GetStandardDeviation()));

    average.AddDatum(6.0);
    Assert.AreEqual(2, average.GetCount());
    Assert.AreEqual(6.0, average.GetAverage(), EPSILON);
    Assert.AreEqual(0.0, average.GetStandardDeviation(), EPSILON);

    average.RemoveDatum(6.0);
    Assert.AreEqual(1, average.GetCount());
    Assert.AreEqual(6.0, average.GetAverage(), EPSILON);
    Assert.True(Double.IsNaN(average.GetStandardDeviation()));

    average.AddDatum(-4.0);
    Assert.AreEqual(2, average.GetCount());
    Assert.AreEqual(1.0, average.GetAverage(), EPSILON);
    Assert.AreEqual(5.0 * 1.4142135623730951, average.GetStandardDeviation(), EPSILON);

    average.RemoveDatum(4.0);
    Assert.AreEqual(1, average.GetCount());
    Assert.AreEqual(-2.0, average.GetAverage(), EPSILON);
    Assert.True(Double.IsNaN(average.GetStandardDeviation()));

  }

  [Test]
  public void testFullBig() {
    IRunningAverageAndStdDev average = new FullRunningAverageAndStdDev();

	var r = RandomUtils.getRandom();
    for (int i = 0; i < 1000000; i++) {
      average.AddDatum(r.nextDouble() * 1000.0);
    }
    Assert.AreEqual(500.0, average.GetAverage(), SMALL_EPSILON);
    Assert.AreEqual(1000.0 / Math.Sqrt(12.0), average.GetStandardDeviation(), SMALL_EPSILON);

  }
  
  [Test]
  public void testStddev() {
    
    IRunningAverageAndStdDev runningAverage = new FullRunningAverageAndStdDev();

    Assert.AreEqual(0, runningAverage.GetCount());
    Assert.True(Double.IsNaN(runningAverage.GetAverage()));
    runningAverage.AddDatum(1.0);
    Assert.AreEqual(1, runningAverage.GetCount());
    Assert.AreEqual(1.0, runningAverage.GetAverage(), EPSILON);
    Assert.True(Double.IsNaN(runningAverage.GetStandardDeviation()));
    runningAverage.AddDatum(1.0);
    Assert.AreEqual(2, runningAverage.GetCount());
    Assert.AreEqual(1.0, runningAverage.GetAverage(), EPSILON);
    Assert.AreEqual(0.0, runningAverage.GetStandardDeviation(), EPSILON);

    runningAverage.AddDatum(7.0);
    Assert.AreEqual(3, runningAverage.GetCount());
    Assert.AreEqual(3.0, runningAverage.GetAverage(), EPSILON); 
    Assert.AreEqual(3.464101552963257, runningAverage.GetStandardDeviation(), EPSILON);

    runningAverage.AddDatum(5.0);
    Assert.AreEqual(4, runningAverage.GetCount());
    Assert.AreEqual(3.5, runningAverage.GetAverage(), EPSILON); 
    Assert.AreEqual(3.0, runningAverage.GetStandardDeviation(), EPSILON);

  }
  

}

}