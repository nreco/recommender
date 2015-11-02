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

using NReco.Math3;

namespace NReco.Math3.Als {

/// see <a href="http://research.yahoo.com/pub/2433">Collaborative Filtering for Implicit Feedback Datasets</a> 
// warining: no unit tests for this class
public class ImplicitFeedbackAlternatingLeastSquaresSolver {

  private int numFeatures;
  private double alpha;
  private double lambda;

  private IDictionary<int,double[]> Y;
  private double[,] YtransposeY;

  public ImplicitFeedbackAlternatingLeastSquaresSolver(int numFeatures, double lambda, double alpha,
      IDictionary<int,double[]> Y) {
    this.numFeatures = numFeatures;
    this.lambda = lambda;
    this.alpha = alpha;
    this.Y = Y;
    YtransposeY = getYtransposeY(Y);
  }

  public double[] solve(IList<Tuple<int, double>> ratings) {
	  var otherM = getYtransponseCuMinusIYPlusLambdaI(ratings);
	  var sumM = new double[YtransposeY.GetLength(0), YtransposeY.GetLength(1)];
	  for (int i = 0; i < sumM.GetLength(0); i++)
		  for (int j = 0; j < sumM.GetLength(1); j++)
			  sumM[i, j] = YtransposeY[i, j] + otherM[i, j];

	  return solve(sumM, getYtransponseCuPu(ratings));
  }

  private static double[] solve(double[,] A, double[,] y) {
    return MatrixUtil.viewColumn( new QRDecomposition(A).solve(y), 0);
  }

  double confidence(double rating) {
    return 1 + alpha * rating;
  }

  /// Y' Y 
  private double[,] getYtransposeY(IDictionary<int,double[]> Y) {

    var indexes = Y.Keys.ToList();
    indexes.Sort(); //.quickSort();
    int numIndexes = indexes.Count;

    double[,] YtY = new double[numFeatures,numFeatures];

    // Compute Y'Y by dot products between the 'columns' of Y
    for (int i = 0; i < numFeatures; i++) {
      for (int j = i; j < numFeatures; j++) {
        double dot = 0;
        for (int k = 0; k < numIndexes; k++) {
          double[] row = Y[ indexes[k] ];
          dot += row[i] * row[j];
        }
        YtY[i,j] = dot;
        if (i != j) {
          YtY[j,i] = dot;
        }
      }
    }
    return YtY;
  }

  /// Y' (Cu - I) Y + λ I 
  private double[,] getYtransponseCuMinusIYPlusLambdaI(IList<Tuple<int, double>> userRatings) {
    //Preconditions.checkArgument(userRatings.isSequentialAccess(), "need sequential access to ratings!");

    /// (Cu -I) Y 
    var CuMinusIY = new Dictionary<int,double[]>(userRatings.Count);
	foreach (var e in userRatings) {
		CuMinusIY[ e.Item1 ] = MatrixUtil.times( Y[e.Item1], confidence(e.Item2) - 1);
    }

    var YtransponseCuMinusIY = new double[numFeatures, numFeatures];

    /// Y' (Cu -I) Y by outer products 
	foreach (var e in userRatings) {
		var currentEIdx = e.Item1;
		var featureIdx = 0;
		foreach (var feature in Y[currentEIdx]) {
			var currentFeatIdx = featureIdx++;
			var partial = MatrixUtil.times( CuMinusIY[currentEIdx], feature);

			for (int i = 0; i < YtransponseCuMinusIY.GetLength(1); i++) {
				//YtransponseCuMinusIY.viewRow(feature.index()).assign(partial, Functions.PLUS);
				YtransponseCuMinusIY[currentFeatIdx, i] += partial[i];
			}
		  }
    }

    /// Y' (Cu - I) Y + λ I  add lambda on the diagonal 
    for (int feature = 0; feature < numFeatures; feature++) {
      YtransponseCuMinusIY[feature, feature] = YtransponseCuMinusIY[feature, feature] + lambda;
    }

    return YtransponseCuMinusIY;
  }

  /// Y' Cu p(u) 
  private double[,] getYtransponseCuPu(IList<Tuple<int, double>> userRatings) {
    //Preconditions.checkArgument(userRatings.isSequentialAccess(), "need sequential access to ratings!");

    double[] YtransponseCuPu = new double[numFeatures];

    foreach (var e in  userRatings ) {
      //YtransponseCuPu.assign(Y.get(e.index()).times(confidence(e.get())), Functions.PLUS);
	//	Y.get(e.index()).times(confidence(e.get()))
		var other =	MatrixUtil.times(Y[e.Item1], confidence(e.Item2) );

		for (int i=0; i<YtransponseCuPu.Length; i++)
			YtransponseCuPu[i] += other[i];
    }

    return columnVectorAsMatrix(YtransponseCuPu);
  }

  private double[,] columnVectorAsMatrix(double[] v) {
    double[,] matrix =  new double[numFeatures,1];
    for (int i=0; i<v.Length; i++) {
      matrix[i,0] =  v[i];
    }
    return matrix;
  }

}

}