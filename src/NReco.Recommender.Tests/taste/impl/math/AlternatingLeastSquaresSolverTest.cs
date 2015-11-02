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
using NUnit.Framework;

namespace NReco.Math3.Als {


public class AlternatingLeastSquaresSolverTest  {

	public const double EPSILON = 0.000001;

  [Test]
  public void addLambdaTimesNuiTimesE() {
    int nui = 5;
    double lambda = 0.2;
    var matrix = new double[5, 5];

    AlternatingLeastSquaresSolver.addLambdaTimesNuiTimesE(matrix, lambda, nui);

    for (int n = 0; n < 5; n++) {
      Assert.AreEqual(1.0, matrix[n, n], EPSILON);
    }
  }

  [Test]
  public void createMiIi() {
    var f1 = new double[] { 1, 2, 3 };
    var f2 = new double[] { 4, 5, 6 };

    var miIi = AlternatingLeastSquaresSolver.createMiIi( new[] { f1, f2 }, 3);

    Assert.AreEqual(1.0, miIi[0, 0], EPSILON);
    Assert.AreEqual(2.0, miIi[1, 0], EPSILON);
    Assert.AreEqual(3.0, miIi[2, 0], EPSILON);
    Assert.AreEqual(4.0, miIi[0, 1], EPSILON);
    Assert.AreEqual(5.0, miIi[1, 1], EPSILON);
    Assert.AreEqual(6.0, miIi[2, 1], EPSILON);
  }

  [Test]
  public void createRiIiMaybeTransposed() {
    var ratings = new double[]{ 0, 1.0, 0, 3.0, 0, 5.0 } ;
	/* SequentialAccessSparseVector(3);
    ratings.setQuick(1, 1.0);
    ratings.setQuick(3, 3.0);
    ratings.setQuick(5, 5.0);*/

    var riIiMaybeTransposed = AlternatingLeastSquaresSolver.createRiIiMaybeTransposed(ratings);
    Assert.AreEqual(1, riIiMaybeTransposed.GetLength(1) /* .numCols()*/, 1);
    Assert.AreEqual(3, riIiMaybeTransposed.GetLength(0) /*.numRows()*/, 3);

    Assert.AreEqual(1.0, riIiMaybeTransposed[0, 0], EPSILON);
    Assert.AreEqual(3.0, riIiMaybeTransposed[1, 0], EPSILON);
    Assert.AreEqual(5.0, riIiMaybeTransposed[2, 0], EPSILON);
  }


}

}