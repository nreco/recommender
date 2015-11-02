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
using NReco.CF.Taste.Common;
using NReco.CF.Taste.Impl;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Impl.Model;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;
using NUnit.Mocks;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Recommender {


 /// Tests {@link AllUnknownItemsCandidateItemsStrategyTest}
public sealed class AllUnknownItemsCandidateItemsStrategyTest : TasteTestCase {

  [Test]  
  public void testStrategy() {
    FastIDSet allItemIDs = new FastIDSet();
    allItemIDs.AddAll(new long[] { 1L, 2L, 3L });

    FastIDSet preferredItemIDs = new FastIDSet(1);
    preferredItemIDs.Add(2L);
    
    var dataModelMock = new DynamicMock( typeof( IDataModel ));
	dataModelMock.ExpectAndReturn("GetNumItems", 3);
	dataModelMock.ExpectAndReturn("GetItemIDs", allItemIDs.GetEnumerator());

    IPreferenceArray prefArrayOfUser123 = new GenericUserPreferenceArray( new List<IPreference>() {
        new GenericPreference(123L, 2L, 1.0f) } );

    ICandidateItemsStrategy strategy = new AllUnknownItemsCandidateItemsStrategy();

    //EasyMock.replay(dataModel);


	FastIDSet candidateItems = strategy.GetCandidateItems(123L, prefArrayOfUser123, (IDataModel)dataModelMock.MockInstance);
    Assert.AreEqual(2, candidateItems.Count() );
    Assert.True(candidateItems.Contains(1L));
    Assert.True(candidateItems.Contains(3L));

	dataModelMock.Verify();
    //EasyMock.verify(dataModel);
  }

}

}