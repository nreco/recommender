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

namespace NReco.CF.Taste.Impl.Similarity {

/// <summary>Abstract superclass encapsulating functionality that is common to most implementations in this package. </summary>
public abstract class AbstractSimilarity : AbstractItemSimilarity, IUserSimilarity {

  private IPreferenceInferrer inferrer;
  private bool weighted;
  private bool centerData;
  private int cachedNumItems;
  private int cachedNumUsers;
  private RefreshHelper refreshHelper;

   /// <p>
   /// Creates a possibly weighted {@link AbstractSimilarity}.
   /// </p>
  public AbstractSimilarity(IDataModel dataModel, Weighting weighting, bool centerData) : base(dataModel) {
    this.weighted = weighting == Weighting.WEIGHTED;
    this.centerData = centerData;
    this.cachedNumItems = dataModel.GetNumItems();
    this.cachedNumUsers = dataModel.GetNumUsers();
    this.refreshHelper = new RefreshHelper( () => {
        cachedNumItems = dataModel.GetNumItems();
        cachedNumUsers = dataModel.GetNumUsers();
      }
    );
  }

  IPreferenceInferrer getPreferenceInferrer() {
    return inferrer;
  }
  
  public void SetPreferenceInferrer(IPreferenceInferrer inferrer) {
    //Preconditions.checkArgument(inferrer != null, "inferrer is null");
    refreshHelper.AddDependency(inferrer);
    refreshHelper.RemoveDependency(this.inferrer);
    this.inferrer = inferrer;
  }
  
  bool isWeighted() {
    return weighted;
  }
  
   /// <p>
   /// Several subclasses in this package implement this method to actually compute the similarity from figures
   /// computed over users or items. Note that the computations in this class "center" the data, such that X and
   /// Y's mean are 0.
   /// </p>
   /// 
   /// <p>
   /// Note that the sum of all X and Y values must then be 0. This value isn't passed down into the standard
   /// similarity computations as a result.
   /// </p>
   /// 
   /// @param n
   ///          total number of users or items
   /// @param sumXY
   ///          sum of product of user/item preference values, over all items/users preferred by both
   ///          users/items
   /// @param sumX2
   ///          sum of the square of user/item preference values, over the first item/user
   /// @param sumY2
   ///          sum of the square of the user/item preference values, over the second item/user
   /// @param sumXYdiff2
   ///          sum of squares of differences in X and Y values
   /// @return similarity value between -1.0 and 1.0, inclusive, or {@link Double#NaN} if no similarity can be
   ///         computed (e.g. when no items have been rated by both users
  protected abstract double computeResult(int n, double sumXY, double sumX2, double sumY2, double sumXYdiff2);
  
  public double UserSimilarity(long userID1, long userID2) {
    IDataModel dataModel = getDataModel();
    IPreferenceArray xPrefs = dataModel.GetPreferencesFromUser(userID1);
    IPreferenceArray yPrefs = dataModel.GetPreferencesFromUser(userID2);
    int xLength = xPrefs.Length();
	int yLength = yPrefs.Length();
    
    if (xLength == 0 || yLength == 0) {
      return Double.NaN;
    }
    
    long xIndex = xPrefs.GetItemID(0);
    long yIndex = yPrefs.GetItemID(0);
    int xPrefIndex = 0;
    int yPrefIndex = 0;
    
    double sumX = 0.0;
    double sumX2 = 0.0;
    double sumY = 0.0;
    double sumY2 = 0.0;
    double sumXY = 0.0;
    double sumXYdiff2 = 0.0;
    int count = 0;
    
    bool hasInferrer = inferrer != null;
    
    while (true) {
      int compare = xIndex < yIndex ? -1 : xIndex > yIndex ? 1 : 0;
      if (hasInferrer || compare == 0) {
        double x;
        double y;
        if (xIndex == yIndex) {
          // Both users expressed a preference for the item
          x = xPrefs.GetValue(xPrefIndex);
          y = yPrefs.GetValue(yPrefIndex);
        } else {
          // Only one user expressed a preference, but infer the other one's preference and tally
          // as if the other user expressed that preference
          if (compare < 0) {
            // X has a value; infer Y's
            x = xPrefs.GetValue(xPrefIndex);
            y = inferrer.InferPreference(userID2, xIndex);
          } else {
            // compare > 0
            // Y has a value; infer X's
            x = inferrer.InferPreference(userID1, yIndex);
            y = yPrefs.GetValue(yPrefIndex);
          }
        }
        sumXY += x * y;
        sumX += x;
        sumX2 += x * x;
        sumY += y;
        sumY2 += y * y;
        double diff = x - y;
        sumXYdiff2 += diff * diff;
        count++;
      }
      if (compare <= 0) {
        if (++xPrefIndex >= xLength) {
          if (hasInferrer) {
            // Must count other Ys; pretend next X is far away
            if (yIndex == long.MaxValue) {
              // ... but stop if both are done!
              break;
            }
            xIndex = long.MaxValue;
          } else {
            break;
          }
        } else {
          xIndex = xPrefs.GetItemID(xPrefIndex);
        }
      }
      if (compare >= 0) {
        if (++yPrefIndex >= yLength) {
          if (hasInferrer) {
            // Must count other Xs; pretend next Y is far away
            if (xIndex == long.MaxValue) {
              // ... but stop if both are done!
              break;
            }
            yIndex = long.MaxValue;
          } else {
            break;
          }
        } else {
          yIndex = yPrefs.GetItemID(yPrefIndex);
        }
      }
    }
    
    // "Center" the data. If my math is correct, this'll do it.
    double result;
    if (centerData) {
      double meanX = sumX / count;
      double meanY = sumY / count;
      // double centeredSumXY = sumXY - meanY * sumX - meanX * sumY + n * meanX * meanY;
      double centeredSumXY = sumXY - meanY * sumX;
      // double centeredSumX2 = sumX2 - 2.0 * meanX * sumX + n * meanX * meanX;
      double centeredSumX2 = sumX2 - meanX * sumX;
      // double centeredSumY2 = sumY2 - 2.0 * meanY * sumY + n * meanY * meanY;
      double centeredSumY2 = sumY2 - meanY * sumY;
      result = computeResult(count, centeredSumXY, centeredSumX2, centeredSumY2, sumXYdiff2);
    } else {
      result = computeResult(count, sumXY, sumX2, sumY2, sumXYdiff2);
    }
    
    if (!Double.IsNaN(result)) {
      result = normalizeWeightResult(result, count, cachedNumItems);
    }
    return result;
  }
  
