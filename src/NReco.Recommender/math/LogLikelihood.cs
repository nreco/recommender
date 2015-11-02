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
 *  Parts of this code are based on Apache Mahout and Apache Commons Mathematics Library that were licensed under the
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

using NReco.Math3.Util;

namespace NReco.Math3.Stats {


 /// Utility methods for working with log-likelihood
public sealed class LogLikelihood {

  private LogLikelihood() {
  }

   /// Calculates the unnormalized Shannon entropy.  This is
   ///
   /// -sum x_i log x_i / N = -N sum x_i/N log x_i/N
   ///
   /// where N = sum x_i
   ///
   /// If the x's sum to 1, then this is the same as the normal
   /// expression.  Leaving this un-normalized makes working with
   /// counts and computing the LLR easier.
   ///
   /// @return The entropy value for the elements
  public static double entropy(params long[] elements) {
    long sum = 0;
    double result = 0.0;
    foreach (long element in elements) {
      //Preconditions.checkArgument(element >= 0);
      result += xLogX(element);
      sum += element;
    }
    return xLogX(sum) - result;
  }

  private static double xLogX(long x) {
    return x == 0 ? 0.0 : x * MathUtil.Log(x);
  }

   /// Merely an optimization for the common two argument case of {@link #entropy(long...)}
   /// @see #logLikelihoodRatio(long, long, long, long)
  private static double entropy(long a, long b) {
    return xLogX(a + b) - xLogX(a) - xLogX(b);
  }

   /// Merely an optimization for the common four argument case of {@link #entropy(long...)}
   /// @see #logLikelihoodRatio(long, long, long, long)
  private static double entropy(long a, long b, long c, long d) {
    return xLogX(a + b + c + d) - xLogX(a) - xLogX(b) - xLogX(c) - xLogX(d);
  }

   /// Calculates the Raw Log-likelihood ratio for two events, call them A and B.  Then we have:
   /// <p/>
   /// <table border="1" cellpadding="5" cellspacing="0">
   /// <tbody><tr><td>&nbsp;</td><td>Event A</td><td>Everything but A</td></tr>
   /// <tr><td>Event B</td><td>A and B together (k_11)</td><td>B, but not A (k_12)</td></tr>
   /// <tr><td>Everything but B</td><td>A without B (k_21)</td><td>Neither A nor B (k_22)</td></tr></tbody>
   /// </table>
   ///
   /// @param k11 The number of times the two events occurred together
   /// @param k12 The number of times the second event occurred WITHOUT the first event
   /// @param k21 The number of times the first event occurred WITHOUT the second event
   /// @param k22 The number of times something else occurred (i.e. was neither of these events
   /// @return The raw log-likelihood ratio
   ///
   /// <p/>
   /// Credit to http://tdunning.blogspot.com/2008/03/surprise-and-coincidence.html for the table and the descriptions.
  public static double logLikelihoodRatio(long k11, long k12, long k21, long k22) {
    //Preconditions.checkArgument(k11 >= 0 && k12 >= 0 && k21 >= 0 && k22 >= 0);
    // note that we have counts here, not probabilities, and that the entropy is not normalized.
    double rowEntropy = entropy(k11 + k12, k21 + k22);
    double columnEntropy = entropy(k11 + k21, k12 + k22);
    double matrixEntropy = entropy(k11, k12, k21, k22);
    if (rowEntropy + columnEntropy < matrixEntropy) {
      // round off error
      return 0.0;
    }
    return 2.0 * (rowEntropy + columnEntropy - matrixEntropy);
  }
  
   /// Calculates the root log-likelihood ratio for two events.
   /// See {@link #logLikelihoodRatio(long, long, long, long)}.

   /// @param k11 The number of times the two events occurred together
   /// @param k12 The number of times the second event occurred WITHOUT the first event
   /// @param k21 The number of times the first event occurred WITHOUT the second event
   /// @param k22 The number of times something else occurred (i.e. was neither of these events
   /// @return The root log-likelihood ratio
   /// 
   /// <p/>
   /// There is some more discussion here: http://s.apache.org/CGL
   ///
   /// And see the response to Wataru's comment here:
   /// http://tdunning.blogspot.com/2008/03/surprise-and-coincidence.html
  public static double rootLogLikelihoodRatio(long k11, long k12, long k21, long k22) {
    double llr = logLikelihoodRatio(k11, k12, k21, k22);
    double sqrt = Math.Sqrt(llr);
    if ((double) k11 / (k11 + k12) < (double) k21 / (k21 + k22)) {
      sqrt = -sqrt;
    }
    return sqrt;
  }

   /// Compares two sets of counts to see which items are interestingly over-represented in the first
   /// set.
   /// @param a  The first counts.
   /// @param b  The reference counts.
   /// @param maxReturn  The maximum number of items to return.  Use maxReturn >= a.elementSet.Count to return all
   /// scores above the threshold.
   /// @param threshold  The minimum score for items to be returned.  Use 0 to return all items more common
   /// in a than b.  Use -Double.MAX_VALUE (not Double.MIN_VALUE !) to not use a threshold.
   /// @return  A list of scored items with their scores.
  /*public static <T> List<ScoredItem<T>> compareFrequencies(Multiset<T> a,
                                                           Multiset<T> b,
                                                           int maxReturn,
                                                           double threshold) {
    int totalA = a.Count;
    int totalB = b.Count;

    Ordering<ScoredItem<T>> byScoreAscending = new Ordering<ScoredItem<T>>() {
      @Override
      public int compare(ScoredItem<T> tScoredItem, ScoredItem<T> tScoredItem1) {
        return Double.compare(tScoredItem.score, tScoredItem1.score);
      }
    };
    Queue<ScoredItem<T>> best = new PriorityQueue<ScoredItem<T>>(maxReturn + 1, byScoreAscending);

    for (T t : a.elementSet()) {
      compareAndAdd(a, b, maxReturn, threshold, totalA, totalB, best, t);
    }

    // if threshold >= 0 we only iterate through a because anything not there can't be as or more common than in b.
    if (threshold < 0) {
      for (T t : b.elementSet()) {
        // only items missing from a need be scored
        if (a.count(t) == 0) {
          compareAndAdd(a, b, maxReturn, threshold, totalA, totalB, best, t);
        }
      }
    }

    List<ScoredItem<T>> r = Lists.newArrayList(best);
    Collections.sort(r, byScoreAscending.reverse());
    return r;
  }

  private static <T> void compareAndAdd(Multiset<T> a,
                                        Multiset<T> b,
                                        int maxReturn,
                                        double threshold,
                                        int totalA,
                                        int totalB,
                                        Queue<ScoredItem<T>> best,
                                        T t) {
    int kA = a.count(t);
    int kB = b.count(t);
    double score = rootLogLikelihoodRatio(kA, totalA - kA, kB, totalB - kB);
    if (score >= threshold) {
      ScoredItem<T> x = new ScoredItem<T>(t, score);
      best.add(x);
      while (best.Count > maxReturn) {
        best.poll();
      }
    }
  }

  public const class ScoredItem<T> {
    private T item;
    private double score;

    public ScoredItem(T item, double score) {
      this.item = item;
      this.score = score;
    }

    public double getScore() {
      return score;
    }

    public T getItem() {
      return item;
    }
  }*/
}

}