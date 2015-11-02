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
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS 
 *  OF ANY KIND, either express or implied.
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;


using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Impl.Model;
using NReco.CF;
using NReco.CF.Taste.Model;

using NUnit.Framework;

namespace NReco.CF.Taste.Impl {


public abstract class TasteTestCase /*: MahoutTestCase*/ {

	public const double EPSILON = 0.000001;

	[SetUp]
	public virtual void SetUp() {
		RandomUtils.useTestSeed();
	}

  public static IDataModel getDataModel(long[] userIDs, double?[][] prefValues) {
    FastByIDMap<IPreferenceArray> result = new FastByIDMap<IPreferenceArray>();
    for (int i = 0; i < userIDs.Length; i++) {
		List<IPreference> prefsList = new List<IPreference>();
      for (int j = 0; j < prefValues[i].Length; j++) {
        if (prefValues[i][j].HasValue) {
          prefsList.Add(new GenericPreference(userIDs[i], j, (float) prefValues[i][j].Value ));
        }
      }
      if (prefsList.Count>0) {
        result.Put(userIDs[i], new GenericUserPreferenceArray(prefsList));
      }
    }
    return new GenericDataModel(result);
  }

  public static IDataModel getBooleanDataModel(long[] userIDs, bool[][] prefs) {
    FastByIDMap<FastIDSet> result = new FastByIDMap<FastIDSet>();
    for (int i = 0; i < userIDs.Length; i++) {
      FastIDSet prefsSet = new FastIDSet();
      for (int j = 0; j < prefs[i].Length; j++) {
        if (prefs[i][j]) {
          prefsSet.Add(j);
        }
      }
      if (!prefsSet.IsEmpty()) {
        result.Put(userIDs[i], prefsSet);
      }
    }
    return new GenericBooleanPrefDataModel(result);
  }

  protected static IDataModel getDataModel() {
    return getDataModel(
            new long[] {1, 2, 3, 4},
            new double?[][] {
                    new double?[] {0.1, 0.3},
                    new double?[] {0.2, 0.3, 0.3},
                    new double?[] {0.4, 0.3, 0.5},
                    new double?[] {0.7, 0.3, 0.8},
            });
  }

  protected static IDataModel getBooleanDataModel() {
    return getBooleanDataModel(new long[] {1, 2, 3, 4},
                               new bool[][] {
                                   new[]{false, true,  false},
                                   new[]{false, true,  true,  false},
                                   new[]{true,  false, false, true},
                                   new[]{true,  false, true,  true},
                               });
  }

  protected static bool arrayContains(long[] array, long value) {
    foreach (long l in array) {
      if (l == value) {
        return true;
      }
    }
    return false;
  }

}

}