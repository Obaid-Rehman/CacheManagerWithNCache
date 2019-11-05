using Alachisoft.NCache.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.NCache
{
    public class NCacheConfiguration
    {
        private readonly CacheConnectionOptions _cacheConnectionOptions;

        public NCacheConfiguration()
        {
            _cacheConnectionOptions =
                new CacheConnectionOptions();
        }

        public NCacheConfiguration(
            string cacheId,
            IList<NCacheEndPoint> servers,
            string appName = "",
            ClientCacheMode cacheMode = ClientCacheMode.Pessimistic,
            double clientRequestTimeoutInSeconds = 90,
            int commandRetries = 5,
            double commandRetryIntervalInSeconds = 1,
            int connectionRetries = 3,
            double retryConnectionDelayInSeconds = 5,
            double connectionRetryIntervalInSeconds = 0.1,
            double connectionTimeoutInSeconds = 60,
            bool enableClientLogs = false,
            ClientLogLevel logLevel = ClientLogLevel.Info,
            bool enableKeepAlive = false,
            double keepAliveIntervalInSeconds = 30,
            bool enableKeynotifications = false
            )
        {
            NotNullOrWhiteSpace(
                cacheId,
                nameof(cacheId));

            NotNull(
                servers,
                nameof(servers));

            if (servers.Count == 0)
            {
                throw new InvalidOperationException
                    ("List of IP v4 addresses must not be empty");
            }

            CacheId = cacheId;

            EnableKeyNotifications = enableKeynotifications;

            _cacheConnectionOptions = new CacheConnectionOptions
            {
                ServerList =
                    NCacheServers(servers),
                ClientCacheMode =
                    CacheMode(cacheMode),
                ClientRequestTimeOut =
                    TimeSpan.FromSeconds(clientRequestTimeoutInSeconds),
                CommandRetries =
                    commandRetries,
                CommandRetryInterval =
                    TimeSpan.FromSeconds(commandRetryIntervalInSeconds),
                ConnectionRetries =
                    connectionRetries,
                RetryConnectionDelay
                    = TimeSpan.FromSeconds(retryConnectionDelayInSeconds),
                RetryInterval =
                    TimeSpan.FromSeconds(connectionRetryIntervalInSeconds),
                ConnectionTimeout =
                    TimeSpan.FromSeconds(connectionTimeoutInSeconds),
                EnableClientLogs =
                    enableClientLogs,
                LogLevel =
                    LogLevel(logLevel),
                EnableKeepAlive =
                    enableKeepAlive,
                KeepAliveInterval =
                    TimeSpan.FromSeconds(keepAliveIntervalInSeconds),
                EnablePipelining =
                    false,
                LoadBalance =
                    true,
                PipeliningTimeout =
                    10,
                DefaultReadThruProvider =
                    "",
                DefaultWriteThruProvider =
                    "",
                ClientBindIP =
                    "",
                UserCredentials =
                    null,
                AppName =
                    GetAppName(appName),
                Mode = 
                    IsolationLevel.OutProc
            };
        }

        public string CacheId { get; set; }

        public CacheConnectionOptions CacheConnectionOptions => _cacheConnectionOptions;

        public bool EnableKeyNotifications { get; set; }

        public IList<NCacheEndPoint> Servers
        {
            get
            {
                return ServerEndpoints(_cacheConnectionOptions.ServerList);
            }
            set
            {
                _cacheConnectionOptions.ServerList =
                    NCacheServers(value);
            }
        }

        public string AppName
        {
            get
            {
                return _cacheConnectionOptions.AppName;
            }
            set
            {
                _cacheConnectionOptions.AppName = value;
            }
        }

        public ClientCacheMode ClientCacheMode
        {
            get
            {
                return CacheMode(_cacheConnectionOptions.ClientCacheMode.Value);
            }
            set
            {
                _cacheConnectionOptions.ClientCacheMode =
                    CacheMode(value);
            }
        }

        public double ClientRequestTimeoutInSeconds
        {
            get
            {
                return _cacheConnectionOptions.ClientRequestTimeOut.Value.TotalSeconds;
            }
            set
            {
                _cacheConnectionOptions.ClientRequestTimeOut = TimeSpan.FromSeconds(value);
            }
        }

        public int CommandRetries
        {
            get
            {
                return _cacheConnectionOptions.CommandRetries.Value;
            }
            set
            {
                _cacheConnectionOptions.CommandRetries = value;
            }
        }

        public double CommandRetryIntervalInSeconds
        {
            get
            {
                return _cacheConnectionOptions.CommandRetryInterval.Value.TotalSeconds;
            }
            set
            {
                _cacheConnectionOptions.CommandRetryInterval = TimeSpan.FromSeconds(value);
            }
        }

        public int ConnectionRetries
        {
            get
            {
                return _cacheConnectionOptions.ConnectionRetries.Value;
            }
            set
            {
                _cacheConnectionOptions.ConnectionRetries = value;
            }
        }

        public double RetryConnectionDelayInSeconds
        {
            get
            {
                return _cacheConnectionOptions.RetryConnectionDelay.Value.TotalSeconds;
            }
            set
            {
                _cacheConnectionOptions.RetryConnectionDelay =
                    TimeSpan.FromSeconds(value);
            }
        }

        public double ConnectionRetryIntervalInSeconds
        {
            get
            {
                return _cacheConnectionOptions.RetryInterval.Value.TotalSeconds;
            }
            set
            {
                _cacheConnectionOptions.RetryInterval =
                     TimeSpan.FromSeconds(value);
            }
        }

        public double ConnectionTimeoutInSeconds
        {
            get
            {
                return _cacheConnectionOptions.ConnectionTimeout.Value.TotalSeconds;
            }
            set
            {
                _cacheConnectionOptions.ConnectionTimeout =
                    TimeSpan.FromSeconds(value);
            }
        }

        public bool EnableClientLogs
        {
            get
            {
                return _cacheConnectionOptions.EnableClientLogs.Value;
            }
            set
            {
                _cacheConnectionOptions.EnableClientLogs = value;
            }
        }

        public ClientLogLevel ClientLogLevel
        {
            get
            {
                return LogLevel(_cacheConnectionOptions.LogLevel.Value);
            }
            set
            {
                _cacheConnectionOptions.LogLevel = LogLevel(value);
            }
        }
        
        public bool EnableKeepAlive
        {
            get
            {
                return _cacheConnectionOptions.EnableKeepAlive.Value;
            }
            set
            {
                _cacheConnectionOptions.EnableKeepAlive = value;
            }
        }

        public double KeepAliveIntervalInSeconds
        {
            get
            {
                if (!_cacheConnectionOptions.EnableKeepAlive.Value)
                    return -1;

                return _cacheConnectionOptions.KeepAliveInterval.Value.TotalSeconds;
            }
            set
            {
                _cacheConnectionOptions.KeepAliveInterval =
                    TimeSpan.FromSeconds(value);
            }
        }


        private IList<ServerInfo> NCacheServers(
            IList<NCacheEndPoint> endpoints)
        {
            List<ServerInfo> servers = new List<ServerInfo>();

            ServerInfo serverInfo = null;

            foreach (var endpoint in endpoints)
            {
                serverInfo = new ServerInfo(
                    endpoint.IpAddress, endpoint.Port);

                servers.Add(serverInfo);
            }

            return servers;
        }

        private IList<NCacheEndPoint> ServerEndpoints(
            IList<ServerInfo> endpoints)
        {
            List<NCacheEndPoint> servers = new List<NCacheEndPoint>();

            NCacheEndPoint ncacheEndPoint = null;

            foreach (var endpoint in endpoints)
            {
                ncacheEndPoint = new NCacheEndPoint(
                    endpoint.Name, endpoint.Port);

                servers.Add(ncacheEndPoint);
            }

            return servers;
        }

        private LogLevel LogLevel(
            ClientLogLevel level)
        {
            LogLevel l1 = Alachisoft.NCache.Client.LogLevel.Debug;

            switch (level)
            {
                case ClientLogLevel.Debug:
                    l1 = Alachisoft.NCache.Client.LogLevel.Debug;
                    break;
                case ClientLogLevel.Error:
                    l1 = Alachisoft.NCache.Client.LogLevel.Error;
                    break;
                default:
                    l1 = Alachisoft.NCache.Client.LogLevel.Info;
                    break;
            }

            return l1;
        }
    
        private ClientLogLevel LogLevel(
            LogLevel level)
        {
            ClientLogLevel l1 = ClientLogLevel.Debug;

            switch (level)
            {
                case Alachisoft.NCache.Client.LogLevel.Debug:
                    l1 = ClientLogLevel.Debug;
                    break;
                case Alachisoft.NCache.Client.LogLevel.Error:
                    l1 = ClientLogLevel.Error;
                    break;
                default:
                    l1 = ClientLogLevel.Info;
                    break;
            }

            return l1;
        }

        private ClientCacheMode CacheMode(
            ClientCacheSyncMode mode)
        {
            return (mode == ClientCacheSyncMode.Pessimistic) ? ClientCacheMode.Pessimistic : ClientCacheMode.Optimistic;
        }

        private ClientCacheSyncMode CacheMode(
            ClientCacheMode mode)
        {
            return (mode == ClientCacheMode.Pessimistic) ? ClientCacheSyncMode.Pessimistic : ClientCacheSyncMode.Optimistic;
        }
    
        private string GetAppName(
            string appName)
        {
            return string.IsNullOrWhiteSpace(appName) ?
                        Process.GetCurrentProcess().Id.ToString() :
                        appName;
        }
    }

    public enum ClientCacheMode
    {
        Pessimistic = 0,
        Optimistic = 1
    }

    public enum ClientLogLevel
    {
        Info = 0,
        Error = 1,
        Debug = 2
    }
    
    public sealed class NCacheEndPoint
    {

        public NCacheEndPoint()
        {
        }

        public NCacheEndPoint(string ipV4String, int port = 9800)
        {
            Port = port;
            IpAddress = ipV4String;
        }


        public int Port { get; set; }

        public string IpAddress { get; set; }
    }


}
