using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;


namespace CacheManager.NCache
{
    public sealed class Server : ConfigurationElement
    {
        [ConfigurationProperty("host", IsRequired = true, IsKey = true)]
        public string IpAddress
        {
            get
            {
                return (string)this["host"];
            }
            set
            {
                this["host"] = value;
            }
        }

        [ConfigurationProperty("port", DefaultValue = 9800, IsRequired = false)]
        public int Port
        {
            get
            {
                return (int)this["port"];
            }
            set
            {
                this["port"] = value;
            }
        }
    }

    public sealed class ServerCollection :
        ConfigurationElementCollection,
        IEnumerable<Server>
    {
        public ServerCollection()
        {
            AddElementName = "server";
        }
        protected override ConfigurationElement CreateNewElement()
        {
            return new Server();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Server)element).IpAddress;
        }

        public new IEnumerator<Server> GetEnumerator()
        {
            var enu = base.GetEnumerator();

            enu.Reset();

            while (enu.MoveNext())
            {
                yield return (Server)enu.Current;
            }
        }
    }

    public sealed class NCacheOptions : ConfigurationElement
    {
        [ConfigurationProperty("cacheid", IsRequired = true)]
        public string CacheID
        {
            get
            {
                return (string)this["cacheid"];
            }
            set
            {
                this["cacheid"] = value;
            }
        }

        [ConfigurationProperty("appname", IsRequired = false)]
        public string AppName
        {
            get
            {
                return (string)this["appname"];
            }
            set
            {
                this["appname"] = value;
            }
        }

        [ConfigurationProperty("clientcachemode", IsRequired = false, DefaultValue = ClientCacheMode.Optimistic)]
        public ClientCacheMode ClientMode
        {
            get
            {
                return (ClientCacheMode)this["clientcachemode"];
            }
            set
            {
                this["clientcachemode"] = value;
            }
        }

        [ConfigurationProperty("clientrequesttimeout", IsRequired = false, DefaultValue = 90.0)]
        public double ClientRequestTimeoutInSeconds
        {
            get
            {
                return (double)this["clientrequesttimeout"];
            }
            set
            {
                this["clientrequesttimeout"] = value;
            }
        }

        [ConfigurationProperty("commandretries", IsRequired = false, DefaultValue = 5)]
        public int CommandRetries
        {
            get
            {
                return (int)this["commandretries"];
            }
            set
            {
                this["commandretries"] = value;
            }
        }

        [ConfigurationProperty("commandretryinterval", IsRequired = false, DefaultValue = 1.0)]
        public double CommandRetryIntervalInSeconds
        {
            get
            {
                return (double)this["commandretryinterval"];
            }
            set
            {
                this["commandretryinterval"] = value;
            }
        }

        [ConfigurationProperty("connectionretries", IsRequired = false, DefaultValue = 3)]
        public int ConnectionRetries
        {
            get
            {
                return (int)this["connectionretries"];
            }
            set
            {
                this["connectionretries"] = value;
            }
        }

        [ConfigurationProperty("connectionretrydelay", IsRequired = false, DefaultValue = 5.0)]
        public double ConnectionRetryDelayInSeconds
        {
            get
            {
                return (double)this["connectionretrydelay"];
            }
            set
            {
                this["connectionretrydelay"] = value;
            }
        }

        [ConfigurationProperty("connectionretryinterval", IsRequired = false, DefaultValue = 5.0)]
        public double ConnectionRetryIntervalInSeconds
        {
            get
            {
                return (double)this["connectionretryinterval"];
            }
            set
            {
                this["connectionretryinterval"] = value;
            }
        }

        [ConfigurationProperty("connectiontimeout", IsRequired = false, DefaultValue = 60.0)]
        public double ConnectionTimeoutInSeconds
        {
            get
            {
                return (double)this["connectiontimeout"];
            }
            set
            {
                this["connectiontimeout"] = value;
            }
        }

        [ConfigurationProperty("enableclientlogs", IsRequired = false, DefaultValue = false)]
        public bool EnableClientLogs
        {
            get
            {
                return (bool)this["enableclientlogs"];
            }
            set
            {
                this["connectiontimeout"] = value;
            }
        }

        [ConfigurationProperty("loglevel", IsRequired = false, DefaultValue = ClientLogLevel.Error)]
        public ClientLogLevel LogLevel
        {
            get
            {
                return (ClientLogLevel)this["loglevel"];
            }
            set
            {
                this["loglevel"] = value;
            }
        }

        [ConfigurationProperty("enablekeepalive", IsRequired = false, DefaultValue = false)]
        public bool EnableKeepAlive
        {
            get
            {
                return (bool)this["enablekeepalive"];
            }
            set
            {
                this["enablekeepalive"] = value;
            }
        }

        [ConfigurationProperty("keepaliveinterval", IsRequired = false, DefaultValue = 30.0)]
        public double KeepAliveIntervalInSeconds
        {
            get
            {
                return (double)this["keepaliveinterval"];
            }
            set
            {
                this["keepaliveinterval"] = value;
            }
        }

        [ConfigurationProperty("enablekeynotifications", IsRequired = false, DefaultValue = false)]
        public bool EnableKeyNotifications
        {
            get
            {
                return (bool)this["enablekeynotifications"];
            }
            set
            {
                this["enablekeynotifications"] = value;
            }

        }

        [ConfigurationProperty("id", IsKey = true, IsRequired = true)]
        public string Key
        {
            get
            {
                return (string)this["id"];
            }
            private set
            {

                this["id"] = value;
            }
        }

        [ConfigurationProperty("servers", IsKey = true)]
        [ConfigurationCollection(typeof(ServerCollection), AddItemName = "server")]
        public ServerCollection Servers
        {
            get
            {
                return (ServerCollection)this["servers"];
            }
        }

    }

    public sealed class NCacheOptionsCollection :
        ConfigurationElementCollection,
        IEnumerable<NCacheOptions>
    {
        public NCacheOptionsCollection()
        {
            AddElementName = "cacheconfiguration";
        }
        protected override ConfigurationElement CreateNewElement()
        {
            return new NCacheOptions();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((NCacheOptions)element).Key;
        }

        public new IEnumerator<NCacheOptions> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (NCacheOptions)enu.Current;
            }
        }
    }


    public sealed class NCacheConfigurationSection : ConfigurationSection
    {
        public const string DEFAULT_SECTION_NAME
            = "cacheManager.NCache";

        public const string CONFIGURATIONS_NAME
            = "cacheconfigurations";

        [ConfigurationProperty(CONFIGURATIONS_NAME)]
        [ConfigurationCollection(typeof(NCacheOptionsCollection), AddItemName = "cacheconfiguration")]
        public NCacheOptionsCollection Configurations
            => (NCacheOptionsCollection)this[CONFIGURATIONS_NAME];

        [ConfigurationProperty("xmlns", IsRequired = false)]
        public string Xmlns { get; set; }
    }
}

