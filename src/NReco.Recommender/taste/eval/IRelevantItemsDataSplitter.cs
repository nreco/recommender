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

namespace NReco.CF.Taste.Eval {

 /// Implementations of this interface determine the items that are considered relevant,
 /// and splits data into a training and test subset, for purposes of precision/recall
 /// tests as implemented by implementations of {@link RecommenderIRStatsEvaluator}.
public interface IRelevantItemsDataSplitter {

   /// During testing, relevant items are removed from a particular users' preferences,
   /// and a model is build using this user's other preferences and all other users.
   ///
   /// @param at                 Maximum number of items to be removed
   /// @param relevanceThreshold Minimum strength of preference for an item to be considered
   ///                           relevant
   /// @return IDs of relevant items
  FastIDSet GetRelevantItemsIDs(long userID,
                                int at,
                                double relevanceThreshold,
                                IDataModel dataModel) ;

   /// Adds a single user and all their preferences to the training model.
   ///
   /// @param userID          ID of user whose preferences we are trying to predict
   /// @param relevantItemIDs IDs of items considered relevant to that user
   /// @param trainingUsers   the database of training preferences to which we will
   ///                        append the ones for otherUserID.
   /// @param otherUserID     for whom we are adding preferences to the training model
  void ProcessOtherUser(long userID,
                        FastIDSet relevantItemIDs,
                        FastByIDMap<IPreferenceArray> trainingUsers,
                        long otherUserID,
                        IDataModel dataModel) ;

}

}