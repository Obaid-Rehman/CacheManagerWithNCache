using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if !NETSTANDARD2
using System.Configuration;
#endif
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using static CacheManager.Core.Utility.Guard;
using static CacheManager.NCache.Utilities;


namespace CacheManager.NCache
{
    public static class NCacheConfigurationManager
    {
        private static ConcurrentDictionary<string, NCacheConfiguration> _configurations =
            new ConcurrentDictionary<string, NCacheConfiguration>();

        public static bool AddConfiguration(
            string configurationKey,
            NCacheConfiguration configuration)
        {
            try
            {
                return _configurations.TryAdd(
                            configurationKey,
                            configuration);
            }
            catch (Exception e)
            {

                throw GetException(e);
            }
        }

#if NETSTANDARD2
        public static NCacheConfiguration GetConfiguration(
            string configurationKey)
        {
            NotNullOrWhiteSpace(
                        configurationKey,
                        nameof(configurationKey));

            if (_configurations.TryGetValue(configurationKey,
                                            out NCacheConfiguration configuration))
            {
                return configuration;
            }

            throw new InvalidOperationException(
                $"No configuration added for configuration name {configurationKey}" );
        }
#endif

#if !NETSTANDARD2
        public static NCacheConfiguration GetConfiguration(
            string configurationKey,
            string sectionName = null,
            string fileName = null)
        {
            try
            {
                NotNullOrWhiteSpace(
                        configurationKey,
                        nameof(configurationKey));

                if (_configurations.TryGetValue(configurationKey,
                                                out NCacheConfiguration configuration))
                {
                    return configuration;
                }

                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    sectionName = !string.IsNullOrWhiteSpace(sectionName) ?
                        sectionName :
                        NCacheConfigurationSection.DEFAULT_SECTION_NAME;

                    return LoadSingleConfiguration(
                        configurationKey,
                        sectionName,
                        fileName);
                }
                else
                {
                    sectionName = !string.IsNullOrWhiteSpace(sectionName) ?
                        sectionName :
                        NCacheConfigurationSection.DEFAULT_SECTION_NAME;

                    return LoadSingleConfiguration(
                        configurationKey,
                        sectionName);
                }


            }
            catch (Exception e)
            {

                throw GetException(e);
            }
        }


