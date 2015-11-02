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
 

using NReco.CF.Taste.Impl;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Impl.Model;
using NReco.CF.Taste.Impl.Recommender.SVD;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Common;
using NReco.CF;
using NReco.Math3;

using NUnit.Framework;

namespace NReco.CF.Taste.Impl.Recommender.SVD {

public class ParallelSGDFactorizerTest : TasteTestCase {

  protected IDataModel dataModel;

  protected int rank;
  protected double lambda;
  protected int numIterations;

  private RandomWrapper random = (RandomWrapper) RandomUtils.getRandom();

  protected IFactorizer factorizer;
  protected SVDRecommender svdRecommender;

  private static Logger logger = LoggerFactory.GetLogger(typeof(ParallelSGDFactorizerTest));

  private double[,] randomMatrix(int numRows, int numColumns, double range) {
    double[,] data = new double[numRows,numColumns];
    for (int i = 0; i < numRows; i++) {
      for (int j = 0; j < numColumns; j++) {
        double sqrtUniform = random.nextDouble();
        data[i,j] = sqrtUniform * range;
      }
    }
    return data;
  }

  private void normalize(double[,] source, double range) {
    double max = source[0,0];
	double min = source[0,0];
  
    for (int i = 0; i < source.GetLength(0); i++) 
      for (int j = 0; j < source.GetLength(1); j++) {	
		  var v = source[i,j];
		  if (v<min) min=v;
		  if (v>max) max=v;
	  }

    for (int i = 0; i < source.GetLength(0); i++)
      for (int j = 0; j < source.GetLength(1); j++) {	
		  var value = source[i,j];
		  source[i,j] = (value - min) * range / (max - min);
	  }

  }

  public double[,] times(double[,] m, double[,] other) {
	  return MatrixUtil.times(m, other);
  }

  public void setUpSyntheticData() {

    int numUsers = 2000;
    int numItems = 1000;
    double sparsity = 0.5;

    this.rank = 20;
    this.lambda = 0.000000001;
    this.numIterations = 100;

    var users = randomMatrix(numUsers, rank, 1);
    var items = randomMatrix(rank, numItems, 1);
    var ratings = times(users, items);
    normalize(ratings, 5);

    FastByIDMap<IPreferenceArray> userData = new FastByIDMap<IPreferenceArray>();
    for (int userIndex = 0; userIndex < numUsers; userIndex++) {
      List<IPreference> row= new List<IPreference>();
      for (int itemIndex = 0; itemIndex < numItems; itemIndex++) {
        if (random.nextDouble() <= sparsity) {
          row.Add(new GenericPreference(userIndex, itemIndex, (float) ratings[userIndex, itemIndex]));
        }
      }

      userData.Put(userIndex, new GenericUserPreferenceArray(row));
    }

    dataModel = new GenericDataModel(userData);
  }

  public void setUpToyData() {
    this.rank = 3;
    this.lambda = 0.01;
    this.numIterations = 1000;

    FastByIDMap<IPreferenceArray> userData = new FastByIDMap<IPreferenceArray>();

    userData.Put(1L, new GenericUserPreferenceArray( new List<IPreference>() {
		new GenericPreference(1L, 1L, 5.0f),
        new GenericPreference(1L, 2L, 5.0f),
        new GenericPreference(1L, 3L, 2.0f) }));

    userData.Put(2L, new GenericUserPreferenceArray(new List<IPreference>() {
		new GenericPreference(2L, 1L, 2.0f),
        new GenericPreference(2L, 3L, 3.0f),
        new GenericPreference(2L, 4L, 5.0f)} ));

    userData.Put(3L, new GenericUserPreferenceArray(new List<IPreference>() {
		new GenericPreference(3L, 2L, 5.0f),
        new GenericPreference(3L, 4L, 3.0f)}));

    userData.Put(4L, new GenericUserPreferenceArray(new List<IPreference>() {
		new GenericPreference(4L, 1L, 3.0f),
        new GenericPreference(4L, 4L, 5.0f)}));
    dataModel = new GenericDataModel(userData);
  }

