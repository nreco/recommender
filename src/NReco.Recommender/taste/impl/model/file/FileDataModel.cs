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
using System.Threading;
using System.Globalization;
using System.Text.RegularExpressions; 

using NReco.CF.Taste.Common;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Impl.Model;
using NReco.CF.Taste.Model;

namespace NReco.CF.Taste.Impl.Model.File {


 /// <summary>
 /// A <see cref="IDataModel"/> backed by a delimited file.
 /// </summary>
 /// <remarks>
 /// This class expects a file where each line
 /// contains a user ID, followed by item ID, followed by optional preference value, followed by
 /// optional timestamp. Commas or tabs delimit fields:
 /// 
 /// <para><code>userID,itemID[,preference[,timestamp]]</code></para>
 ///
 /// <para>
 /// Preference value is optional to accommodate applications that have no notion of a
 /// preference value (that is, the user simply expresses a
 /// preference for an item, but no degree of preference).
 /// </para>
 ///
 /// <para>
	/// The preference value is assumed to be parseable as a <code>double</code>. The user IDs and item IDs are
 /// read parsed as <code>long</code>s. The timestamp, if present, is assumed to be parseable as a
 /// <code>long</code>, though this can be overridden via <see cref="FileDataModel.readTimestampFromString"/>.
 /// The preference value may be empty, to indicate "no preference value", but cannot be empty. That is,
 /// this is legal:
 /// </para>
 ///
 /// <para><code>123,456,,129050099059</code></para>
 ///
 /// <p>But this isn't:</p>
 ///
 /// <para><code>123,456,129050099059</code></para>
 ///
 /// <para>
 /// It is also acceptable for the lines to contain additional fields. Fields beyond the third will be ignored.
 /// An empty line, or one that begins with '#' will be ignored as a comment.
 /// </para>
 ///
 /// <para>
 /// This class will reload data from the data file when <see cref="FileDataModel.Refresh"/> is called, unless the file
 /// has been reloaded very recently already.
 /// </para>
 ///
 /// <para>
 /// This class will also look for update "delta" files in the same directory, with file names that start the
 /// same way (up to the first period). These files have the same format, and provide updated data that
 /// supersedes what is in the main data file. This is a mechanism that allows an application to push updates to
 /// <see cref="FileDataModel"/> without re-copying the entire data file.
 /// </para>
 ///
 /// <para>
 /// One small format difference exists. Update files must also be able to express deletes.
 /// This is done by ending with a blank preference value, as in "123,456,".
 /// </para>
 ///
 /// <para>
 /// Note that it's all-or-nothing -- all of the items in the file must express no preference, or the all must.
 /// These cannot be mixed. Put another way there will always be the same number of delimiters on every line of
 /// the file!
 /// </para>
 ///
 /// <para>
 /// This class is not intended for use with very large amounts of data (over, say, tens of millions of rows).
 /// For that, a JDBC-backed {@link DataModel} and a database are more appropriate.
 /// </para>
 ///
 /// <para>
 /// It is possible and likely useful to subclass this class and customize its behavior to accommodate
 /// application-specific needs and input formats.
 /// </para>
 /// </remarks>
public class FileDataModel : AbstractDataModel {

  private static Logger log = LoggerFactory.GetLogger(typeof(FileDataModel));

  public const long DEFAULT_MIN_RELOAD_INTERVAL_MS = 60 * 1000L; // 1 minute?
  private static char COMMENT_CHAR = '#';
  private static char[] DELIMIETERS = {',', '\t'};

  private string dataFile;
  private DateTime lastModified;
  private DateTime lastUpdateFileModified;
  private char delimiter;
  private Regex delimiterRegex;

  //private Splitter delimiterPattern;
  private bool hasPrefValues;
  private IDataModel _delegate;
  //private ReentrantLock reloadLock;
  private bool transpose;
  private bool uniqueUserItemCheck;
  private long minReloadIntervalMS;

   /// @param dataFile
   ///          file containing preferences data. If file is compressed (and name ends in .gz or .zip
   ///          accordingly) it will be decompressed as it is read)
   /// @throws FileNotFoundException
   ///           if dataFile does not exist
   /// @throws IOException
   ///           if file can't be read
  public FileDataModel(string dataFile) : this(dataFile, false, DEFAULT_MIN_RELOAD_INTERVAL_MS) {
  }

