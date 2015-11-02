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
using NReco.CF.Taste.Impl.Neighborhood;
using NReco.CF.Taste.Impl.Recommender;
using NReco.CF.Taste.Impl.Similarity;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Neighborhood;
using NReco.CF.Taste.Recommender;
using NReco.CF.Taste.Similarity;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Model.File {


/// <p>Tests {@link FileDataModel}.</p> 
public sealed class FileDataModelTest : TasteTestCase {

  private static string[] DATA = {
      "123,456,0.1",
      "123,789,0.6",
      "123,654,0.7",
      "234,123,0.5",
      "234,234,1.0",
      "234,999,0.9",
      "345,789,0.6",
      "345,654,0.7",
      "345,123,1.0",
      "345,234,0.5",
      "345,999,0.5",
      "456,456,0.1",
      "456,789,0.5",
      "456,654,0.0",
      "456,999,0.2",};


  
  private static string[] DATA_SPLITTED_WITH_TWO_SPACES = {
      "123  456  0.1",
      "123  789  0.6",
      "123  654  0.7",
      "234  123  0.5",
      "234  234  1.0",
      "234  999  0.9",
      "345  789  0.6",
      "345  654  0.7",
      "345  123  1.0",
      "345  234  0.5",
      "345  999  0.5",
      "456  456  0.1",
      "456  789  0.5",
      "456  654  0.0",
      "456  999  0.2",};

  private IDataModel model;
  private string testFileName;

  [SetUp]
  public override void SetUp() {
    base.SetUp();
    testFileName = Path.Combine( Path.GetTempPath(), "test.txt");
    System.IO.File.WriteAllLines(testFileName, DATA);
    model = new FileDataModel(testFileName);
  }

	[TearDown]
	public void TearDown() {
		if (System.IO.File.Exists(testFileName))
			System.IO.File.Delete(testFileName);
	}

  [Test]
  public void testReadRegexSplittedFile() {
		var testRegexFileName = Path.Combine(Path.GetTempPath(), "testRegex.txt");
		System.IO.File.WriteAllLines(testRegexFileName, DATA_SPLITTED_WITH_TWO_SPACES);

		try {
			FileDataModel model = new FileDataModel(testRegexFileName, "\\s+");
			Assert.AreEqual(3, model.GetItemIDsFromUser(123).Count() );
			Assert.AreEqual(4, model.GetItemIDsFromUser(456).Count());
		} finally {
			if (System.IO.File.Exists(testRegexFileName))
				System.IO.File.Delete(testRegexFileName);
		}
  }


  [Test]
  public void testFile() {
    IUserSimilarity userSimilarity = new PearsonCorrelationSimilarity(model);
    IUserNeighborhood neighborhood = new NearestNUserNeighborhood(3, userSimilarity, model);
    IRecommender recommender = new GenericUserBasedRecommender(model, neighborhood, userSimilarity);
    Assert.AreEqual(1, recommender.Recommend(123, 3).Count);
    Assert.AreEqual(0, recommender.Recommend(234, 3).Count);
    Assert.AreEqual(1, recommender.Recommend(345, 3).Count);

    // Make sure this doesn't throw an exception
    model.Refresh(null);
  }

  [Test]
  public void testTranspose() {
    FileDataModel tModel = new FileDataModel(testFileName, true, FileDataModel.DEFAULT_MIN_RELOAD_INTERVAL_MS);
    IPreferenceArray userPrefs = tModel.GetPreferencesFromUser(456);
    Assert.NotNull(userPrefs, "user prefs are null and it shouldn't be");
    IPreferenceArray pref = tModel.GetPreferencesForItem(123);
    Assert.NotNull(pref, "pref is null and it shouldn't be");
    Assert.AreEqual(3, pref.Length(), "pref Size: " + pref.Length().ToString() + " is not: " + 3 );
  }

  [Test] //(expected = NoSuchElementException.class)
  public void testGetItems() {
    var it = model.GetItemIDs();
    Assert.NotNull(it);
    Assert.True(it.MoveNext());
    Assert.AreEqual(123, it.Current);
    Assert.True(it.MoveNext());
    Assert.AreEqual(234, it.Current);
    Assert.True(it.MoveNext());
    Assert.AreEqual(456, it.Current);
    Assert.True(it.MoveNext());
    Assert.AreEqual(654, it.Current);
    Assert.True(it.MoveNext());
    Assert.AreEqual(789, it.Current);
    Assert.True(it.MoveNext());
    Assert.AreEqual(999, it.Current);
    Assert.False(it.MoveNext());
    it.MoveNext(); // exception
  }

  [Test]
  public void testPreferencesForItem() {
    IPreferenceArray prefs = model.GetPreferencesForItem(456);
    Assert.NotNull(prefs);
    IPreference pref1 = prefs.Get(0);
    Assert.AreEqual(123, pref1.GetUserID());
    Assert.AreEqual(456, pref1.GetItemID());
    IPreference pref2 = prefs.Get(1);
    Assert.AreEqual(456, pref2.GetUserID());
    Assert.AreEqual(456, pref2.GetItemID());
    Assert.AreEqual(2, prefs.Length());
  }

  [Test]
  public void testGetNumUsers() {
    Assert.AreEqual(4, model.GetNumUsers());
  }

  [Test]
  public void testNumUsersPreferring() {
    Assert.AreEqual(2, model.GetNumUsersWithPreferenceFor(456));
    Assert.AreEqual(0, model.GetNumUsersWithPreferenceFor(111));
    Assert.AreEqual(0, model.GetNumUsersWithPreferenceFor(111, 456));
    Assert.AreEqual(2, model.GetNumUsersWithPreferenceFor(123, 234));
  }

  [Test]
  public void testRefresh() {
    var initialized = false;
	  System.Threading.Tasks.Task.Factory.StartNew( () => {
		  model.GetNumUsers();
		  initialized=true;
	  });
   /* Runnable initializer = new Runnable() {
      public void run() {
        try {
          model.getNumUsers();
          initialized.setValue(true);
        } catch (TasteException te) {
          // oops
        }
      }
    };
      Thread(initializer).start();*/
    System.Threading.Thread.Sleep(1000); // wait a second for thread to start and call getNumUsers()
    model.GetNumUsers(); // should block
    Assert.True(initialized);
    Assert.AreEqual(4, model.GetNumUsers());
  }

  [Test]
  public void testExplicitRefreshAfterCompleteFileUpdate() {
    var file = Path.Combine( Path.GetTempPath(), "refresh");
	  try {
		  System.IO.File.WriteAllLines(file, new string[] { "123,456,3.0" });

			/// create a FileDataModel that always reloads when the underlying file has changed 
			FileDataModel dataModel = new FileDataModel(file, false, 0L);
			Assert.AreEqual(3.0f, dataModel.GetPreferenceValue(123L, 456L), EPSILON);

			/// change the underlying file,
			 /// we have to wait at least a second to see the change in the file's lastModified timestamp 
			System.Threading.Thread.Sleep(2000);
			System.IO.File.WriteAllLines(file, new string[] { "123,456,5.0" });
			dataModel.Refresh(null);

			Assert.AreEqual(5.0f, dataModel.GetPreferenceValue(123L, 456L), EPSILON);
	  } finally {
		  if (System.IO.File.Exists(file))
			  System.IO.File.Delete(file);
	  }
  }

  [Test]
  public void testToString() {
    Assert.False( String.IsNullOrEmpty( model.ToString() ));
  }

[Test]//(expected = IllegalArgumentException.class)
[ExpectedException(typeof(ArgumentException))]
  public void testEmptyFile() {
	  var file = Path.Combine(Path.GetTempPath(), "empty");
	  try {
		  System.IO.File.Create(file).Close(); //required to create file.
		  new FileDataModel(file);
	  } finally {
		  if (System.IO.File.Exists(file))
			  try { System.IO.File.Delete(file); } catch { }
	  }
  }
}

}