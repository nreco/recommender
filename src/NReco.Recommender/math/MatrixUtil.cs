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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NReco.Math3 {
	
	public static class MatrixUtil {

		public static IEnumerable<double> nonZeroes(double[] vector) {
			for (int i = 0; i < vector.Length; i++)
				if (vector[i] != 0.0)
					yield return vector[i];
		}

		public static double vectorDot(double[] v1, double[] v2) {
			double r = 0d;
			for (int i = 0; i < v1.Length; i++) {
				r += (v1[i] * v2[i]);
			}
			return r;
		}

		public static void WriteVector(string msg, double[] v) {
			Console.Write("{0}: ",msg);
			foreach (var x in v)
				Console.Write("{0} ", x);
			Console.WriteLine();
		}

		public static double[] viewColumn(double[,] m, int column) {
			var v = new double[m.GetLength(0)];
			for (int i = 0; i < v.Length; i++) {
				v[i] = m[i, column];
			}
			return v;
		}

		public static double[] viewRow(double[,] m, int row) {
			var v = new double[m.GetLength(1)];
			for (int i = 0; i < v.Length; i++) {
				v[i] = m[row, i];
			}
			return v;
		}

		public static double[] viewDiagonal(double[,] m) {
			var v = new double[Math.Min(m.GetLength(0), m.GetLength(1))];
			for (int i = 0; i < v.Length; i++)
				v[i] = m[i, i];
			return v;
		}

		public static double norm2(double[] v) {
			return Math.Sqrt( vectorDot(v,v) );
		}

		public static double norm1(double[] v) {
			double res = 0;
			for (int i = 0; i < v.Length; i++)
				res += Math.Abs(v[i]);
			return res;
		}

		public static double[,] viewPart(double[,] m, int rowOff, int rowRequested, int colOff, int colRequested) {
			var r = new double[rowRequested - rowOff, colRequested - colOff];
			for (int i = rowOff; i < rowRequested; i++)
				for (int j = colOff; j < colRequested; j++)
					r[i - rowOff, j - colOff] = m[i, j];
			return r;
		}

		public static double[,] transpose(double[,] m) {
			int rows = m.GetLength(0); //rowSize();
			int columns = m.GetLength(1); //columnSize();
			var result = new double[columns, rows];
			for (int row = 0; row < rows; row++) {
				for (int col = 0; col < columns; col++) {
					result[col, row] = m[row, col]; //.setQuick(col, row, getQuick(row, col));
				}
			}
			return result;
		}

		public static void assign(double[,] m, Func<double, double> f) {
			var rLen = m.GetLength(0);
			var cLen = m.GetLength(1);
			for (int i = 0; i < rLen; i++)
				for (int j = 0; j < cLen; j++)
					m[i, j] = f( m[i,j] );
		}

		public static void assign(double[] v, Func<double, double> f) {
			for (int i = 0; i < v.Length; i++)
				v[i] = f(v[i]);
		}

		public static double[,] times(double[,] m, double[,] other) {
			int columns = m.GetLength(1); //columnSize();
			if (columns != other.GetLength(0)) { //.rowSize()
				throw new System.Exception();
			}
			int rows = m.GetLength(0); //rowSize();
			int otherColumns = other.GetLength(1);  //columnSize();
			double[,] result = new double[rows, otherColumns];
			for (int row = 0; row < rows; row++) {
				for (int col = 0; col < otherColumns; col++) {
					double sum = 0.0;
					for (int k = 0; k < columns; k++) {
						sum += m[row, k] * other[k, col];
					}
					result[row, col] = sum;
				}
			}
			return result;
		}

		public static double[] times(double[] v, double x) {
			var vCopy = new double[v.Length];
			for (int i = 0; i < v.Length; i++)
				vCopy[i] = v[i] * x;
			return vCopy;
		}


	}
}