  /**
   * @param delimiterRegex If your data file don't use '\t' or ',' as delimiter, you can specify 
   * a custom regex pattern.
   */
  public FileDataModel(string dataFile, string delimiterRegex) :
    this(dataFile, false, DEFAULT_MIN_RELOAD_INTERVAL_MS, true, delimiterRegex) {
  }

   /// @param transpose
   ///          transposes user IDs and item IDs -- convenient for 'flipping' the data model this way
   /// @param minReloadIntervalMS
   ///  the minimum interval in milliseconds after which a full reload of the original datafile is done
   ///  when refresh() is called
   /// @see #FileDataModel(File)
  public FileDataModel(string dataFile, bool transpose, long minReloadIntervalMS, bool uniqueUserItemCheck = true, string delimiterRegex = null) {
    this.dataFile = Path.GetFullPath( dataFile );
    if (!System.IO.File.Exists(dataFile) || Directory.Exists(dataFile)) {
      throw new FileNotFoundException(dataFile);
    }
	if (new FileInfo(dataFile).Length <= 0)
		throw new ArgumentException("dataFile is empty"); //Preconditions.checkArgument(dataFile.Length() > 0L, "dataFile is empty");
    //Preconditions.checkArgument(minReloadIntervalMS >= 0L, "minReloadIntervalMs must be non-negative");

    log.Info("Creating FileDataModel for file {0}", dataFile);

    this.lastModified = System.IO.File.GetLastWriteTime(dataFile);
    this.lastUpdateFileModified = readLastUpdateFileModified();

    var rdr = new StreamReader(dataFile);
    string firstLine = rdr.ReadLine();
    while ( firstLine==String.Empty || firstLine[0] == COMMENT_CHAR) {
      firstLine = rdr.ReadLine();
    }
    rdr.Close();

	if (delimiterRegex != null) {
		this.delimiterRegex = new Regex(delimiterRegex, RegexOptions.Singleline | RegexOptions.Compiled);
	} else {
		delimiter = DetermineDelimiter(firstLine);
	}

	var firstLineSplit = SplitLine(firstLine);
    
	  // If preference value exists and isn't empty then the file is specifying pref values
    hasPrefValues = firstLineSplit.Length >= 3 && !String.IsNullOrWhiteSpace( firstLineSplit[2] );

    this.transpose = transpose;
    this.minReloadIntervalMS = minReloadIntervalMS;
	this.uniqueUserItemCheck = uniqueUserItemCheck;

    reload();
  }

  private string[] SplitLine(string line) {
	  if (delimiterRegex != null) {
		  return delimiterRegex.Split(line);
	  } else {
		  return line.Split(delimiter);
	  }
  }

  public string GetDataFile() {
    return dataFile;
  }

  public char GetDelimiter() {
    return delimiter;
  }

  public IDataModel GetLoadedDataModel() {
	  return _delegate;
  }

  protected void reload() {
    if (Monitor.TryEnter(this)) {
      try {
        _delegate = buildModel();
      } catch (IOException ioe) {
        log.Warn("Exception while reloading", ioe);
      } finally {
        Monitor.Exit(this);
      }
    }
  }

