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
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Impl.Common;
using NReco.CF;
using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Model {

public sealed class PlusAnonymousConcurrentUserDataModelTest : TasteTestCase {

	 /// Prepares a testable object without delegate data
	private static PlusAnonymousConcurrentUserDataModel getTestableWithoutDelegateData(int maxConcurrentUsers) {
		FastByIDMap<IPreferenceArray> delegatePreferences = new FastByIDMap<IPreferenceArray>();
		return new PlusAnonymousConcurrentUserDataModel(new GenericDataModel(delegatePreferences), maxConcurrentUsers);
	}

	 /// Prepares a testable object with delegate data
  private static PlusAnonymousConcurrentUserDataModel getTestableWithDelegateData(
        int maxConcurrentUsers, FastByIDMap<IPreferenceArray> delegatePreferences) {
		return new PlusAnonymousConcurrentUserDataModel(new GenericDataModel(delegatePreferences), maxConcurrentUsers);
	}

	 /// Test taking the first available user
	[Test]
	public void testTakeFirstAvailableUser() {
		PlusAnonymousConcurrentUserDataModel instance = getTestableWithoutDelegateData(10);
		long expResult = PlusAnonymousUserDataModel.TEMP_USER_ID;
		long? result = instance.TakeAvailableUser();
		Assert.AreEqual(expResult, result);
	}

	 /// Test taking the next available user
	[Test]
	public void testTakeNextAvailableUser() {
		PlusAnonymousConcurrentUserDataModel instance = getTestableWithoutDelegateData(10);
    // Skip first user
    instance.TakeAvailableUser();
		long? result = instance.TakeAvailableUser();
    long expResult = PlusAnonymousUserDataModel.TEMP_USER_ID + 1;
    Assert.AreEqual(expResult, result);
	}

	 /// Test taking an unavailable user
	[Test]
	public void testTakeUnavailableUser() {
		PlusAnonymousConcurrentUserDataModel instance = getTestableWithoutDelegateData(1);
		// Take the only available user
		instance.TakeAvailableUser();
		// There are no more users available
		Assert.IsNull(instance.TakeAvailableUser());
	}

	 /// Test releasing a valid previously taken user
	[Test]
	public void testReleaseValidUser() {
		PlusAnonymousConcurrentUserDataModel instance = getTestableWithoutDelegateData(10);
		long? takenUserID = instance.TakeAvailableUser();
		Assert.True(instance.ReleaseUser(takenUserID.Value));
	}

	 /// Test releasing an invalid user
	[Test]
	public void testReleaseInvalidUser() {
		PlusAnonymousConcurrentUserDataModel instance = getTestableWithoutDelegateData(10);
		Assert.False(instance.ReleaseUser(long.MaxValue));
	}

	 /// Test releasing a user which had been released earlier
	[Test]
	public void testReleasePreviouslyReleasedUser() {
		PlusAnonymousConcurrentUserDataModel instance = getTestableWithoutDelegateData(10);
		long takenUserID = instance.TakeAvailableUser().Value;
		Assert.True(instance.ReleaseUser(takenUserID));
		Assert.False(instance.ReleaseUser(takenUserID));
	}

	 /// Test setting anonymous user preferences
	[Test]
	public void testSetAndGetTempPreferences() {
		PlusAnonymousConcurrentUserDataModel instance = getTestableWithoutDelegateData(10);
		long anonymousUserID = instance.TakeAvailableUser().Value;
		IPreferenceArray tempPrefs = new GenericUserPreferenceArray(1);
		tempPrefs.SetUserID(0, anonymousUserID);
		tempPrefs.SetItemID(0, 1);
		instance.SetTempPrefs(tempPrefs, anonymousUserID);
		Assert.AreEqual(tempPrefs, instance.GetPreferencesFromUser(anonymousUserID));
		instance.ReleaseUser(anonymousUserID);
	}

	 /// Test setting and getting preferences from several concurrent anonymous users
	[Test]
	public void testSetMultipleTempPreferences() {
		PlusAnonymousConcurrentUserDataModel instance = getTestableWithoutDelegateData(10);

		long anonymousUserID1 = instance.TakeAvailableUser().Value;
		long anonymousUserID2 = instance.TakeAvailableUser().Value;

		IPreferenceArray tempPrefs1 = new GenericUserPreferenceArray(1);
		tempPrefs1.SetUserID(0, anonymousUserID1);
		tempPrefs1.SetItemID(0, 1);

		IPreferenceArray tempPrefs2 = new GenericUserPreferenceArray(2);
		tempPrefs2.SetUserID(0, anonymousUserID2);
		tempPrefs2.SetItemID(0, 2);
		tempPrefs2.SetUserID(1, anonymousUserID2);
		tempPrefs2.SetItemID(1, 3);

		instance.SetTempPrefs(tempPrefs1, anonymousUserID1);
		instance.SetTempPrefs(tempPrefs2, anonymousUserID2);

		Assert.AreEqual(tempPrefs1, instance.GetPreferencesFromUser(anonymousUserID1));
		Assert.AreEqual(tempPrefs2, instance.GetPreferencesFromUser(anonymousUserID2));
	}

	 /// Test counting the number of delegate users
	[Test]
	public void testGetNumUsersWithDelegateUsersOnly() {
    IPreferenceArray prefs = new GenericUserPreferenceArray(1);
    long sampleUserID = 1;
		prefs.SetUserID(0, sampleUserID);
    long sampleItemID = 11;
    prefs.SetItemID(0, sampleItemID);

		FastByIDMap<IPreferenceArray> delegatePreferences = new FastByIDMap<IPreferenceArray>();
		delegatePreferences.Put(sampleUserID, prefs);

		PlusAnonymousConcurrentUserDataModel instance = getTestableWithDelegateData(10, delegatePreferences);

		Assert.AreEqual(1, instance.GetNumUsers());
	}

	 /// Test counting the number of anonymous users
	[Test]
	public void testGetNumAnonymousUsers() {
		PlusAnonymousConcurrentUserDataModel instance = getTestableWithoutDelegateData(10);

		long anonymousUserID1 = instance.TakeAvailableUser().Value;

		IPreferenceArray tempPrefs1 = new GenericUserPreferenceArray(1);
		tempPrefs1.SetUserID(0, anonymousUserID1);
		tempPrefs1.SetItemID(0, 1);

		instance.SetTempPrefs(tempPrefs1, anonymousUserID1);

		// Anonymous users should not be included into the universe.
		Assert.AreEqual(0, instance.GetNumUsers());
	}

	 /// Test retrieve a single preference value of an anonymous user
	[Test]
	public void testGetPreferenceValue() {
		PlusAnonymousConcurrentUserDataModel instance = getTestableWithoutDelegateData(10);

		long anonymousUserID = instance.TakeAvailableUser().Value;

		IPreferenceArray tempPrefs = new GenericUserPreferenceArray(1);
    tempPrefs.SetUserID(0, anonymousUserID);
    long sampleItemID = 1;
    tempPrefs.SetItemID(0, sampleItemID);
    tempPrefs.SetValue(0, float.MaxValue);

		instance.SetTempPrefs(tempPrefs, anonymousUserID);

		Assert.AreEqual(float.MaxValue, instance.GetPreferenceValue(anonymousUserID, sampleItemID), EPSILON);
	}

	 /// Test retrieve preferences for existing non-anonymous user
	[Test]
	public void testGetPreferencesForNonAnonymousUser() {
    IPreferenceArray prefs = new GenericUserPreferenceArray(1);
    long sampleUserID = 1;
		prefs.SetUserID(0, sampleUserID);
    long sampleItemID = 11;
    prefs.SetItemID(0, sampleItemID);

		FastByIDMap<IPreferenceArray> delegatePreferences = new FastByIDMap<IPreferenceArray>();
		delegatePreferences.Put(sampleUserID, prefs);

		PlusAnonymousConcurrentUserDataModel instance = getTestableWithDelegateData(10, delegatePreferences);

		Assert.AreEqual(prefs, instance.GetPreferencesFromUser(sampleUserID));
	}

	 /// Test retrieve preferences for non-anonymous and non-existing user
	[Test]
	[ExpectedException(typeof(NoSuchUserException))]
	public void testGetPreferencesForNonExistingUser() {
		PlusAnonymousConcurrentUserDataModel instance = getTestableWithoutDelegateData(10);
		// Exception is expected since such user does not exist
		instance.GetPreferencesFromUser(1);
	}

	 /// Test retrieving the user IDs and verifying that anonymous ones are not included
	[Test]
	public void testGetUserIDs() {
    IPreferenceArray prefs = new GenericUserPreferenceArray(1);
    long sampleUserID = 1;
		prefs.SetUserID(0, sampleUserID);
    long sampleItemID = 11;
    prefs.SetItemID(0, sampleItemID);

		FastByIDMap<IPreferenceArray> delegatePreferences = new FastByIDMap<IPreferenceArray>();
		delegatePreferences.Put(sampleUserID, prefs);

		PlusAnonymousConcurrentUserDataModel instance = getTestableWithDelegateData(10, delegatePreferences);

		long anonymousUserID = instance.TakeAvailableUser().Value;

		IPreferenceArray tempPrefs = new GenericUserPreferenceArray(1);
		tempPrefs.SetUserID(0, anonymousUserID);
		tempPrefs.SetItemID(0, 22);

		instance.SetTempPrefs(tempPrefs, anonymousUserID);

		var userIDs = instance.GetUserIDs();
		userIDs.MoveNext();

		Assert.AreEqual(sampleUserID, userIDs.Current);
		Assert.False(userIDs.MoveNext());
	}

	 /// Test getting preferences for an item.
	 ///
	 /// @throws TasteException
	[Test]
	public void testGetPreferencesForItem() {
    IPreferenceArray prefs = new GenericUserPreferenceArray(2);
    long sampleUserID = 4;
		prefs.SetUserID(0, sampleUserID);
    long sampleItemID = 11;
    prefs.SetItemID(0, sampleItemID);
		prefs.SetUserID(1, sampleUserID);
    long sampleItemID2 = 22;
    prefs.SetItemID(1, sampleItemID2);

		FastByIDMap<IPreferenceArray> delegatePreferences = new FastByIDMap<IPreferenceArray>();
		delegatePreferences.Put(sampleUserID, prefs);

		PlusAnonymousConcurrentUserDataModel instance = getTestableWithDelegateData(10, delegatePreferences);

		long anonymousUserID = instance.TakeAvailableUser().Value;

		IPreferenceArray tempPrefs = new GenericUserPreferenceArray(2);
		tempPrefs.SetUserID(0, anonymousUserID);
		tempPrefs.SetItemID(0, sampleItemID);
		tempPrefs.SetUserID(1, anonymousUserID);
    long sampleItemID3 = 33;
    tempPrefs.SetItemID(1, sampleItemID3);

		instance.SetTempPrefs(tempPrefs, anonymousUserID);

		Assert.AreEqual(sampleUserID, instance.GetPreferencesForItem(sampleItemID).Get(0).GetUserID());
		Assert.AreEqual(2, instance.GetPreferencesForItem(sampleItemID).Length());
		Assert.AreEqual(1, instance.GetPreferencesForItem(sampleItemID2).Length());
		Assert.AreEqual(1, instance.GetPreferencesForItem(sampleItemID3).Length());

		Assert.AreEqual(2, instance.GetNumUsersWithPreferenceFor(sampleItemID));
		Assert.AreEqual(1, instance.GetNumUsersWithPreferenceFor(sampleItemID, sampleItemID2));
		Assert.AreEqual(1, instance.GetNumUsersWithPreferenceFor(sampleItemID, sampleItemID3));
	}

}

}