  [Test]
  public void testPreferenceShufflerWithSyntheticData() {
    setUpSyntheticData();

    ParallelSGDFactorizer.PreferenceShuffler shuffler = new ParallelSGDFactorizer.PreferenceShuffler(dataModel);
    shuffler.shuffle();
    shuffler.stage();

    FastByIDMap<FastByIDMap<bool?>> checkedLst = new FastByIDMap<FastByIDMap<bool?>>();

    for (int i = 0; i < shuffler.size(); i++) {
      IPreference pref=shuffler.get(i);

      float? value = dataModel.GetPreferenceValue(pref.GetUserID(), pref.GetItemID());
      Assert.AreEqual(pref.GetValue(), value.Value, 0.0);
      if (!checkedLst.ContainsKey(pref.GetUserID())) {
        checkedLst.Put(pref.GetUserID(), new FastByIDMap<bool?>());
      }

      Assert.IsNull(checkedLst.Get(pref.GetUserID()).Get(pref.GetItemID()));

      checkedLst.Get(pref.GetUserID()).Put(pref.GetItemID(), true);
    }

    var userIDs = dataModel.GetUserIDs();
    int index=0;
    while (userIDs.MoveNext()) {
      long userID = userIDs.Current;
      IPreferenceArray preferencesFromUser = dataModel.GetPreferencesFromUser(userID);
      foreach (IPreference preference in preferencesFromUser) {
        Assert.True(checkedLst.Get(preference.GetUserID()).Get(preference.GetItemID()).Value);
        index++;
      }
    }
    Assert.AreEqual(index, shuffler.size());
  }

	double vectorDot(double[] v1, double[] v2) {
		return MatrixUtil.vectorDot(v1, v2);
	}

  [Test]
  public void testFactorizerWithToyData() {

    setUpToyData();

    var stopWatch = new System.Diagnostics.Stopwatch();
	stopWatch.Start();

    factorizer = new ParallelSGDFactorizer(dataModel, rank, lambda, numIterations, 0.01, 1, 0, 0);

    Factorization factorization = factorizer.Factorize();

	stopWatch.Stop();
    long duration = stopWatch.ElapsedMilliseconds;

    /// a hold out test would be better, but this is just a toy example so we only check that the
     /// factorization is close to the original matrix 
    IRunningAverage avg = new FullRunningAverage();
    var userIDs = dataModel.GetUserIDs();
    IEnumerator<long> itemIDs;

    while (userIDs.MoveNext()) {
      long userID = userIDs.Current;
      foreach (IPreference pref in dataModel.GetPreferencesFromUser(userID)) {
        double rating = pref.GetValue();
        var userVector = factorization.getUserFeatures(userID);
        var itemVector = factorization.getItemFeatures(pref.GetItemID());
        double estimate = vectorDot(userVector, itemVector); //userVector.dot(itemVector);

        double err = rating - estimate;

        avg.AddDatum(err * err);
      }
    }

    double sum = 0.0;

    userIDs = dataModel.GetUserIDs();
    while (userIDs.MoveNext()) {
      long userID = userIDs.Current;
      var userVector = factorization.getUserFeatures(userID);
      double regularization = vectorDot(userVector,userVector);
      sum += regularization;
    }

    itemIDs = dataModel.GetItemIDs();
    while (itemIDs.MoveNext()) {
      long itemID = itemIDs.Current;
      var itemVector = factorization.getUserFeatures(itemID);
      double regularization = vectorDot( itemVector, itemVector);
      sum += regularization;
    }

    double rmse = Math.Sqrt(avg.GetAverage());
    double loss = avg.GetAverage() / 2 + lambda / 2 * sum;
    logger.Info("RMSE: " + rmse + ";\tLoss: " + loss + ";\tTime Used: " + duration);
    Assert.True(rmse < 0.2);
  }

