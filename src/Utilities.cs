
using Alachisoft.NCache.Runtime.Exceptions;
using CacheManager.Core.Logging;
using System;
using System.Threading.Tasks;

namespace CacheManager.NCache
{
    internal static class Utilities
    {
        private const string ErrorMessage = "Maximum number of tries exceeded to perform the action: {0}.";
        private const string WarningMessage = "Exception occurred performing an action. Retrying... {0}/{1}";


        internal static Exception GetException(Exception e)
        {
            return new Exception($"Exception occured in {e.TargetSite.Name} because {e.Message}", e);
        }

        internal static string GetExceptionInfo(CacheException e)
        {
            return $"Exception details: \nError Code:{e.ErrorCode}\nStacktrace:\n{e.StackTrace}";
        }

        public static T Retry<T>(
            Func<T> retryme,
            int timeOut,
            int retries,
            ILogger logger)
        {
            var tries = 0;
            do
            {
                tries++;

                try
                {
                    return retryme();
                }
                catch (CacheException e)
                {
                    logger.LogCritical(GetExceptionInfo(e));
                    throw;
                }
                catch (System.TimeoutException e)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(e, ErrorMessage, retries);
                        throw;
                    }

                    logger.LogWarn(e, WarningMessage, tries, retries);
#if NET40
                    TaskEx.Delay(timeOut).Wait();
#else
                    Task.Delay(timeOut).Wait();
#endif
                }
                catch (System.AggregateException e)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(e, ErrorMessage, retries);
                        throw;
                    }

                    e.Handle(
                        ex =>
                        {
                            if (ex is CacheException)
                            {
                                logger.LogCritical(GetExceptionInfo((CacheException)ex));
                                return false;
                            }

                            if (ex is System.TimeoutException)
                            {

                                logger.LogWarn(e, WarningMessage, tries, retries);
#if NET40
                            TaskEx.Delay(timeOut).Wait();
#else
                                Task.Delay(timeOut).Wait();
#endif

                                return true;
                            }


                            logger.LogCritical("Unhandled exception occurred.", e);
                            return false;
                        });
                }
                catch (Exception e)
                {
                    logger.LogCritical("Unhandled exception occurred.", e);
                    throw;
                }

            }
            while (tries < retries);

            return default;
        }

        public static void Retry(Action retryme, int timeOut, int retries, ILogger logger)
        {
            Retry(
                () =>
                {
                    retryme();
                    return true;
                },
                timeOut,
                retries,
                logger);
        }
    }
}
