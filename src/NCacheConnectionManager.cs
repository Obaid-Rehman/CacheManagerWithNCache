using Alachisoft.NCache.Client;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Caching.Messaging;
using Alachisoft.NCache.Runtime.Exceptions;
using CacheManager.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.NCache
{
    internal class NCacheConnectionManager
    {
        private static volatile IDictionary<string, ICache> _connections =
            new Dictionary<string, ICache>();

        private static volatile IDictionary<string, ITopic> _topics =
            new Dictionary<string, ITopic>();

        private static volatile Dictionary<string, IDurableTopicSubscription> _subscriptions =
            new Dictionary<string, IDurableTopicSubscription>();

        private static object _connectionLock =
            new object();

        private static object _topicLock =
            new object();

        private static object _subscriptionLock =
            new object();


        private readonly ILogger _logger;
        private readonly string _configurationKey;

        public NCacheConnectionManager(
            string configurationKey,
            ILoggerFactory loggerFactory)
        {
            NotNullOrWhiteSpace(
                configurationKey,
                nameof(configurationKey));

            NotNull(
                loggerFactory,
                nameof(loggerFactory));

            _configurationKey = configurationKey;
            _logger = loggerFactory.CreateLogger(this);
        }

        public ICache GetConnection(
            string configurationKey)
        {
            ICache cache;

            try
            {

                if (!_connections.TryGetValue(
                        configurationKey,
                        out cache))
                {
                    lock (_connectionLock)
                    {
                        if (!_connections.TryGetValue(
                                configurationKey,
                                out cache))
                        {
                            if (_logger.IsEnabled(Core.Logging.LogLevel.Information))
                            {
                                _logger.LogInfo("Trying to connect with the following configuration key: '{0}'", _configurationKey);

                                var ncacheConfiguration =
                                    NCacheConfigurationManager.GetConfiguration(_configurationKey)
                                    ?? throw new InvalidOperationException(
                                                $"No configuration found with key {_configurationKey}"); ;

                                var connectionOptions =
                                    ncacheConfiguration.CacheConnectionOptions;

                                var cacheID =
                                    ncacheConfiguration.CacheId;

                                cache =
                                    Alachisoft.NCache.Client.CacheManager.GetCache(
                                        cacheID,
                                        connectionOptions);

                                _connections.Add(_configurationKey, cache);
                            }
                        }
                    }
                }
            }
            catch (OperationFailedException e)
            {
                if (e.ErrorCode == NCacheErrorCodes.NO_SERVER_AVAILABLE ||
                    e.ErrorCode == NCacheErrorCodes.CACHE_ID_NOT_REGISTERED ||
                    e.ErrorCode == NCacheErrorCodes.CACHE_NOT_REGISTERED_ON_NODE)
                {
                    _logger.LogCritical(
                        $"Cache can not be connected to. Please check connectivity settings and/or IP addresses of your servers");
                }

                throw;
            }
            catch (Exception)
            {
                throw;
            }

            return cache;
        }

        public ICache Connect()
        {
            ICache cache;

            try
            {

                if (!_connections.TryGetValue(
                        _configurationKey,
                        out cache))
                {
                    lock (_connectionLock)
                    {
                        if (!_connections.TryGetValue(
                                _configurationKey,
                                out cache))
                        {
                            if (_logger.IsEnabled(Core.Logging.LogLevel.Information))
                            {
                                _logger.LogInfo("Trying to connect with the following configuration key: '{0}'", _configurationKey);

                                var ncacheConfiguration =
                                    NCacheConfigurationManager.GetConfiguration(_configurationKey)
                                    ?? throw new InvalidOperationException(
                                                $"No configuration found with key {_configurationKey}"); ;

                                var connectionOptions =
                                    ncacheConfiguration.CacheConnectionOptions;

                                var cacheID =
                                    ncacheConfiguration.CacheId;

                                cache =
                                    Alachisoft.NCache.Client.CacheManager.GetCache(
                                        cacheID,
                                        connectionOptions);

                                _connections.Add(_configurationKey, cache);
                            }
                        }
                    }
                }
            }
            catch (OperationFailedException e)
            {
                if (e.ErrorCode == NCacheErrorCodes.NO_SERVER_AVAILABLE ||
                    e.ErrorCode == NCacheErrorCodes.CACHE_ID_NOT_REGISTERED ||
                    e.ErrorCode == NCacheErrorCodes.CACHE_NOT_REGISTERED_ON_NODE)
                {
                    _logger.LogCritical(
                        $"Cache can not be connected to. Please check connectivity settings and/or IP addresses of your servers");
                }

                throw;
            }
            catch (Exception)
            {
                throw;
            }

            return cache;
        }

        public static void RemoveConnection(
            string configurationKey)
        {
            lock(_connectionLock)
            {
                if (_connections.ContainsKey(configurationKey))
                {
                    _connections.Remove(configurationKey);
                }
            }
        }

        public static void AddConnection(
            string configurationKey, 
            ICache cache)
        {
            lock(_connectionLock)
            {
                if (!_connections.ContainsKey(configurationKey))
                {
                    _connections.Add(
                                configurationKey,
                                cache); 
                }
            }
        }


        public ITopic GetChannel(
            string channelName)
        {
            try
            {
                return GetChannel($"{channelName}-{_configurationKey}", false);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private ITopic GetChannel(
            string channelName,
            bool exceptionRaised)
        {
            try
            {
                NotNull(channelName, nameof(channelName));

                if (exceptionRaised)
                {
                    return GetTopic(channelName);
                }
                else
                {
                    if (!_topics.ContainsKey(channelName))
                    {
                        lock (_topicLock)
                        {
                            if (!_topics.ContainsKey(channelName))
                            {
                                return GetTopic(channelName);
                            }
                        }
                    }

                    return _topics[channelName];
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private ITopic GetTopic(
                string channelName)
        {
            try
            {
                ITopic topic = Connect().MessagingService.GetTopic(
                        channelName,
                        TopicSearchOptions.ByName);

                if (topic == null || topic.IsClosed)
                {
                    topic = AddTopic(channelName);
                }

                _topics.Add(channelName, topic);
                return topic;
            }

            catch (Exception)
            {

                throw;
            }
        }

        private ITopic AddTopic(
            string channelName)
        {

            try
            {
                var topic = Connect().MessagingService.CreateTopic(channelName);
                topic.OnTopicDeleted =
                (o, args) =>
                {
                    if (_logger.IsEnabled(Core.Logging.LogLevel.Information))
                    {
                        _logger
                               .LogInfo($"{args.TopicName} has been deleted on cache"); 
                    }
                    _topics.Remove(channelName);
                };

                topic.MessageDeliveryFailure +=
                (o, args) =>
                {
                    var topicName =
                        args.TopicName;
                    var messageId =
                        args.Message.MessageId;
                    var reason =
                        args.MessgeFailureReason;

                    var reasonString =
                        (reason == MessgeFailureReason.Evicted) ?
                            "Eviction" :
                            "Expiration";

                    _logger
                        .LogError(
                        $"{messageId} on channel {topicName} " +
                        $"failed due to " +
                        $"{reasonString}");
                };

                if (_logger.IsEnabled(Core.Logging.LogLevel.Information))
                {
                    _logger
                         .LogInfo($"Topic {topic.Name} has been created"); 
                }


                return topic;
            }
            catch (OperationFailedException e)
            {
                if (e.ErrorCode == NCacheErrorCodes.DEFAULT_TOPICS)
                {
                    _logger.LogError(
                        $"Operation can not be performed on default topic");
                }

                throw;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
