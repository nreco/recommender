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
using NReco.Math3;
using NReco.Math3.Exception;
using NReco.Math3.Util;
using NUnit.Framework;

namespace NReco.Math3.Distribution {

 /// Abstract base class for {@link IntegerDistribution} tests.
 /// <p>
 /// To create a concrete test class for an integer distribution implementation,
 ///  implement makeDistribution() to return a distribution instance to use in
 ///  tests and each of the test data generation methods below.  In each case, the
 ///  test points and test values arrays returned represent parallel arrays of
 ///  inputs and expected values for the distribution returned by makeDistribution().
 ///  <p>
 ///  makeDensityTestPoints() -- arguments used to test probability density calculation
 ///  makeDensityTestValues() -- expected probability densities
 ///  makeCumulativeTestPoints() -- arguments used to test cumulative probabilities
 ///  makeCumulativeTestValues() -- expected cumulative probabilites
 ///  makeInverseCumulativeTestPoints() -- arguments used to test inverse cdf evaluation
 ///  makeInverseCumulativeTestValues() -- expected inverse cdf values
 /// <p>
 ///  To implement additional test cases with different distribution instances and test data,
 ///  use the setXxx methods for the instance data in test cases and call the verifyXxx methods
 ///  to verify results.
 ///
 /// @version $Id: IntegerDistributionAbstractTest.java 1533974 2013-10-20 20:42:41Z psteitz $
public abstract class IntegerDistributionAbstractTest {

//-------------------- Private test instance data -------------------------
    /// Discrete distribution instance used to perform tests */
    private AbstractIntegerDistribution distribution;

    /// Tolerance used in comparing expected and returned values */
    private double tolerance = 1E-12;

    /// Arguments used to test probability density calculations */
    private int[] densityTestPoints;

    /// Values used to test probability density calculations */
    private double[] densityTestValues;

    /// Values used to test logarithmic probability density calculations */
    private double[] logDensityTestValues;

    /// Arguments used to test cumulative probability density calculations */
    private int[] cumulativeTestPoints;

    /// Values used to test cumulative probability density calculations */
    private double[] cumulativeTestValues;

    /// Arguments used to test inverse cumulative probability density calculations */
    private double[] inverseCumulativeTestPoints;

    /// Values used to test inverse cumulative probability density calculations */
    private int[] inverseCumulativeTestValues;

    //-------------------- Abstract methods -----------------------------------

    /// Creates the default discrete distribution instance to use in tests. */
    public abstract AbstractIntegerDistribution makeDistribution();

    /// Creates the default probability density test input values */
    public abstract int[] makeDensityTestPoints();

    /// Creates the default probability density test expected values */
    public abstract double[] makeDensityTestValues();

    /// Creates the default logarithmic probability density test expected values.
     ///
     /// The default implementation simply computes the logarithm of all the values in
     /// {@link #makeDensityTestValues()}.
     ///
     /// @return double[] the default logarithmic probability density test expected values.
    public double[] makeLogDensityTestValues() {
        double[] densityTestValues = makeDensityTestValues();
        double[] logDensityTestValues = new double[densityTestValues.Length];
        for (int i = 0; i < densityTestValues.Length; i++) {
            logDensityTestValues[i] = MathUtil.Log(densityTestValues[i]);
        }
        return logDensityTestValues;
    }

    /// Creates the default cumulative probability density test input values */
    public abstract int[] makeCumulativeTestPoints();

    /// Creates the default cumulative probability density test expected values */
    public abstract double[] makeCumulativeTestValues();

    /// Creates the default inverse cumulative probability test input values */
    public abstract double[] makeInverseCumulativeTestPoints();

    /// Creates the default inverse cumulative probability density test expected values */
    public abstract int[] makeInverseCumulativeTestValues();

    //-------------------- Setup / tear down ----------------------------------

     /// Setup sets all test instance data to default values
    [SetUp]
    public virtual void setUp() {
        distribution = makeDistribution();
        densityTestPoints = makeDensityTestPoints();
        densityTestValues = makeDensityTestValues();
        logDensityTestValues = makeLogDensityTestValues();
        cumulativeTestPoints = makeCumulativeTestPoints();
        cumulativeTestValues = makeCumulativeTestValues();
        inverseCumulativeTestPoints = makeInverseCumulativeTestPoints();
        inverseCumulativeTestValues = makeInverseCumulativeTestValues();
    }

     /// Cleans up test instance data
    [TearDown]
    public virtual void tearDown() {
        distribution = null;
        densityTestPoints = null;
        densityTestValues = null;
        logDensityTestValues = null;
        cumulativeTestPoints = null;
        cumulativeTestValues = null;
        inverseCumulativeTestPoints = null;
        inverseCumulativeTestValues = null;
    }