  [Test]
  public void testRecommenderWithToyData() {

    setUpToyData();

    factorizer = new ParallelSGDFactorizer(dataModel, rank, lambda, numIterations, 0.01, 1, 0,0);
    svdRecommender = new SVDRecommender(dataModel, factorizer);

    /// a hold out test would be better, but this is just a toy example so we only check that the
     /// factorization is close to the original matrix 
    IRunningAverage avg = new FullRunningAverage();
    var userIDs = dataModel.GetUserIDs();
    while (userIDs.MoveNext()) {
      long userID = userIDs.Current;
      foreach (IPreference pref in dataModel.GetPreferencesFromUser(userID)) {
        double rating = pref.GetValue();
        double estimate = svdRecommender.EstimatePreference(userID, pref.GetItemID());
        double err = rating - estimate;
        avg.AddDatum(err * err);
      }
    }

    double rmse = Math.Sqrt(avg.GetAverage());
    logger.Info("rmse: " + rmse);
    Assert.True(rmse < 0.2);
  }

  [Test]
  public void testFactorizerWithWithSyntheticData() {

    setUpSyntheticData();

    var stopWatch = new System.Diagnostics.Stopwatch();
	stopWatch.Start();

    factorizer = new ParallelSGDFactorizer(dataModel, rank, lambda, numIterations, 0.01, 1, 0, 0);

    Factorization factorization = factorizer.Factorize();

	stopWatch.Stop();
    long duration = stopWatch.ElapsedMilliseconds;

    /// a hold out test would be better, but this is just a toy example so we only check that the
     /// factorization is close to the original matrix 
    IRunningAverage avg = new FullRunningAverage();
    var userIDs = dataModel.GetUserIDs();
    IEnumerator<long> itemIDs;

    while (userIDs.MoveNext()) {
      long userID = userIDs.Current;
      foreach (IPreference pref in dataModel.GetPreferencesFromUser(userID)) {
        double rating = pref.GetValue();
        var userVector = factorization.getUserFeatures(userID);
        var itemVector = factorization.getItemFeatures(pref.GetItemID());
        double estimate = vectorDot( userVector, itemVector);
        double err = rating - estimate;

        avg.AddDatum(err * err);
      }
    }

    double sum = 0.0;

    userIDs = dataModel.GetUserIDs();
    while (userIDs.MoveNext()) {
      long userID = userIDs.Current;
      var userVector = factorization.getUserFeatures(userID);
      double regularization = vectorDot( userVector, userVector);
      sum += regularization;
    }

    itemIDs = dataModel.GetItemIDs();
    while (itemIDs.MoveNext()) {
      long itemID = itemIDs.Current;
      var itemVector = factorization.getUserFeatures(itemID);
      double regularization = vectorDot( itemVector, itemVector);
      sum += regularization;
    }

    double rmse = Math.Sqrt(avg.GetAverage());
    double loss = avg.GetAverage() / 2 + lambda / 2 * sum;
    logger.Info("RMSE: " + rmse + ";\tLoss: " + loss + ";\tTime Used: " + duration + "ms");
    Assert.True(rmse < 0.2);
  }

  [Test]
  public void testRecommenderWithSyntheticData() {

    setUpSyntheticData();

    factorizer= new ParallelSGDFactorizer(dataModel, rank, lambda, numIterations, 0.01, 1, 0, 0);
    svdRecommender = new SVDRecommender(dataModel, factorizer);

    /// a hold out test would be better, but this is just a toy example so we only check that the
     /// factorization is close to the original matrix 
    IRunningAverage avg = new FullRunningAverage();
    var userIDs = dataModel.GetUserIDs();
    while (userIDs.MoveNext()) {
      long userID = userIDs.Current;
      foreach (IPreference pref in dataModel.GetPreferencesFromUser(userID)) {
        double rating = pref.GetValue();
        double estimate = svdRecommender.EstimatePreference(userID, pref.GetItemID());
        double err = rating - estimate;
        avg.AddDatum(err * err);
      }
    }

    double rmse = Math.Sqrt(avg.GetAverage());
    logger.Info("rmse: " + rmse);
    Assert.True(rmse < 0.2);
  }
}
}

