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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using NReco.CF.Taste.Impl;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Model {

 /// Tests {@link GenericDataModel}.
public sealed class GenericDataModelTest : TasteTestCase {

  [Test]  
  public void testSerialization() {
    GenericDataModel model = (GenericDataModel) getDataModel();
    
	  var formatter = new BinaryFormatter();
	  var ms = new MemoryStream();
		formatter.Serialize(ms, model);
	  ms.Position = 0;

	  GenericDataModel newModel = (GenericDataModel)formatter.Deserialize(ms);

    Assert.AreEqual(model.GetNumItems(), newModel.GetNumItems());
    Assert.AreEqual(model.GetNumUsers(), newModel.GetNumUsers());
    Assert.True(model.GetPreferencesFromUser(1L).Equals( newModel.GetPreferencesFromUser(1L) ) );    
    Assert.True(model.GetPreferencesForItem(1L).Equals(newModel.GetPreferencesForItem(1L) ) );
    Assert.AreEqual(model.GetRawUserData(), newModel.GetRawUserData());
  }

  // Lots of other stuff should be tested but is kind of covered by FileDataModelTest

}

}