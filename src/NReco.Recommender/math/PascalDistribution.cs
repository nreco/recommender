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
using NReco.Math3.Exception;
using NReco.Math3.Special;
using NReco.Math3.Util;
using NReco.Math3.Random;

using NReco.CF.Taste;

namespace NReco.Math3.Distribution {

 /// <summary>
 /// Implementation of the Pascal distribution. The Pascal distribution is a
 /// special case of the Negative Binomial distribution where the number of
 /// successes parameter is an integer.
 /// </summary>
 /// <p>
 /// There are various ways to express the probability mass and distribution
 /// functions for the Pascal distribution. The present implementation represents
 /// the distribution of the number of failures before {@code r} successes occur.
 /// This is the convention adopted in e.g.
 /// <a href="http://mathworld.wolfram.com/NegativeBinomialDistribution.html">MathWorld</a>,
 /// but <em>not</em> in
 /// <a href="http://en.wikipedia.org/wiki/Negative_binomial_distribution">Wikipedia</a>.
 /// </p>
 /// <p>
 /// For a random variable {@code X} whose values are distributed according to this
 /// distribution, the probability mass function is given by<br/>
 /// {@code P(X = k) = C(k + r - 1, r - 1) * p^r * (1 - p)^k,}<br/>
 /// where {@code r} is the number of successes, {@code p} is the probability of
 /// success, and {@code X} is the total number of failures. {@code C(n, k)} is
 /// the binomial coefficient ({@code n} choose {@code k}). The mean and variance
 /// of {@code X} are<br/>
 /// {@code E(X) = (1 - p) * r / p, var(X) = (1 - p) * r / p^2.}<br/>
 /// Finally, the cumulative distribution function is given by<br/>
 /// {@code P(X <= k) = I(p, r, k + 1)},
 /// where I is the regularized incomplete Beta function.
 /// </p>
 ///
 /// @see <a href="http://en.wikipedia.org/wiki/Negative_binomial_distribution">
 /// Negative binomial distribution (Wikipedia)</a>
 /// @see <a href="http://mathworld.wolfram.com/NegativeBinomialDistribution.html">
 /// Negative binomial distribution (MathWorld)</a>
 /// @version $Id: PascalDistribution.java 1533974 2013-10-20 20:42:41Z psteitz $
 /// @since 1.2 (changed to concrete class in 3.0)
public class PascalDistribution : AbstractIntegerDistribution {
    /// Serializable version identifier. */
    private static long serialVersionUID = 6751309484392813623L;
    /// The number of successes. */
    private int numberOfSuccesses;
    /// The probability of success. */
    private double probabilityOfSuccess;
    /// The value of {@code log(p)}, where {@code p} is the probability of success,
     /// stored for faster computation. */
    private double logProbabilityOfSuccess;
    /// The value of {@code log(1-p)}, where {@code p} is the probability of success,
     /// stored for faster computation. */
    private double log1mProbabilityOfSuccess;


    /**
     * Create a Pascal distribution with the given number of successes and
     * probability of success.
     *
     * @param r Number of successes.
     * @param p Probability of success.
     * @throws NotStrictlyPositiveException if the number of successes is not positive
     * @throws OutOfRangeException if the probability of success is not in the
     * range {@code [0, 1]}.
     */
    public PascalDistribution(int r, double p) : this(new MersenneTwister(), r, p) {
    }

     /// Create a Pascal distribution with the given number of successes and
     /// probability of success.
     ///
     /// @param rng Random number generator.
     /// @param r Number of successes.
     /// @param p Probability of success.
     /// @throws NotStrictlyPositiveException if the number of successes is not positive
     /// @throws OutOfRangeException if the probability of success is not in the
     /// range {@code [0, 1]}.
     /// @since 3.1
    public PascalDistribution(IRandomGenerator rng,
                              int r,
                              double p) : base(rng)
        {

        if (r <= 0) {
            throw new NotStrictlyPositiveException(r);
        }
        if (p < 0 || p > 1) {
            throw new ArgumentOutOfRangeException("p", p, "(0, 1)");
        }

        numberOfSuccesses = r;
        probabilityOfSuccess = p;
        logProbabilityOfSuccess = MathUtil.Log(p);
        log1mProbabilityOfSuccess = MathUtil.Log1p(-p);  //.log1p(-p);
    }

     /// Access the number of successes for this distribution.
     ///
     /// @return the number of successes.
    public int getNumberOfSuccesses() {
        return numberOfSuccesses;
    }

