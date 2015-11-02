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

namespace NReco.CF.Taste.Impl.Common {
/// <summary>
/// Extends <see cref="FullRunningAverage"/> to add a running standard deviation computation.
/// Uses Welford's method, as described at http://www.johndcook.com/standard_deviation.html
/// </summary>
public sealed class FullRunningAverageAndStdDev : FullRunningAverage, IRunningAverageAndStdDev {

  private double stdDev;
  private double mk;
  private double sk;
  
  public FullRunningAverageAndStdDev() {
    mk = 0.0;
    sk = 0.0;
    recomputeStdDev();
  }
  
  public FullRunningAverageAndStdDev(int count, double average, double mk, double sk) : base(count, average) {
    this.mk = mk;
    this.sk = sk;
    recomputeStdDev();
  }

  public double getMk() {
    return mk;
  }
  
  public double getSk() {
    return sk;
  }

  public double GetStandardDeviation() {
    return stdDev;
  }
  
  public override void AddDatum(double datum) {
    lock (this) {
		base.AddDatum(datum);
		int count = GetCount();
		if (count == 1) {
		  mk = datum;
		  sk = 0.0;
		} else {
		  double oldmk = mk;
		  double diff = datum - oldmk;
		  mk += diff / count;
		  sk += diff * (datum - mk);
		}
		recomputeStdDev();
	}
  }
  
  public override void RemoveDatum(double datum) {
    lock (this) {
		int oldCount = GetCount();
		base.RemoveDatum(datum);
		double oldmk = mk;
		mk = (oldCount * oldmk - datum) / (oldCount - 1);
		sk -= (datum - mk) * (datum - oldmk);
		recomputeStdDev();
	}
  }
  
   /// @throws NotSupportedException
  public override void ChangeDatum(double delta) {
    throw new NotSupportedException();
  }
  
  private void recomputeStdDev() {
    int count = GetCount();
    stdDev = count > 1 ? Math.Sqrt(sk / (count - 1)) : Double.NaN;
  }

  public IRunningAverageAndStdDev Inverse() {
    return new InvertedRunningAverageAndStdDev(this);
  }
  
  public override string ToString() {
    return String.Format("{0},{1}", GetAverage(), stdDev);
  }
  
}

}