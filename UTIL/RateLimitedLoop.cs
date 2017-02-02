using System;
using System.Threading;
using log4net;

namespace Utils
{
    /// <summary>
    /// This class represents an error-handled rate-limited loop which is specifically designed for 
    /// operations which might be executed only once in a defined timespan.
    /// </summary>
    public class RateLimitedLoop
    {
        Thread backgroundThread;
        Action<Func<bool>> target;
        int minTimeBetweenCalls;
        DateTime lastInvocation;
        bool shouldRun;

        protected readonly ILog logger;

        /// <summary>
        /// Gets the time between two attempts.
        /// If an invocation takes longer than this time and fails, it will be reexecuted immediately.
        /// If it takes shorter, the RLL makes sure the target is not called at shorter intervals than this property.
        /// </summary>
        /// <value>The rate limit time.</value>
        public int RateLimitTime
        {
            get { return minTimeBetweenCalls; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LedControl.Common.RateLimitedLoop"/> class.
        /// </summary>
        /// <param name="func">The target function to execute</param>
        /// <param name="minimumTimeBetweenCalls">Minimum time between calls in milliseconds.</param>
        /// <param name="name">Optional: A name of the loop for debugging purposes</param>
        public RateLimitedLoop(Action<Func<bool>> func, int minimumTimeBetweenCalls = 100, string name = null)
        {
            name = name ?? "Unnamed";
            backgroundThread = new Thread(threadRun);
            minTimeBetweenCalls = minimumTimeBetweenCalls;
            target = func;
            lastInvocation = new DateTime(0);
            logger = LogManager.GetLogger($"RLL: {name}");
        }

        /// <summary>
        /// Start the rate limited loop.
        /// </summary>
        public void Start()
        {
            shouldRun = true;
            backgroundThread.Start();
            logger.Debug($"Starting RLL@{minTimeBetweenCalls}ms.");
        }

        /// <summary>
        /// Stop the rate limited loop.
        /// </summary>
        public void Stop()
        {
            logger.Debug($"Stopping RLL.");
            shouldRun = false;
            if (!backgroundThread.Join(1000))
            {
                logger.Debug($"Terminating RLL.");
                backgroundThread.Abort();
                if (!backgroundThread.Join(1000))
                {
                    logger.Warn("Failed to terminate RLL!");
                }
            }
            else
            {
                logger.Debug("RLL finished in a clean state.");
            }
            backgroundThread = new Thread(threadRun);
        }

        /// <summary>
        /// The internal run function for the spawned background thread.
        /// </summary>
        void threadRun()
        {
            while (shouldRun)
            {
                var timeSinceLastInvocation = (DateTime.UtcNow - lastInvocation).TotalMilliseconds;
                var diff = minTimeBetweenCalls - timeSinceLastInvocation;
                if (diff > 0)
                {
                    Thread.Sleep((int)diff);
                }
                lastInvocation = DateTime.UtcNow;

                try
                {
                    target(() => shouldRun);
                }
                catch (ThreadAbortException)
                {
                    logger.Debug("RLL was aborted.");
                }
                catch (Exception e)
                {
                    logger.Warn("RLL call failed, retrying.", e);
                }
            }
        }
    }
}
