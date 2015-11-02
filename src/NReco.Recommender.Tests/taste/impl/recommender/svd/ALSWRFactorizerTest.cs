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
using NReco.CF.Taste.Model;
using NReco.Math3;
using NUnit.Framework;


namespace NReco.CF.Taste.Impl.Recommender.SVD {


public class ALSWRFactorizerTest : TasteTestCase {

  private ALSWRFactorizer factorizer;
  private IDataModel dataModel;

       ///  rating-matrix
       ///
       ///          burger  hotdog  berries  icecream
       ///  dog       5       5        2        -
       ///  rabbit    2       -        3        5
       ///  cow       -       5        -        3
       ///  donkey    3       -        -        5

  [SetUp]
  public override void SetUp() {
    base.SetUp();
    FastByIDMap<IPreferenceArray> userData = new FastByIDMap<IPreferenceArray>();

    userData.Put(1L, new GenericUserPreferenceArray( new List<IPreference>() {new GenericPreference(1L, 1L, 5.0f),
                                                                  new GenericPreference(1L, 2L, 5.0f),
                                                                  new GenericPreference(1L, 3L, 2.0f) } ));

    userData.Put(2L, new GenericUserPreferenceArray( new List<IPreference>() {new GenericPreference(2L, 1L, 2.0f),
                                                                  new GenericPreference(2L, 3L, 3.0f),
                                                                  new GenericPreference(2L, 4L, 5.0f) } ));

    userData.Put(3L, new GenericUserPreferenceArray( new List<IPreference>() {new GenericPreference(3L, 2L, 5.0f),
                                                                  new GenericPreference(3L, 4L, 3.0f) } ));

    userData.Put(4L, new GenericUserPreferenceArray(new List<IPreference>() {new GenericPreference(4L, 1L, 3.0f),
                                                                  new GenericPreference(4L, 4L, 5.0f)}));

    dataModel = new GenericDataModel(userData);
    factorizer = new ALSWRFactorizer(dataModel, 3, 0.065, 10);
  }

  [Test]
  public void setFeatureColumn() {
    ALSWRFactorizer.Features features = new ALSWRFactorizer.Features(factorizer);
    var vector = new double[] { 0.5, 2.0, 1.5 };
    int index = 1;

    features.setFeatureColumnInM(index, vector);
    double[][] matrix = features.getM();

    Assert.AreEqual(vector[0], matrix[index][0], EPSILON);
    Assert.AreEqual(vector[1], matrix[index][1], EPSILON);
    Assert.AreEqual(vector[2], matrix[index][2], EPSILON);
  }

  [Test]
  public void ratingVector() {
    IPreferenceArray prefs = dataModel.GetPreferencesFromUser(1);

    double[] ratingVector = ALSWRFactorizer.ratingVector(prefs);

    Assert.AreEqual(prefs.Length(), ratingVector.Length);
    Assert.AreEqual(prefs.Get(0).GetValue(), ratingVector[0], EPSILON);
    Assert.AreEqual(prefs.Get(1).GetValue(), ratingVector[1], EPSILON);
    Assert.AreEqual(prefs.Get(2).GetValue(), ratingVector[2], EPSILON);
  }

  [Test]
  public void averageRating() {
    ALSWRFactorizer.Features features = new ALSWRFactorizer.Features(factorizer);
    Assert.AreEqual(2.5, features.averateRating(3L), EPSILON);
  }

  [Test]
  public void initializeM() {
    ALSWRFactorizer.Features features = new ALSWRFactorizer.Features(factorizer);
    double[][] M = features.getM();

    Assert.AreEqual(3.333333333, M[0][0], EPSILON);
    Assert.AreEqual(5, M[1][0], EPSILON);
    Assert.AreEqual(2.5, M[2][0], EPSILON);
    Assert.AreEqual(4.333333333, M[3][0], EPSILON);

    for (int itemIndex = 0; itemIndex < dataModel.GetNumItems(); itemIndex++) {
      for (int feature = 1; feature < 3; feature++ ) {
        Assert.True(M[itemIndex][feature] >= 0);
        Assert.True(M[itemIndex][feature] <= 0.1);
      }
    }
  }

  [Test]
  public void toyExample() {

    SVDRecommender svdRecommender = new SVDRecommender(dataModel, factorizer);

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
    Assert.True(rmse < 0.2);
  }

  [Test]
  public void toyExampleImplicit() {

    var observations = new double[4,4] {
        { 5.0, 5.0, 2.0, 0 },
        { 2.0, 0,   3.0, 5.0 },
        { 0,   5.0, 0,   3.0 },
        { 3.0, 0,   0,   5.0 } };

    var preferences = new double[4, 4] {
        { 1.0, 1.0, 1.0, 0 },
        { 1.0, 0,   1.0, 1.0 },
        { 0,   1.0, 0,   1.0 },
        { 1.0, 0,   0,   1.0 } };

    double alpha = 20;

    ALSWRFactorizer factorizer = new ALSWRFactorizer(dataModel, 3, 0.065, 5, true, alpha);

    SVDRecommender svdRecommender = new SVDRecommender(dataModel, factorizer);

    IRunningAverage avg = new FullRunningAverage();
    for (int sliceIdx = 0; sliceIdx < preferences.GetLength(0); sliceIdx++) {
      var slice = MatrixUtil.viewRow(preferences, sliceIdx);
      for (var eIndex=0; eIndex<slice.Length; eIndex++) {
		  var e = slice[eIndex];
		  long userID = sliceIdx + 1;
		  long itemID = eIndex + 1;

        if (!Double.IsNaN(e)) {
          double pref = e;
          double estimate = svdRecommender.EstimatePreference(userID, itemID);

		  double confidence = 1 + alpha * observations[sliceIdx, eIndex];
          double err = confidence * (pref - estimate) * (pref - estimate);
          avg.AddDatum(err);
          Console.WriteLine("Comparing preference of user [{0}] towards item [{1}], was [{2}] with confidence [{3}] "
			  + "estimate is [{4}]", sliceIdx, eIndex, pref, confidence, estimate);
        }
      }
    }
    double rmse = Math.Sqrt(avg.GetAverage());
    Console.WriteLine("RMSE: {0}", rmse);

    Assert.True(rmse < 0.4);
  }
}

}