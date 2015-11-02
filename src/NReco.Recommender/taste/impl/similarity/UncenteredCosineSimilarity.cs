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
using NReco.CF.Taste.Common;
using NReco.CF.Taste.Model;

namespace NReco.CF.Taste.Impl.Similarity {

/// <summary>
/// An implementation of the cosine similarity. The result is the cosine of the angle formed between
/// the two preference vectors.
/// </summary>
/// <remarks>
/// Note that this similarity does not "center" its data, shifts the user's preference values so that each of their
/// means is 0. For this behavior, use {@link PearsonCorrelationSimilarity}, which actually is mathematically
/// equivalent for centered data.
/// </remarks>
public sealed class UncenteredCosineSimilarity : AbstractSimilarity {

   /// @{@link DataModel} does not have preference values
	public UncenteredCosineSimilarity(IDataModel dataModel)
		: this(dataModel, Weighting.UNWEIGHTED) {
    
  }

   /// @{@link DataModel} does not have preference values
	public UncenteredCosineSimilarity(IDataModel dataModel, Weighting weighting)
		: base(dataModel, weighting, false) {
    
    //Preconditions.checkArgument(dataModel.hasPreferenceValues(), "DataModel doesn't have preference values");
  }

  override protected double computeResult(int n, double sumXY, double sumX2, double sumY2, double sumXYdiff2) {
    if (n == 0) {
      return Double.NaN;
    }
    double denominator = Math.Sqrt(sumX2) * Math.Sqrt(sumY2);
    if (denominator == 0.0) {
      // One or both parties has -all- the same ratings;
      // can't really say much similarity under this measure
      return Double.NaN;
    }
    return sumXY / denominator;
  }

}

}