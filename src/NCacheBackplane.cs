using Alachisoft.NCache.Client;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Exceptions;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.NCache
{
    public sealed class NCacheBackplane : CacheBackplane
    {
        private readonly byte[] _identifier;
        private readonly string _channelName;
        private readonly ILogger _logger;
        private readonly NCachePersistantConnection _ncacheConnection;
        private readonly ICacheManagerConfiguration _managerConfiguration;
        public NCacheBackplane(
            ICacheManagerConfiguration managerConfiguration,
            ILoggerFactory loggerFactory)
                    : base(managerConfiguration)
        {
            try
            {
                NotNull(managerConfiguration, nameof(managerConfiguration));
                NotNull(loggerFactory, nameof(loggerFactory));

                _identifier =
                    Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
                _managerConfiguration =
                    managerConfiguration;
                _logger =
                    loggerFactory.CreateLogger(this);
                _channelName =
                    managerConfiguration.BackplaneChannelName ?? "CacheManagerBackplane";

                var ncacheConfiguration
                        = NCacheConfigurationManager.GetConfiguration(
                            ConfigurationKey) ?? throw new InvalidOperationException(
                                $"No configuration found with key {ConfigurationKey}");
                _ncacheConnection =
                        new NCachePersistantConnection(
                            ncacheConfiguration,
                            loggerFactory);

                Subscribe(
                    _managerConfiguration.MaxRetries,
                    _managerConfiguration.RetryTimeout,
                    0);
            }
            catch (Exception)
            {

            }
        }



        public override void NotifyChange(string key, CacheItemChangedEventAction action)
        {
            try
            {
                var message = BackplaneMessage.ForChanged(
                        _identifier,
                        key,
                        action);

                Publish(
                    message);

                Interlocked.Increment(ref MessagesSent);
                if (_logger.IsEnabled(Core.Logging.LogLevel.Information))
                {
                    _logger.LogInfo($"Notified message {message}");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public override void NotifyChange(string key, string region, CacheItemChangedEventAction action)
        {
            try
            {
                var message = BackplaneMessage.ForChanged(
                        _identifier,
                        key,
                        region,
                        action);
                Publish(
                    message);

                Interlocked.Increment(ref MessagesSent);
                Interlocked.Increment(ref SentChunks);

                if (_logger.IsEnabled(Core.Logging.LogLevel.Information))
                {
                    _logger.LogInfo($"Notified message {message}");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public override void NotifyClear()
        {
            try
            {
                var message = BackplaneMessage.ForClear(
                        _identifier);

                Publish(
                    message);

                Interlocked.Increment(ref MessagesSent);

                if (_logger.IsEnabled(Core.Logging.LogLevel.Information))
                {
                    _logger.LogInfo($"Notified message {message}");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public override void NotifyClearRegion(string region)
        {
            try
            {
                var message = BackplaneMessage.ForClearRegion(
                        _identifier,
                        region);

                Publish(
                    message);

                Interlocked.Increment(ref MessagesSent);

                if (_logger.IsEnabled(Core.Logging.LogLevel.Information))
                {
                    _logger.LogInfo($"Notified message {message}");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public override void NotifyRemove(string key)
        {
            try
            {
                var message = BackplaneMessage.ForRemoved(
                        _identifier,
                        key);

                Publish(
                    message);

                Interlocked.Increment(ref MessagesSent);

                if (_logger.IsEnabled(Core.Logging.LogLevel.Information))
                {
                    _logger.LogInfo($"Notified message {message}");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public override void NotifyRemove(string key, string region)
        {
            try
            {
                var message = BackplaneMessage.ForRemoved(
                        _identifier,
                        key,
                        region);


                    Publish(
                        message);

                Interlocked.Increment(ref MessagesSent);

                if (_logger.IsEnabled(Core.Logging.LogLevel.Information))
                {
                    _logger.LogInfo($"Notified message {message}");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void Publish(BackplaneMessage message)
        {
            try
            {
                Publish(message, false);
            }
            catch (OperationFailedException ex)
            {
                if (ex.ErrorCode == NCacheErrorCodes.TOPIC_DISPOSED)
                {
                    Publish(message, true);
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
       
        private void Publish(BackplaneMessage message, bool exceptionRaised)
        {
            try
            {
                var payload = BackplaneMessage.Serialize(message);
                var msg = new Message(payload);

                _ncacheConnection
                    .GetChannel(_channelName)
                    .Publish(
                    msg,
                    DeliveryOption.All,
                    true);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void Subscribe(int maxRetries, int retryTimeout, int tryAttempts)
        {
            try
            {
                Ensure(maxRetries > 0 && tryAttempts < maxRetries,
                    "Wrong configuration parameters");

                var subscriptionName =
                    $"subscription-" +
                            $"{Encoding.UTF8.GetString(_identifier, 0, _identifier.Length)}-" +
                            $"{Thread.CurrentThread.ManagedThreadId}";

                _ncacheConnection.GetSubscription(
                        _channelName,
                        subscriptionName,
                        (o, args) =>
                        {
                            var payload = args.Message.Payload;
                            ProcessMessage(payload);
                        });
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (OperationFailedException e)
            {
                if (e.ErrorCode == NCacheErrorCodes.NO_SERVER_AVAILABLE ||
                        e.ErrorCode == NCacheErrorCodes.CACHE_ID_NOT_REGISTERED ||
                        e.ErrorCode == NCacheErrorCodes.CACHE_NOT_REGISTERED_ON_NODE)
                {
                    throw;
                }

                tryAttempts++;

                if (tryAttempts == maxRetries)
                {
                    throw;
                }

                Task.Delay(retryTimeout);

                Subscribe(maxRetries, retryTimeout, tryAttempts);
            }
            catch(Exception)
            {
                throw;
            }
        }

        private void ProcessMessage(object payload)
        {
            try
            {
                var msg =
                    payload as byte[];

                var messages =
                    BackplaneMessage.Deserialize(msg, _identifier);

                var fullMessages =
                    messages.ToArray();

                Interlocked
                    .Add(ref MessagesReceived, fullMessages.Length);

                if (!messages.Any())
                {
                    return;
                }

                if (_logger.IsEnabled(Core.Logging.LogLevel.Information))
                {
                    _logger.LogInfo("Backplane got notified with {0} new messages.", fullMessages.Length);
                }

                foreach (var message in fullMessages)
                {
                    switch (message.Action)
                    {
                        case BackplaneAction.Clear:
                            TriggerCleared();
                            break;

                        case BackplaneAction.ClearRegion:
                            TriggerClearedRegion(message.Region);
                            break;

                        case BackplaneAction.Changed:
                            if (string.IsNullOrWhiteSpace(message.Region))
                            {
                                TriggerChanged(message.Key, message.ChangeAction);
                            }
                            else
                            {
                                TriggerChanged(message.Key, message.Region, message.ChangeAction);
                            }
                            break;

                        case BackplaneAction.Removed:
                            if (string.IsNullOrWhiteSpace(message.Region))
                            {
                                TriggerRemoved(message.Key);
                            }
                            else
                            {
                                TriggerRemoved(message.Key, message.Region);
                            }
                            break;
                    }
                }
            }
            catch (Exception)
            {
                _logger.LogWarn("Error reading messages");
                throw;
            }
        }
    }
}
