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

 /// <p>Tests {@link WeightedRunningAverage} and {@link WeightedRunningAverageAndStdDev}.</p>
public sealed class WeightedRunningAverageTest : TasteTestCase {

  [Test]
  public void testWeighted() {

    WeightedRunningAverage runningAverage = new WeightedRunningAverage();

    Assert.AreEqual(0, runningAverage.GetCount());
    Assert.True(Double.IsNaN(runningAverage.GetAverage()));
    runningAverage.AddDatum(1.0, 2.0);
    Assert.AreEqual(1.0, runningAverage.GetAverage(), EPSILON);
    runningAverage.AddDatum(1.0);
    Assert.AreEqual(1.0, runningAverage.GetAverage(), EPSILON);
    runningAverage.AddDatum(8.0, 0.5);
    Assert.AreEqual(2.0, runningAverage.GetAverage(), EPSILON);
    runningAverage.AddDatum(-4.0);
    Assert.AreEqual(2.0/3.0, runningAverage.GetAverage(), EPSILON);

    runningAverage.RemoveDatum(-4.0);
    Assert.AreEqual(2.0, runningAverage.GetAverage(), EPSILON);
    runningAverage.RemoveDatum(2.0, 2.0);
    Assert.AreEqual(2.0, runningAverage.GetAverage(), EPSILON);

    runningAverage.ChangeDatum(0.0);
    Assert.AreEqual(2.0, runningAverage.GetAverage(), EPSILON);
    runningAverage.ChangeDatum(4.0, 0.5);
    Assert.AreEqual(5.0/1.5, runningAverage.GetAverage(), EPSILON);
  }

  [Test]
  public void testWeightedAndStdDev() {

    WeightedRunningAverageAndStdDev runningAverage = new WeightedRunningAverageAndStdDev();

    Assert.AreEqual(0, runningAverage.GetCount());
    Assert.True(Double.IsNaN(runningAverage.GetAverage()));
    Assert.True(Double.IsNaN(runningAverage.GetStandardDeviation()));

    runningAverage.AddDatum(1.0);
    Assert.AreEqual(1.0, runningAverage.GetAverage(), EPSILON);
    Assert.True(Double.IsNaN(runningAverage.GetStandardDeviation()));
    runningAverage.AddDatum(1.0, 2.0);
    Assert.AreEqual(1.0, runningAverage.GetAverage(), EPSILON);
    Assert.AreEqual(0.0, runningAverage.GetStandardDeviation(), EPSILON);
    runningAverage.AddDatum(8.0, 0.5);
    Assert.AreEqual(2.0, runningAverage.GetAverage(), EPSILON);
    Assert.AreEqual(Math.Sqrt(10.5), runningAverage.GetStandardDeviation(), EPSILON);
    runningAverage.AddDatum(-4.0);
    Assert.AreEqual(2.0/3.0, runningAverage.GetAverage(), EPSILON);
    Assert.AreEqual(Math.Sqrt(15.75), runningAverage.GetStandardDeviation(), EPSILON);

    runningAverage.RemoveDatum(-4.0);
    Assert.AreEqual(2.0, runningAverage.GetAverage(), EPSILON);
    Assert.AreEqual(Math.Sqrt(10.5), runningAverage.GetStandardDeviation(), EPSILON);
    runningAverage.RemoveDatum(2.0, 2.0);
    Assert.AreEqual(2.0, runningAverage.GetAverage(), EPSILON);
    Assert.AreEqual(Math.Sqrt(31.5), runningAverage.GetStandardDeviation(), EPSILON);
  }

}

}