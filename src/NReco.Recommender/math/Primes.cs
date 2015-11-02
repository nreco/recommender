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

namespace NReco.Math3.Primes {

	/// Methods related to prime numbers in the range of <code>int</code>:
	/// <ul>
	/// <li>primality test</li>
	/// <li>prime number generation</li>
	/// <li>factorization</li>
	/// </ul>
	///
	/// @version $Id: Primes.java 1538368 2013-11-03 13:57:37Z erans $
	/// @since 3.2
	public class Primes {

		/// Hide utility class.
		private Primes() {
		}

		/// Primality test: tells if the argument is a (provable) prime or not.
		/// <p>
		/// It uses the Miller-Rabin probabilistic test in such a way that a result is guaranteed:
		/// it uses the firsts prime numbers as successive base (see Handbook of applied cryptography
		/// by Menezes, table 4.1).
		///
		/// @param n number to test.
		/// @return true if n is prime. (All numbers &lt; 2 return false).
		public static bool isPrime(int n) {
			if (n < 2) {
				return false;
			}

			foreach (int p in SmallPrimes.PRIMES) {
				if (0 == (n % p)) {
					return n == p;
				}
			}
			return SmallPrimes.millerRabinPrimeTest(n);
		}

		/// Return the smallest prime greater than or equal to n.
		///
		/// @param n a positive number.
		/// @return the smallest prime greater than or equal to n.
		/// @; 0.
		public static int nextPrime(int n) {
        if (n < 0) {
            throw new ArgumentException();// MathIllegalArgumentException(LocalizedFormats.NUMBER_TOO_SMALL, n, 0);
        }
        if (n == 2) {
            return 2;
        }
        n |= 1;//make sure n is odd
        if (n == 1) {
            return 2;
        }

        if (isPrime(n)) {
            return n;
        }

        // prepare entry in the +2, +4 loop:
        // n should not be a multiple of 3
        int rem = n % 3;
        if (0 == rem) { // if n % 3 == 0
            n += 2; // n % 3 == 2
        } else if (1 == rem) { // if n % 3 == 1
            // if (isPrime(n)) return n;
            n += 4; // n % 3 == 2
        }
        while (true) { // this loop skips all multiple of 3
            if (isPrime(n)) {
                return n;
            }
            n += 2; // n % 3 == 1
            if (isPrime(n)) {
                return n;
            }
            n += 4; // n % 3 == 2
        }
    }

		/// Prime factors decomposition
		///
		/// @param n number to factorize: must be &ge; 2
		/// @return list of prime factors of n
		/// @; 2.
		public static List<int> primeFactors(int n) {

			if (n < 2) {
				throw new ArgumentException(); // MathIllegalArgumentException(LocalizedFormats.NUMBER_TOO_SMALL, n, 2);
			}
			// slower than trial div unless we do an awful lot of computation
			// (then it finally gets JIT-compiled efficiently
			// List<Integer> out = PollardRho.primeFactors(n);
			return SmallPrimes.trialDivision(n);

		}

	}


}