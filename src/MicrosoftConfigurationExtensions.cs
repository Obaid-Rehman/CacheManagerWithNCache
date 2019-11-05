using CacheManager.NCache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Configuration
{
    public static class MicrosoftConfigurationExtensions
    {
        public static void LoadNCacheConfigurations(
            this IConfiguration configuration)
        {
            if (configuration
                .GetSection("ncache")
                .GetChildren()
                .Count() > 0)
            {
                try
                {
                    var ncacheConfigurationType =
                        Type.GetType("CacheManager.NCache.NCacheConfiguration, CacheManager.NCache");

                    var ncacheConfigurationsType =
                        Type.GetType("CacheManager.NCache.NCacheConfigurationManager, CacheManager.NCache");


                    var addNcacheConfiguration =
                        ncacheConfigurationsType
                        .GetTypeInfo()
                        .DeclaredMethods
                        .FirstOrDefault(
                            p => p.Name == "AddConfiguration" &&
                            p.GetParameters().Length == 2 &&
                            p.GetParameters()[0].ParameterType == typeof(string) &&
                            p.GetParameters()[1].ParameterType == ncacheConfigurationType);

                    if (addNcacheConfiguration == null)
                    {
                        throw new InvalidOperationException("NCacheConfigurationManager type might have changed or cannot be invoked.");
                    }

                    foreach (var ncacheConfig in configuration.GetSection("ncache").GetChildren())
                    {
                        string key = ncacheConfig["key"];
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            throw new InvalidOperationException(
                            $"Key is required in ncache configuration but is not configured in '{ncacheConfig.Path}'."); 
                        }

                        if (string.IsNullOrWhiteSpace(ncacheConfig["cacheid"]) &&
                           ncacheConfig.GetSection("servers").GetChildren().Count() == 0)
                        {
                            throw new InvalidOperationException(
                                $"Both NCache cacheID and server info on atleast one of the cache server nodes must be configured in '{ncacheConfig.Path}' for a ncache connection.");
                        }

                        var configInstance =
                            Activator.CreateInstance(ncacheConfigurationType);

                        

                        ncacheConfig.Bind(configInstance);

                        addNcacheConfiguration
                            .Invoke(
                                null, 
                                new object[] 
                                { 
                                    key, 
                                    configInstance 
                                });
                    }
                }
                catch (FileNotFoundException ex)
                {
                    throw new InvalidOperationException(
                        "Configuration file not found",
                        ex);
                }
                catch (TypeLoadException ex)
                {
                    throw new InvalidOperationException(
                        "NCache types could not be loaded. Make sure that you have the CacheManager.NCache package installed.",
                        ex);
                }
            }
        }
    }
}
