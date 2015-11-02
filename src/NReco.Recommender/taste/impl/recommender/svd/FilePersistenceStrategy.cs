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

namespace NReco.CF.Taste.Impl.Recommender.SVD {

/// <summary>Provides a file-based persistent store. </summary>
public class FilePersistenceStrategy : IPersistenceStrategy {

  private string file;

  private static Logger log = LoggerFactory.GetLogger(typeof(FilePersistenceStrategy));

   /// @param file the file to use for storage. If the file does not exist it will be created when required.
  public FilePersistenceStrategy(string file) {
    this.file = file; // Preconditions.checkNotNull(file);
  }

  public Factorization Load() {
    if (!File.Exists(file)) {
      log.Info("{0} does not yet exist, no factorization found", file);
      return null;
    }
    Stream inFile = null;
    try {
      log.Info("Reading factorization from {0}...", file);
      inFile = new FileStream(file, FileMode.Open, FileAccess.Read);
      return readBinary(inFile);
    } finally {
      inFile.Close();
    }
  }

  public void MaybePersist(Factorization factorization) {
    Stream outFile = null;
    try {
      log.Info("Writing factorization to {0}...", file);
      outFile = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write);
      writeBinary(factorization, outFile);
    } finally {
      outFile.Close();
    }
  }

  protected static void writeBinary(Factorization factorization, Stream outFile) {
	var binWr = new BinaryWriter(outFile);
    binWr.Write( factorization.numFeatures() );
    binWr.Write( factorization.numUsers() );
    binWr.Write( factorization.numItems() );

    foreach (var mappingEntry in factorization.getUserIDMappings()) {
	  if (!mappingEntry.Value.HasValue)
		  continue; //?correct?

	  long userID = mappingEntry.Key;
      binWr.Write(mappingEntry.Value.Value);
      binWr.Write( userID );
      try {
        double[] userFeatures = factorization.getUserFeatures(userID);
        for (int feature = 0; feature < factorization.numFeatures(); feature++) {
          binWr.Write(userFeatures[feature]);
        }
      } catch (NoSuchUserException e) {
        throw new IOException("Unable to persist factorization", e);
      }
    }

    foreach (var entry in factorization.getItemIDMappings()) {
	  if (!entry.Value.HasValue)
		  continue; //?correct?

      long itemID = entry.Key;
      binWr.Write(entry.Value.Value);
      binWr.Write(itemID);
      try {
        double[] itemFeatures = factorization.getItemFeatures(itemID);
        for (int feature = 0; feature < factorization.numFeatures(); feature++) {
          binWr.Write(itemFeatures[feature]);
        }
      } catch (NoSuchItemException e) {
        throw new IOException("Unable to persist factorization", e);
      }
    }
  }

  public static Factorization readBinary(Stream inFile) {
	  var binRdr = new BinaryReader(inFile);

    int numFeatures = binRdr.ReadInt32();
    int numUsers = binRdr.ReadInt32();
    int numItems = binRdr.ReadInt32();

    FastByIDMap<int?> userIDMapping = new FastByIDMap<int?>(numUsers);
    double[][] userFeatures = new double[numUsers][];

    for (int n = 0; n < numUsers; n++) {
      int userIndex = binRdr.ReadInt32();
      long userID = binRdr.ReadInt64();

		userFeatures[userIndex] = new double[numFeatures];

      userIDMapping.Put(userID, userIndex);
      for (int feature = 0; feature < numFeatures; feature++) {
        userFeatures[userIndex][feature] = binRdr.ReadDouble();
      }
    }

    FastByIDMap<int?> itemIDMapping = new FastByIDMap<int?>(numItems);
    double[][] itemFeatures = new double[numItems][];

    for (int n = 0; n < numItems; n++) {
      int itemIndex = binRdr.ReadInt32();
      long itemID = binRdr.ReadInt64();

		itemFeatures[itemIndex] = new double[numFeatures];

      itemIDMapping.Put(itemID, itemIndex);
      for (int feature = 0; feature < numFeatures; feature++) {
		  itemFeatures[itemIndex][feature] = binRdr.ReadDouble();
      }
    }

    return new Factorization(userIDMapping, itemIDMapping, userFeatures, itemFeatures);
  }

}

}