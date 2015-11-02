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

 /// See
 /// <a href="http://www.hpl.hp.com/personal/Robert_Schreiber/papers/2008%20AAIM%20Netflix/netflix_aaim08(submitted).pdf">
 /// this paper.</a>
public sealed class AlternatingLeastSquaresSolver {

  private AlternatingLeastSquaresSolver() {}

  //TODO make feature vectors a simple array
  public static double[] solve(IList<double[]> featureVectors, double[] ratingVector, double lambda, int numFeatures) {

    //Preconditions.checkNotNull(featureVectors, "Feature Vectors cannot be null");
    //Preconditions.checkArgument(!Iterables.isEmpty(featureVectors));
    //Preconditions.checkNotNull(ratingVector, "Rating Vector cannot be null");
    //Preconditions.checkArgument(ratingVector.getNumNondefaultElements() > 0, "Rating Vector cannot be empty");
    //Preconditions.checkArgument(Iterables.size(featureVectors) == ratingVector.getNumNondefaultElements());

	int nui = ratingVector.Length; //.getNumNondefaultElements();

    var MiIi = createMiIi(featureVectors, numFeatures);
    var RiIiMaybeTransposed = createRiIiMaybeTransposed(ratingVector);

    /// compute Ai = MiIi * t(MiIi) + lambda * nui * E 
    var Ai = miTimesMiTransposePlusLambdaTimesNuiTimesE(MiIi, lambda, nui);
    /// compute Vi = MiIi * t(R(i,Ii)) 
    var Vi = MatrixUtil.times( MiIi, RiIiMaybeTransposed);
    /// compute Ai * ui = Vi 
    return solve(Ai, Vi);
  }

  private static double[] solve(double[,] Ai, double[,] Vi) {
    return MatrixUtil.viewColumn( new QRDecomposition(Ai).solve(Vi), 0);
  }

  public static double[,] addLambdaTimesNuiTimesE(double[,] matrix, double lambda, int nui) {
    //Preconditions.checkArgument(matrix.numCols() == matrix.numRows(), "Must be a Square Matrix");
    double lambdaTimesNui = lambda * nui;
    int numCols = matrix.GetLength(1); //numCols();
    for (int n = 0; n < numCols; n++) {
      matrix[n, n] += matrix[n, n] + lambdaTimesNui;
    }
    return matrix;
  }

  private static double[,] miTimesMiTransposePlusLambdaTimesNuiTimesE(double[,] MiIi, double lambda, int nui) {

    double lambdaTimesNui = lambda * nui;
    int rows = MiIi.GetLength(0); //.numRows();

    double[,] result = new double[rows,rows];

    for (int i = 0; i < rows; i++) {
      for (int j = i; j < rows; j++) {
        double dot = MatrixUtil.vectorDot( MatrixUtil.viewRow( MiIi,i ), MatrixUtil.viewRow(MiIi,j) );
        if (i != j) {
          result[i,j] = dot;
          result[j,i] = dot;
        } else {
          result[i,i] = dot + lambdaTimesNui;
        }
      }
    }
    return result;
  }


  public static double[,] createMiIi(IList<double[]> featureVectors, int numFeatures) {
    double[,] MiIi =  new double[numFeatures,featureVectors.Count];
    int n = 0;
    foreach (var featureVector in featureVectors) {
      for (int m = 0; m < numFeatures; m++) {
        MiIi[m,n] = featureVector[m];
      }
      n++;
    }
    return MiIi;
  }

  public static double[,] createRiIiMaybeTransposed(double[] ratingVector) {
    //Preconditions.checkArgument(ratingVector.isSequentialAccess(), "Ratings should be iterable in Index or Sequential Order");

    double[,] RiIiMaybeTransposed = new double[ratingVector.Length,1];  //getNumNondefaultElements()
    int index = 0;
    foreach (var elem in MatrixUtil.nonZeroes( ratingVector ) ) {
      RiIiMaybeTransposed[index++,0] = elem;
    }
    return RiIiMaybeTransposed;
  }
}

}