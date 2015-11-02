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

namespace NReco.CF.Taste.Impl.Common {
/// <summary>
/// Interface for classes that can keep track of a running average of a series of numbers. One can add to or
/// remove from the series, as well as update a datum in the series. The class does not actually keep track of
/// the series of values, just its running average, so it doesn't even matter if you remove/change a value that
/// wasn't added.
/// </summary>
public interface IRunningAverage {
  
   /// @param datum
   ///          new item to add to the running average
   /// @throws IllegalArgumentException
   ///           if datum is {@link Double#NaN}
  void AddDatum(double datum);
  
   /// @param datum
   ///          item to remove to the running average
   /// @throws IllegalArgumentException
   ///           if datum is {@link Double#NaN}
   /// @throws InvalidOperationException
   ///           if count is 0
  void RemoveDatum(double datum);
  
   /// @param delta
   ///          amount by which to change a datum in the running average
   /// @throws IllegalArgumentException
   ///           if delta is {@link Double#NaN}
   /// @throws InvalidOperationException
   ///           if count is 0
  void ChangeDatum(double delta);
  
  int GetCount();
  
  double GetAverage();

   /// @return a (possibly immutable) object whose average is the negative of this object's
  IRunningAverage Inverse();
  
}

}