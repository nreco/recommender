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
 *  Parts of this code are based on Apache Mahout and Apache Commons Mathematics Library that were licensed under the
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
using NUnit.Framework;

namespace NReco.Math3.Distribution {

 /// Test cases for PascalDistribution.
 /// Extends IntegerDistributionAbstractTest.  See class javadoc for
 /// IntegerDistributionAbstractTest for details.
 ///
 /// @version $Id: PascalDistributionTest.java 1364028 2012-07-21 00:42:49Z erans $
public class PascalDistributionTest : IntegerDistributionAbstractTest {

    // --------------------- Override tolerance  --------------
	protected double defaultTolerance = 1e-9; //NormalDistribution.DEFAULT_INVERSE_ABSOLUTE_ACCURACY;
    
	[SetUp]
    public override void setUp() {
        base.setUp();
        setTolerance(defaultTolerance);
    }

    //-------------- Implementations for abstract methods -----------------------

    /// Creates the default discrete distribution instance to use in tests. */
    public override AbstractIntegerDistribution makeDistribution() {
        return new PascalDistribution(10,0.70);
    }

    /// Creates the default probability density test input values */
    public override int[] makeDensityTestPoints() {
      return new int[] {-1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11};
    }

    /// Creates the default probability density test expected values */
    public override double[] makeDensityTestValues() {
      return new double[] {0, 0.0282475249, 0.0847425747, 0.139825248255, 0.167790297906, 0.163595540458,
              0.137420253985, 0.103065190489, 0.070673273478, 0.0450542118422, 0.0270325271053,
              0.0154085404500, 0.0084046584273};
    }

    /// Creates the default cumulative probability density test input values */
    public override int[] makeCumulativeTestPoints() {
      return makeDensityTestPoints();
    }

    /// Creates the default cumulative probability density test expected values */
    public override double[] makeCumulativeTestValues() {
      return new double[] {0, 0.0282475249, 0.1129900996, 0.252815347855, 0.420605645761, 0.584201186219,
              0.721621440204, 0.824686630693, 0.895359904171, 0.940414116013, 0.967446643119,
              0.982855183569, 0.991259841996};
        }

    /// Creates the default inverse cumulative probability test input values */
    public override double[] makeInverseCumulativeTestPoints() {
      return new double[] {0.0, 0.001, 0.010, 0.025, 0.050, 0.100, 0.999,
          0.990, 0.975, 0.950, 0.900, 1.0};
        }

    /// Creates the default inverse cumulative probability density test expected values */
    public override int[] makeInverseCumulativeTestValues() {
      return new int[] {0, 0, 0, 0, 1, 1, 14, 11, 10, 9, 8, Int32.MaxValue};
    }

    //----------------- Additional test cases ---------------------------------

    /// Test degenerate case p = 0   */
    [Test]
    public void testDegenerate0() {
        setDistribution(new PascalDistribution(5, 0.0d));
        setCumulativeTestPoints(new int[] {-1, 0, 1, 5, 10 });
        setCumulativeTestValues(new double[] {0d, 0d, 0d, 0d, 0d});
        setDensityTestPoints(new int[] {-1, 0, 1, 10, 11});
        setDensityTestValues(new double[] {0d, 0d, 0d, 0d, 0d});
        setInverseCumulativeTestPoints(new double[] {0.1d, 0.5d});
        setInverseCumulativeTestValues(new int[] {Int32.MaxValue, Int32.MaxValue});
        verifyDensities();
        verifyCumulativeProbabilities();
        verifyInverseCumulativeProbabilities();
    }

    /// Test degenerate case p = 1   */
    [Test]
    public void testDegenerate1() {
        setDistribution(new PascalDistribution(5, 1.0d));
        setCumulativeTestPoints(new int[] {-1, 0, 1, 2, 5, 10 });
        setCumulativeTestValues(new double[] {0d, 1d, 1d, 1d, 1d, 1d});
        setDensityTestPoints(new int[] {-1, 0, 1, 2, 5, 10});
        setDensityTestValues(new double[] {0d, 1d, 0d, 0d, 0d, 0d});
        setInverseCumulativeTestPoints(new double[] {0.1d, 0.5d});
        setInverseCumulativeTestValues(new int[] {0, 0});
        verifyDensities();
        verifyCumulativeProbabilities();
        verifyInverseCumulativeProbabilities();
    }

    [Test]
    public void testMoments() {
        double tol = 1e-9;
        PascalDistribution dist;

        dist = new PascalDistribution(10, 0.5);
        Assert.AreEqual(dist.getNumericalMean(), ( 10d * 0.5d ) / 0.5d, tol);
        Assert.AreEqual(dist.getNumericalVariance(), ( 10d * 0.5d ) / (0.5d * 0.5d), tol);

        dist = new PascalDistribution(25, 0.7);
        Assert.AreEqual(dist.getNumericalMean(), ( 25d * 0.3d ) / 0.7d, tol);
        Assert.AreEqual(dist.getNumericalVariance(), ( 25d * 0.3d ) / (0.7d * 0.7d), tol);
    }
}


}