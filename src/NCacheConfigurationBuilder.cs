using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.NCache
{
    public class NCacheConfigurationBuilder
    {
        private readonly string _configurationKey;
        private IList<NCacheEndPoint> _servers = 
            new List<NCacheEndPoint>();
        private string _appName = 
            "";
        private ClientCacheMode _cacheMode = 
            ClientCacheMode.Optimistic;
        private double _clientRequestTimeoutInSeconds = 
            90;
        private int _commandRetries =
            5;
        private double _commandRetryIntervalInSeconds =
            1;
        private int _connectionRetries =
            3;
        private double _retryConnectionDelayInSeconds =
            5;
        private double _connectionRetryIntervalInSeconds =
            0.1;
        private double _connectionTimeoutInSeconds =
            60;
        private bool _enableClientLogs =
            false;
        private ClientLogLevel _logLevel =
            ClientLogLevel.Info;
        private bool _enableKeepAlive =
            false;
        private double _keepAliveIntervalInSeconds =
            30;
        private string _cacheId =
            "";
        private bool _enableKeyNotifications =
            false;

        public NCacheConfigurationBuilder(
            string configurationKey)
        {
            try
            {
                NotNullOrWhiteSpace(
                        configurationKey,
                        nameof(configurationKey));

                _configurationKey = configurationKey;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public NCacheConfiguration Build()
        {
            try
            {
                var ncacheConfiguration = new NCacheConfiguration(
                        cacheId: _cacheId,
                        servers: _servers,
                        appName: _appName,
                        cacheMode: _cacheMode,
                        clientRequestTimeoutInSeconds: _clientRequestTimeoutInSeconds,
                        commandRetries: _commandRetries,
                        commandRetryIntervalInSeconds: _commandRetryIntervalInSeconds,
                        connectionRetries: _connectionRetries,
                        retryConnectionDelayInSeconds: _retryConnectionDelayInSeconds,
                        connectionRetryIntervalInSeconds: _connectionRetryIntervalInSeconds,
                        connectionTimeoutInSeconds: _connectionTimeoutInSeconds,
                        enableClientLogs: _enableClientLogs,
                        logLevel: _logLevel,
                        enableKeepAlive: _enableKeepAlive,
                        keepAliveIntervalInSeconds: _keepAliveIntervalInSeconds,
                        enableKeynotifications:_enableKeyNotifications);

                NCacheConfigurationManager.AddConfiguration(
                    _configurationKey,
                    ncacheConfiguration);

                return ncacheConfiguration;
            }
            catch (Exception)
            {

                throw;
            }

        }
    
        public NCacheConfigurationBuilder WithAppName(
            string appName)
        {
            _appName = appName;
            return this;
        }

        public NCacheConfigurationBuilder WithCacheId(
            string cacheId)
        {
            _cacheId = cacheId;
            return this;
        }

        public NCacheConfigurationBuilder WithEndPoint(
            string host, 
            int port = 9800)
        {
            _servers.Add(new NCacheEndPoint(host, port));
            return this;
        }

        public NCacheConfigurationBuilder WithClientCacheMode(
            ClientCacheMode mode)
        {
            _cacheMode = mode;
            return this;
        }

        public NCacheConfigurationBuilder WithClientRequestTimeout(
            double clientRequestTimeout)
        {
            _clientRequestTimeoutInSeconds = clientRequestTimeout;
            return this;
        }

        public NCacheConfigurationBuilder WithCommandRetries(
            int commandRetries)
        {
            _commandRetries = commandRetries;
            return this;
        }

        public NCacheConfigurationBuilder WithCommandRetryInterval(
            double commandRetryInterval)
        {
            _commandRetryIntervalInSeconds = commandRetryInterval;
            return this;
        }

        public NCacheConfigurationBuilder WithConnectionRetries(
            int connectionRetries)
        {
            _connectionRetries = connectionRetries;
            return this;
        }

        public NCacheConfigurationBuilder WithConnectionRetryInterval(
            double connectionRetryInterval)
        {
            _connectionRetryIntervalInSeconds = connectionRetryInterval;
            return this;
        }

        public NCacheConfigurationBuilder WithConnectionRetryDelay(
            double connectionRetryDelay)
        {
            _retryConnectionDelayInSeconds = connectionRetryDelay;
            return this;
        }

        public NCacheConfigurationBuilder WithConnectionTimout(
            double connectionTimeout)
        {
            _connectionTimeoutInSeconds = connectionTimeout;
            return this;
        }

        public NCacheConfigurationBuilder WithClientLogsEnabled(
            bool enableClientLogs)
        {
            _enableClientLogs = enableClientLogs;
            return this;
        }

        public NCacheConfigurationBuilder WithClientLogLevel(
            ClientLogLevel logLevel)
        {
            _logLevel = logLevel;
            return this;
        }

        public NCacheConfigurationBuilder WithKeepAliveEnabled(
            bool enableKeepAlive)
        {
            _enableKeepAlive = enableKeepAlive;
            return this;
        }

        public NCacheConfigurationBuilder WithKeepAliveInterval(
            double keepAliveInterval)
        {
            _keepAliveIntervalInSeconds = keepAliveInterval;
            return this;
        }

        public NCacheConfigurationBuilder WithKeyNotificationsEnabled(
            bool enableKeyNotifications)
        {
            _enableKeyNotifications = enableKeyNotifications;
            return this;
        }

    }
}
