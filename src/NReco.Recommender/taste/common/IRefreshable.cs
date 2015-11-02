/*
 *  Copyright 2013-2014 Vitalii Fedorchenko
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
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS 
 *  OF ANY KIND, either express or implied.
 */

namespace NReco.CF.Taste.Common {
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Implementations of this interface have state that can be periodically refreshed. For example, an
/// implementation instance might contain some pre-computed information that should be periodically refreshed.
/// The <see cref="IRefreshable.Refresh"/> method triggers such a refresh.
/// <para>
/// All Taste components implement this. In particular, <see cref="NReco.CF.Taste.Recommender.IRecommender"/>s do. Callers may want to call
/// <see cref="IRefreshable.Refresh"/> periodically to re-compute information throughout the system and bring it up
/// to date, though this operation may be expensive.
/// </para>
/// </summary>
public interface IRefreshable {
  
	/// <summery>
	/// Triggers "refresh" -- whatever that means -- of the implementation. The general contract is that any
	/// {@link Refreshable} should always leave itself in a consistent, operational state, and that the refresh
	/// atomically updates internal state from old to new.
	/// </summery>
	/// <param name="alreadyRefreshed">
	/// <see cref="NReco.CF.Taste.Common.IRefreshable"/>s that are known to have already been
	/// refreshed as a result of an initial call to a <see cref="NReco.CF.Taste.Common.IRefreshable.Refresh"/> method on some
	/// object. This ensure that objects in a refresh dependency graph aren't refreshed twice
	/// needlessly.
	/// </param>
	void Refresh(IList<IRefreshable> alreadyRefreshed);
  
}

}