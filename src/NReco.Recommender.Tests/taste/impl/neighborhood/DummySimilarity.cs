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
using NReco.CF.Taste.Impl.Similarity;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Similarity;

namespace NReco.CF.Taste.Impl.Neighborhood {


sealed class DummySimilarity : AbstractItemSimilarity, IUserSimilarity {

	public DummySimilarity(IDataModel dataModel)
		: base(dataModel) {
    
  }
  
  public double UserSimilarity(long userID1, long userID2) {
    IDataModel dataModel = getDataModel();
    return 1.0 / (1.0 + Math.Abs(dataModel.GetPreferencesFromUser(userID1).Get(0).GetValue()
                                 - dataModel.GetPreferencesFromUser(userID2).Get(0).GetValue()));
  }
  
  public override double ItemSimilarity(long itemID1, long itemID2) {
    // Make up something wacky
    return 1.0 / (1.0 + Math.Abs(itemID1 - itemID2));
  }

  public override double[] ItemSimilarities(long itemID1, long[] itemID2s) {
    int length = itemID2s.Length;
    double[] result = new double[length];
    for (int i = 0; i < length; i++) {
      result[i] = ItemSimilarity(itemID1, itemID2s[i]);
    }
    return result;
  }
  
  public void SetPreferenceInferrer(IPreferenceInferrer inferrer) {
    throw new NotSupportedException();
  }

  public void Refresh(IList<IRefreshable> alreadyRefreshed) {
  // do nothing
  }
  
}

}