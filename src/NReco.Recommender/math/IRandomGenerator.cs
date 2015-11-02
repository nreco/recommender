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

namespace NReco.Math3.Random {


	/// Interface extracted from <code>java.util.Random</code>.  This interface is
	/// implemented by {@link AbstractRandomGenerator}.
	///
	/// @since 1.1
	/// @version $Id: RandomGenerator.java 1416643 2012-12-03 19:37:14Z tn $
	public interface IRandomGenerator {

		/// Sets the seed of the underlying random number generator using an
		/// <code>int</code> seed.
		/// <p>Sequences of values generated starting with the same seeds
		/// should be identical.
		/// </p>
		/// @param seed the seed value
		void setSeed(int seed);

		/// Sets the seed of the underlying random number generator using an
		/// <code>int</code> array seed.
		/// <p>Sequences of values generated starting with the same seeds
		/// should be identical.
		/// </p>
		/// @param seed the seed value
		void setSeed(int[] seed);

		/// Sets the seed of the underlying random number generator using a
		/// <code>long</code> seed.
		/// <p>Sequences of values generated starting with the same seeds
		/// should be identical.
		/// </p>
		/// @param seed the seed value
		void setSeed(long seed);

		/// Generates random bytes and places them into a user-supplied
		/// byte array.  The number of random bytes produced is equal to
		/// the length of the byte array.
		///
		/// @param bytes the non-null byte array in which to put the
		/// random bytes
		void nextBytes(byte[] bytes);

		/// Returns the next pseudorandom, uniformly distributed <code>int</code>
		/// value from this random number generator's sequence.
		/// All 2<font size="-1"><sup>32</sup></font> possible <tt>int</tt> values
		/// should be produced with  (approximately) equal probability.
		///
		/// @return the next pseudorandom, uniformly distributed <code>int</code>
		///  value from this random number generator's sequence
		int nextInt();

		/// Returns a pseudorandom, uniformly distributed <tt>int</tt> value
		/// between 0 (inclusive) and the specified value (exclusive), drawn from
		/// this random number generator's sequence.
		///
		/// @param n the bound on the random number to be returned.  Must be
		/// positive.
		/// @return  a pseudorandom, uniformly distributed <tt>int</tt>
		/// value between 0 (inclusive) and n (exclusive).
		/// @throws IllegalArgumentException  if n is not positive.
		int nextInt(int n);

		/// Returns the next pseudorandom, uniformly distributed <code>long</code>
		/// value from this random number generator's sequence.  All
		/// 2<font size="-1"><sup>64</sup></font> possible <tt>long</tt> values
		/// should be produced with (approximately) equal probability.
		///
		/// @return  the next pseudorandom, uniformly distributed <code>long</code>
		///value from this random number generator's sequence
		long nextlong();

		/// Returns the next pseudorandom, uniformly distributed
		/// <code>bool</code> value from this random number generator's
		/// sequence.
		///
		/// @return  the next pseudorandom, uniformly distributed
		/// <code>bool</code> value from this random number generator's
		/// sequence
		bool nextBoolean();

		/// Returns the next pseudorandom, uniformly distributed <code>float</code>
		/// value between <code>0.0</code> and <code>1.0</code> from this random
		/// number generator's sequence.
		///
		/// @return  the next pseudorandom, uniformly distributed <code>float</code>
		/// value between <code>0.0</code> and <code>1.0</code> from this
		/// random number generator's sequence
		float nextFloat();

		/// Returns the next pseudorandom, uniformly distributed
		/// <code>double</code> value between <code>0.0</code> and
		/// <code>1.0</code> from this random number generator's sequence.
		///
		/// @return  the next pseudorandom, uniformly distributed
		///  <code>double</code> value between <code>0.0</code> and
		///  <code>1.0</code> from this random number generator's sequence
		double nextDouble();

		/// Returns the next pseudorandom, Gaussian ("normally") distributed
		/// <code>double</code> value with mean <code>0.0</code> and standard
		/// deviation <code>1.0</code> from this random number generator's sequence.
		///
		/// @return  the next pseudorandom, Gaussian ("normally") distributed
		/// <code>double</code> value with mean <code>0.0</code> and
		/// standard deviation <code>1.0</code> from this random number
		///  generator's sequence
		double nextGaussian();
	}

}