    //-------------------- Verification methods -------------------------------

     /// Verifies that probability density calculations match expected values
     /// using current test instance data
    protected void verifyDensities() {
        for (int i = 0; i < densityTestPoints.Length; i++) {
            Assert.AreEqual(
                    densityTestValues[i],
                    distribution.probability(densityTestPoints[i]), getTolerance(),
					"Incorrect density value returned for " + densityTestPoints[i]);
        }
    }

     /// Verifies that logarithmic probability density calculations match expected values
     /// using current test instance data.
    protected void verifyLogDensities() {
        for (int i = 0; i < densityTestPoints.Length; i++) {
            // FIXME: when logProbability methods are added to IntegerDistribution in 4.0, remove cast below
            Assert.AreEqual(logDensityTestValues[i],
                    ((AbstractIntegerDistribution) distribution).logProbability(densityTestPoints[i]),
					tolerance,
					"Incorrect log density value returned for " + densityTestPoints[i]);
        }
    }

     /// Verifies that cumulative probability density calculations match expected values
     /// using current test instance data
    protected void verifyCumulativeProbabilities() {
        for (int i = 0; i < cumulativeTestPoints.Length; i++) {
            Assert.AreEqual(
                    cumulativeTestValues[i],
                    distribution.cumulativeProbability(cumulativeTestPoints[i]), getTolerance(),
					"Incorrect cumulative probability value returned for " + cumulativeTestPoints[i]);
        }
    }


     /// Verifies that inverse cumulative probability density calculations match expected values
     /// using current test instance data
    protected void verifyInverseCumulativeProbabilities() {
        for (int i = 0; i < inverseCumulativeTestPoints.Length; i++) {
            Assert.AreEqual( inverseCumulativeTestValues[i],
                    distribution.inverseCumulativeProbability(inverseCumulativeTestPoints[i]),
					"Incorrect inverse cumulative probability value returned for "
                    + inverseCumulativeTestPoints[i]);
        }
    }

    //------------------------ Default test cases -----------------------------

     /// Verifies that probability density calculations match expected values
     /// using default test instance data
    [Test]
    public void testDensities() {
        verifyDensities();
    }

     /// Verifies that logarithmic probability density calculations match expected values
     /// using default test instance data
    [Test]
    public void testLogDensities() {
        verifyLogDensities();
    }

     /// Verifies that cumulative probability density calculations match expected values
     /// using default test instance data
    [Test]
    public void testCumulativeProbabilities() {
        verifyCumulativeProbabilities();
    }

     /// Verifies that inverse cumulative probability density calculations match expected values
     /// using default test instance data
    [Test]
    public void testInverseCumulativeProbabilities() {
        verifyInverseCumulativeProbabilities();
    }

    [Test]
    public void testConsistencyAtSupportBounds() {
        int lower = distribution.getSupportLowerBound();
        Assert.AreEqual(
                0.0, distribution.cumulativeProbability(lower - 1), 0.0,
				"Cumulative probability mmust be 0 below support lower bound.");
        Assert.AreEqual(
                distribution.probability(lower), distribution.cumulativeProbability(lower), getTolerance(),
				"Cumulative probability of support lower bound must be equal to probability mass at this point.");
        Assert.AreEqual(
                lower, distribution.inverseCumulativeProbability(0.0),
				"Inverse cumulative probability of 0 must be equal to support lower bound.");

        int upper = distribution.getSupportUpperBound();
        if (upper != Int32.MaxValue)
            Assert.AreEqual(
                    1.0, distribution.cumulativeProbability(upper), 0.0,
					"Cumulative probability of support upper bound must be equal to 1.");
        Assert.AreEqual(
                upper, distribution.inverseCumulativeProbability(1.0),
				"Inverse cumulative probability of 1 must be equal to support upper bound.");
    }

     /// Verifies that illegal arguments are correctly handled
    [Test]
    public void testIllegalArguments() {
        try {
            distribution.cumulativeProbability(1, 0);
            Assert.Fail("Expecting MathIllegalArgumentException for bad cumulativeProbability interval");
        } catch (ArgumentException ex) {
            // expected
        }
        try {
            distribution.inverseCumulativeProbability(-1);
            Assert.Fail("Expecting MathIllegalArgumentException for p = -1");
        } catch (ArgumentException ex) {
            // expected
        }
        try {
            distribution.inverseCumulativeProbability(2);
            Assert.Fail("Expecting MathIllegalArgumentException for p = 2");
        } catch (ArgumentException ex) {
            // expected
        }
    }