  protected IDataModel buildModel() {

    var newLastModified = System.IO.File.GetLastWriteTime( dataFile ); //.lastModified(
    var newLastUpdateFileModified = readLastUpdateFileModified();

    bool loadFreshData = _delegate == null || newLastModified > lastModified.AddMilliseconds( minReloadIntervalMS );

    var oldLastUpdateFileModifieid = lastUpdateFileModified;
    lastModified = newLastModified;
    lastUpdateFileModified = newLastUpdateFileModified;

    var timestamps = new FastByIDMap<FastByIDMap<DateTime?>>();

    if (hasPrefValues) {

      if (loadFreshData) {

        FastByIDMap<IList<IPreference>> data = new FastByIDMap<IList<IPreference>>();
        using(var iterator = new StreamReader(dataFile)) {
			processFile(iterator, data, timestamps, false);
		}

        foreach (var updateFile in findUpdateFilesAfter(newLastModified)) {
			using (var updFileIterator = new StreamReader(updateFile)) {
				processFile(updFileIterator, data, timestamps, false);
			}
        }

        return new GenericDataModel(GenericDataModel.ToDataMap(data, true), timestamps);

      } else {

        FastByIDMap<IPreferenceArray> rawData = ((GenericDataModel) _delegate).GetRawUserData();

		  var maxLastMod = oldLastUpdateFileModifieid>newLastModified ? oldLastUpdateFileModifieid : newLastModified;

        foreach (var updateFile in findUpdateFilesAfter(maxLastMod)) {
			using (var updFileIterator = new StreamReader(updateFile)) {
				processFile(updFileIterator, rawData, timestamps, true);
			}
        }

        return new GenericDataModel(rawData, timestamps);

      }

    } else {

      if (loadFreshData) {

        FastByIDMap<FastIDSet> data = new FastByIDMap<FastIDSet>();
        using (var iterator = new StreamReader(dataFile)) {
			processFileWithoutID(iterator, data, timestamps);
		}

        foreach (var updateFile in findUpdateFilesAfter(newLastModified)) {
			using (var updFileIterator = new StreamReader(updateFile) ) {
				processFileWithoutID(updFileIterator, data, timestamps);
			}
        }

        return new GenericBooleanPrefDataModel(data, timestamps);

      } else {

        FastByIDMap<FastIDSet> rawData = ((GenericBooleanPrefDataModel) _delegate).getRawUserData();
		var maxLastModified = oldLastUpdateFileModifieid > newLastModified ? oldLastUpdateFileModifieid : newLastModified;

        foreach (var updateFile in findUpdateFilesAfter(maxLastModified)) {
			using (var iterator = new StreamReader(updateFile)) {
				processFileWithoutID(iterator, rawData, timestamps);
			}
        }

        return new GenericBooleanPrefDataModel(rawData, timestamps);

      }

    }
  }

   /// Finds update delta files in the same directory as the data file. This finds any file whose name starts
   /// the same way as the data file (up to first period) but isn't the data file itself. For example, if the
   /// data file is /foo/data.txt.gz, you might place update files at /foo/data.1.txt.gz, /foo/data.2.txt.gz,
   /// etc.
  private IList<string> findUpdateFilesAfter(DateTime minimumLastModified) {
    var dataFileName = Path.GetFileName( dataFile ); //.getName();
    int period = dataFileName.IndexOf('.');
    string startName = period < 0 ? dataFileName : dataFileName.Substring(0, period);
    var parentDir = Path.GetDirectoryName( dataFile );
    IDictionary<DateTime, string> modTimeToUpdateFile = new Dictionary<DateTime,string>();
    /*FileFilter onlyFiles = new FileFilter() {
      public bool accept(File file) {
        return !file.isDirectory();
      }
    };*/
    foreach (var updateFile in Directory.GetFiles(parentDir)) {
      var updateFileName = Path.GetFileName( updateFile );
      if (updateFileName.StartsWith(startName)
          && !updateFileName.Equals(dataFileName)
          && System.IO.File.GetLastWriteTime( updateFile ) >= minimumLastModified) {
        modTimeToUpdateFile[ System.IO.File.GetLastWriteTime( updateFile ) ] = updateFile;
      }
    }
    return modTimeToUpdateFile.Values.ToList();
  }

  private DateTime readLastUpdateFileModified() {
    var mostRecentModification = DateTime.MinValue;
    foreach (var updateFile in findUpdateFilesAfter(DateTime.MinValue)) {
		var updFileLastModified = System.IO.File.GetLastWriteTime( updateFile); // updateFile.lastModified()
		if (updFileLastModified>mostRecentModification)
			mostRecentModification =  updFileLastModified;
    }
    return mostRecentModification;
  }

  public static char DetermineDelimiter(string line) {
    foreach (char possibleDelimieter in DELIMIETERS) {
      if (line.IndexOf(possibleDelimieter) >= 0) {
        return possibleDelimieter;
      }
    }
    throw new ArgumentException("Did not find a delimiter in first line");
  }

  protected void processFile<T>(TextReader dataOrUpdateFileIterator,
                             FastByIDMap<T> data,
                             FastByIDMap<FastByIDMap<DateTime?>> timestamps,
                             bool fromPriorData) {
    log.Info("Reading file info...");
    int count = 0;
	string line;
    while ( (line=dataOrUpdateFileIterator.ReadLine()) != null) {
      if (!String.IsNullOrWhiteSpace(line)) {
        processLine(line, data, timestamps, fromPriorData);
        if (++count % 1000000 == 0) {
          log.Info("Processed {0} lines", count);
        }
      }
    }
    log.Info("Read lines: {0}", count);
  }

