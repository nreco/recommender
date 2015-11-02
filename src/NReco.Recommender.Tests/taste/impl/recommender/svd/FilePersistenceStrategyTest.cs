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

using NReco.CF.Taste.Impl;
using NReco.CF.Taste.Impl.Common;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Recommender.SVD {


public class FilePersistenceStrategyTest : TasteTestCase {

  [Test]
  public void persistAndLoad() {
    FastByIDMap<int?> userIDMapping = new FastByIDMap<int?>();
    FastByIDMap<int?> itemIDMapping = new FastByIDMap<int?>();

    userIDMapping.Put(123, 0);
    userIDMapping.Put(456, 1);

    itemIDMapping.Put(12, 0);
    itemIDMapping.Put(34, 1);

    double[][] userFeatures = new double[][] { new double[] { 0.1, 0.2, 0.3 }, new double[] { 0.4, 0.5, 0.6 } };
    double[][] itemFeatures = new double[][] { new double[] { 0.7, 0.8, 0.9 }, new double[] { 1.0, 1.1, 1.2 } };

    Factorization original = new Factorization(userIDMapping, itemIDMapping, userFeatures, itemFeatures);
    var storage = Path.Combine( Path.GetTempPath(), "storage.bin");
	try {
		IPersistenceStrategy persistenceStrategy = new FilePersistenceStrategy(storage);

		Assert.IsNull(persistenceStrategy.Load());

		persistenceStrategy.MaybePersist(original);
		Factorization clone = persistenceStrategy.Load();

		Assert.True(original.Equals( clone ) );
	} finally {
		if (File.Exists(storage))
			try { File.Delete(storage); } catch { }
	}
  }
}

}