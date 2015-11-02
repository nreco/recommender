/*
 *  Copyright 2013-2014 Vitalii Fedorchenko
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
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS 
 *  OF ANY KIND, either express or implied.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NReco.CF.Taste.Common {
	
	public static class Utils {
		

		public static int GetArrayHashCode(Array arr) {
			int arrHash = arr.Length;
			for (int i = 0; i < arr.Length; ++i) {
				arrHash = arrHash ^ arr.GetValue(i).GetHashCode();
			}
			return arrHash;
		}

		public static int GetArrayDeepHashCode(Array arr) {
			int arrHash = arr.Length;
			for (int i = 0; i < arr.Length; ++i) {
				var val = arr.GetValue(i);
				var valHashCode = val is Array ? GetArrayDeepHashCode( (Array)val ) : val.GetHashCode();
				arrHash = arrHash ^ valHashCode;
			}
			return arrHash;
		}

		public static bool ArrayDeepEquals(Array arr1, Array arr2) {
			if (arr1.Length!=arr2.Length || arr1.GetType()!=arr2.GetType())
				return false;

			for (int i=0; i<arr1.Length; i++) {
				var v1 = arr1.GetValue(i);
				var v2 = arr2.GetValue(i);
				if (v1 is Array && v2 is Array)
					if (!ArrayDeepEquals((Array)v1, (Array)v2)) {
						return false;
					} else
						continue;

				if (v1==null && v2==null)
					continue;

				if (v1!=null)
					if (!v1.Equals(v2))
						return false;
				if (v2 != null)
					if (!v2.Equals(v1))
						return false;
			}
			return true;
		}



	}


}