        public static NCacheConfiguration LoadSingleConfiguration(
            string configurationKey,
            string sectionName = null,
            string fileName = null)
        {
            NotNull(
                configurationKey,
                nameof(configurationKey));
           
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                if (!string.IsNullOrWhiteSpace(sectionName))
                {
                    return LoadSingleConfigurationInternal(
                        configurationKey,
                        sectionName,
                        fileName);
                }
                else
                {
                    return LoadSingleConfigurationInternal(
                        configurationKey,
                        NCacheConfigurationSection.DEFAULT_SECTION_NAME,
                        fileName);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(sectionName))
                {
                    return LoadSingleConfigurationInternal(
                        configurationKey,
                        sectionName);
                }
                else
                {
                    return LoadSingleConfigurationInternal(
                        configurationKey,
                        NCacheConfigurationSection.DEFAULT_SECTION_NAME);
                }
            }
        }

        

        public static void LoadConfiguration(
            string configFileName,
            string sectionName)
        {
            NotNullOrWhiteSpace(configFileName, nameof(configFileName));
            NotNullOrWhiteSpace(sectionName, nameof(sectionName));

            Ensure(File.Exists(configFileName), $"Configuration file not found [{configFileName}]");

            var fileConfig =
                new ExeConfigurationFileMap()
                {
                    ExeConfigFilename = configFileName
                };

            var cfg =
                ConfigurationManager.OpenMappedExeConfiguration(
                    fileConfig,
                    ConfigurationUserLevel.None);

            var section =
                cfg.GetSection(sectionName) as NCacheConfigurationSection;

            EnsureNotNull(
                section,
                $"No section with name {sectionName} found in file {configFileName}");

            LoadConfigurationSection(section);
        }

        public static void LoadConfiguration(
            string sectionName)
        {
            NotNullOrWhiteSpace(sectionName, nameof(sectionName));

            var section =
                ConfigurationManager.GetSection(sectionName) as NCacheConfigurationSection;

            EnsureNotNull(
                section,
                $"No section with name {sectionName} found in file");

            LoadConfigurationSection(section);

        }


        public static void LoadConfiguration()
        {
            LoadConfiguration(
                NCacheConfigurationSection.DEFAULT_SECTION_NAME);
        }

        public static void LoadConfigurationSection(NCacheConfigurationSection section)
        {
            try
            {
                NotNull(section, nameof(section));

                foreach (var ncacheOption in section.Configurations)
                {
                    GetNCacheConfiguration(
                        ncacheOption);
                }
            }
            catch (Exception)
            {

                throw;
            }
            
        }

        private static NCacheConfiguration LoadSingleConfigurationInternal(
            string configurationKey,
            string sectionName,
            string configFileName)
        {
            Ensure(File.Exists(configFileName), $"Configuration file not found [{configFileName}]");

            var fileConfig =
                new ExeConfigurationFileMap()
                {
                    ExeConfigFilename = configFileName
                };

            var cfg =
                ConfigurationManager.OpenMappedExeConfiguration(
                    fileConfig,
                    ConfigurationUserLevel.None);

            var section =
                cfg.GetSection(sectionName) as NCacheConfigurationSection;

            EnsureNotNull(
                section,
                $"No section with name {sectionName} found in file {configFileName}");

            return LoadConfigurationSectionInternal(
                configurationKey,
                section);
        }

        private static NCacheConfiguration LoadSingleConfigurationInternal(
            string configurationKey,
            string sectionName)
        {
            var section =
                ConfigurationManager.GetSection(sectionName) as NCacheConfigurationSection;

            EnsureNotNull(
                section,
                $"No section with name {sectionName} found in file");

            return LoadConfigurationSectionInternal(
                configurationKey,
                section);
        }


        private static NCacheConfiguration LoadConfigurationSectionInternal(
            string configurationKey,
            NCacheConfigurationSection section)
        {
            var configurationList = section.Configurations.ToList();

            Expression<Func<NCacheOptions, bool>> predicate = o => o.Key == configurationKey;

            var configuration = configurationList.FirstOrDefault(o => o.Key == configurationKey);

            if (configuration == null)
            {
                throw new InvalidOperationException(
                    $"No configuration with configuration key {configurationKey} exists in current file");
            }

            GetNCacheConfiguration(configuration);

            return _configurations[configurationKey];
        }

        private static void GetNCacheConfiguration(
            NCacheOptions configuration)
        {
            NotNull(
                        configuration,
                        nameof(configuration));

            var servers =
                        new List<NCacheEndPoint>();

            foreach (var server in configuration.Servers)
            {
                servers.Add(
                    new NCacheEndPoint(
                            server.IpAddress,
                            server.Port));
            }

            NCacheConfiguration ncacheConfiguration =
                    new NCacheConfiguration(
                        cacheId: configuration.CacheID,
                        servers: servers,
                        appName: configuration.AppName,
                        cacheMode: configuration.ClientMode,
                        clientRequestTimeoutInSeconds: configuration.ClientRequestTimeoutInSeconds,
                        commandRetries: configuration.CommandRetries,
                        commandRetryIntervalInSeconds: configuration.CommandRetryIntervalInSeconds,
                        connectionRetries: configuration.ConnectionRetries,
                        retryConnectionDelayInSeconds: configuration.ConnectionRetryDelayInSeconds,
                        connectionRetryIntervalInSeconds: configuration.ConnectionRetryIntervalInSeconds,
                        connectionTimeoutInSeconds: configuration.ConnectionTimeoutInSeconds,
                        enableClientLogs: configuration.EnableClientLogs,
                        logLevel: configuration.LogLevel,
                        enableKeepAlive: configuration.EnableKeepAlive,
                        keepAliveIntervalInSeconds: configuration.KeepAliveIntervalInSeconds,
                        enableKeynotifications: configuration.EnableKeyNotifications

                    );

            AddConfiguration
                    (configuration.Key,
                    ncacheConfiguration);
        }


#endif
    }
}
