using Alachisoft.NCache.Client;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Exceptions;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CacheManager.Core.Utility.Guard;
using static CacheManager.NCache.Utilities;

namespace CacheManager.NCache
{
    public class NCacheHandle<TCacheValue> :
        BaseCacheHandle<TCacheValue>
    {
        private const string Base64Prefix = "base64\0";

        private readonly NCachePersistantConnection _ncacheConnection;
        private readonly ICacheManagerConfiguration _managerConfiguration;
        private readonly NCacheConfiguration _ncacheConfiguration = null;
        private readonly CustomDateTimeConverter _datetimeConverter = null;

        public NCacheHandle(

            ICacheManagerConfiguration managerConfiguration,
            CacheHandleConfiguration configuration,
            ILoggerFactory loggerFactory) : this(managerConfiguration, configuration, loggerFactory, null)
        { }

        public NCacheHandle(
            ICacheManagerConfiguration managerConfiguration,
            CacheHandleConfiguration configuration,
            ILoggerFactory loggerFactory,
            CustomDateTimeConverter converter
            )
            : base(managerConfiguration, configuration)
        {
            try
            {
                NotNull(loggerFactory, nameof(loggerFactory));
                NotNull(managerConfiguration, nameof(managerConfiguration));
                NotNull(configuration, nameof(configuration));

                _managerConfiguration = managerConfiguration;
                Logger
                    = loggerFactory.CreateLogger(this);

                var ncacheConfiguration
                    = NCacheConfigurationManager.GetConfiguration(
                        configuration.Key) ?? throw new InvalidOperationException(
                            $"No configuration found with key {configuration.Key}");
                _ncacheConfiguration = ncacheConfiguration;

                _ncacheConnection =
                    new NCachePersistantConnection(
                        _ncacheConfiguration,
                        loggerFactory);

                _datetimeConverter = converter ?? new CustomDateTimeConverter();

            }
            catch (Exception)
            {

                throw;
            }
        }


        protected override ILogger Logger { get; }

        public override int Count
        {
            get
            {
                return Retry(() => (int)_ncacheConnection.Cache.Count);
            }
        }

        public override bool IsDistributedCache
        {
            get
            {
                return true;
            }
        }

        public override void Clear()
        {
            Retry(() => _ncacheConnection.Cache.Clear());
        }

        public override void ClearRegion(
            string region)
        {
            NotNullOrWhiteSpace(
                region,
                nameof(region));

            Retry(() => _ncacheConnection.Cache.SearchService.RemoveByTag(new Tag(region)));
        }

        public override bool Exists(
            string key)
        {
            NotNullOrWhiteSpace(
                key,
                nameof(key));

            return Retry(() =>
            {
                var fullKey = GetKey(key);
                return _ncacheConnection.Cache.Contains(fullKey);
            });
        }

        public override bool Exists(
            string key,
            string region)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));

            return Retry(() =>
            {
                var fullKey = GetKey(key, region);
                return _ncacheConnection.Cache.Contains(fullKey);
            });
        }

        public override UpdateItemResult<TCacheValue> Update(
            string key,
            Func<TCacheValue, TCacheValue> updateValue,
            int maxRetries)
        {
            return Update(key, null, updateValue, maxRetries);
        }


        public override UpdateItemResult<TCacheValue> Update(
            string key,
            string region,
            Func<TCacheValue, TCacheValue> updateValue,
            int maxRetries)
        {
            return Retry(() =>
            {
                var fullKey = GetKey(key, region);

                var tries = 1;

                CacheItem<TCacheValue> item;
                CacheItem cachedItem;

                do
                {
                    tries++;

                    CacheItemVersion version = null;
                    cachedItem = _ncacheConnection.Cache.GetCacheItem(
                    fullKey,
                    ref version);

                    if (cachedItem == null)
                    {
                        return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                    }

                    item = GetItem(cachedItem);

                    var newValue = updateValue(item.Value);

                    if (newValue == null)
                    {
                        return UpdateItemResult.ForFactoryReturnedNull<TCacheValue>();
                    }

                    item = item.WithValue(newValue);

                    cachedItem = GetCachedItem(item);

                    if (cachedItem == null)
                    {
                        return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                    }


                    cachedItem.Version = version;

                    try
                    {
                        _ncacheConnection.Cache.Insert(
                                        fullKey,
                                        cachedItem);

                        if (Logger.IsEnabled(Core.Logging.LogLevel.Debug))
                        {
                            Logger.LogDebug("Update of {0} {1} failed with version conflict, retrying {2}/{3}", key, region, tries, maxRetries);
                        }

                        return UpdateItemResult
                            .ForSuccess<TCacheValue>(item, tries > 1, tries);
                    }
                    catch (OperationFailedException ex)
                    {
                        if (ex.ErrorCode == NCacheErrorCodes.ITEM_WITH_VERSION_DOESNT_EXIST)
                        {
                            continue;
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

                } while (tries <= maxRetries);

                return UpdateItemResult.ForTooManyRetries<TCacheValue>(tries);
            });
        }


        protected override bool AddInternalPrepared(
            CacheItem<TCacheValue> item)
        {
            return Retry(() =>
            {
                try
                {
                    NotNull(item, nameof(item));

                    var key = item.Key;
                    var region = item.Region;

                    var fullKey = GetKey(key, region);

                    var cachedItem = GetCachedItem(item);

                    if (cachedItem == null)
                    {
                        if (Logger.IsEnabled(Core.Logging.LogLevel.Debug))
                        {
                            Logger.LogDebug("Failed to add item either because it was expired or the value/value type is null");
                        }

                        return false;
                    }


                    _ncacheConnection.Cache.Add(fullKey, cachedItem);

                    return true;

                }
                catch (OperationFailedException ex)
                {
                    if (ex.ErrorCode == NCacheErrorCodes.KEY_ALREADY_EXISTS)
                    {
                        if (Logger.IsEnabled(Core.Logging.LogLevel.Debug))
                        {
                            Logger.LogDebug($"Failed to add {item.ToString()} as it already exists in cache");
                        }
                        return false;
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
            });

        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(
            string key)
        {
            NotNullOrWhiteSpace(
                key,
                nameof(key));

            return GetCacheItemInternal(key, null);
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(
            string key,
            string region)
        {
            return Retry(() =>
            {
                var fullKey = GetKey(key, region);

                var cachedItem =
                    _ncacheConnection.Cache.GetCacheItem(fullKey);


                if (cachedItem == null)
                {
                    return null;
                }

                var item = GetItem(cachedItem);

                if (item != null)
                {
                    _ncacheConnection.Cache.Insert(fullKey, cachedItem);
                }

                return item;
            });
        }


        protected override void PutInternalPrepared(
            CacheItem<TCacheValue> item)
        {

            NotNull(item, nameof(item));

            Retry(() =>
            {
                var key = item.Key;
                var region = item.Region;

                var fullKey = GetKey(key, region);

                var cachedItem = GetCachedItem(item);

                if (cachedItem != null)
                {
                    _ncacheConnection.Cache.Insert(fullKey, cachedItem);
                }
            });
        }

        protected override bool RemoveInternal(
            string key)
        {
            NotNullOrWhiteSpace(
                key,
                nameof(key));

            return RemoveInternal(key, null);
        }

        protected override bool RemoveInternal(
            string key,
            string region)
        {
            return Retry(() =>
            {
                var fullKey = GetKey(key, region);
                _ncacheConnection.Cache.Remove(fullKey);

                return true;
            });

        }


        private string GetKey(
            string key,
            string region = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (_ncacheConfiguration.EnableKeyNotifications && key.Contains(":"))
                {
                    key = Base64Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
                }

                var fullKey = key;

                if (!string.IsNullOrWhiteSpace(region))
                {
                    if (_ncacheConfiguration.EnableKeyNotifications && region.Contains(":"))
                    {
                        region = Base64Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(region));
                    }

                    fullKey = string.Concat(region, ":", key);
                }

                return fullKey;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private static Tuple<string, string> ParseKey(
            string value)
        {
            if (value == null)
            {
                return Tuple.Create<string, string>(null, null);
            }

            var sepIndex = value.IndexOf(':');
            var hasRegion = sepIndex > 0;
            var key = value;
            string region = null;

            if (hasRegion)
            {
                region = value.Substring(0, sepIndex);
                key = value.Substring(sepIndex + 1);

                if (region.StartsWith(Base64Prefix))
                {
                    region = region.Substring(Base64Prefix.Length);
                    region = Encoding.UTF8.GetString(Convert.FromBase64String(region));
                }
            }

            if (key.StartsWith(Base64Prefix))
            {
                key = key.Substring(Base64Prefix.Length);
                key = Encoding.UTF8.GetString(Convert.FromBase64String(key));
            }

            return Tuple.Create(key, region);
        }

        private void OnCacheDataModification(
            string key,
            CacheEventArg args)
        {
            if (args.EventType == EventType.ItemRemoved)
            {
                RemoveNotificationHandling(key, args);
            }
        }

        private void RemoveNotificationHandling(
            string key,
            CacheEventArg args)
        {
            var tupple = ParseKey(key);
            var key1 = tupple.Item1;
            var region1 = tupple.Item2;

            EventCacheItem eventItem = args.Item;
            var removedItem = eventItem.GetValue<object>();


            string removeMessage = "Got {0} event event for key '{1}:{2}'";
            string @event = "";

            Core.Internal.CacheItemRemovedReason reason;

            if (
                args.CacheItemRemovedReason == Alachisoft.NCache.Client.CacheItemRemovedReason.Expired)
            {
                reason = Core.Internal.CacheItemRemovedReason.Expired;
                @event = "expired";
            }
            else if (
                args.CacheItemRemovedReason ==
                Alachisoft.NCache.Client.CacheItemRemovedReason.DependencyChanged)
            {
                reason = Core.Internal.CacheItemRemovedReason.Evicted;
                @event = "evicted";
            }
            else if (
                args.CacheItemRemovedReason ==
                Alachisoft.NCache.Client.CacheItemRemovedReason.Underused)
            {
                reason = Core.Internal.CacheItemRemovedReason.Evicted;
                @event = "evicted";
            }
            else
            {
                reason = Core.Internal.CacheItemRemovedReason.ExternalDelete;
                @event = "deleted";
            }

            if (Logger.IsEnabled(Core.Logging.LogLevel.Debug))
            {
                Logger.LogDebug(
                    removeMessage,
                    @event,
                    region1,
                    key1);
            }


            TriggerCacheSpecificRemove(
                        key1,
                        region1,
                        reason,
                        removedItem);


        }

        private CacheItem GetCachedItem(
            CacheItem<TCacheValue> item)
        {

            if (item.IsExpired)
            {
                TriggerCacheSpecificRemove(
                    item.Key,
                    item.Region,
                    Core.Internal.CacheItemRemovedReason.Expired,
                    item.Value);

                return null;
            }

            var cachedDict =
                new Dictionary<string, string>();

            var value =
                item.Value;
            var valueType =
                item.ValueType;

            if (value == null && valueType == null)
            {
                return null;
            }
            else
            {
                cachedDict.Add(
                    "valueTypeName",
                    valueType.AssemblyQualifiedName);

                var jsonString =
                    JsonConvert.SerializeObject(
                        value,
                        _datetimeConverter
                        );

                cachedDict["value"] =
                    jsonString;
            }

            var expirationMode =
                item.ExpirationMode;
            var expirationTimeout =
                item.ExpirationTimeout;

            cachedDict["expirationMode"] =
                ((int)expirationMode).ToString();
            cachedDict["expirationTimout"] =
                expirationTimeout.Ticks.ToString();
            cachedDict["usesExpirationDefaults"] =
                item.UsesExpirationDefaults.ToString();


            if (!string.IsNullOrWhiteSpace(item.Region))
            {
                cachedDict["region"] =
                    item.Region;
            }

            cachedDict["key"] =
                item.Key;

            cachedDict["createdTimeUtcTicks"] =
                item.CreatedUtc.Ticks.ToString();


            var cachedItem =
                new CacheItem(cachedDict);

            ExpirationType expirationType =
                GetExpirationType(expirationMode);

            cachedItem.Expiration =
                new Expiration(expirationType, expirationTimeout);

            if (_ncacheConfiguration.EnableKeyNotifications)
            {
                cachedItem.SetCacheDataNotification(
                    OnCacheDataModification,
                    EventType.ItemRemoved,
                    EventDataFilter.DataWithMetadata);
            }

            return cachedItem;
        }


        private CacheItem<TCacheValue> GetItem(
            CacheItem cachedItem)
        {
            var cachedDict =
                    cachedItem.GetValue<Dictionary<string, string>>();

            var key =
                cachedDict["key"];

            var valueTypeName =
                cachedDict["valueTypeName"];


            var value =
                JsonConvert.DeserializeObject(
                    cachedDict["value"],
                    Type.GetType(valueTypeName),
                    _datetimeConverter);

            var region = cachedDict.ContainsKey("region") ? cachedDict["region"] : null;


            var usesExpirationDefaults =
                    bool.Parse(cachedDict["usesExpirationDefaults"]);

            var expirationMode1 = ExpirationMode.None;
            var expirationTimeout1 = default(TimeSpan);

            int expMode;
            long expTimeout;

            bool flag1 = int.TryParse(cachedDict["expirationMode"], out expMode);
            bool flag2 = long.TryParse(cachedDict["expirationTimout"], out expTimeout);

            if (flag1 && flag2)
            {
                expirationMode1 = (ExpirationMode)expMode;
                expirationTimeout1 = TimeSpan.FromMilliseconds(expTimeout);
            }
            else
            {
                Logger
                    .LogWarn(
                        "Invalid values are set for the expiration mode and/or timeout '{0} '{1}'",
                        cachedDict["expirationMode"],
                        cachedDict["expirationTimeout"]);
            }

            var item =
                usesExpirationDefaults ?
                    region == null ?
                        new CacheItem<TCacheValue>(
                            key,
                            (TCacheValue)value) :
                        new CacheItem<TCacheValue>(
                            key,
                            region,
                            (TCacheValue)value) :
                    region == null ?
                        new CacheItem<TCacheValue>(
                            key,
                            (TCacheValue)value,
                            expirationMode1,
                            expirationTimeout1) :
                        new CacheItem<TCacheValue>(
                            key,
                            region,
                            (TCacheValue)value,
                            expirationMode1,
                            expirationTimeout1);

            var TimeCreateValue = cachedDict["createdTimeUtcTicks"];
            item = item.WithCreated(new DateTime(long.Parse(TimeCreateValue), DateTimeKind.Utc));


            cachedItem.SetValue(cachedDict);

            if (item.IsExpired)
            {
                TriggerCacheSpecificRemove(
                    key,
                    region,
                    Core.Internal.CacheItemRemovedReason.Expired,
                    item.Value);

                return null;
            }

            return item;
        }


        private static ExpirationType GetExpirationType(
            ExpirationMode expirationMode)
        {
            try
            {
                ExpirationType ex1;

                switch (expirationMode)
                {
                    case ExpirationMode.Absolute:
                        ex1 = ExpirationType.Absolute;
                        break;
                    case ExpirationMode.Sliding:
                        ex1 = ExpirationType.Sliding;
                        break;
                    case ExpirationMode.None:
                        ex1 = ExpirationType.None;
                        break;
                    default:
                        ex1 = ExpirationType.None;
                        break;
                }

                return ex1;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private T Retry<T>(
            Func<T> retryme) =>
            Utilities.Retry(
                retryme,
                _managerConfiguration.RetryTimeout,
                _managerConfiguration.MaxRetries,
                Logger);

        private void Retry(
            Action retryme)
            => Retry(
                () =>
                {
                    retryme();
                    return true;
                });

    }

}
