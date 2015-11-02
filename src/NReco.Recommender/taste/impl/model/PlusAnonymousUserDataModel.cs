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
 *  Unless required by applicable law or agreed to in writing, software distributed on an
 *  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using NReco.CF.Taste.Common;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Model;


namespace NReco.CF.Taste.Impl.Model {

/// <summary>
/// <para>
/// This <see cref="IDataModel"/> decorator class is useful in a situation where you wish to recommend to a user that
/// doesn't really exist yet in your actual <see cref="IDataModel"/>. For example maybe you wish to recommend DVDs to
/// a user who has browsed a few titles on your DVD store site, but, the user is not yet registered.
/// </para>
///
/// <para>
/// This enables you to temporarily add a temporary user to an existing <see cref="IDataModel"/> in a way that
/// recommenders can then produce recommendations anyway.
/// </para>
/// </summary>
/// <example>
///
/// <code>
/// IDataModel realModel = ...;
/// IDataModel plusModel = new PlusAnonymousUserDataModel(realModel);
/// ...
/// var similarity = new LogLikelihoodSimilarity(realModel); // not plusModel
/// </code>
///
/// <para>
/// But, you may continue to use <code>realModel</code> as input to other components. To recommend, first construct and
/// set the temporary user information on the model and then simply call the recommender. The
/// <code>lock</code> block exists to remind you that this is of course not thread-safe. Only one set
/// of temp data can be inserted into the model and used at one time.
/// </para>
///
/// <code>
/// IRecommender recommender = ...;
/// ...
/// lock(...) {
///   IPreferenceArray tempPrefs = ...;
///   plusModel.SetTempPrefs(tempPrefs);
///   recommender.Recommend(PlusAnonymousUserDataModel.TEMP_USER_ID, 10);
///   plusModel.SetTempPrefs(null);
/// }
/// </code>
/// </example>
public class PlusAnonymousUserDataModel : IDataModel {

  public const long TEMP_USER_ID = Int64.MinValue;
  
  private IDataModel _delegate;
  private IPreferenceArray tempPrefs;
  private FastIDSet prefItemIDs;

  private static Logger log = LoggerFactory.GetLogger(typeof(PlusAnonymousUserDataModel));

  public PlusAnonymousUserDataModel(IDataModel deleg) {
    this._delegate = deleg;
    this.prefItemIDs = new FastIDSet();
  }

  protected IDataModel getDelegate() {
    return _delegate;
  }
  
  public void SetTempPrefs(IPreferenceArray prefs) {
    //Preconditions.checkArgument(prefs != null && prefs.Length() > 0, "prefs is null or empty");
    this.tempPrefs = prefs;
    this.prefItemIDs.Clear();
    for (int i = 0; i < prefs.Length(); i++) {
      this.prefItemIDs.Add(prefs.GetItemID(i));
    }
  }

  public void clearTempPrefs() {
    tempPrefs = null;
    prefItemIDs.Clear();
  }
  
  public virtual IEnumerator<long> GetUserIDs() {
    if (tempPrefs == null) {
      return _delegate.GetUserIDs();
    }
    return new PlusAnonymousUserlongPrimitiveIterator(_delegate.GetUserIDs(), TEMP_USER_ID);
  }

  public virtual IPreferenceArray GetPreferencesFromUser(long userID) {
    if (userID == TEMP_USER_ID) {
      if (tempPrefs == null) {
        throw new NoSuchUserException(TEMP_USER_ID);
      }
      return tempPrefs;
    }
    return _delegate.GetPreferencesFromUser(userID);
  }
  
  public virtual FastIDSet GetItemIDsFromUser(long userID) {
    if (userID == TEMP_USER_ID) {
      if (tempPrefs == null) {
        throw new NoSuchUserException(TEMP_USER_ID);
      }
      return prefItemIDs;
    }
    return _delegate.GetItemIDsFromUser(userID);
  }

  public virtual IEnumerator<long> GetItemIDs() {
    return _delegate.GetItemIDs();
    // Yeah ignoring items that only the plus-one user knows about... can't really happen
  }
  
  public virtual IPreferenceArray GetPreferencesForItem(long itemID) {
    if (tempPrefs == null) {
      return _delegate.GetPreferencesForItem(itemID);
    }
    IPreferenceArray delegatePrefs = null;
    try {
      delegatePrefs = _delegate.GetPreferencesForItem(itemID);
    } catch (NoSuchItemException nsie) {
      // OK. Probably an item that only the anonymous user has
      //if (log.isDebugEnabled()) {
        log.Debug("Item {} unknown", itemID);
      //}
    }
    for (int i = 0; i < tempPrefs.Length(); i++) {
      if (tempPrefs.GetItemID(i) == itemID) {
        return cloneAndMergeInto(delegatePrefs, itemID, tempPrefs.GetUserID(i), tempPrefs.GetValue(i));
      }
    }
    if (delegatePrefs == null) {
      // No, didn't find it among the anonymous user prefs
      throw new NoSuchItemException(itemID);
    }
    return delegatePrefs;
  }

