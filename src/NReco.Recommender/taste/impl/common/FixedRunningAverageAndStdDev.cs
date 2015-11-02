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

namespace NReco.CF.Taste.Impl.Common {
 /// <p>
 /// A simple class that represents a fixed value of an average, count and standard deviation. This is useful
 /// when an API needs to return {@link RunningAverageAndStdDev} but is not in a position to accept
 /// updates to it.
 /// </p>
public sealed class FixedRunningAverageAndStdDev : FixedRunningAverage, IRunningAverageAndStdDev {

  private double stdDev;

  public FixedRunningAverageAndStdDev(double average, double stdDev, int count) : base(average, count) {
    this.stdDev = stdDev;
  }

  public IRunningAverageAndStdDev Inverse() {
    return new InvertedRunningAverageAndStdDev(this);
  }

  public override string ToString() {
    return base.ToString() + ',' + stdDev.ToString();
  }

  public double GetStandardDeviation() {
    return stdDev;
  }

}

}