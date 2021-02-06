using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Maersk.Sorting.Api
{
    public class SortJobProcessor : ISortJobProcessor
    {
        private readonly ILogger<SortJobProcessor> _logger;
        private readonly ConcurrentQueue<SortJob> _jobQueue = new ConcurrentQueue<SortJob>();
        public SortJobProcessor(ILogger<SortJobProcessor> logger)
        {
            _logger = logger;
            Task.Run(() => ProcessQueue());
        }

        #region Public Methods
        public SortJob GetSortJobById(System.Guid id)
        {
            return _jobQueue.Where(x => x.Id == id).FirstOrDefault();
        }

        public List<SortJob> GetSortJobs()
        {
            return _jobQueue.ToList();
        }

        public async Task<SortJob> Process(SortJob job)
        {
            _logger.LogInformation("Processing job with ID '{JobId}'.", job.Id);

            var stopwatch = Stopwatch.StartNew();

            var output = job.Input.OrderBy(n => n).ToArray();
            await Task.Delay(5000); // NOTE: This is just to simulate a more expensive operation

            var duration = stopwatch.Elapsed;

            _logger.LogInformation("Completed processing job with ID '{JobId}'. Duration: '{Duration}'.", job.Id, duration);

            return new SortJob(
                id: job.Id,
                status: SortJobStatus.Completed,
                duration: duration,
                input: job.Input,
                output: output);
        }

        public SortJob PushQueue(SortJob sortJob)
        {
            if (!_jobQueue.Any(x => x.Id == sortJob.Id))
            {
                _logger.LogInformation("Enqueueing the JobID {0}", sortJob.Id);
                _jobQueue.Enqueue(sortJob);
            }
            return sortJob;
        }

        #endregion Public Methods

        #region Private Methods

        private async void ProcessQueue()
        {
            _logger.LogInformation("Processing queue");
            while (true)
            {
                if (!_jobQueue.Any())
                {
                    _logger.LogInformation("Waiting for the record in queue");
                    await Task.Delay(1000);
                }

                else
                {
                    if (_jobQueue.Any(x => x.Status == SortJobStatus.Pending))
                    {
                        SortJob? jobInQueue;
                        if (_jobQueue.TryPeek(out jobInQueue))
                        {
                            if (jobInQueue.Status == SortJobStatus.Completed)
                            {
                                _logger.LogInformation("Dequeueing the item if the status is Completed and enqueue again.");

                                var dequeue = _jobQueue.TryDequeue(out SortJob? deQueue);
                                _jobQueue.Enqueue(jobInQueue);
                            }
                            var response = await Process(jobInQueue);
                            if (jobInQueue.Status != response.Status)
                            {
                                if (_jobQueue.TryDequeue(out SortJob? dequeueJob))
                                {
                                    _logger.LogInformation("De queueing the job to update the status.");
                                    _jobQueue.Enqueue(response);
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No records in the queue");
                    }
                }
            }
        }

        #endregion Private Methods
    }
}
