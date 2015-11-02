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
using java.sql;

using javax.sql;

using org.apache.mahout.cf.taste.common;
using org.apache.mahout.cf.taste.model;
using org.apache.mahout.common;

namespace org.apache.mahout.cf.taste.impl.model {

 /// Implementation which stores the reverse long-to-String mapping in a database. Subclasses can override and
 /// configure the class to operate with particular databases by supplying appropriate SQL statements to the
 /// constructor.
public abstract class AbstractJDBCIDMigrator : AbstractIDMigrator, UpdatableIDMigrator {
  
  public const String DEFAULT_MAPPING_TABLE = "taste_id_mapping";
  public const String DEFAULT_LONG_ID_COLUMN = "long_id";
  public const String DEFAULT_STRING_ID_COLUMN = "string_id";
  
  private DataSource dataSource;
  private String getStringIDSQL;
  private String storeMappingSQL;
  
   /// @param getStringIDSQL
   ///          SQL statement which selects one column, the String ID, from a mapping table. The statement
   ///          should take one long parameter.
   /// @param storeMappingSQL
   ///          SQL statement which saves a mapping from long to String. It should take two parameters, a long
   ///          and a String.
  protected AbstractJDBCIDMigrator(DataSource dataSource, String getStringIDSQL, String storeMappingSQL) {
    this.dataSource = dataSource;
    this.getStringIDSQL = getStringIDSQL;
    this.storeMappingSQL = storeMappingSQL;
  }
  
  public void storeMapping(long longID, String stringID) {
    Connection conn = null;
    PreparedStatement stmt = null;
    try {
      conn = dataSource.getConnection();
      stmt = conn.prepareStatement(storeMappingSQL);
      stmt.setlong(1, longID);
      stmt.setString(2, stringID);
      stmt.executeUpdate();
    } catch (SQLException sqle) {
      throw new TasteException(sqle);
    } finally {
      IOUtils.quietClose(null, stmt, conn);
    }
  }
  
  public string toStringID(long longID) {
    Connection conn = null;
    PreparedStatement stmt = null;
    ResultSet rs = null;
    try {
      conn = dataSource.getConnection();
      stmt = conn.prepareStatement(getStringIDSQL, ResultSet.TYPE_FORWARD_ONLY, ResultSet.CONCUR_READ_ONLY);
      stmt.setFetchDirection(ResultSet.FETCH_FORWARD);
      stmt.setFetchSize(1);
      stmt.setlong(1, longID);
      rs = stmt.executeQuery();
      if (rs.next()) {
        return rs.getString(1);
      } else {
        return null;
      }
    } catch (SQLException sqle) {
      throw new TasteException(sqle);
    } finally {
      IOUtils.quietClose(rs, stmt, conn);
    }
  }

  public void initialize(Iterable<String> stringIDs) {
    for (String stringID : stringIDs) {
      storeMapping(tolongID(stringID), stringID);
    }
  }

}

}