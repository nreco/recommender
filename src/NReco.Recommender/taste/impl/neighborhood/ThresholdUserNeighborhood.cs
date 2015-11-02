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
using NReco.CF.Taste.Similarity;

namespace NReco.CF.Taste.Impl.Neighborhood {

 /// <summary>
 /// Computes a neigbhorhood consisting of all users whose similarity to the given user meets or exceeds a
 /// certain threshold. Similarity is defined by the given <see cref="IUserSimilarity"/>.
 /// </summary>
public sealed class ThresholdUserNeighborhood : AbstractUserNeighborhood {
  
  private double threshold;
  
   /// @param threshold
   ///          similarity threshold
   /// @param userSimilarity
   ///          similarity metric
   /// @param dataModel
   ///          data model
   /// @throws IllegalArgumentException
   ///           if threshold is {@link Double#NaN}, or if samplingRate is not positive and less than or equal
   ///           to 1.0, or if userSimilarity or dataModel are {@code null}
  public ThresholdUserNeighborhood(double threshold, IUserSimilarity userSimilarity, IDataModel dataModel) :
	  this(threshold, userSimilarity, dataModel, 1.0) {
    
  }
  
   /// @param threshold
   ///          similarity threshold
   /// @param userSimilarity
   ///          similarity metric
   /// @param dataModel
   ///          data model
   /// @param samplingRate
   ///          percentage of users to consider when building neighborhood -- decrease to trade quality for
   ///          performance
   /// @throws IllegalArgumentException
   ///           if threshold or samplingRate is {@link Double#NaN}, or if samplingRate is not positive and less
   ///           than or equal to 1.0, or if userSimilarity or dataModel are {@code null}
  public ThresholdUserNeighborhood(double threshold,
                                   IUserSimilarity userSimilarity,
                                   IDataModel dataModel,
								   double samplingRate)
	  : base(userSimilarity, dataModel, samplingRate) {
    
    //Preconditions.checkArgument(!Double.isNaN(threshold), "threshold must not be NaN");
    this.threshold = threshold;
  }
  
  public override long[] GetUserNeighborhood(long userID) {
    
    IDataModel dataModel = getDataModel();
    FastIDSet neighborhood = new FastIDSet();
    var usersIterable = SamplinglongPrimitiveIterator.MaybeWrapIterator(dataModel
        .GetUserIDs(), getSamplingRate());
    IUserSimilarity userSimilarityImpl = getUserSimilarity();
    
    while (usersIterable.MoveNext()) {
      long otherUserID = usersIterable.Current;
      if (userID != otherUserID) {
        double theSimilarity = userSimilarityImpl.UserSimilarity(userID, otherUserID);
        if (!Double.IsNaN(theSimilarity) && theSimilarity >= threshold) {
          neighborhood.Add(otherUserID);
        }
      }
    }
    
    return neighborhood.ToArray();
  }
  
  public override string ToString() {
    return "ThresholdUserNeighborhood";
  }
  
}

}