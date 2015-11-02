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
 *  Unless required by applicable law or agreed to in writing, software distributed on an
 *  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using NReco.Math3.Distribution;
using NReco.CF;

namespace NReco.CF.Taste.Impl.Common {

/// <summary>
/// Wraps a <see cref="IEnumerator<long>"/> and returns only some subset of the elements that it would,
/// as determined by a sampling rate parameter.
/// </summary>
public sealed class SamplinglongPrimitiveIterator : IEnumerator<long> {
  
  private PascalDistribution geometricDistribution;
  private IEnumerator<long> enumerator;

  public SamplinglongPrimitiveIterator(IEnumerator<long> enumerator, double samplingRate)
	  : this(RandomUtils.getRandom(), enumerator, samplingRate) {

  }

  public SamplinglongPrimitiveIterator(RandomWrapper random, IEnumerator<long> enumerator, double samplingRate) {
	  if (enumerator == null)
		  throw new ArgumentException("enumerator");
	  if (!(samplingRate > 0.0 && samplingRate <= 1.0))
		  throw new ArgumentException("samplingRate");
    //Preconditions.checkArgument(samplingRate > 0.0 && samplingRate <= 1.0, "Must be: 0.0 < samplingRate <= 1.0");
    // Geometric distribution is special case of negative binomial (aka Pascal) with r=1:
    geometricDistribution = new PascalDistribution(random.getRandomGenerator(), 1, samplingRate);
	this.enumerator = enumerator;
    
    SkipNext();
  }
  

	public void Remove() {
		throw new NotSupportedException();
	}
  
	public void Skip(int n) {
		int toSkip = 0;
		for (int i = 0; i < n; i++) {
			toSkip += geometricDistribution.sample();
		}

		for (int i=0;i<toSkip;i++) {
			if (!enumerator.MoveNext())
				break;
		}
	}

	public static IEnumerator<long> MaybeWrapIterator(IEnumerator<long> enumerator, double samplingRate) {
		return samplingRate >= 1.0 ? enumerator : new SamplinglongPrimitiveIterator(enumerator, samplingRate);
	}


	public long Current {
		get { return enumerator.Current; }
	}

	public void Dispose() {
	
	}

	object IEnumerator.Current {
		get { return Current; }
	}

	protected void SkipNext() {
		int toSkip = geometricDistribution.sample();
	 
		//_Delegate.skip(toSkip);
		for (int i = 0; i < toSkip; i++) {
			if (!enumerator.MoveNext())
				break;
		}
	}

	public bool MoveNext() {
		SkipNext();
		return enumerator.MoveNext();
	}

	public void Reset() {
		enumerator.Reset();
	}
}

}