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

namespace NReco.CF.Taste.Impl.Common {

public class WeightedRunningAverage : IRunningAverage {

  private double totalWeight;
  private double average;

  public WeightedRunningAverage() {
    totalWeight = 0.0;
    average = Double.NaN;
  }

  public virtual void AddDatum(double datum) {
    AddDatum(datum, 1.0);
  }

  public virtual void AddDatum(double datum, double weight) {
    double oldTotalWeight = totalWeight;
    totalWeight += weight;
    if (oldTotalWeight <= 0.0) {
      average = datum;
    } else {
      average = average * oldTotalWeight / totalWeight + datum * weight / totalWeight;
    }
  }

  public virtual void RemoveDatum(double datum) {
    RemoveDatum(datum, 1.0);
  }

  public virtual void RemoveDatum(double datum, double weight) {
    double oldTotalWeight = totalWeight;
    totalWeight -= weight;
    if (totalWeight <= 0.0) {
      average = Double.NaN;
      totalWeight = 0.0;
    } else {
      average = average * oldTotalWeight / totalWeight - datum * weight / totalWeight;
    }
  }

  public virtual void ChangeDatum(double delta) {
    ChangeDatum(delta, 1.0);
  }

  public virtual void ChangeDatum(double delta, double weight) {
    //Preconditions.checkArgument(weight <= totalWeight, "weight must be <= totalWeight");
    average += delta * weight / totalWeight;
  }

  public virtual double GetTotalWeight() {
    return totalWeight;
  }

  /// @return {@link #getTotalWeight()} 
  public virtual int GetCount() {
    return (int) totalWeight;
  }

  public virtual double GetAverage() {
    return average;
  }

  public virtual IRunningAverage Inverse() {
    return new InvertedRunningAverage(this);
  }

  public override string ToString() {
    return Convert.ToString(average);
  }

}

}