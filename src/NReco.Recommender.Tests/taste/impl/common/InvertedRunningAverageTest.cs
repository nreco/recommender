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

public sealed class InvertedRunningAverageTest : TasteTestCase {

  [Test]
  public void testAverage() {
    IRunningAverage avg = new FullRunningAverage();
    IRunningAverage inverted = new InvertedRunningAverage(avg);
    Assert.AreEqual(0, inverted.GetCount());
    avg.AddDatum(1.0);
    Assert.AreEqual(1, inverted.GetCount());
    Assert.AreEqual(-1.0, inverted.GetAverage(), EPSILON);
    avg.AddDatum(2.0);
    Assert.AreEqual(2, inverted.GetCount());
    Assert.AreEqual(-1.5, inverted.GetAverage(), EPSILON);
  }

  [Test]
  [ExpectedException(typeof(NotSupportedException))]
  public void testUnsupported1() {
    IRunningAverage inverted = new InvertedRunningAverage(new FullRunningAverage());
    inverted.AddDatum(1.0);
  }

  [Test]
[ExpectedException(typeof(NotSupportedException))]
  public void testUnsupported2() {
    IRunningAverage inverted = new InvertedRunningAverage(new FullRunningAverage());
    inverted.ChangeDatum(1.0);
  }

  [Test]
	[ExpectedException(typeof(NotSupportedException))]
  public void testUnsupported3() {
    IRunningAverage inverted = new InvertedRunningAverage(new FullRunningAverage());
    inverted.RemoveDatum(1.0);
  }

  [Test]
  public void testAverageAndStdDev() {
    IRunningAverageAndStdDev avg = new FullRunningAverageAndStdDev();
    IRunningAverageAndStdDev inverted = new InvertedRunningAverageAndStdDev(avg);
    Assert.AreEqual(0, inverted.GetCount());
    avg.AddDatum(1.0);
    Assert.AreEqual(1, inverted.GetCount());
    Assert.AreEqual(-1.0, inverted.GetAverage(), EPSILON);
    avg.AddDatum(2.0);
    Assert.AreEqual(2, inverted.GetCount());
    Assert.AreEqual(-1.5, inverted.GetAverage(), EPSILON);
    Assert.AreEqual(Math.Sqrt(2.0)/2.0, inverted.GetStandardDeviation(), EPSILON);
  }

  [Test]
	[ExpectedException(typeof(NotSupportedException))]
  public void testAndStdDevUnsupported1() {
    IRunningAverage inverted = new InvertedRunningAverageAndStdDev(new FullRunningAverageAndStdDev());
    inverted.AddDatum(1.0);
  }

  [Test]
	[ExpectedException(typeof(NotSupportedException))]
  public void testAndStdDevUnsupported2() {
    IRunningAverage inverted = new InvertedRunningAverageAndStdDev(new FullRunningAverageAndStdDev());
    inverted.ChangeDatum(1.0);
  }

  [Test]
  [ExpectedException(typeof(NotSupportedException))]
  public void testAndStdDevUnsupported3() {
    IRunningAverage inverted = new InvertedRunningAverageAndStdDev(new FullRunningAverageAndStdDev());
    inverted.RemoveDatum(1.0);
  }

}
}