   /// <p>
   /// Reads one line from the input file and adds the data to a {@link FastByIDMap} data structure which maps user IDs
   /// to preferences. This assumes that each line of the input file corresponds to one preference. After
   /// reading a line and determining which user and item the preference pertains to, the method should look to
   /// see if the data contains a mapping for the user ID already, and if not, add an empty data structure of preferences
   /// as appropriate to the data.
   /// </p>
   ///
   /// <p>
   /// Note that if the line is empty or begins with '#' it will be ignored as a comment.
   /// </p>
   ///
   /// @param line
   ///          line from input data file
   /// @param data
   ///          all data read so far, as a mapping from user IDs to preferences
   /// @param fromPriorData an implementation detail -- if true, data will map IDs to
   ///  {@link PreferenceArray} since the framework is attempting to read and update raw
   ///  data that is already in memory. Otherwise it maps to {@link Collection}s of
   ///  {@link Preference}s, since it's reading fresh data. Subclasses must be prepared
   ///  to handle this wrinkle.
  protected void processLine<T>(string line,
                             FastByIDMap<T> data, 
                             FastByIDMap<FastByIDMap<DateTime?>> timestamps,
                             bool fromPriorData) {

    // Ignore empty lines and comments
    if (line.Length==0 || line[0] == COMMENT_CHAR) {
      return;
    }

    var tokens = SplitLine(line);
    string userIDString = tokens[0];
    string itemIDString = tokens[1];
    string preferenceValueString = tokens[2];
    bool hasTimestamp = tokens.Length > 3;
    string timestampString = hasTimestamp ? tokens[3] : null;

    long userID = readUserIDFromString(userIDString);
    long itemID = readItemIDFromString(itemIDString);

    if (transpose) {
      long tmp = userID;
      userID = itemID;
      itemID = tmp;
    }

    // This is kind of gross but need to handle two types of storage
    var maybePrefs = data.Get(userID);
    if (fromPriorData) {
      // Data are PreferenceArray

      IPreferenceArray prefs = (IPreferenceArray) maybePrefs;
      if (!hasTimestamp && String.IsNullOrWhiteSpace( preferenceValueString )) {
        // Then line is of form "userID,itemID,", meaning remove
        if (prefs != null) {
          bool exists = false;
          int length = prefs.Length();
          for (int i = 0; i < length; i++) {
            if (prefs.GetItemID(i) == itemID) {
              exists = true;
              break;
            }
          }
          if (exists) {
            if (length == 1) {
              data.Remove(userID);
            } else {
              IPreferenceArray newPrefs = new GenericUserPreferenceArray(length - 1);
              for (int i = 0, j = 0; i < length; i++, j++) {
                if (prefs.GetItemID(i) == itemID) {
                  j--;
                } else {
                  newPrefs.Set(j, prefs.Get(i));
                }
              }
              data.Put(userID, (T)newPrefs);
            }
          }
        }

        removeTimestamp(userID, itemID, timestamps);

      } else {

        float preferenceValue = float.Parse(preferenceValueString, CultureInfo.InvariantCulture);

        bool exists = false;
        if (uniqueUserItemCheck && prefs != null) {
          for (int i = 0; i < prefs.Length(); i++) {
            if (prefs.GetItemID(i) == itemID) {
              exists = true;
              prefs.SetValue(i, preferenceValue);
              break;
            }
          }
        }

        if (!exists) {
          if (prefs == null) {
            prefs = new GenericUserPreferenceArray(1);
          } else {
            IPreferenceArray newPrefs = new GenericUserPreferenceArray(prefs.Length() + 1);
            for (int i = 0, j = 1; i < prefs.Length(); i++, j++) {
              newPrefs.Set(j, prefs.Get(i));
            }
            prefs = newPrefs;
          }
          prefs.SetUserID(0, userID);
          prefs.SetItemID(0, itemID);
          prefs.SetValue(0, preferenceValue);
          data.Put(userID, (T)prefs);          
        }
      }

      addTimestamp(userID, itemID, timestampString, timestamps);

    } else {
      // Data are IEnumerable<Preference>

		IEnumerable<IPreference> prefs = ((IEnumerable<IPreference>)maybePrefs);

      if (!hasTimestamp && String.IsNullOrWhiteSpace( preferenceValueString )) {
        // Then line is of form "userID,itemID,", meaning remove
        if (prefs != null) {
          // remove pref
          var prefsIterator = ((IEnumerable<IPreference>)prefs.ToArray()).GetEnumerator();
          while (prefsIterator.MoveNext()) {
            IPreference pref = prefsIterator.Current;
            if (pref.GetItemID() == itemID) {

				if (prefs is IList<IPreference>)
				((IList<IPreference>)maybePrefs).Remove(pref);// prefsIterator.remove()
					
              break;
            }
          }
        }

        removeTimestamp(userID, itemID, timestamps);
        
      } else {

        float preferenceValue = float.Parse(preferenceValueString, CultureInfo.InvariantCulture);

        bool exists = false;
        if (uniqueUserItemCheck && prefs != null) {
          foreach (IPreference pref in prefs) {
            if (pref.GetItemID() == itemID) {
              exists = true;
              pref.SetValue(preferenceValue);
              break;
            }
          }
        }

        if (!exists) {
          if (prefs == null) {
            prefs = new List<IPreference>(5);
            data.Put(userID, (T)prefs);
          }

		  if (prefs is IList<IPreference>)
			  ((IList<IPreference>)prefs).Add(new GenericPreference(userID, itemID, preferenceValue));
        }

        addTimestamp(userID, itemID, timestampString, timestamps);

      }

    }
  }

