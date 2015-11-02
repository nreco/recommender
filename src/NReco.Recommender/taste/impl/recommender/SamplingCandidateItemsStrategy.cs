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
using NReco.Math3.Util;

namespace NReco.CF.Taste.Impl.Recommender {


 /// <summary>
 /// <para>Returns all items that have not been rated by the user <em>(3)</em> and that were preferred by another user
 /// <em>(2)</em> that has preferred at least one item <em>(1)</em> that the current user has preferred too.</para>
 ///
 /// <para>This strategy uses sampling to limit the number of items that are considered, by sampling three different
 /// things, noted above:</para>
 /// <ol>
 ///   <li>The items that the user has preferred</li>
 ///   <li>The users who also prefer each of those items</li>
 ///   <li>The items those users also prefer</li>
 /// </ol>
 /// 
 /// <para>There is a maximum associated with each of these three things; if the number of items or users exceeds
 /// that max, it is sampled so that the expected number of items or users actually used in that part of the
 /// computation is equal to the max.</para>
 /// 
 /// <para>Three arguments control these three maxima. Each is a "factor" f, which establishes the max at
 /// f * log2(n), where n is the number of users or items in the data. For example if factor #2 is 5,
 /// which controls the number of users sampled per item, then 5 * log2(# users) is the maximum for this
 /// part of the computation.</para>
 /// 
 /// <para>Each can be set to not do any limiting with value <see cref="SamplingCandidateItemsStrategy.NO_LIMIT_FACTOR"/>.</para>
 /// </summary>
public class SamplingCandidateItemsStrategy : AbstractCandidateItemsStrategy {

  private static Logger log = LoggerFactory.GetLogger(typeof(SamplingCandidateItemsStrategy));

   /// Default factor used if not otherwise specified, for all limits. (30).
  public const int DEFAULT_FACTOR = 30;
   /// Specify this value as a factor to mean no limit.
  public const int NO_LIMIT_FACTOR = Int32.MaxValue;
  private static int MAX_LIMIT = Int32.MaxValue;
  private static double LOG2 = MathUtil.Log(2.0);

  private int maxItems;
  private int maxUsersPerItem;
  private int maxItemsPerUser;

   /// Defaults to using no limit ({@link #NO_LIMIT_FACTOR}) for all factors, except 
   /// {@code candidatesPerUserFactor} which defaults to {@link #DEFAULT_FACTOR}.
   ///
   /// @see #SamplingCandidateItemsStrategy(int, int, int, int, int)
  public SamplingCandidateItemsStrategy(int numUsers, int numItems) : this(DEFAULT_FACTOR, DEFAULT_FACTOR, DEFAULT_FACTOR, numUsers, numItems) {
  }

   /// @param itemsFactor factor controlling max items considered for a user
   /// @param usersPerItemFactor factor controlling max users considered for each of those items
   /// @param candidatesPerUserFactor factor controlling max candidate items considered from each of those users
   /// @param numUsers number of users currently in the data
   /// @param numItems number of items in the data
  public SamplingCandidateItemsStrategy(int itemsFactor,
                                        int usersPerItemFactor,
                                        int candidatesPerUserFactor,
                                        int numUsers,
                                        int numItems) {
    //Preconditions.checkArgument(itemsFactor > 0, "itemsFactor must be greater then 0!");
    //Preconditions.checkArgument(usersPerItemFactor > 0, "usersPerItemFactor must be greater then 0!");
    //Preconditions.checkArgument(candidatesPerUserFactor > 0, "candidatesPerUserFactor must be greater then 0!");
    //Preconditions.checkArgument(numUsers > 0, "numUsers must be greater then 0!");
    //Preconditions.checkArgument(numItems > 0, "numItems must be greater then 0!");
    maxItems = computeMaxFrom(itemsFactor, numItems);
    maxUsersPerItem = computeMaxFrom(usersPerItemFactor, numUsers);
    maxItemsPerUser = computeMaxFrom(candidatesPerUserFactor, numItems);
    log.Debug("maxItems {0}, maxUsersPerItem {0}, maxItemsPerUser {0}", maxItems, maxUsersPerItem, maxItemsPerUser);
  }

  private static int computeMaxFrom(int factor, int numThings) {
    if (factor == NO_LIMIT_FACTOR) {
      return MAX_LIMIT;
    }
    long max = (long) (factor * (1.0 + MathUtil.Log(numThings) / LOG2));
    return max > MAX_LIMIT ? MAX_LIMIT : (int) max;
  }

  protected override FastIDSet doGetCandidateItems(long[] preferredItemIDs, IDataModel dataModel) {
    var preferredItemIDsIterator = ((IEnumerable<long>)preferredItemIDs).GetEnumerator();
    if (preferredItemIDs.Length > maxItems) {
      double samplingRate = (double) maxItems / preferredItemIDs.Length;
      log.Info("preferredItemIDs.Length {0}, samplingRate {1}", preferredItemIDs.Length, samplingRate);
      preferredItemIDsIterator = 
          new SamplinglongPrimitiveIterator(preferredItemIDsIterator, samplingRate);
    }
    FastIDSet possibleItemsIDs = new FastIDSet();
    while (preferredItemIDsIterator.MoveNext()) {
      long itemID = preferredItemIDsIterator.Current;
      IPreferenceArray prefs = dataModel.GetPreferencesForItem(itemID);
      int prefsLength = prefs.Length();
	  if (prefsLength > maxUsersPerItem) {
        var sampledPrefs =
			new FixedSizeSamplingIterator<IPreference>(maxUsersPerItem, prefs.GetEnumerator());
        while (sampledPrefs.MoveNext()) {
          addSomeOf(possibleItemsIDs, dataModel.GetItemIDsFromUser(sampledPrefs.Current.GetUserID()));
        }
      } else {
        for (int i = 0; i < prefsLength; i++) {
          addSomeOf(possibleItemsIDs, dataModel.GetItemIDsFromUser(prefs.GetUserID(i)));
        }
      }
    }
    possibleItemsIDs.RemoveAll(preferredItemIDs);
    return possibleItemsIDs;
  }

  private void addSomeOf(FastIDSet possibleItemIDs, FastIDSet itemIDs) {
    if (itemIDs.Count() > maxItemsPerUser) {
      var it =
          new SamplinglongPrimitiveIterator(itemIDs.GetEnumerator(), (double) maxItemsPerUser / itemIDs.Count() );
      while (it.MoveNext()) {
        possibleItemIDs.Add(it.Current);
      }
    } else {
      possibleItemIDs.AddAll(itemIDs);
    }
  }

}

}