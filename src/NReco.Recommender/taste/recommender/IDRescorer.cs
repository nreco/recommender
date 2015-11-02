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

namespace NReco.CF.Taste.Recommender {
 /// <p>
 /// A {@link Rescorer} which operates on {@code long} primitive IDs, rather than arbitrary {@link Object}s.
 /// This is provided since most uses of this interface in the framework take IDs (as {@code long}) as an
 /// argument, and so this can be used to avoid unnecessary boxing/unboxing.
 /// </p>
public interface IDRescorer {
  
   /// @param id
   ///          ID of thing (user, item, etc.) to rescore
   /// @param originalScore
   ///          original score
   /// @return modified score, or {@link Double#NaN} to indicate that this should be excluded entirely
  double rescore(long id, double originalScore);
  
   /// Returns {@code true} to exclude the given thing.
   ///
   /// @param id
   ///          ID of thing (user, item, etc.) to rescore
   /// @return {@code true} to exclude, {@code false} otherwise
  bool isFiltered(long id);
  
}

}