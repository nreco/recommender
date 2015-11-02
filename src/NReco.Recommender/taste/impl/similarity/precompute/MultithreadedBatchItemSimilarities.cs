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
using java.util.concurrent;
using java.util.concurrent.atomic;

using com.google.common.collect;
using com.google.common.io;
using org.apache.mahout.cf.taste.common;
using org.apache.mahout.cf.taste.impl.common;
using org.apache.mahout.cf.taste.model;
using org.apache.mahout.cf.taste.recommender;
using org.apache.mahout.cf.taste.similarity.precompute;

namespace org.apache.mahout.cf.taste.impl.similarity.precompute {

 /// Precompute item similarities in parallel on a single machine. The recommender given to this class must use a
 /// DataModel that holds the interactions in memory (such as
 /// {@link org.apache.mahout.cf.taste.impl.model.GenericDataModel} or
 /// {@link org.apache.mahout.cf.taste.impl.model.file.FileDataModel}) as fast random access to the data is required
public class MultithreadedBatchItemSimilarities : BatchItemSimilarities {

  private int batchSize;

  private static int DEFAULT_BATCH_SIZE = 100;

  private static Logger log = LoggerFactory.getLogger(typeof(MultithreadedBatchItemSimilarities));

   /// @param recommender recommender to use
   /// @param similarItemsPerItem number of similar items to compute per item
  public MultithreadedBatchItemSimilarities(ItemBasedRecommender recommender, int similarItemsPerItem) :
	  this(recommender, similarItemsPerItem, DEFAULT_BATCH_SIZE) {
  }

   /// @param recommender recommender to use
   /// @param similarItemsPerItem number of similar items to compute per item
   /// @param batchSize size of item batches sent to worker threads
  public MultithreadedBatchItemSimilarities(ItemBasedRecommender recommender, int similarItemsPerItem, int batchSize) :
	  base(recommender, similarItemsPerItem) {
    this.batchSize = batchSize;
  }

  public int computeItemSimilarities(int degreeOfParallelism, int maxDurationInHours, SimilarItemsWriter writer)
    {

    ExecutorService executorService = Executors.newFixedThreadPool(degreeOfParallelism + 1);

    Output output = null;
    try {
      writer.open();

      DataModel dataModel = getRecommender().getDataModel();

      BlockingQueue<long[]> itemsIDsInBatches = queueItemIDsInBatches(dataModel, batchSize);
      BlockingQueue<List<SimilarItems>> results = new LinkedBlockingQueue<List<SimilarItems>>();

      AtomicInteger numActiveWorkers = new AtomicInteger(degreeOfParallelism);
      for (int n = 0; n < degreeOfParallelism; n++) {
        executorService.execute(new SimilarItemsWorker(n, itemsIDsInBatches, results, numActiveWorkers));
      }

      output = new Output(results, writer, numActiveWorkers);
      executorService.execute(output);

    } catch (Exception e) {
      throw new IOException(e);
    } finally {
      executorService.shutdown();
      try {
        bool succeeded = executorService.awaitTermination(maxDurationInHours, TimeUnit.HOURS);
        if (!succeeded) {
          throw new RuntimeException("Unable to complete the computation in " + maxDurationInHours + " hours!");
        }
      } catch (InterruptedException e) {
        throw new RuntimeException(e);
      }
      Closeables.close(writer, false);
    }

    return output.getNumSimilaritiesProcessed();
  }

  private static BlockingQueue<long[]> queueItemIDsInBatches(DataModel dataModel, int batchSize) {

    longPrimitiveIterator itemIDs = dataModel.getItemIDs();
    int numItems = dataModel.getNumItems();

    BlockingQueue<long[]> itemIDBatches = new LinkedBlockingQueue<long[]>((numItems / batchSize) + 1);

    long[] batch = new long[batchSize];
    int pos = 0;
    while (itemIDs.hasNext()) {
      if (pos == batchSize) {
        itemIDBatches.add(batch.clone());
        pos = 0;
      }
      batch[pos] = itemIDs.nextlong();
      pos++;
    }
    int nonQueuedItemIDs = batchSize - pos;
    if (nonQueuedItemIDs > 0) {
      long[] lastBatch = new long[nonQueuedItemIDs];
      Array.Copy(batch, 0, lastBatch, 0, nonQueuedItemIDs);
      itemIDBatches.add(lastBatch);
    }

    log.info("Queued {} items in {} batches", numItems, itemIDBatches.Count);

    return itemIDBatches;
  }


  private static class Output : Runnable {

    private BlockingQueue<List<SimilarItems>> results;
    private SimilarItemsWriter writer;
    private AtomicInteger numActiveWorkers;
    private int numSimilaritiesProcessed = 0;

    Output(BlockingQueue<List<SimilarItems>> results, SimilarItemsWriter writer, AtomicInteger numActiveWorkers) {
      this.results = results;
      this.writer = writer;
      this.numActiveWorkers = numActiveWorkers;
    }

    private int getNumSimilaritiesProcessed() {
      return numSimilaritiesProcessed;
    }

    public void run() {
      while (numActiveWorkers.get() != 0) {
        try {
          List<SimilarItems> similarItemsOfABatch = results.poll(10, TimeUnit.MILLISECONDS);
          if (similarItemsOfABatch != null) {
            foreach (SimilarItems similarItems in similarItemsOfABatch) {
              writer.add(similarItems);
              numSimilaritiesProcessed += similarItems.numSimilarItems();
            }
          }
        } catch (Exception e) {
          throw new RuntimeException(e);
        }
      }
    }
  }

  private class SimilarItemsWorker : Runnable {

    private int number;
    private BlockingQueue<long[]> itemIDBatches;
    private BlockingQueue<List<SimilarItems>> results;
    private AtomicInteger numActiveWorkers;

    SimilarItemsWorker(int number, BlockingQueue<long[]> itemIDBatches, BlockingQueue<List<SimilarItems>> results,
        AtomicInteger numActiveWorkers) {
      this.number = number;
      this.itemIDBatches = itemIDBatches;
      this.results = results;
      this.numActiveWorkers = numActiveWorkers;
    }

    public void run() {

      int numBatchesProcessed = 0;
      while (!itemIDBatches.isEmpty()) {
        try {
          long[] itemIDBatch = itemIDBatches.take();

          List<SimilarItems> similarItemsOfBatch = Lists.newArrayListWithCapacity(itemIDBatch.Length);
          foreach (long itemID in itemIDBatch) {
            List<RecommendedItem> similarItems = getRecommender().mostSimilarItems(itemID, getSimilarItemsPerItem());

            similarItemsOfBatch.add(new SimilarItems(itemID, similarItems));
          }

          results.offer(similarItemsOfBatch);

          if (++numBatchesProcessed % 5 == 0) {
            log.info("worker {} processed {} batches", number, numBatchesProcessed);
          }

        } catch (Exception e) {
          throw new RuntimeException(e);
        }
      }
      log.info("worker {} processed {} batches. done.", number, numBatchesProcessed);
      numActiveWorkers.decrementAndGet();
    }
  }
}

}