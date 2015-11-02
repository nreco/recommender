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

 /// Tests {@link PreferredItemsNeighborhoodCandidateItemsStrategy}
public sealed class PreferredItemsNeighborhoodCandidateItemsStrategyTest : TasteTestCase {

  [Test]
  public void testStrategy() {
    FastIDSet itemIDsFromUser123 = new FastIDSet();
    itemIDsFromUser123.Add(1L);

    FastIDSet itemIDsFromUser456 = new FastIDSet();
    itemIDsFromUser456.Add(1L);
    itemIDsFromUser456.Add(2L);

    List<IPreference> prefs = new List<IPreference>();
    prefs.Add(new GenericPreference(123L, 1L, 1.0f));
    prefs.Add(new GenericPreference(456L, 1L, 1.0f));
    IPreferenceArray preferencesForItem1 = new GenericItemPreferenceArray(prefs);

    var dataModelMock = new DynamicMock(typeof(IDataModel));
	dataModelMock.ExpectAndReturn("GetPreferencesForItem", preferencesForItem1,  (1L));
	dataModelMock.ExpectAndReturn("GetItemIDsFromUser", itemIDsFromUser123, (123L));
	dataModelMock.ExpectAndReturn("GetItemIDsFromUser", itemIDsFromUser456, (456L));

    IPreferenceArray prefArrayOfUser123 =
        new GenericUserPreferenceArray( new List<IPreference>() {new GenericPreference(123L, 1L, 1.0f)} );

    ICandidateItemsStrategy strategy = new PreferredItemsNeighborhoodCandidateItemsStrategy();

    //EasyMock.replay(dataModel);

    FastIDSet candidateItems = strategy.GetCandidateItems(123L, prefArrayOfUser123, (IDataModel)dataModelMock.MockInstance);
    Assert.AreEqual(1, candidateItems.Count());
    Assert.True(candidateItems.Contains(2L));

	dataModelMock.Verify(); //  EasyMock.verify(dataModel);
  }
  
}

}