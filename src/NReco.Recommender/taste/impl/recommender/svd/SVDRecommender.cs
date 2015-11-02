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
using NReco.CF.Taste.Impl.Recommender;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Recommender;

namespace NReco.CF.Taste.Impl.Recommender.SVD {

/// <summary>
/// A <see cref="NReco.CF.Taste.Recommender.IRecommender"/> that uses matrix factorization (a projection of users
/// and items onto a feature space)
/// </summary>
public sealed class SVDRecommender : AbstractRecommender {

  private Factorization factorization;
  private IFactorizer factorizer;
  private IPersistenceStrategy persistenceStrategy;
  private RefreshHelper refreshHelper;

  private static Logger log = LoggerFactory.GetLogger(typeof(SVDRecommender));

  public SVDRecommender(IDataModel dataModel, IFactorizer factorizer) :
	  this(dataModel, factorizer, new AllUnknownItemsCandidateItemsStrategy(), getDefaultPersistenceStrategy()) {
    
  }

  public SVDRecommender(IDataModel dataModel, IFactorizer factorizer, ICandidateItemsStrategy candidateItemsStrategy) :
	  this(dataModel, factorizer, candidateItemsStrategy, getDefaultPersistenceStrategy())
    {
   
  }

   /// Create an SVDRecommender using a persistent store to cache factorizations. A factorization is loaded from the
   /// store if present, otherwise a new factorization is computed and saved in the store.
   ///
   /// The {@link #refresh(java.util.Collection) refresh} method recomputes the factorization and overwrites the store.
   ///
   /// @param dataModel
   /// @param factorizer
   /// @param persistenceStrategy
   /// @throws TasteException
   /// @throws IOException
  public SVDRecommender(IDataModel dataModel, IFactorizer factorizer, IPersistenceStrategy persistenceStrategy) 
	:this(dataModel, factorizer, GetDefaultCandidateItemsStrategy(), persistenceStrategy) {
  }

   /// Create an SVDRecommender using a persistent store to cache factorizations. A factorization is loaded from the
   /// store if present, otherwise a new factorization is computed and saved in the store. 
   ///
   /// The {@link #refresh(java.util.Collection) refresh} method recomputes the factorization and overwrites the store.
   ///
   /// @param dataModel
   /// @param factorizer
   /// @param candidateItemsStrategy
   /// @param persistenceStrategy
   ///
   /// @throws TasteException
  public SVDRecommender(IDataModel dataModel, IFactorizer factorizer, ICandidateItemsStrategy candidateItemsStrategy,
      IPersistenceStrategy persistenceStrategy) : base(dataModel, candidateItemsStrategy) {
    this.factorizer = factorizer; //Preconditions.checkNotNull(factorizer);
    this.persistenceStrategy = persistenceStrategy; // Preconditions.checkNotNull(persistenceStrategy);
    try {
      factorization = persistenceStrategy.Load();
    } catch (IOException e) {
      throw new TasteException("Error loading factorization", e);
    }
    
    if (factorization == null) {
      train();
    }
    
    refreshHelper = new RefreshHelper( () => {
        train();
    });
    refreshHelper.AddDependency(GetDataModel());
    refreshHelper.AddDependency(factorizer);
    refreshHelper.AddDependency(candidateItemsStrategy);
  }

  static IPersistenceStrategy getDefaultPersistenceStrategy() {
    return new NoPersistenceStrategy();
  }

  private void train() {
    factorization = factorizer.Factorize();
    try {
      persistenceStrategy.MaybePersist(factorization);
    } catch (IOException e) {
      throw new TasteException("Error persisting factorization", e);
    }
  }
  
  public override IList<IRecommendedItem> Recommend(long userID, int howMany, IDRescorer rescorer) {
    //Preconditions.checkArgument(howMany >= 1, "howMany must be at least 1");
    log.Debug("Recommending items for user ID '{}'", userID);

    IPreferenceArray preferencesFromUser = GetDataModel().GetPreferencesFromUser(userID);
    FastIDSet possibleItemIDs = GetAllOtherItems(userID, preferencesFromUser);

    List<IRecommendedItem> topItems = TopItems.GetTopItems(howMany, possibleItemIDs.GetEnumerator(), rescorer,
        new Estimator(this, userID));
    log.Debug("Recommendations are: {}", topItems);

    return topItems;
  }

   /// a preference is estimated by computing the dot-product of the user and item feature vectors
  public override float EstimatePreference(long userID, long itemID) {
    double[] userFeatures = factorization.getUserFeatures(userID);
    double[] itemFeatures = factorization.getItemFeatures(itemID);
    double estimate = 0;
    for (int feature = 0; feature < userFeatures.Length; feature++) {
      estimate += userFeatures[feature] * itemFeatures[feature];
    }
    return (float) estimate;
  }

  private sealed class Estimator : TopItems.IEstimator<long> {

    private long theUserID;
	SVDRecommender svdRecommender;

    internal Estimator(SVDRecommender svdRecommender, long theUserID) {
      this.theUserID = theUserID;
	  this.svdRecommender = svdRecommender;
    }

    public double Estimate(long itemID) {
		return svdRecommender.EstimatePreference(theUserID, itemID);
    }
  }

   /// Refresh the data model and factorization.
  public override void Refresh(IList<IRefreshable> alreadyRefreshed) {
    refreshHelper.Refresh(alreadyRefreshed);
  }

}

}