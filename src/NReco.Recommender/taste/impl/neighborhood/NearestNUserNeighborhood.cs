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
using NReco.CF.Taste.Similarity;


namespace NReco.CF.Taste.Impl.Neighborhood {

 /// <summary>
 /// Computes a neighborhood consisting of the nearest n users to a given user. "Nearest" is defined by the
 /// given <see cref="IUserSimilarity"/>.
 /// </summary>
public sealed class NearestNUserNeighborhood : AbstractUserNeighborhood {
  
  private int n;
  private double minSimilarity;
  
   /// @param n neighborhood size; capped at the number of users in the data model
   /// @throws IllegalArgumentException
   ///           if {@code n < 1}, or userSimilarity or dataModel are {@code null}
  public NearestNUserNeighborhood(int n, IUserSimilarity userSimilarity, IDataModel dataModel) :
	  this(n, Double.NegativeInfinity, userSimilarity, dataModel, 1.0) {
    
  }
  
   /// @param n neighborhood size; capped at the number of users in the data model
   /// @param minSimilarity minimal similarity required for neighbors
   /// @throws IllegalArgumentException
   ///           if {@code n < 1}, or userSimilarity or dataModel are {@code null}
  public NearestNUserNeighborhood(int n,
                                  double minSimilarity,
                                  IUserSimilarity userSimilarity,
                                  IDataModel dataModel) :
	  this(n, minSimilarity, userSimilarity, dataModel, 1.0) {
   
  }
  
   /// @param n neighborhood size; capped at the number of users in the data model
   /// @param minSimilarity minimal similarity required for neighbors
   /// @param samplingRate percentage of users to consider when building neighborhood -- decrease to trade quality for
   ///   performance
   /// @throws IllegalArgumentException
   ///           if {@code n < 1} or samplingRate is NaN or not in (0,1], or userSimilarity or dataModel are
   ///           {@code null}
  public NearestNUserNeighborhood(int n,
                                  double minSimilarity,
                                  IUserSimilarity userSimilarity,
                                  IDataModel dataModel,
								  double samplingRate)
	  : base(userSimilarity, dataModel, samplingRate) {
    //Preconditions.checkArgument(n >= 1, "n must be at least 1");
    int numUsers = dataModel.GetNumUsers();
    this.n = n > numUsers ? numUsers : n;
    this.minSimilarity = minSimilarity;
  }
  
  public override long[] GetUserNeighborhood(long userID) {
    
    IDataModel dataModel = getDataModel();
    IUserSimilarity userSimilarityImpl = getUserSimilarity();
    
    TopItems.IEstimator<long> estimator = new Estimator(userSimilarityImpl, userID, minSimilarity);
    
    var userIDs = SamplinglongPrimitiveIterator.MaybeWrapIterator(dataModel.GetUserIDs(),
      getSamplingRate());
    return TopItems.GetTopUsers(n, userIDs, null, estimator);
  }
  
  public override string ToString() {
    return "NearestNUserNeighborhood";
  }
  
  private sealed class Estimator : TopItems.IEstimator<long> {
    private IUserSimilarity userSimilarityImpl;
    private long theUserID;
    private double minSim;
    
    internal Estimator(IUserSimilarity userSimilarityImpl, long theUserID, double minSim) {
      this.userSimilarityImpl = userSimilarityImpl;
      this.theUserID = theUserID;
      this.minSim = minSim;
    }
    
    public double Estimate(long userID) {
      if (userID == theUserID) {
        return Double.NaN;
      }
      double sim = userSimilarityImpl.UserSimilarity(theUserID, userID);
      return sim >= minSim ? sim : Double.NaN;
    }
  }
}

}