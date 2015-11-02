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
using NReco.CF.Taste.Recommender;

using NReco.CF.Taste.Common;
using NReco.CF.Taste.Impl.Common;


namespace NReco.CF.Taste.Impl.Recommender {

public abstract class AbstractRecommender : IRecommender {
  
  private static Logger log = LoggerFactory.GetLogger(typeof(AbstractRecommender));
  
  private IDataModel dataModel;
  private ICandidateItemsStrategy candidateItemsStrategy;
  
  protected AbstractRecommender(IDataModel dataModel, ICandidateItemsStrategy candidateItemsStrategy) {
    this.dataModel = dataModel; //Preconditions.checkNotNull(dataModel);
	this.candidateItemsStrategy = candidateItemsStrategy; // Preconditions.checkNotNull(candidateItemsStrategy);
  }

  protected AbstractRecommender(IDataModel dataModel)
	  : this(dataModel, GetDefaultCandidateItemsStrategy()) {
  }

  protected static ICandidateItemsStrategy GetDefaultCandidateItemsStrategy() {
    return new PreferredItemsNeighborhoodCandidateItemsStrategy();
  }

   /// <p>
   /// Default implementation which just calls
   /// {@link Recommender#recommend(long, int, NReco.CF.Taste.Recommender.IDRescorer)}, with a
   /// {@link NReco.CF.Taste.Recommender.Rescorer} that does nothing.
   /// </p>
  public virtual IList<IRecommendedItem> Recommend(long userID, int howMany) {
    return Recommend(userID, howMany, null);
  }

  public abstract IList<IRecommendedItem> Recommend(long userID, int howMany, IDRescorer rescorer);

  public abstract float EstimatePreference(long userID, long itemID);

   /// <p>
   /// Default implementation which just calls {@link DataModel#setPreference(long, long, float)}.
   /// </p>
   ///
   /// @throws IllegalArgumentException
   ///           if userID or itemID is {@code null}, or if value is {@link Double#NaN}
  public virtual void SetPreference(long userID, long itemID, float value) {
    //Preconditions.checkArgument(!Float.isNaN(value), "NaN value");
    log.Debug("Setting preference for user {}, item {}", userID, itemID);
    dataModel.SetPreference(userID, itemID, value);
  }
  
   /// <p>
   /// Default implementation which just calls {@link DataModel#removePreference(long, long)} (Object, Object)}.
   /// </p>
   ///
   /// @throws IllegalArgumentException
   ///           if userID or itemID is {@code null}
  public virtual void RemovePreference(long userID, long itemID) {
    log.Debug("Remove preference for user '{}', item '{}'", userID, itemID);
    dataModel.RemovePreference(userID, itemID);
  }
  
  public virtual IDataModel GetDataModel() {
    return dataModel;
  }
  
   /// @param userID
   ///          ID of user being evaluated
   /// @param preferencesFromUser
   ///          the preferences from the user
   /// @return all items in the {@link DataModel} for which the user has not expressed a preference and could
   ///         possibly be recommended to the user
   /// @throws TasteException
   ///           if an error occurs while listing items
  protected virtual FastIDSet GetAllOtherItems(long userID, IPreferenceArray preferencesFromUser) {
    return candidateItemsStrategy.GetCandidateItems(userID, preferencesFromUser, dataModel);
  }

  public abstract void Refresh(IList<IRefreshable> alreadyRefreshed);
  
}

}