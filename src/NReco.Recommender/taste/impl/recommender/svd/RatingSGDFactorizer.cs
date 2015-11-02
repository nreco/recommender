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
using NReco.CF;

namespace NReco.CF.Taste.Impl.Recommender.SVD {

/// <summary>Matrix factorization with user and item biases for rating prediction, trained with plain vanilla SGD </summary>
public class RatingSGDFactorizer : AbstractFactorizer {

  protected static readonly int FEATURE_OFFSET = 3;
  
  /// Multiplicative decay factor for learning_rate 
  protected double learningRateDecay;
  /// Learning rate (step size) 
  protected double learningRate;
  /// Parameter used to prevent overfitting. 
  protected double preventOverfitting;
  /// Number of features used to compute this factorization 
  protected int numFeatures;
  /// Number of iterations 
  private int numIterations;
  /// Standard deviation for random initialization of features 
  protected double randomNoise;
  /// User features 
  protected double[][] userVectors;
  /// Item features 
  protected double[][] itemVectors;
  protected IDataModel dataModel;
  private long[] cachedUserIDs;
  private long[] cachedItemIDs;

  protected double biasLearningRate = 0.5;
  protected double biasReg = 0.1;

  /// place in user vector where the bias is stored 
  protected static int USER_BIAS_INDEX = 1;
  /// place in item vector where the bias is stored 
  protected static int ITEM_BIAS_INDEX = 2;

  public RatingSGDFactorizer(IDataModel dataModel, int numFeatures, int numIterations)
	  : this(dataModel, numFeatures, 0.01, 0.1, 0.01, numIterations, 1.0) {
  }

  public RatingSGDFactorizer(IDataModel dataModel, int numFeatures, double learningRate, double preventOverfitting,
	  double randomNoise, int numIterations, double learningRateDecay)
	  : base(dataModel) {
    this.dataModel = dataModel;
    this.numFeatures = numFeatures + FEATURE_OFFSET;
    this.numIterations = numIterations;

    this.learningRate = learningRate;
    this.learningRateDecay = learningRateDecay;
    this.preventOverfitting = preventOverfitting;
    this.randomNoise = randomNoise;
  }

  protected virtual void prepareTraining() {
    RandomWrapper random = RandomUtils.getRandom();
	userVectors = new double[dataModel.GetNumUsers()][]; //numFeatures
    itemVectors = new double[dataModel.GetNumItems()][];

    double globalAverage = getAveragePreference();
    for (int userIndex = 0; userIndex < userVectors.Length; userIndex++) {
		userVectors[userIndex] = new double[numFeatures];
	
      userVectors[userIndex][0] = globalAverage;
      userVectors[userIndex][USER_BIAS_INDEX] = 0; // will store user bias
      userVectors[userIndex][ITEM_BIAS_INDEX] = 1; // corresponding item feature contains item bias
      for (int feature = FEATURE_OFFSET; feature < numFeatures; feature++) {
        userVectors[userIndex][feature] = random.nextGaussian() * randomNoise;
      }
    }
    for (int itemIndex = 0; itemIndex < itemVectors.Length; itemIndex++) {
		itemVectors[itemIndex] = new double[numFeatures];

      itemVectors[itemIndex][0] = 1; // corresponding user feature contains global average
      itemVectors[itemIndex][USER_BIAS_INDEX] = 1; // corresponding user feature contains user bias
      itemVectors[itemIndex][ITEM_BIAS_INDEX] = 0; // will store item bias
      for (int feature = FEATURE_OFFSET; feature < numFeatures; feature++) {
        itemVectors[itemIndex][feature] = random.nextGaussian() * randomNoise;
      }
    }

    cachePreferences();
    shufflePreferences();
  }

  private int countPreferences() {
    int numPreferences = 0;
    var userIDs = dataModel.GetUserIDs();
    while (userIDs.MoveNext()) {
      IPreferenceArray preferencesFromUser = dataModel.GetPreferencesFromUser(userIDs.Current);
      numPreferences += preferencesFromUser.Length();
    }
    return numPreferences;
  }