  public override double ItemSimilarity(long itemID1, long itemID2) {
    IDataModel dataModel = getDataModel();
    IPreferenceArray xPrefs = dataModel.GetPreferencesForItem(itemID1);
    IPreferenceArray yPrefs = dataModel.GetPreferencesForItem(itemID2);
    int xLength = xPrefs.Length();
    int yLength = yPrefs.Length();
    
    if (xLength == 0 || yLength == 0) {
      return Double.NaN;
    }
    
    long xIndex = xPrefs.GetUserID(0);
    long yIndex = yPrefs.GetUserID(0);
    int xPrefIndex = 0;
    int yPrefIndex = 0;
    
    double sumX = 0.0;
    double sumX2 = 0.0;
    double sumY = 0.0;
    double sumY2 = 0.0;
    double sumXY = 0.0;
    double sumXYdiff2 = 0.0;
    int count = 0;
    
    // No, pref inferrers and transforms don't apply here. I think.
    
    while (true) {
      int compare = xIndex < yIndex ? -1 : xIndex > yIndex ? 1 : 0;
      if (compare == 0) {
        // Both users expressed a preference for the item
        double x = xPrefs.GetValue(xPrefIndex);
        double y = yPrefs.GetValue(yPrefIndex);
        sumXY += x * y;
        sumX += x;
        sumX2 += x * x;
        sumY += y;
        sumY2 += y * y;
        double diff = x - y;
        sumXYdiff2 += diff * diff;
        count++;
      }
      if (compare <= 0) {
        if (++xPrefIndex == xLength) {
          break;
        }
        xIndex = xPrefs.GetUserID(xPrefIndex);
      }
      if (compare >= 0) {
        if (++yPrefIndex == yLength) {
          break;
        }
        yIndex = yPrefs.GetUserID(yPrefIndex);
      }
    }

    double result;
    if (centerData) {
      // See comments above on these computations
      double n = (double) count;
      double meanX = sumX / n;
      double meanY = sumY / n;
      // double centeredSumXY = sumXY - meanY * sumX - meanX * sumY + n * meanX * meanY;
      double centeredSumXY = sumXY - meanY * sumX;
      // double centeredSumX2 = sumX2 - 2.0 * meanX * sumX + n * meanX * meanX;
      double centeredSumX2 = sumX2 - meanX * sumX;
      // double centeredSumY2 = sumY2 - 2.0 * meanY * sumY + n * meanY * meanY;
      double centeredSumY2 = sumY2 - meanY * sumY;
      result = computeResult(count, centeredSumXY, centeredSumX2, centeredSumY2, sumXYdiff2);
    } else {
      result = computeResult(count, sumXY, sumX2, sumY2, sumXYdiff2);
    }
    
    if (!Double.IsNaN(result)) {
      result = normalizeWeightResult(result, count, cachedNumUsers);
    }
    return result;
  }

  public override double[] ItemSimilarities(long itemID1, long[] itemID2s) {
    int length = itemID2s.Length;
    double[] result = new double[length];
    for (int i = 0; i < length; i++) {
      result[i] = ItemSimilarity(itemID1, itemID2s[i]);
    }
    return result;
  }
  
  double normalizeWeightResult(double result, int count, int num) {
    double normalizedResult = result;
    if (weighted) {
      double scaleFactor = 1.0 - (double) count / (double) (num + 1);
      if (normalizedResult < 0.0) {
        normalizedResult = -1.0 + scaleFactor * (1.0 + normalizedResult);
      } else {
        normalizedResult = 1.0 - scaleFactor * (1.0 - normalizedResult);
      }
    }
    // Make sure the result is not accidentally a little outside [-1.0, 1.0] due to rounding:
    if (normalizedResult < -1.0) {
      normalizedResult = -1.0;
    } else if (normalizedResult > 1.0) {
      normalizedResult = 1.0;
    }
    return normalizedResult;
  }
  
  public override void Refresh(IList<IRefreshable> alreadyRefreshed) {
    base.Refresh(alreadyRefreshed);
    refreshHelper.Refresh(alreadyRefreshed);
  }
  
  public override string ToString() {
    return GetType().Name + "[dataModel:" + getDataModel() + ",inferrer:" + inferrer + ']';
  }
  
}

}