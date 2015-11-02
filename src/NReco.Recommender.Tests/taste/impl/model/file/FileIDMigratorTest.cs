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
using org.apache.mahout.cf.taste.model;
using NUnit.Framework;

namespace org.apache.mahout.cf.taste.impl.model.file {

 /// Tests {@link FileIDMigrator}
public sealed class FileIDMigratorTest : TasteTestCase {

  private static String[] STRING_IDS = {
      "dog",
      "cow" };

  private static String[] UPDATED_STRING_IDS = {
      "dog",
      "cow",
      "donkey" };

  private File testFile;

  @Before
  public void setUp() {
    super.setUp();
    testFile = getTestTempFile("test.txt");
    writeLines(testFile, STRING_IDS);
  }

  [Test]
  public void testLoadFromFile() {
    IDMigrator migrator = new FileIDMigrator(testFile);
    long dogAslong = migrator.tolongID("dog");
    long cowAslong = migrator.tolongID("cow");
    long donkeyAslong = migrator.tolongID("donkey");
    Assert.AreEqual("dog", migrator.toStringID(dogAslong));
    Assert.AreEqual("cow", migrator.toStringID(cowAslong));
    Assert.IsNull(migrator.toStringID(donkeyAslong));
  }

  [Test]
  public void testNoRefreshAfterFileUpdate() {
    IDMigrator migrator = new FileIDMigrator(testFile, 0L);

    /// call a method to make sure the original file is loaded 
    long dogAslong = migrator.tolongID("dog");
    migrator.toStringID(dogAslong);

    /// change the underlying file,
     /// we have to wait at least a second to see the change in the file's lastModified timestamp 
    Thread.sleep(2000L);
    writeLines(testFile, UPDATED_STRING_IDS);

    /// we shouldn't see any changes in the data as we have not yet refreshed 
    long cowAslong = migrator.tolongID("cow");
    long donkeyAslong = migrator.tolongID("donkey");
    Assert.AreEqual("dog", migrator.toStringID(dogAslong));
    Assert.AreEqual("cow", migrator.toStringID(cowAslong));
    Assert.IsNull(migrator.toStringID(donkeyAslong));
  }

  [Test]
  public void testRefreshAfterFileUpdate() {
    IDMigrator migrator = new FileIDMigrator(testFile, 0L);

    /// call a method to make sure the original file is loaded 
    long dogAslong = migrator.tolongID("dog");
    migrator.toStringID(dogAslong);

    /// change the underlying file,
     /// we have to wait at least a second to see the change in the file's lastModified timestamp 
    Thread.sleep(2000L);
    writeLines(testFile, UPDATED_STRING_IDS);

    migrator.refresh(null);

    long cowAslong = migrator.tolongID("cow");
    long donkeyAslong = migrator.tolongID("donkey");
    Assert.AreEqual("dog", migrator.toStringID(dogAslong));
    Assert.AreEqual("cow", migrator.toStringID(cowAslong));
    Assert.AreEqual("donkey", migrator.toStringID(donkeyAslong));
  }
}

}