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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

using NReco.Math3.Primes;

namespace NReco.CF {

/// <summary>
/// The source of random stuff for the whole project. This lets us make all randomness in the project
/// predictable, if desired, for when we run unit tests, which should be repeatable.
/// </summary>
public sealed class RandomUtils {

  /// The largest prime less than 2<sup>31</sup>-1 that is the smaller of a twin prime pair. 
  public const int MAX_INT_SMALLER_TWIN_PRIME = 2147482949;

  private static IDictionary<RandomWrapper,Boolean> INSTANCES =
      new ConcurrentDictionary<RandomWrapper,Boolean>();

  private static bool testSeed = false;

  private RandomUtils() { }
  
  public static void useTestSeed() {
    testSeed = true;
    lock (INSTANCES) {
      foreach (RandomWrapper rng in INSTANCES.Keys) {
        rng.resetToTestSeed();
      }
    }
  }
  
  public static RandomWrapper getRandom() {
    RandomWrapper random = new RandomWrapper();
    if (testSeed) {
      random.resetToTestSeed();
    }
    INSTANCES[random ] = true;
    return random;
  }
  
  public static RandomWrapper getRandom(long seed) {
    RandomWrapper random = new RandomWrapper(seed);
    INSTANCES[random] = true;
    return random;
  }
  
  /// @return what {@link Double#hashCode()} would return for the same value 
  public static int hashDouble(double value) {
    return BitConverter.DoubleToInt64Bits(value).GetHashCode();
  }

  /// @return what {@link Float#hashCode()} would return for the same value 
  public static int hashFloat(float value) {
    return BitConverter.ToInt32( BitConverter.GetBytes(value), 0); // float.floatToIntBits(value);
  }
  
   /// <p>
   /// Finds next-largest "twin primes": numbers p and p+2 such that both are prime. Finds the smallest such p
   /// such that the smaller twin, p, is greater than or equal to n. Returns p+2, the larger of the two twins.
   /// </p>
  public static int nextTwinPrime(int n) {
    if (n > MAX_INT_SMALLER_TWIN_PRIME) {
      throw new ArgumentException();
    }
    if (n <= 3) {
      return 5;
    }
    int next = Primes.nextPrime(n);
    while (!Primes.isPrime(next + 2)) {
      next = Primes.nextPrime(next + 4);
    }
    return next + 2;
  }
  
}

}