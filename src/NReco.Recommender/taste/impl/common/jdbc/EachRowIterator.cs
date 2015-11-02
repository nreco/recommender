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
using javax.sql;
using java.sql;

using com.google.common.collect;
using org.apache.mahout.common;
using org.slf4j;

namespace org.apache.mahout.cf.taste.impl.common.jdbc {

 /// Provides an {@link java.util.Iterator} over the result of an SQL query, as an iteration over the {@link ResultSet}.
 /// While the same object will be returned from the iteration each time, it will be returned once for each row
 /// of the result.
sealed class EachRowIterator : AbstractIEnumerable<ResultSet>, Closeable {

  private static Logger log = LoggerFactory.getLogger(EachRowIterator.class);

  private Connection connection;
  private PreparedStatement statement;
  private ResultSet resultSet;

  EachRowIterator(DataSource dataSource, String sqlQuery) {
    try {
      connection = dataSource.getConnection();
      statement = connection.prepareStatement(sqlQuery, ResultSet.TYPE_FORWARD_ONLY, ResultSet.CONCUR_READ_ONLY);
      statement.setFetchDirection(ResultSet.FETCH_FORWARD);
      //statement.setFetchSize(getFetchSize());
      log.debug("Executing SQL query: {}", sqlQuery);
      resultSet = statement.executeQuery();
    } catch (SQLException sqle) {
      close();
      throw sqle;
    }
  }

  protected ResultSet computeNext() {
    try {
      if (resultSet.next()) {
        return resultSet;
      } else {
        close();
        return null;
      }
    } catch (SQLException sqle) {
      close();
      throw new InvalidOperationException(sqle);
    }
  }

  public void skip(int n) {
    try {
      resultSet.relative(n);
    } catch (SQLException sqle) {
      // Can't use relative on MySQL Connector/J; try advancing manually
      int i = 0;
      while (i < n && resultSet.next()) {
        i++;
      }
    }
  }

  public void close() {
    IOUtils.quietClose(resultSet, statement, connection);
    endOfData();
  }

}

}