     /// Test sampling
	/*[Test]  -- assertChiSquareAccept not ported*/ 
    public void testSampling() {
        int[] densityPoints = makeDensityTestPoints();
        double[] densityValues = makeDensityTestValues();
        int sampleSize = 1000;
		int length = eliminateZeroMassPoints(densityPoints, densityValues);//TestUtils
        AbstractIntegerDistribution distribution = (AbstractIntegerDistribution) makeDistribution();
        double[] expectedCounts = new double[length];
        long[] observedCounts = new long[length];
        for (int i = 0; i < length; i++) {
            expectedCounts[i] = sampleSize * densityValues[i];
        }
        distribution.reseedRandomGenerator(1000); // Use fixed seed
        int[] sample = distribution.sample(sampleSize);
        for (int i = 0; i < sampleSize; i++) {
          for (int j = 0; j < length; j++) {
              if (sample[i] == densityPoints[j]) {
                  observedCounts[j]++;
              }
          }
        }
		// NOT PORTED:
        //TestUtils.assertChiSquareAccept(densityPoints, expectedCounts, observedCounts, .001);
    }


	public static int eliminateZeroMassPoints(int[] densityPoints, double[] densityValues) {
		int positiveMassCount = 0;
		for (int i = 0; i < densityValues.Length; i++) {
			if (densityValues[i] > 0) {
				positiveMassCount++;
			}
		}
		if (positiveMassCount < densityValues.Length) {
			int[] newPoints = new int[positiveMassCount];
			double[] newValues = new double[positiveMassCount];
			int j = 0;
			for (int i = 0; i < densityValues.Length; i++) {
				if (densityValues[i] > 0) {
					newPoints[j] = densityPoints[i];
					newValues[j] = densityValues[i];
					j++;
				}
			}
			Array.Copy(newPoints, 0, densityPoints, 0, positiveMassCount);
			Array.Copy(newValues, 0, densityValues, 0, positiveMassCount);
		}
		return positiveMassCount;
	}


    //------------------ Getters / Setters for test instance data -----------
     /// @return Returns the cumulativeTestPoints.
    protected int[] getCumulativeTestPoints() {
        return cumulativeTestPoints;
    }

     /// @param cumulativeTestPoints The cumulativeTestPoints to set.
    protected void setCumulativeTestPoints(int[] cumulativeTestPoints) {
        this.cumulativeTestPoints = cumulativeTestPoints;
    }

     /// @return Returns the cumulativeTestValues.
    protected double[] getCumulativeTestValues() {
        return cumulativeTestValues;
    }

     /// @param cumulativeTestValues The cumulativeTestValues to set.
    protected void setCumulativeTestValues(double[] cumulativeTestValues) {
        this.cumulativeTestValues = cumulativeTestValues;
    }

     /// @return Returns the densityTestPoints.
    protected int[] getDensityTestPoints() {
        return densityTestPoints;
    }

     /// @param densityTestPoints The densityTestPoints to set.
    protected void setDensityTestPoints(int[] densityTestPoints) {
        this.densityTestPoints = densityTestPoints;
    }

     /// @return Returns the densityTestValues.
    protected double[] getDensityTestValues() {
        return densityTestValues;
    }

     /// @param densityTestValues The densityTestValues to set.
    protected void setDensityTestValues(double[] densityTestValues) {
        this.densityTestValues = densityTestValues;
    }

     /// @return Returns the distribution.
    protected AbstractIntegerDistribution getDistribution() {
        return distribution;
    }

     /// @param distribution The distribution to set.
	protected void setDistribution(AbstractIntegerDistribution distribution) {
        this.distribution = distribution;
    }

     /// @return Returns the inverseCumulativeTestPoints.
    protected double[] getInverseCumulativeTestPoints() {
        return inverseCumulativeTestPoints;
    }

     /// @param inverseCumulativeTestPoints The inverseCumulativeTestPoints to set.
    protected void setInverseCumulativeTestPoints(double[] inverseCumulativeTestPoints) {
        this.inverseCumulativeTestPoints = inverseCumulativeTestPoints;
    }

     /// @return Returns the inverseCumulativeTestValues.
    protected int[] getInverseCumulativeTestValues() {
        return inverseCumulativeTestValues;
    }

     /// @param inverseCumulativeTestValues The inverseCumulativeTestValues to set.
    protected void setInverseCumulativeTestValues(int[] inverseCumulativeTestValues) {
        this.inverseCumulativeTestValues = inverseCumulativeTestValues;
    }

     /// @return Returns the tolerance.
    protected double getTolerance() {
        return tolerance;
    }

     /// @param tolerance The tolerance to set.
    protected void setTolerance(double tolerance) {
        this.tolerance = tolerance;
    }

}


}