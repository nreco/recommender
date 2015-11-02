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

using org.apache.mahout.cf.taste.common;
using org.apache.mahout.cf.taste.impl.similarity;
using org.apache.mahout.cf.taste.similarity;

namespace org.apache.mahout.cf.taste.impl.similarity.file {

 /// <p>
 /// An {@link ItemSimilarity} backed by a comma-delimited file. This class typically expects a file where each line
 /// contains an item ID, followed by another item ID, followed by a similarity value, separated by commas. You may also
 /// use tabs.
 /// </p>
 ///
 /// <p>
 /// The similarity value is assumed to be parseable as a {@code double} having a value between -1 and 1. The
 /// item IDs are parsed as {@code long}s. Similarities are symmetric so for a pair of items you do not have to
 /// include 2 lines in the file.
 /// </p>
 ///
 /// <p>
 /// This class will reload data from the data file when {@link #refresh(Collection)} is called, unless the file
 /// has been reloaded very recently already.
 /// </p>
 ///
 /// <p>
 /// This class is not intended for use with very large amounts of data. For that, a JDBC-backed {@link ItemSimilarity}
 /// and a database are more appropriate.
 /// </p>
public class FileItemSimilarity : ItemSimilarity {

  public const long DEFAULT_MIN_RELOAD_INTERVAL_MS = 60 * 1000L; // 1 minute?

  private ItemSimilarity _delegate;
  private File dataFile;
  private long lastModified;
  private long minReloadIntervalMS;

  private static Logger log = LoggerFactory.getLogger(typeof(FileItemSimilarity));

   /// @param dataFile
   ///          file containing the similarity data
  public FileItemSimilarity(File dataFile) : this(dataFile, DEFAULT_MIN_RELOAD_INTERVAL_MS) {
  }

   /// @param minReloadIntervalMS
   ///          the minimum interval in milliseconds after which a full reload of the original datafile is done
   ///          when refresh() is called
   /// @see #FileItemSimilarity(File)
  public FileItemSimilarity(File dataFile, long minReloadIntervalMS) {
    //Preconditions.checkArgument(dataFile != null, "dataFile is null");
    //Preconditions.checkArgument(dataFile.exists() && !dataFile.isDirectory(),
    //  "dataFile is missing or a directory: %s", dataFile);

    log.info("Creating FileItemSimilarity for file {}", dataFile);

    this.dataFile = dataFile.getAbsoluteFile();
    this.lastModified = dataFile.lastModified();
    this.minReloadIntervalMS = minReloadIntervalMS;
    this.reloadLock = new ReentrantLock();

    reload();
  }

  public double[] itemSimilarities(long itemID1, long[] itemID2s) {
    return _delegate.itemSimilarities(itemID1, itemID2s);
  }

  public long[] allSimilarItemIDs(long itemID) {
    return _delegate.allSimilarItemIDs(itemID);
  }

  public double itemSimilarity(long itemID1, long itemID2) {
    return _delegate.itemSimilarity(itemID1, itemID2);
  }

  public void refresh(IEnumerable<Refreshable> alreadyRefreshed) {
    if (dataFile.lastModified() > lastModified + minReloadIntervalMS) {
      log.debug("File has changed; reloading...");
      reload();
    }
  }

  protected void reload() {
    if (reloadLock.tryLock()) {
      try {
        long newLastModified = dataFile.lastModified();
        _delegate = new GenericItemSimilarity(new FileItemItemSimilarityIterable(dataFile));
        lastModified = newLastModified;
      } finally {
        reloadLock.unlock();
      }
    }
  }

  public String toString() {
    return "FileItemSimilarity[dataFile:" + dataFile + ']';
  }

}

}