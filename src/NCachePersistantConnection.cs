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
using static CacheManager.NCache.Utilities;

namespace CacheManager.NCache
{
    internal sealed class NCachePersistantConnection
    {
        private static readonly object padlock = new object();

        private static readonly object padlock2 = new object();

        private static readonly object padlock3 = new object();

        private volatile ICache _cache = null;

        private static volatile Dictionary<string, ITopic> _topics =
            new Dictionary<string, ITopic>();

        private static volatile Dictionary<string, IDurableTopicSubscription> _subscriptions =
            new Dictionary<string, IDurableTopicSubscription>();

        private readonly NCacheConfiguration _ncacheConfiguration;

        public NCachePersistantConnection(
            NCacheConfiguration ncacheConfiguration,
            ILoggerFactory loggerFactory
            )
        {
            NotNull(
                ncacheConfiguration,
                nameof(ncacheConfiguration));
            NotNull(
                loggerFactory,
                nameof(loggerFactory));

            _ncacheConfiguration =
                ncacheConfiguration;
            Logger = loggerFactory.CreateLogger(this);
        }

        public ILogger Logger { get; }

        public void Disconnect()
        {
            lock (padlock)
            {
                if (_cache != null)
                {
                    lock (padlock)
                    {
                        if (_cache != null)
                        {
                            _cache = null;
                        }
                    }
                }
            }
        }

        public ICache Cache
        {
            get
            {
                if (_cache == null)
                {
                    lock (padlock)
                    {
                        if (_cache == null)
                        {
                            _cache =
                                Alachisoft.NCache.Client.CacheManager.GetCache(
                                    _ncacheConfiguration.CacheId,
                                    _ncacheConfiguration.CacheConnectionOptions);


                        }

                    }
                }

                return _cache;

            }
        }

        public ITopic GetChannel(
            string channelName)
        {
            NotNull(channelName, nameof(channelName));

            if (!_topics.ContainsKey(channelName))
            {
                lock (padlock2)
                {
                    if (!_topics.ContainsKey(channelName))
                    {
                        return GetTopic(channelName);
                    }
                }
            }

            return _topics[channelName];

        }

        private ITopic GetTopic(
            string channelName)
        {
            ITopic topic = Cache.MessagingService.GetTopic(
                    channelName,
                    TopicSearchOptions.ByName);

            if (topic == null || topic.IsClosed)
            {
                topic = AddTopic(channelName);
            }

            _topics.Add(channelName, topic);
            return topic;

        }

        private ITopic AddTopic(
            string channelName)
        {
            var topic = _cache.MessagingService.CreateTopic(channelName);
            topic.OnTopicDeleted =
            (o, args) =>
            {
                if (Logger.IsEnabled(Core.Logging.LogLevel.Information))
                {
                    Logger
                            .LogInfo($"{args.TopicName} has been deleted on cache                           {_ncacheConfiguration.CacheId}");
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

                Logger
                    .LogError(
                    $"{messageId} on channel {topicName} " +
                    $"failed due to " +
                    $"{reasonString}");
            };

            if (Logger.IsEnabled(Core.Logging.LogLevel.Information))
            {
                Logger
                        .LogInfo($"Topic {topic.Name} has been created");
            }


            return topic;

        }


        public IDurableTopicSubscription GetSubscription(
        string channelName,
        string subscriptionName,
        MessageReceivedCallback callback)
        {
            var key = $"{subscriptionName}-subscription on-{channelName}";
            if (!_subscriptions.ContainsKey(key))
            {
                lock (padlock3)
                {
                    if (!_subscriptions.ContainsKey(key))
                    {
                        return AddSubscription(
                            channelName,
                            subscriptionName,
                            callback);
                    }
                }
            }

            return _subscriptions[key];

        }


        private IDurableTopicSubscription AddSubscription(
            string channelName,
            string subscriptionName,
            MessageReceivedCallback callback)
        {
            var key = $"{subscriptionName}-subscription on-{channelName}";

            try
            {
                var channel = GetChannel(channelName);

                var subscription = channel.CreateDurableSubscription(
                    subscriptionName,
                    SubscriptionPolicy.Shared,
                    callback);


                _subscriptions[key] = subscription;

                return subscription;

            }
            catch (OperationFailedException ex)
            {
                if (ex.ErrorCode == NCacheErrorCodes.SUBSCRIPTION_EXISTS)
                {
                    return _subscriptions[key];
                }
                else if (ex.ErrorCode == NCacheErrorCodes.TOPIC_DISPOSED)
                {
                    var channel = GetChannel(channelName);
                    return AddSubscription(channelName, subscriptionName, callback);
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }



}

