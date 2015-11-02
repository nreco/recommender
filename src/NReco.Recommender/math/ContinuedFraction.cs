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


namespace NReco.Math3.Util {

	/// Provides a generic means to evaluate continued fractions.  Subclasses simply
	/// provided the a and b coefficients to evaluate the continued fraction.
	///
	/// <p>
	/// References:
	/// <ul>
	/// <li><a href="http://mathworld.wolfram.com/ContinuedFraction.html">
	/// Continued Fraction</a></li>
	/// </ul>
	/// </p>
	///
	/// @version $Id: ContinuedFraction.java 1416643 2012-12-03 19:37:14Z tn $
	public abstract class ContinuedFraction {
		/// Maximum allowed numerical error. */
		private static double DEFAULT_EPSILON = 10e-9;

		/// Default constructor.
		protected ContinuedFraction() {
		}

		/// Access the n-th a coefficient of the continued fraction.  Since a can be
		/// a function of the evaluation point, x, that is passed in as well.
		/// @param n the coefficient index to retrieve.
		/// @param x the evaluation point.
		/// @return the n-th a coefficient.
		protected abstract double getA(int n, double x);

		/// Access the n-th b coefficient of the continued fraction.  Since b can be
		/// a function of the evaluation point, x, that is passed in as well.
		/// @param n the coefficient index to retrieve.
		/// @param x the evaluation point.
		/// @return the n-th b coefficient.
		protected abstract double getB(int n, double x);

		/// Evaluates the continued fraction at the value x.
		/// @param x the evaluation point.
		/// @return the value of the continued fraction evaluated at x.
		/// @throws ConvergenceException if the algorithm fails to converge.
		public double evaluate(double x) {
			return evaluate(x, DEFAULT_EPSILON, Int32.MaxValue);
		}

		/// Evaluates the continued fraction at the value x.
		/// @param x the evaluation point.
		/// @param epsilon maximum error allowed.
		/// @return the value of the continued fraction evaluated at x.
		/// @throws ConvergenceException if the algorithm fails to converge.
		public double evaluate(double x, double epsilon) {
			return evaluate(x, epsilon, Int32.MaxValue);
		}

		/// Evaluates the continued fraction at the value x.
		/// @param x the evaluation point.
		/// @param maxIterations maximum number of convergents
		/// @return the value of the continued fraction evaluated at x.
		/// @throws ConvergenceException if the algorithm fails to converge.
		/// @throws MaxCountExceededException if maximal number of iterations is reached
		public double evaluate(double x, int maxIterations) {
			return evaluate(x, DEFAULT_EPSILON, maxIterations);
		}

		protected bool PrecisionEquals(double x, double y, double eps) {
			return x==y || Math.Abs(y - x) <= eps;
		}

		/// Evaluates the continued fraction at the value x.
		/// <p>
		/// The implementation of this method is based on the modified Lentz algorithm as described
		/// on page 18 ff. in:
		/// <ul>
		///   <li>
		///   I. J. Thompson,  A. R. Barnett. "Coulomb and Bessel Functions of Complex Arguments and Order."
		///   <a target="_blank" href="http://www.fresco.org.uk/papers/Thompson-JCP64p490.pdf">
		///   http://www.fresco.org.uk/papers/Thompson-JCP64p490.pdf</a>
		///   </li>
		/// </ul>
		/// <b>Note:</b> the implementation uses the terms a<sub>i</sub> and b<sub>i</sub> as defined in
		/// <a href="http://mathworld.wolfram.com/ContinuedFraction.html">Continued Fraction @ MathWorld</a>.
		/// </p>
		///
		/// @param x the evaluation point.
		/// @param epsilon maximum error allowed.
		/// @param maxIterations maximum number of convergents
		/// @return the value of the continued fraction evaluated at x.
		/// @throws ConvergenceException if the algorithm fails to converge.
		/// @throws MaxCountExceededException if maximal number of iterations is reached
		public double evaluate(double x, double epsilon, int maxIterations)
        {
        double small = 1e-50;
        double hPrev = getA(0, x);

        // use the value of small as epsilon criteria for zero checks
        if (PrecisionEquals(hPrev, 0.0, small)) {
            hPrev = small;
        }

        int n = 1;
        double dPrev = 0.0;
        double cPrev = hPrev;
        double hN = hPrev;

        while (n < maxIterations) {
            double a = getA(n, x);
            double b = getB(n, x);

            double dN = a + b * dPrev;
            if (PrecisionEquals(dN, 0.0, small)) {
                dN = small;
            }
            double cN = a + b / cPrev;
            if (PrecisionEquals(cN, 0.0, small)) {
                cN = small;
            }

            dN = 1 / dN;
            double deltaN = cN * dN;
            hN = hPrev * deltaN;

            if (Double.IsInfinity(hN)) {
				throw new OverflowException( String.Format("CONTINUED_FRACTION_INFINITY_DIVERGENCE {0}", x) );
				/*throw new ConvergenceException(LocalizedFormats.CONTINUED_FRACTION_INFINITY_DIVERGENCE,
                                               x);*/
            }
            if (Double.IsNaN(hN)) {
				throw new OverflowException(String.Format("CONTINUED_FRACTION_NAN_DIVERGENCE {0}", x));
                /*throw new ConvergenceException(LocalizedFormats.CONTINUED_FRACTION_NAN_DIVERGENCE,
                                               x);*/
            }

            if (Math.Abs(deltaN - 1.0) < epsilon) {
                break;
            }

            dPrev = dN;
            cPrev = cN;
            hPrev = hN;
            n++;
        }

        if (n >= maxIterations) {
			throw new System.Exception(String.Format("NON_CONVERGENT_CONTINUED_FRACTION iter={0} x={1}", maxIterations, x));
            /*throw new MaxCountExceededException(LocalizedFormats.NON_CONVERGENT_CONTINUED_FRACTION,
                                                maxIterations, x);*/
        }

        return hN;
    }

	}

}