  private static IPreferenceArray cloneAndMergeInto(IPreferenceArray delegatePrefs,
                                                   long itemID,
                                                   long newUserID,
                                                   float value) {

    int length = delegatePrefs == null ? 0 : delegatePrefs.Length();
    int newLength = length + 1;
    IPreferenceArray newPreferenceArray = new GenericItemPreferenceArray(newLength);

    // Set item ID once
    newPreferenceArray.SetItemID(0, itemID);

    int positionToInsert = 0;
    while (positionToInsert < length && newUserID > delegatePrefs.GetUserID(positionToInsert)) {
      positionToInsert++;
    }

    for (int i = 0; i < positionToInsert; i++) {
      newPreferenceArray.SetUserID(i, delegatePrefs.GetUserID(i));
      newPreferenceArray.SetValue(i, delegatePrefs.GetValue(i));
    }
    newPreferenceArray.SetUserID(positionToInsert, newUserID);
    newPreferenceArray.SetValue(positionToInsert, value);
    for (int i = positionToInsert + 1; i < newLength; i++) {
      newPreferenceArray.SetUserID(i, delegatePrefs.GetUserID(i - 1));
      newPreferenceArray.SetValue(i, delegatePrefs.GetValue(i - 1));
    }

    return newPreferenceArray;
  }
  
  public virtual float? GetPreferenceValue(long userID, long itemID) {
    if (userID == TEMP_USER_ID) {
      if (tempPrefs == null) {
        throw new NoSuchUserException(TEMP_USER_ID);
      }
      for (int i = 0; i < tempPrefs.Length(); i++) {
        if (tempPrefs.GetItemID(i) == itemID) {
          return tempPrefs.GetValue(i);
        }
      }
      return null;
    }
    return _delegate.GetPreferenceValue(userID, itemID);
  }

  public virtual DateTime? GetPreferenceTime(long userID, long itemID) {
    if (userID == TEMP_USER_ID) {
      if (tempPrefs == null) {
        throw new NoSuchUserException(TEMP_USER_ID);
      }
      return null;
    }
    return _delegate.GetPreferenceTime(userID, itemID);
  }

  public virtual int GetNumItems() {
    return _delegate.GetNumItems();
  }

  public virtual int GetNumUsers() {
    return _delegate.GetNumUsers() + (tempPrefs == null ? 0 : 1);
  }

  public virtual int GetNumUsersWithPreferenceFor(long itemID) {
    if (tempPrefs == null) {
      return _delegate.GetNumUsersWithPreferenceFor(itemID);
    }
    bool found = false;
    for (int i = 0; i < tempPrefs.Length(); i++) {
      if (tempPrefs.GetItemID(i) == itemID) {
        found = true;
        break;
      }
    }
    return _delegate.GetNumUsersWithPreferenceFor(itemID) + (found ? 1 : 0);
  }

  public virtual int GetNumUsersWithPreferenceFor(long itemID1, long itemID2) {
    if (tempPrefs == null) {
      return _delegate.GetNumUsersWithPreferenceFor(itemID1, itemID2);
    }
    bool found1 = false;
    bool found2 = false;
    for (int i = 0; i < tempPrefs.Length() && !(found1 && found2); i++) {
      long itemID = tempPrefs.GetItemID(i);
      if (itemID == itemID1) {
        found1 = true;
      }
      if (itemID == itemID2) {
        found2 = true;
      }
    }
    return _delegate.GetNumUsersWithPreferenceFor(itemID1, itemID2) + (found1 && found2 ? 1 : 0);
  }
  
  public virtual void SetPreference(long userID, long itemID, float value) {
    if (userID == TEMP_USER_ID) {
      if (tempPrefs == null) {
        throw new NoSuchUserException(TEMP_USER_ID);
      }
      throw new NotSupportedException();
    }
    _delegate.SetPreference(userID, itemID, value);
  }
  
  public virtual void RemovePreference(long userID, long itemID) {
    if (userID == TEMP_USER_ID) {
      if (tempPrefs == null) {
        throw new NoSuchUserException(TEMP_USER_ID);
      }
      throw new NotSupportedException();
    }
    _delegate.RemovePreference(userID, itemID);
  }
  
  public virtual void Refresh(IList<IRefreshable> alreadyRefreshed) {
    _delegate.Refresh(alreadyRefreshed);
  }

  public virtual bool HasPreferenceValues() {
    return _delegate.HasPreferenceValues();
  }

  public virtual float GetMaxPreference() {
    return _delegate.GetMaxPreference();
  }

  public virtual float GetMinPreference() {
    return _delegate.GetMinPreference();
  }

}

}