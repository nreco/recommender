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
using NReco.CF.Taste.Impl.Common;

namespace NReco.CF.Taste.Impl.Model {

public sealed class PlusAnonymousUserlongPrimitiveIterator : IEnumerator<long> {

	private IEnumerator<long> enumerator;
  private long extraDatum;
  private bool datumConsumed;
  private bool currentDatum = false;
  private bool prevMoveNext = false;

  public PlusAnonymousUserlongPrimitiveIterator(IEnumerator<long> enumerator, long extraDatum) {
	  this.enumerator = enumerator;
    this.extraDatum = extraDatum;
    datumConsumed = false;
  }
  

 
  
  /*public void skip(int n) {
    for (int i = 0; i < n; i++) {
      nextlong();
    }
  }*/


  public long Current {
	  get {
		  return currentDatum ? extraDatum : enumerator.Current;
	  }
  }

  public void Dispose() {
  }

  object IEnumerator.Current {
	  get { return Current; }
  }

  public bool MoveNext() {
	  if (currentDatum) {
		  currentDatum = false;
		  return prevMoveNext;
	  }

	  prevMoveNext = enumerator.MoveNext();

	  if (prevMoveNext && !datumConsumed && extraDatum <= Current) {
		  datumConsumed = true;
		  currentDatum = true;
		  return true;
	  }

	  if (!prevMoveNext && !datumConsumed) {
		  datumConsumed = true;
		  currentDatum = true;
		  return true;
	  }
	  return prevMoveNext;
  }

  public void Reset() {
	  datumConsumed = false;
	  enumerator.Reset();
  }
}

}