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

using NReco.CF.Taste.Eval;


namespace NReco.CF.Taste.Impl.Eval {

public sealed class IRStatisticsImpl : IRStatistics {

  private double precision;
  private double recall;
  private double fallOut;
  private double ndcg;
  private double reach;

  public IRStatisticsImpl(double precision, double recall, double fallOut, double ndcg, double reach) {
    /*Preconditions.checkArgument(Double.isNaN(precision) || (precision >= 0.0 && precision <= 1.0),
        "Illegal precision: " + precision + ". Must be: 0.0 <= precision <= 1.0 or NaN");
    Preconditions.checkArgument(Double.isNaN(recall) || (recall >= 0.0 && recall <= 1.0), 
        "Illegal recall: " + recall + ". Must be: 0.0 <= recall <= 1.0 or NaN");
    Preconditions.checkArgument(Double.isNaN(fallOut) || (fallOut >= 0.0 && fallOut <= 1.0),
        "Illegal fallOut: " + fallOut + ". Must be: 0.0 <= fallOut <= 1.0 or NaN");
    Preconditions.checkArgument(Double.isNaN(ndcg) || (ndcg >= 0.0 && ndcg <= 1.0), 
        "Illegal nDCG: " + ndcg + ". Must be: 0.0 <= nDCG <= 1.0 or NaN");
    Preconditions.checkArgument(Double.isNaN(reach) || (reach >= 0.0 && reach <= 1.0), 
        "Illegal reach: " + reach + ". Must be: 0.0 <= reach <= 1.0 or NaN");*/
    this.precision = precision;
    this.recall = recall;
    this.fallOut = fallOut;
    this.ndcg = ndcg;
    this.reach = reach;
  }

  public double GetPrecision() {
    return precision;
  }

  public double GetRecall() {
    return recall;
  }

  public double GetFallOut() {
    return fallOut;
  }

  public double GetF1Measure() {
    return GetFNMeasure(1.0);
  }

  public double GetFNMeasure(double b) {
    double b2 = b * b;
    double sum = b2 * precision + recall;
    return sum == 0.0 ? Double.NaN : (1.0 + b2) * precision * recall / sum;
  }

  public double GetNormalizedDiscountedCumulativeGain() {
    return ndcg;
  }

  public double GetReach() {
    return reach;
  }

  public override string ToString() {
    return "IRStatisticsImpl[precision:" + precision + ",recall:" + recall + ",fallOut:"
        + fallOut + ",nDCG:" + ndcg + ",reach:" + reach + ']';
  }

}

}