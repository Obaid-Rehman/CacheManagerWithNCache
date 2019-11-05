using System;
using System.Collections.Generic;
using CacheManager.NCache;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    public static class NCacheConfigurationBuilderExtensions
    {
        public static ConfigurationBuilderCachePart WithNCacheConfiguration(
            this ConfigurationBuilderCachePart part,
            string configurationKey,
            Action<NCacheConfigurationBuilder> configuration)
        {
            NotNull(
                configuration, 
                nameof(configuration));

            NotNullOrWhiteSpace(
                configurationKey, 
                nameof(configurationKey));

            var builder = 
                new NCacheConfigurationBuilder(
                    configurationKey);

            configuration(
                builder);

            NCacheConfigurationManager
                .AddConfiguration(
                    configurationKey, 
                    builder.Build());

            return part;
        }

        public static ConfigurationBuilderCachePart WithNCacheConfiguration(
            this ConfigurationBuilderCachePart part,
            string configurationKey,
            NCacheConfiguration ncacheConfiguration)
        {
            NotNull(
                ncacheConfiguration, 
                nameof(ncacheConfiguration));

            NotNullOrWhiteSpace(
                configurationKey, 
                nameof(configurationKey));

            NCacheConfigurationManager
                .AddConfiguration(
                    configurationKey, 
                    ncacheConfiguration);

            return part;
        }

        public static ConfigurationBuilderCachePart WithNCacheBackplane(
            this ConfigurationBuilderCachePart part,
            string configurationKey)
        {
            NotNull(part, nameof(part));
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
            return part.WithBackplane(typeof(NCacheBackplane), configurationKey);
        }

        public static ConfigurationBuilderCachePart WithNCacheBackplane(
            this ConfigurationBuilderCachePart part,
            string configurationKey,
            string channelName
            )
        {
            NotNull(part, nameof(part));
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
            return part.WithBackplane(typeof(NCacheBackplane), configurationKey, channelName);
        }

        public static ConfigurationBuilderCacheHandlePart WithNCacheHandle(
            this ConfigurationBuilderCachePart part,
            string configurationKey,
            bool isBackPlaneSource = true,
            CustomDateTimeConverter datetimeJsonConverter = null)
        {
            NotNull(part, nameof(part));
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

            return part?.WithHandle(typeof(NCacheHandle<>), configurationKey, isBackPlaneSource, datetimeJsonConverter);
        }
    }
}
