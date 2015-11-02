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
/// Wraps a <see cref="IEnumerator<T>"/> and returns only some subset of the elements that it would,
/// as determined by a sampling rate parameter.
/// </summary>
public sealed class FixedSizeSamplingIterator<T> : IEnumerator<T> {
	
	List<T> buf;
	IEnumerator<T> enumerator;

	public FixedSizeSamplingIterator(int size, IEnumerator<T> source) {
		buf = new List<T>(size);

		int sofar = 0;
		var random = RandomUtils.getRandom();
		while (source.MoveNext()) {
		  T v = source.Current;
		  sofar++;
		  if (buf.Count < size) {
			buf.Add(v);
		  } else {
			int position = random.nextInt(sofar);
			if (position < buf.Count) {
			  buf[position] = v;
			}
		  }
		}
		enumerator = buf.GetEnumerator();
	}
  
  public T Current {
	  get { return enumerator.Current; }
  }

  public void Dispose() {
	enumerator.Dispose();
  }

  object IEnumerator.Current {
	  get { return Current; }
  }

  public bool MoveNext() {
	  return enumerator.MoveNext();
  }

  public void Reset() {
	  enumerator.Reset();
  }
}

}