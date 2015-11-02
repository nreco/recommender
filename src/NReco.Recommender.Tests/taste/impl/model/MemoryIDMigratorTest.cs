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

namespace org.apache.mahout.cf.taste.impl.model {

public sealed class MemoryIDMigratorTest : TasteTestCase {

  private static String DUMMY_STRING = "Mahout";
  private static long DUMMY_ID = -6311185995763544451L;

  [Test]
  public void testTolong() {
    IDMigrator migrator = new MemoryIDMigrator();
    long id = migrator.tolongID(DUMMY_STRING);
    Assert.AreEqual(DUMMY_ID, id);
  }

  [Test]
  public void testStore() {
    UpdatableIDMigrator migrator = new MemoryIDMigrator();
    long id = migrator.tolongID(DUMMY_STRING);
    Assert.IsNull(migrator.toStringID(id));
    migrator.storeMapping(id, DUMMY_STRING);
    Assert.AreEqual(DUMMY_STRING, migrator.toStringID(id));
  }

  [Test]
  public void testInitialize() {
    UpdatableIDMigrator migrator = new MemoryIDMigrator();
    long id = migrator.tolongID(DUMMY_STRING);
    Assert.IsNull(migrator.toStringID(id));
    migrator.initialize(Collections.singleton(DUMMY_STRING));
    Assert.AreEqual(DUMMY_STRING, migrator.toStringID(id));
  }

}

}