  protected void processFileWithoutID(TextReader dataOrUpdateFileIterator,
                                      FastByIDMap<FastIDSet> data,
                                      FastByIDMap<FastByIDMap<DateTime?>> timestamps) {
    log.Info("Reading file info...");
    int count = 0;
	string line;
	while ((line = dataOrUpdateFileIterator.ReadLine()) != null) {
      if (!String.IsNullOrWhiteSpace(line)) {
        processLineWithoutID(line, data, timestamps);
        if (++count % 100000 == 0) {
          log.Info("Processed {0} lines", count);
        }
      }
    }
    log.Info("Read lines: {0}", count);
  }

  protected void processLineWithoutID(String line,
                                      FastByIDMap<FastIDSet> data,
                                      FastByIDMap<FastByIDMap<DateTime?>> timestamps) {

    if (String.IsNullOrWhiteSpace(line) || line[0] == COMMENT_CHAR) {
      return;
    }

    var tokens = SplitLine( line );
    string userIDString = tokens[0];
    string itemIDString = tokens[1];
    bool hasPreference = tokens.Length>2;
    string preferenceValueString = hasPreference ? tokens[2] : "";
    bool hasTimestamp = tokens.Length>3;
    string timestampString = hasTimestamp ? tokens[3] : null;

    long userID = readUserIDFromString(userIDString);
    long itemID = readItemIDFromString(itemIDString);

    if (transpose) {
      long tmp = userID;
      userID = itemID;
      itemID = tmp;
    }

    if (hasPreference && !hasTimestamp && String.IsNullOrEmpty( preferenceValueString)) {
      // Then line is of form "userID,itemID,", meaning remove

      FastIDSet itemIDs = data.Get(userID);
      if (itemIDs != null) {
        itemIDs.Remove(itemID);
      }

      removeTimestamp(userID, itemID, timestamps);

    } else {

      FastIDSet itemIDs = data.Get(userID);
      if (itemIDs == null) {
        itemIDs = new FastIDSet(2);
        data.Put(userID, itemIDs);
      }
      itemIDs.Add(itemID);

      addTimestamp(userID, itemID, timestampString, timestamps);

    }
  }

  private void addTimestamp(long userID,
                            long itemID,
                            string timestampString,
                            FastByIDMap<FastByIDMap<DateTime?>> timestamps) {
    if (timestampString != null) {
      FastByIDMap<DateTime?> itemTimestamps = timestamps.Get(userID);
      if (itemTimestamps == null) {
        itemTimestamps = new FastByIDMap<DateTime?>();
        timestamps.Put(userID, itemTimestamps);
      }
      var timestamp = readTimestampFromString(timestampString);
      itemTimestamps.Put(itemID, timestamp);
    }
  }

