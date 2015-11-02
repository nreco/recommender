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

namespace NReco.CF.Taste.Impl.Recommender.SVD {

/// <summary>
/// Base class for <see cref="IFactorizer"/>s, provides ID to index mapping
/// </summary>
public abstract class AbstractFactorizer : IFactorizer {

  private IDataModel dataModel;
  private FastByIDMap<int?> userIDMapping;
  private FastByIDMap<int?> itemIDMapping;
  private RefreshHelper refreshHelper;

  protected AbstractFactorizer(IDataModel dataModel) {
    this.dataModel = dataModel;
    buildMappings();
    refreshHelper = new RefreshHelper( () => {
        buildMappings();
      });
    refreshHelper.AddDependency(dataModel);
  }

  public abstract Factorization Factorize();

  private void buildMappings() {
    userIDMapping = createIDMapping(dataModel.GetNumUsers(), dataModel.GetUserIDs());
    itemIDMapping = createIDMapping(dataModel.GetNumItems(), dataModel.GetItemIDs());
  }

  protected Factorization createFactorization(double[][] userFeatures, double[][] itemFeatures) {
    return new Factorization(userIDMapping, itemIDMapping, userFeatures, itemFeatures);
  }

  protected int userIndex(long userID) {
    int? userIndex = userIDMapping.Get(userID);
    if (userIndex == null) {
      userIndex = userIDMapping.Count();
      userIDMapping.Put(userID, userIndex);
    }
    return userIndex.Value;
  }

  protected int itemIndex(long itemID) {
    int? itemIndex = itemIDMapping.Get(itemID);
    if (itemIndex == null) {
      itemIndex = itemIDMapping.Count();
      itemIDMapping.Put(itemID, itemIndex);
    }
    return itemIndex.Value;
  }

  private static FastByIDMap<int?> createIDMapping(int size, IEnumerator<long> idIterator) {
    var mapping = new FastByIDMap<int?>(size);
    int index = 0;
    while (idIterator.MoveNext()) {
      mapping.Put(idIterator.Current, index++);
    }
    return mapping;
  }

  public void Refresh(IList<IRefreshable> alreadyRefreshed) {
    refreshHelper.Refresh(alreadyRefreshed);
  }
  
}

}