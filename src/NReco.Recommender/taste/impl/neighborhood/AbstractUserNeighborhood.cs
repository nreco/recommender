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
using NReco.CF.Taste.Neighborhood;
using NReco.CF.Taste.Similarity;


namespace NReco.CF.Taste.Impl.Neighborhood {

/// <summary>
/// Contains methods and resources useful to all classes in this package.
/// </summary>
public abstract class AbstractUserNeighborhood : IUserNeighborhood {
  
  private IUserSimilarity userSimilarity;
  private IDataModel dataModel;
  private double samplingRate;
  private RefreshHelper refreshHelper;
  
  public AbstractUserNeighborhood(IUserSimilarity userSimilarity, IDataModel dataModel, double samplingRate) {
    //Preconditions.checkArgument(userSimilarity != null, "userSimilarity is null");
    //Preconditions.checkArgument(dataModel != null, "dataModel is null");
    //Preconditions.checkArgument(samplingRate > 0.0 && samplingRate <= 1.0, "samplingRate must be in (0,1]");
    this.userSimilarity = userSimilarity;
    this.dataModel = dataModel;
    this.samplingRate = samplingRate;
    this.refreshHelper = new RefreshHelper(null);
    this.refreshHelper.AddDependency(this.dataModel);
    this.refreshHelper.AddDependency(this.userSimilarity);
  }

  public abstract long[] GetUserNeighborhood(long userID);
  
  public virtual IUserSimilarity getUserSimilarity() {
    return userSimilarity;
  }
  
  public virtual IDataModel getDataModel() {
    return dataModel;
  }
  
  public virtual double getSamplingRate() {
    return samplingRate;
  }
  
  public virtual void Refresh(IList<IRefreshable> alreadyRefreshed) {
    refreshHelper.Refresh(alreadyRefreshed);
  }
  
}

}