  private static void removeTimestamp(long userID,
                                      long itemID,
                                      FastByIDMap<FastByIDMap<DateTime?>> timestamps) {
    FastByIDMap<DateTime?> itemTimestamps = timestamps.Get(userID);
    if (itemTimestamps != null) {
      itemTimestamps.Remove(itemID);
    }
  }

   /// Subclasses may wish to override this if ID values in the file are not numeric. This provides a hook by
   /// which subclasses can inject an {@link NReco.CF.Taste.Model.IDMigrator} to perform
   /// translation.
  protected long readUserIDFromString(String value) {
	  return long.Parse(value, CultureInfo.InvariantCulture);
  }

   /// Subclasses may wish to override this if ID values in the file are not numeric. This provides a hook by
   /// which subclasses can inject an {@link NReco.CF.Taste.Model.IDMigrator} to perform
   /// translation.
  protected long readItemIDFromString(String value) {
	  return long.Parse(value, CultureInfo.InvariantCulture);
  }

   /// Subclasses may wish to override this to change how time values in the input file are parsed.
   /// By default they are expected to be numeric, expressing a time as milliseconds since the epoch.
  static DateTime unixTimestampEpochStart = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
  protected DateTime readTimestampFromString(string value) {
	  var unixTimestamp = long.Parse(value, CultureInfo.InvariantCulture);
	  return unixTimestampEpochStart.AddMilliseconds(unixTimestamp);
  }

  public override IEnumerator<long> GetUserIDs() {
    return _delegate.GetUserIDs();
  }

  public override IPreferenceArray GetPreferencesFromUser(long userID) {
    return _delegate.GetPreferencesFromUser(userID);
  }

  public override FastIDSet GetItemIDsFromUser(long userID) {
    return _delegate.GetItemIDsFromUser(userID);
  }

  public override IEnumerator<long> GetItemIDs() {
    return _delegate.GetItemIDs();
  }

  public override IPreferenceArray GetPreferencesForItem(long itemID) {
    return _delegate.GetPreferencesForItem(itemID);
  }

  public override float? GetPreferenceValue(long userID, long itemID) {
    return _delegate.GetPreferenceValue(userID, itemID);
  }

  public override DateTime? GetPreferenceTime(long userID, long itemID) {
    return _delegate.GetPreferenceTime(userID, itemID);
  }

  public override int GetNumItems() {
    return _delegate.GetNumItems();
  }

  public override int GetNumUsers() {
    return _delegate.GetNumUsers();
  }

  public override int GetNumUsersWithPreferenceFor(long itemID) {
    return _delegate.GetNumUsersWithPreferenceFor(itemID);
  }

  public override int GetNumUsersWithPreferenceFor(long itemID1, long itemID2) {
    return _delegate.GetNumUsersWithPreferenceFor(itemID1, itemID2);
  }

   /// Note that this method only updates the in-memory preference data that this {@link FileDataModel}
   /// maintains; it does not modify any data on disk. Therefore any updates from this method are only
   /// temporary, and lost when data is reloaded from a file. This method should also be considered relatively
   /// slow.
  public override void SetPreference(long userID, long itemID, float value) {
    _delegate.SetPreference(userID, itemID, value);
  }

  /// See the warning at {@link #setPreference(long, long, float)}. 
  public override void RemovePreference(long userID, long itemID) {
    _delegate.RemovePreference(userID, itemID);
  }

  public override void Refresh(IList<IRefreshable> alreadyRefreshed) {
    if ( System.IO.File.GetLastWriteTime( dataFile ) > lastModified.AddMilliseconds(minReloadIntervalMS)
        || readLastUpdateFileModified() > lastUpdateFileModified.AddMilliseconds(minReloadIntervalMS) ) {
      log.Debug("File has changed; reloading...");
      reload();
    }
  }

  public override bool HasPreferenceValues() {
    return _delegate.HasPreferenceValues();
  }

  public override float GetMaxPreference() {
    return _delegate.GetMaxPreference();
  }

  public override float GetMinPreference() {
    return _delegate.GetMinPreference();
  }

  public override string ToString() {
    return "FileDataModel[dataFile:" + dataFile + ']';
  }

}

}