     /// Access the probability of success for this distribution.
     ///
     /// @return the probability of success.
    public double getProbabilityOfSuccess() {
        return probabilityOfSuccess;
    }

    /** {@inheritDoc} */
    public override double probability(int x) {
        double ret;
        if (x < 0) {
            ret = 0.0;
        } else {
            ret = binomialCoefficientDouble(x +
                  numberOfSuccesses - 1, numberOfSuccesses - 1) *
                  Math.Pow(probabilityOfSuccess, numberOfSuccesses) *
                  Math.Pow(1.0 - probabilityOfSuccess, x);
        }
        return ret;
    }

    /** {@inheritDoc} */
    public override double logProbability(int x) {
        double ret;
        if (x < 0) {
            ret = Double.NegativeInfinity;
        } else {
            ret = binomialCoefficientLog(x +
                  numberOfSuccesses - 1, numberOfSuccesses - 1) +
                  logProbabilityOfSuccess * numberOfSuccesses +
                  log1mProbabilityOfSuccess * x;
        }
        return ret;
    }


    /// {@inheritDoc} */
    public override double cumulativeProbability(int x) {
        double ret;
        if (x < 0) {
            ret = 0.0;
        } else {
            ret = Beta.regularizedBeta(probabilityOfSuccess,
                    numberOfSuccesses, x + 1.0);
        }
        return ret;
    }

     /// {@inheritDoc}
     ///
     /// For number of successes {@code r} and probability of success {@code p},
     /// the mean is {@code r * (1 - p) / p}.
    public override double getNumericalMean() {
        double p = getProbabilityOfSuccess();
        double r = getNumberOfSuccesses();
        return (r * (1 - p)) / p;
    }

     /// {@inheritDoc}
     ///
     /// For number of successes {@code r} and probability of success {@code p},
     /// the variance is {@code r * (1 - p) / p^2}.
    public override double getNumericalVariance() {
        double p = getProbabilityOfSuccess();
        double r = getNumberOfSuccesses();
        return r * (1 - p) / (p * p);
    }

     /// {@inheritDoc}
     ///
     /// The lower bound of the support is always 0 no matter the parameters.
     ///
     /// @return lower bound of the support (always 0)
    public override int getSupportLowerBound() {
        return 0;
    }

     /// {@inheritDoc}
     ///
     /// The upper bound of the support is always positive infinity no matter the
     /// parameters. Positive infinity is symbolized by {@code Integer.MAX_VALUE}.
     ///
     /// @return upper bound of the support (always {@code Integer.MAX_VALUE}
     /// for positive infinity)
    public override int getSupportUpperBound() {
        return Int32.MaxValue;
    }

     /// {@inheritDoc}
     ///
     /// The support of this distribution is connected.
     ///
     /// @return {@code true}
    public bool isSupportConnected() {
        return true;
    }


   /**
     * Returns the natural {@code log} of the <a
     * href="http://mathworld.wolfram.com/BinomialCoefficient.html"> Binomial
     * Coefficient</a>, "{@code n choose k}", the number of
     * {@code k}-element subsets that can be selected from an
     * {@code n}-element set.
     * <p>
     * <Strong>Preconditions</strong>:
     * <ul>
     * <li> {@code 0 <= k <= n } (otherwise
     * {@code IllegalArgumentException} is thrown)</li>
     * </ul></p>
     *
     * @param n the size of the set
     * @param k the size of the subsets to be counted
     * @return {@code n choose k}
     * @throws NotPositiveException if {@code n < 0}.
     * @throws NumberIsTooLargeException if {@code k > n}.
     * @throws MathArithmeticException if the result is too large to be
     * represented by a long integer.
     */
    public double binomialCoefficientLog(int n, int k) {
        //CombinatoricsUtils.checkBinomial(n, k);
        if ((n == k) || (k == 0)) {
            return 0;
        }
        if ((k == 1) || (k == n - 1)) {
            return MathUtil.Log(n);
        }

        /*
         * For values small enough to do exact integer computation,
         * return the log of the exact value
         */
        if (n < 67) {
            return MathUtil.Log(binomialCoefficient(n,k));
        }

        /*
         * Return the log of binomialCoefficientDouble for values that will not
         * overflow binomialCoefficientDouble
         */
        if (n < 1030) {
            return MathUtil.Log(binomialCoefficientDouble(n, k));
        }

        if (k > n / 2) {
            return binomialCoefficientLog(n, n - k);
        }

        /*
         * Sum logs for values that could overflow
         */
        double logSum = 0;

        // n!/(n-k)!
        for (int i = n - k + 1; i <= n; i++) {
            logSum += MathUtil.Log(i);
        }

        // divide by k!
        for (int i = 2; i <= k; i++) {
            logSum -= MathUtil.Log(i);
        }

        return logSum;
    }