  private void cachePreferences() {
    int numPreferences = countPreferences();
    cachedUserIDs = new long[numPreferences];
    cachedItemIDs = new long[numPreferences];

    var userIDs = dataModel.GetUserIDs();
    int index = 0;
    while (userIDs.MoveNext()) {
      long userID = userIDs.Current;
      IPreferenceArray preferencesFromUser = dataModel.GetPreferencesFromUser(userID);
      foreach (var preference in preferencesFromUser) {
        cachedUserIDs[index] = userID;
        cachedItemIDs[index] = preference.GetItemID();
        index++;
      }
    }
  }

  protected void shufflePreferences() {
    RandomWrapper random = RandomUtils.getRandom();
    /// Durstenfeld shuffle 
    for (int currentPos = cachedUserIDs.Length - 1; currentPos > 0; currentPos--) {
      int swapPos = random.nextInt(currentPos + 1);
      swapCachedPreferences(currentPos, swapPos);
    }
  }

  private void swapCachedPreferences(int posA, int posB) {
    long tmpUserIndex = cachedUserIDs[posA];
    long tmpItemIndex = cachedItemIDs[posA];

    cachedUserIDs[posA] = cachedUserIDs[posB];
    cachedItemIDs[posA] = cachedItemIDs[posB];

    cachedUserIDs[posB] = tmpUserIndex;
    cachedItemIDs[posB] = tmpItemIndex;
  }

  public override Factorization Factorize() {
    prepareTraining();
    double currentLearningRate = learningRate;


    for (int it = 0; it < numIterations; it++) {
      for (int index = 0; index < cachedUserIDs.Length; index++) {
        long userId = cachedUserIDs[index];
        long itemId = cachedItemIDs[index];
        float? rating = dataModel.GetPreferenceValue(userId, itemId);
        updateParameters(userId, itemId, rating.Value, currentLearningRate);
      }
      currentLearningRate *= learningRateDecay;
    }
    return createFactorization(userVectors, itemVectors);
  }

  double getAveragePreference() {
    IRunningAverage average = new FullRunningAverage();
    var it = dataModel.GetUserIDs();
    while (it.MoveNext()) {
      foreach (IPreference pref in dataModel.GetPreferencesFromUser(it.Current)) {
        average.AddDatum(pref.GetValue());
      }
    }
    return average.GetAverage();
  }

  protected void updateParameters(long userID, long itemID, float rating, double currentLearningRate) {
    int userIdx = userIndex(userID);
    int itemIdx = itemIndex(itemID);

    double[] userVector = userVectors[userIdx];
    double[] itemVector = itemVectors[itemIdx];
    double prediction = predictRating(userIdx, itemIdx);
    double err = rating - prediction;

    // adjust user bias
    userVector[USER_BIAS_INDEX] +=
        biasLearningRate * currentLearningRate * (err - biasReg * preventOverfitting * userVector[USER_BIAS_INDEX]);

    // adjust item bias
    itemVector[ITEM_BIAS_INDEX] +=
        biasLearningRate * currentLearningRate * (err - biasReg * preventOverfitting * itemVector[ITEM_BIAS_INDEX]);

    // adjust features
    for (int feature = FEATURE_OFFSET; feature < numFeatures; feature++) {
      double userFeature = userVector[feature];
      double itemFeature = itemVector[feature];

      double deltaUserFeature = err * itemFeature - preventOverfitting * userFeature;
      userVector[feature] += currentLearningRate * deltaUserFeature;

      double deltaItemFeature = err * userFeature - preventOverfitting * itemFeature;
      itemVector[feature] += currentLearningRate * deltaItemFeature;
    }
  }

  private double predictRating(int userID, int itemID) {
    double sum = 0;
    for (int feature = 0; feature < numFeatures; feature++) {
      sum += userVectors[userID][feature] * itemVectors[itemID][feature];
    }
    return sum;
  }
}

}