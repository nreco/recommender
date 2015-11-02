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
 *  
 * Copyright 1999 CERN - European Organization for Nuclear Research.
 * Permission to use, copy, modify, distribute and sell this software and its documentation for any purpose
 * is hereby granted without fee, provided that the above copyright notice appear in all copies and
 * that both that copyright notice and this permission notice appear in supporting documentation.
 * CERN makes no representations about the suitability of this software for any purpose.
 * It is provided "as is" without expressed or implied warranty. * 
 */


using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace NReco.Math3 {

/*

 For an <tt>m x n</tt> matrix <tt>A</tt> with <tt>m >= n</tt>, the QR decomposition is an <tt>m x n</tt>
 orthogonal matrix <tt>Q</tt> and an <tt>n x n</tt> upper triangular matrix <tt>R</tt> so that
 <tt>A = Q*R</tt>.
 <P>
 The QR decomposition always exists, even if the matrix does not have
 full rank, so the constructor will never fail.  The primary use of the
 QR decomposition is in the least squares solution of non-square systems
 of simultaneous linear equations.  This will fail if <tt>isFullRank()</tt>
 returns <tt>false</tt>.
*/
public class QRDecomposition {
  private double[,] q;
  private double[,] r;
  private bool fullRank;
  private int rows;
  private int columns;

   /// Constructs and returns a new QR decomposition object;  computed by Householder reflections; The
   /// decomposed matrices can be retrieved via instance methods of the returned decomposition
   /// object.
   ///
   /// @param a A rectangular matrix.
   /// @throws IllegalArgumentException if <tt>A.rows() < A.columns()</tt>.
  public QRDecomposition(double[,] a) {

	  rows = a.GetLength(0); //rowSize();
	  columns = a.GetLength(1); //columnSize();
    int min = Math.Min( a.GetLength(0) /*a.rowSize()*/, a.GetLength(1) /*a.columnSize()*/);


    double[,] qTmp = (double[,]) a.Clone();

    bool fullRank = true;

    r = new double[min, columns];

    for (int i = 0; i < min; i++) {
		var qi = MatrixUtil.viewColumn(qTmp, i); // qTmp.viewColumn(i);

	  double alpha = MatrixUtil.norm2(qi); //qi.norm(2);
	  if (Math.Abs(alpha) > Double.Epsilon) {  // java Double.MIN_VALUE -> C# Double.Epsilon (hrr)
		  //qi.assign(Functions.div(alpha));
		  for (int rIdx = 0; rIdx < qTmp.GetLength(0); rIdx++) {
			  qi[rIdx] = ( qTmp[rIdx, i] /= alpha );
		  }
      } else {
        if (Double.IsInfinity(alpha) || Double.IsNaN(alpha)) {
          throw new ArithmeticException("Invalid intermediate result");
        }
        fullRank = false;
      }
      r[i, i] = alpha;

      for (int j = i + 1; j < columns; j++) {
		  var qj = MatrixUtil.viewColumn(qTmp, j); // qTmp.viewColumn(j);

		  double norm = MatrixUtil.norm2(qj); // qj.norm(2);
		  if (Math.Abs(norm) > Double.Epsilon) { // java Double.MIN_VALUE -> C# Double.Epsilon (hrr)
          double beta = MatrixUtil.vectorDot( qi, qj);
          r[i, j] = beta;
          if (j < min) {
            //qj.assign(qi, Functions.plusMult(-beta));
			  for (int rIdx = 0; rIdx < qj.Length; rIdx++) {
				  qTmp[rIdx, j] = (qj[rIdx] = qj[rIdx] + qi[rIdx] * (-beta) );
			  }
          }
        } else {
          if (Double.IsInfinity(norm) || Double.IsNaN(norm)) {
            throw new ArithmeticException("Invalid intermediate result");
          }
        }
      }
    }
    if (columns > min) {

		q = MatrixUtil.viewPart(qTmp, 0, rows, 0, min); // qTmp.viewPart(0, rows, 0, min).clone();
    } else {
      q = qTmp;
    }
    this.fullRank = fullRank;
  }

   /// Generates and returns the (economy-sized) orthogonal factor <tt>Q</tt>.
   ///
   /// @return <tt>Q</tt>
  public double[,] getQ() {
    return q;
  }

   /// Returns the upper triangular factor, <tt>R</tt>.
   ///
   /// @return <tt>R</tt>
  public double[,] getR() {
    return r;
  }

   /// Returns whether the matrix <tt>A</tt> has full rank.
   ///
   /// @return true if <tt>R</tt>, and hence <tt>A</tt>, has full rank.
  public bool hasFullRank() {
    return fullRank;
  }

   /// Least squares solution of <tt>A*X = B</tt>; <tt>returns X</tt>.
   ///
   /// @param B A matrix with as many rows as <tt>A</tt> and any number of columns.
   /// @return <tt>X</tt> that minimizes the two norm of <tt>Q*R*X - B</tt>.
   /// @throws IllegalArgumentException if <tt>B.rows() != A.rows()</tt>.
  public double[,] solve(double[,] B) {
    if ( B.GetLength(0)/*B.numRows()*/ != rows) {
      throw new ArgumentException("Matrix row dimensions must agree.");
    }

	int cols = B.GetLength(1); //B.numCols();
    double[,] x = new double[columns, cols];

    // this can all be done a bit more efficiently if we don't actually
    // form explicit versions of Q^T and R but this code isn't so bad
    // and it is much easier to understand
    var qt = MatrixUtil.transpose( getQ() );
    var y = MatrixUtil.times( qt, B);

    var r = getR();
    for (int k = Math.Min(columns, rows) - 1; k >= 0; k--) {
      // X[k,] = Y[k,] / R[k,k], note that X[k,] starts with 0 so += is same as =
      // x.viewRow(k).assign(y.viewRow(k), Functions.plusMult(1 / r.get(k, k)));

	  for (int colIdx = 0; colIdx < x.GetLength(1); colIdx++)
		  x[k, colIdx] = x[k, colIdx] + y[k, colIdx] / r[k, k];
		 

      // Y[0:(k-1),] -= R[0:(k-1),k] * X[k,]
	  var rColumn = MatrixUtil.viewColumn( r, k ); //.viewPart(0, k);
      for (int c = 0; c < cols; c++) {
        //y.viewColumn(c).viewPart(0, k).assign(rColumn, Functions.plusMult(-x.get(k, c)));

		  for (int rowIdx = 0; rowIdx < k; rowIdx++)
			  y[ rowIdx, c ] = y[ rowIdx, c ] + rColumn[rowIdx] * (-x[k,c] );

      }
    }
    return x;
  }

   /// Returns a rough string rendition of a QR.
  public override string ToString() {
    return String.Format("QR({0} x {1},fullRank={2})", rows, columns, hasFullRank());
  }
}

}