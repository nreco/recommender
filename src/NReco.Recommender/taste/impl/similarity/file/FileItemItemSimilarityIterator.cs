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
using System.Text.RegularExpressions;

using org.apache.mahout.cf.taste.impl.similarity;
using org.apache.mahout.common.iterator;


namespace org.apache.mahout.cf.taste.impl.similarity.file {

	/// a simple iterator using a {@link FileLineIterator} internally, parsing each
	/// line into an {@link GenericItemSimilarity.ItemItemSimilarity}.
	sealed class FileItemItemSimilarityIterator : IEnumerable<GenericItemSimilarity.ItemItemSimilarity> {

	  private static char[] SEPARATORS = new [] { '\t', ',' };

	  private IEnumerable<GenericItemSimilarity.ItemItemSimilarity> _Delegate;

	  FileItemItemSimilarityIterator(Stream similaritiesFile) {
		  var rdr = new StreamReader(similaritiesFile);
		  string line;
		  var rs = new List<GenericItemSimilarity.ItemItemSimilarity>();
		  while ( (line = rdr.ReadLine())!=null) {
			  var tokens = line.Split( SEPARATORS );
			  rs.Add( new GenericItemSimilarity.ItemItemSimilarity(Convert.ToInt64(tokens[0]),
																	Convert.ToInt64(tokens[1]),
																	Convert.ToDouble(tokens[2])) );
		  }
		  _Delegate = rs.ToArray();
	  }

	  protected IEnumerable<GenericItemSimilarity.ItemItemSimilarity> Delegate {
		get {
			return _Delegate;
		}
	  }

	}

}