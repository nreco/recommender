///
 /// Licensed to the Apache Software Foundation (ASF) under one or more
 /// contributor license agreements.  See the NOTICE file distributed with
 /// this work for additional information regarding copyright ownership.
 /// The ASF licenses this file to You under the Apache License, Version 2.0
 /// (the "License"); you may not use this file except in compliance with
 /// the License.  You may obtain a copy of the License at
 ///
 ///     http://www.apache.org/licenses/LICENSE-2.0
 ///
 /// Unless required by applicable law or agreed to in writing, software
 /// distributed under the License is distributed on an "AS IS" BASIS,
 /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 /// See the License for the specific language governing permissions and
 /// limitations under the License.


using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using org.apache.mahout.cf.taste.impl;
using org.apache.mahout.cf.taste.impl.similarity;
using org.apache.mahout.cf.taste.impl.similarity.GenericItemSimilarity;
using org.apache.mahout.cf.taste.similarity;
using NUnit.Framework;

namespace org.apache.mahout.cf.taste.impl.similarity.file {

/// <p>Tests {@link FileItemSimilarity}.</p> 
public sealed class FileItemSimilarityTest : TasteTestCase {

  private static String[] data = {
      "1,5,0.125",
      "1,7,0.5" };

  private static String[] changedData = {
      "1,5,0.125",
      "1,7,0.9",
      "7,8,0.112" };

  private File testFile;

  @Before
  public void setUp() {
    super.setUp();
    testFile = getTestTempFile("test.txt");
    writeLines(testFile, data);
  }

  [Test]
  public void testLoadFromFile() {
    ItemSimilarity similarity = new FileItemSimilarity(testFile);

    Assert.AreEqual(0.125, similarity.itemSimilarity(1L, 5L), EPSILON);
    Assert.AreEqual(0.125, similarity.itemSimilarity(5L, 1L), EPSILON);
    Assert.AreEqual(0.5, similarity.itemSimilarity(1L, 7L), EPSILON);
    Assert.AreEqual(0.5, similarity.itemSimilarity(7L, 1L), EPSILON);

    Assert.True(Double.IsNaN(similarity.itemSimilarity(7L, 8L)));

    double[] valuesForOne = similarity.itemSimilarities(1L, new long[] { 5L, 7L });
    Assert.NotNull(valuesForOne);
    Assert.AreEqual(2, valuesForOne.Length);
    Assert.AreEqual(0.125, valuesForOne[0], EPSILON);
    Assert.AreEqual(0.5, valuesForOne[1], EPSILON);
  }

  [Test]
  public void testNoRefreshAfterFileUpdate() {
    ItemSimilarity similarity = new FileItemSimilarity(testFile, 0L);

    /// call a method to make sure the original file is loaded
    similarity.itemSimilarity(1L, 5L);

    /// change the underlying file,
     /// we have to wait at least a second to see the change in the file's lastModified timestamp 
    Thread.sleep(2000L);
    writeLines(testFile, changedData);

    /// we shouldn't see any changes in the data as we have not yet refreshed 
    Assert.AreEqual(0.5, similarity.itemSimilarity(1L, 7L), EPSILON);
    Assert.AreEqual(0.5, similarity.itemSimilarity(7L, 1L), EPSILON);
    Assert.True(Double.IsNaN(similarity.itemSimilarity(7L, 8L)));
  }

  [Test]
  public void testRefreshAfterFileUpdate() {
    ItemSimilarity similarity = new FileItemSimilarity(testFile, 0L);

    /// call a method to make sure the original file is loaded 
    similarity.itemSimilarity(1L, 5L);

    /// change the underlying file,
     /// we have to wait at least a second to see the change in the file's lastModified timestamp 
    Thread.sleep(2000L);
    writeLines(testFile, changedData);

    similarity.refresh(null);

    /// we should now see the changes in the data 
    Assert.AreEqual(0.9, similarity.itemSimilarity(1L, 7L), EPSILON);
    Assert.AreEqual(0.9, similarity.itemSimilarity(7L, 1L), EPSILON);
    Assert.AreEqual(0.125, similarity.itemSimilarity(1L, 5L), EPSILON);
    Assert.AreEqual(0.125, similarity.itemSimilarity(5L, 1L), EPSILON);

    Assert.False(Double.IsNaN(similarity.itemSimilarity(7L, 8L)));
    Assert.AreEqual(0.112, similarity.itemSimilarity(7L, 8L), EPSILON);
    Assert.AreEqual(0.112, similarity.itemSimilarity(8L, 7L), EPSILON);
  }

  [Test](expected = IllegalArgumentException.class)
  public void testFileNotFoundExceptionForNonExistingFile() {
    new FileItemSimilarity(new File("xKsdfksdfsdf"));
  }

  [Test]
  public void testFileItemItemSimilarityIterable() {
    Iterable<ItemItemSimilarity> similarityIterable = new FileItemItemSimilarityIterable(testFile);
    GenericItemSimilarity similarity = new GenericItemSimilarity(similarityIterable);

    Assert.AreEqual(0.125, similarity.itemSimilarity(1L, 5L), EPSILON);
    Assert.AreEqual(0.125, similarity.itemSimilarity(5L, 1L), EPSILON);
    Assert.AreEqual(0.5, similarity.itemSimilarity(1L, 7L), EPSILON);
    Assert.AreEqual(0.5, similarity.itemSimilarity(7L, 1L), EPSILON);

    Assert.True(Double.IsNaN(similarity.itemSimilarity(7L, 8L)));

    double[] valuesForOne = similarity.itemSimilarities(1L, new long[] { 5L, 7L });
    Assert.NotNull(valuesForOne);
    Assert.AreEqual(2, valuesForOne.Length);
    Assert.AreEqual(0.125, valuesForOne[0], EPSILON);
    Assert.AreEqual(0.5, valuesForOne[1], EPSILON);
  }

  [Test]
  public void testToString() {
    ItemSimilarity similarity = new FileItemSimilarity(testFile);
    Assert.True(!similarity.toString().isEmpty());
  }

}

}