	/**
	 * Returns an exact representation of the <a
	 * href="http://mathworld.wolfram.com/BinomialCoefficient.html"> Binomial
	 * Coefficient</a>, "{@code n choose k}", the number of
	 * {@code k}-element subsets that can be selected from an
	 * {@code n}-element set.
	 * <p>
	 * <Strong>Preconditions</strong>:
	 * <ul>
	 * <li> {@code 0 <= k <= n } (otherwise
	 * {@code IllegalArgumentException} is thrown)</li>
	 * <li> The result is small enough to fit into a {@code long}. The
	 * largest value of {@code n} for which all coefficients are
	 * {@code  < Long.MAX_VALUE} is 66. If the computed value exceeds
	 * {@code Long.MAX_VALUE} an {@code ArithMeticException} is
	 * thrown.</li>
	 * </ul></p>
	 *
	 * @param n the size of the set
	 * @param k the size of the subsets to be counted
	 * @return {@code n choose k}
	 * @throws NotPositiveException if {@code n < 0}.
	 * @throws NumberIsTooLargeException if {@code k > n}.
	 * @throws MathArithmeticException if the result is too large to be
	 * represented by a long integer.
	 */
	public long binomialCoefficient(int n, int k) {
		//CombinatoricsUtils.checkBinomial(n, k);
		if ((n == k) || (k == 0)) {
			return 1;
		}
		if ((k == 1) || (k == n - 1)) {
			return n;
		}
		// Use symmetry for large k
		if (k > n / 2) {
			return binomialCoefficient(n, n - k);
		}

		// We use the formula
		// (n choose k) = n! / (n-k)! / k!
		// (n choose k) == ((n-k+1)*...*n) / (1*...*k)
		// which could be written
		// (n choose k) == (n-1 choose k-1) * n / k
		long result = 1;
		if (n <= 61) {
			// For n <= 61, the naive implementation cannot overflow.
			int i = n - k + 1;
			for (int j = 1; j <= k; j++) {
				result = result * i / j;
				i++;
			}
		} else if (n <= 66) {
			// For n > 61 but n <= 66, the result cannot overflow,
			// but we must take care not to overflow intermediate values.
			int i = n - k + 1;
			for (int j = 1; j <= k; j++) {
				// We know that (result * i) is divisible by j,
				// but (result * i) may overflow, so we split j:
				// Filter out the gcd, d, so j/d and i/d are integer.
				// result is divisible by (j/d) because (j/d)
				// is relative prime to (i/d) and is a divisor of
				// result * (i/d).
				long d = gcd(i, j);
				result = (result / (j / d)) * (i / d);
				i++;
			}
		} else {
			// For n > 66, a result overflow might occur, so we check
			// the multiplication, taking care to not overflow
			// unnecessary.
			int i = n - k + 1;
			for (int j = 1; j <= k; j++) {
				long d = gcd(i, j);
				result = mulAndCheck( (int) (result / (j / d) ), (int) (i / d) );
				i++;
			}
		}
		return result;
	}


