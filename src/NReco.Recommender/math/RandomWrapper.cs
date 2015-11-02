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
using NReco.Math3.Random;

namespace NReco.CF {


public sealed class RandomWrapper {

  private static long STANDARD_SEED = unchecked( (long) 0xCAFEDEADBEEFBABEL );

  private IRandomGenerator random;

  public RandomWrapper() {
    random = new MersenneTwister();
	random.setSeed( Environment.TickCount + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this));
  }

  public RandomWrapper(long seed) {
    random = new MersenneTwister(seed);
  }

  public void setSeed(long seed) {
    // Since this will be called by the java.util.Random() constructor before we construct
    // the delegate... and because we don't actually care about the result of this for our
    // purpose:
    if (random != null) {
      random.setSeed(seed);
    }
  }

  public void resetToTestSeed() {
    setSeed(STANDARD_SEED);
  }

  public IRandomGenerator getRandomGenerator() {
    return random;
  }

  protected int next(int bits) {
    // Ugh, can't delegate this method -- it's protected
    // Callers can't use it and other methods are delegated, so shouldn't matter
    throw new NotSupportedException();
  }

  public void nextBytes(byte[] bytes) {
    random.nextBytes(bytes);
  }

  public int nextInt() {
    return random.nextInt();
  }

  public int nextInt(int n) {
    return random.nextInt(n);
  }

  public long nextlong() {
    return random.nextlong();
  }

  public bool nextBoolean() {
    return random.nextBoolean();
  }

  public float nextFloat() {
    return random.nextFloat();
  }

  public double nextDouble() {
    return random.nextDouble();
  }

  public double nextGaussian() {
    return random.nextGaussian();
  }

}

}