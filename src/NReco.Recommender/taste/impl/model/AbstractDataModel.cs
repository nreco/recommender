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
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Common;
using NReco.CF.Taste.Impl.Common;

namespace NReco.CF.Taste.Impl.Model {

/// <summary>
/// Contains some features common to all implementations.
/// </summary>
[Serializable]
public abstract class AbstractDataModel : IDataModel {

  private float maxPreference;
  private float minPreference;

  protected AbstractDataModel() {
    maxPreference = float.NaN;
    minPreference = float.NaN;
  }

  public abstract IEnumerator<long> GetUserIDs();

  public abstract IPreferenceArray GetPreferencesFromUser(long userID);

  public abstract FastIDSet GetItemIDsFromUser(long userID);

  public abstract IEnumerator<long> GetItemIDs();

  public abstract void Refresh(IList<IRefreshable> alreadyRefreshed);

  public abstract IPreferenceArray GetPreferencesForItem(long itemID);

  public abstract int GetNumItems();

  public abstract int GetNumUsers();

  public abstract int GetNumUsersWithPreferenceFor(long itemID);

  public abstract int GetNumUsersWithPreferenceFor(long itemID1, long itemID2);

  public abstract float? GetPreferenceValue(long userID, long itemID);

  public abstract DateTime? GetPreferenceTime(long userID, long itemID);

  public abstract bool HasPreferenceValues();

  public abstract void SetPreference(long userID, long itemID, float value);

  public abstract void RemovePreference(long userID, long itemID);

  public virtual float GetMaxPreference() {
    return maxPreference;
  }

  protected virtual void setMaxPreference(float maxPreference) {
    this.maxPreference = maxPreference;
  }

  public virtual float GetMinPreference() {
    return minPreference;
  }

  protected virtual void setMinPreference(float minPreference) {
    this.minPreference = minPreference;
  }

}

}