	public double binomialCoefficientDouble(int n, int k) {
		//CombinatoricsUtils.checkBinomial(n, k);
		if ((n == k) || (k == 0)) {
			return 1d;
		}
		if ((k == 1) || (k == n - 1)) {
			return n;
		}
		if (k > n / 2) {
			return binomialCoefficientDouble(n, n - k);
		}
		if (n < 67) {
			return binomialCoefficient(n, k);
		}

		double result = 1d;
		for (int i = 1; i <= k; i++) {
			result *= (double)(n - k + i) / (double)i;
		}

		return Math.Floor(result + 0.5);
	}

  
    /**
     * Computes the greatest common divisor of the absolute value of two
     * numbers, using a modified version of the "binary gcd" method.
     * See Knuth 4.5.2 algorithm B.
     * The algorithm is due to Josef Stein (1961).
     * <br/>
     * Special cases:
     * <ul>
     *  <li>The invocations
     *   {@code gcd(Integer.MIN_VALUE, Integer.MIN_VALUE)},
     *   {@code gcd(Integer.MIN_VALUE, 0)} and
     *   {@code gcd(0, Integer.MIN_VALUE)} throw an
     *   {@code ArithmeticException}, because the result would be 2^31, which
     *   is too large for an int value.</li>
     *  <li>The result of {@code gcd(x, x)}, {@code gcd(0, x)} and
     *   {@code gcd(x, 0)} is the absolute value of {@code x}, except
     *   for the special cases above.</li>
     *  <li>The invocation {@code gcd(0, 0)} is the only one which returns
     *   {@code 0}.</li>
     * </ul>
     *
     * @param p Number.
     * @param q Number.
     * @return the greatest common divisor (never negative).
     * @throws MathArithmeticException if the result cannot be represented as
     * a non-negative {@code int} value.
     * @since 1.1
     */
    public int gcd(int p, int q) {
        int a = p;
        int b = q;
        if (a == 0 ||
            b == 0) {
            if (a == Int32.MinValue ||
				b == Int32.MinValue) {
                throw new ArithmeticException( String.Format( "GCD_OVERFLOW_32_BITS {0},{1}", p, q ) );
            }
            return Math.Abs(a + b);
        }

        long al = a;
        long bl = b;
        bool useLong = false;
        if (a < 0) {
			if (Int32.MinValue == a) {
                useLong = true;
            } else {
                a = -a;
            }
            al = -al;
        }
        if (b < 0) {
			if (Int32.MinValue == b) {
                useLong = true;
            } else {
                b = -b;
            }
            bl = -bl;
        }
        if (useLong) {
            if(al == bl) {
				throw new ArithmeticException(String.Format("GCD_OVERFLOW_32_BITS {0},{1}", p, q));
            }
            long blbu = bl;
            bl = al;
            al = blbu % al;
            if (al == 0) {
				if (bl > Int32.MinValue) {
					throw new ArithmeticException(String.Format("GCD_OVERFLOW_32_BITS {0},{1}", p, q));
                }
                return (int) bl;
            }
            blbu = bl;

            // Now "al" and "bl" fit in an "int".
            b = (int) al;
            a = (int) (blbu % al);
        }

        return gcdPositive(a, b);
    }

    /**
     * Computes the greatest common divisor of two <em>positive</em> numbers
     * (this precondition is <em>not</em> checked and the result is undefined
     * if not fulfilled) using the "binary gcd" method which avoids division
     * and modulo operations.
     * See Knuth 4.5.2 algorithm B.
     * The algorithm is due to Josef Stein (1961).
     * <br/>
     * Special cases:
     * <ul>
     *  <li>The result of {@code gcd(x, x)}, {@code gcd(0, x)} and
     *   {@code gcd(x, 0)} is the value of {@code x}.</li>
     *  <li>The invocation {@code gcd(0, 0)} is the only one which returns
     *   {@code 0}.</li>
     * </ul>
     *
     * @param a Positive number.
     * @param b Positive number.
     * @return the greatest common divisor.
     */
    private int gcdPositive(int a, int b) {
        if (a == 0) {
            return b;
        }
        else if (b == 0) {
            return a;
        }

        // Make "a" and "b" odd, keeping track of common power of 2.
        int aTwos = MathUtil.NumberOfTrailingZeros(a);
        a >>= aTwos;
		int bTwos = MathUtil.NumberOfTrailingZeros(b);
        b >>= bTwos;
        int shift = Math.Min(aTwos, bTwos);

        // "a" and "b" are positive.
        // If a > b then "gdc(a, b)" is equal to "gcd(a - b, b)".
        // If a < b then "gcd(a, b)" is equal to "gcd(b - a, a)".
        // Hence, in the successive iterations:
        //  "a" becomes the absolute difference of the current values,
        //  "b" becomes the minimum of the current values.
        while (a != b) {
            int delta = a - b;
            b = Math.Min(a, b);
            a = Math.Abs(delta);

            // Remove any power of 2 in "a" ("b" is guaranteed to be odd).
			a >>= MathUtil.NumberOfTrailingZeros(a);
        }

        // Recover the common power of 2.
        return a << shift;
    }

    /**
     * Multiply two integers, checking for overflow.
     *
     * @param x Factor.
     * @param y Factor.
     * @return the product {@code x * y}.
     * @throws MathArithmeticException if the result can not be
     * represented as an {@code int}.
     * @since 1.1
     */
    public static int mulAndCheck(int x, int y) {
        long m = ((long)x) * ((long)y);
        if (m < Int32.MinValue || m > Int32.MaxValue) {
            throw new ArithmeticException();
        }
        return (int)m;
    }







}

}
