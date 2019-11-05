using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.NCache;
using Microsoft.Extensions.Logging;
using Unity;
using Unity.Injection;
using Unity.Lifetime;

namespace CacheManager.NCache.Examples
{
    public class Program
    {
        private static string NCacheID = "demo1";
        private static string NCacheHost = "20.200.20.45";
        private static int NCachePort = 9800;

        private static void Main()
        {
            Console.WriteLine("Events example");
            EventsExample();

            Console.WriteLine("\n\nNCache Sample");
            NCacheSample();

            Console.WriteLine("\n\nUnity Injection Example");
            UnityInjectionExample();

            Console.WriteLine("\n\nUnity Injection Example Advanced");
            UnityInjectionExample_Advanced();

            Console.WriteLine("\n\nSimple Custom Build Configs Using Config Builder");
            SimpleCustomBuildConfigurationUsingConfigBuilder();

            Console.WriteLine("\n\nSimple Custom Build Configs using Factory");
            SimpleCustomBuildConfigurationUsingFactory();

            Console.WriteLine("\n\nUpdate Test");
            UpdateTest();

            Console.WriteLine("\n\nUpdate Counter Test");
            UpdateCounterTest();


            Console.WriteLine("\n\nParallel update counter test");
            ParallelUpdateCounterTest();

#if NET461
            Console.WriteLine("\n\nLoading from Configuration File Example");
            AppConfigLoadInstalledCacheCfg();
#endif

            Console.WriteLine("\n\nLogging Sample");
            LoggingSample();

            Console.WriteLine("\n\nMulticache Eviction without NCache handle");
            MultiCacheEvictionWithoutCacheHandle();


            Console.WriteLine("\n\nMulticache Pub Sub");
            MultiCachePubSub();
        }


        private static void LoggingSample()
        {
            var cache = CacheFactory.Build<string>(
                c =>
                c.WithMicrosoftLogging(log =>
                {
                    log.AddConsole(LogLevel.Trace);
                })
                .WithNCacheConfiguration(
                    "ncache",
                    config =>
                    {
                        config
                        .WithCacheId(NCacheID)
                        .WithEndPoint(NCacheHost);
                    })
                .WithNCacheHandle("ncache")
                .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10)));

            cache.AddOrUpdate("myKey", "someregion", "value", _ => "new value");
            cache.AddOrUpdate("myKey", "someregion", "value", _ => "new value");
            cache.Expire("myKey", "someregion", TimeSpan.FromMinutes(10));
            var val = cache.Get("myKey", "someregion");

            cache.Clear();
        }

#if NET461

        private static void AppConfigLoadInstalledCacheCfg()
        {
            var cache = CacheFactory.FromConfiguration<object>("myCache");
            cache.Add("key", "value");
            Console.WriteLine($"Value added is {cache["key"]}");

            cache.Clear();
        }

#endif

        private static void EventsExample()
        {
            var cache = CacheFactory.Build<string>(
                s =>
                s.WithNCacheHandle("ncache")
                .And
                .WithNCacheConfiguration(
                    "ncache",
                    config =>
                    {
                        config
                        .WithCacheId(NCacheID)
                        .WithEndPoint(NCacheHost);
                    }));
            cache.OnAdd += (sender, args) => Console.WriteLine("Added " + args.Key);
            cache.OnGet += (sender, args) => Console.WriteLine("Got " + args.Key);
            cache.OnRemove += (sender, args) => Console.WriteLine("Removed " + args.Key);

            cache.Clear();
            cache.Add("key", "value");
            var val = cache.Get("key");
            cache.Remove("key");

            cache.Clear();
        }


        private static void NCacheSample()
        {
            var cache = CacheFactory.Build<int>(settings =>
            {
                settings
                    .WithDictionaryHandle()
                    .And
                    .WithNCacheConfiguration("ncache", config =>
                    {
                        config
                        .WithCacheId(NCacheID)
                        .WithEndPoint(NCacheHost);
                    })
                    .WithMaxRetries(1000)
                    .WithRetryTimeout(100)
                    .WithNCacheBackplane("ncache")
                    .WithNCacheHandle("ncache", true);
            });

            cache.Add("test", 123456);

            cache.Update("test", p => p + 1);


            var result = cache.Get("test");

            Console.WriteLine(result);

            cache.Clear();
        }


        private static void SimpleCustomBuildConfigurationUsingConfigBuilder()
        {
            // this is using the CacheManager.Core.Configuration.ConfigurationBuilder to build a
            // custom config you can do the same with the CacheFactory
            var cfg = ConfigurationBuilder.BuildConfiguration(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Up)
                    .WithNCacheConfiguration(
                        "ncache",
                        config =>
                        {
                            config
                                .WithCacheId(NCacheID)
                                .WithEndPoint(NCacheHost);
                        })
                    .WithNCacheHandle("ncache")
                    .EnablePerformanceCounters()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
            });

            var cache = CacheFactory.FromConfiguration<string>(cfg);
            cache.Add("key", "value");

            // reusing the configuration and using the same cache for different types:
            var numbers = CacheFactory.FromConfiguration<int>(cfg);
            numbers.Add("intKey", 2323);
            numbers.Update("intKey", v => v + 1);

            var val = numbers.Get("intKey");

            Console.WriteLine(val);

            cache.Clear();
        }

        private static void SimpleCustomBuildConfigurationUsingFactory()
        {
            var cache = CacheFactory.Build(settings =>
            {
                settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithNCacheHandle("ncache")
                    .EnablePerformanceCounters()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10))
                    .And
                    .WithNCacheConfiguration(
                        "ncache",
                        config =>
                        {
                            config
                                .WithEndPoint(NCacheHost)
                                .WithCacheId(NCacheID);
                        });
            });

            cache.Add("key", "value");

            cache.Update("key", o => o + "-updated");

            var val = cache.Get("key");

            Console.WriteLine(val);

            cache.Clear();
        }

        private static void UnityInjectionExample()
        {
            var container = new UnityContainer();
            container.RegisterType<ICacheManager<object>>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory((c) => CacheFactory.Build(
                    s =>
                        s.WithNCacheHandle("ncache")
                                    .And
                                    .WithNCacheConfiguration(
                                        "ncache",
                                        config =>
                                        {
                                            config
                                                .WithCacheId(NCacheID)
                                                .WithEndPoint(NCacheHost);
                                        }
                                    )
                        )
                )
           );

            container.RegisterType<UnityInjectionExampleTarget>();

            // resolving the test target object should also resolve the cache instance
            var target = container.Resolve<UnityInjectionExampleTarget>();
            target.PutSomethingIntoTheCache();

            // our cache manager instance should still be there so should the object we added in the
            // previous step.
            var checkTarget = container.Resolve<UnityInjectionExampleTarget>();
            checkTarget.GetSomething();
            Console.WriteLine("Didn't throw error so cache instance was successfully resolved");
        }

        private static void UnityInjectionExample_Advanced()
        {
            var container = new UnityContainer();
            container.RegisterType(
                typeof(ICacheManager<>),
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    (c, t, n) => CacheFactory.FromConfiguration(
                        t.GetGenericArguments()[0],
                        ConfigurationBuilder.BuildConfiguration(
                            s =>
                                    s.WithNCacheHandle("ncache")
                                    .And
                                    .WithNCacheConfiguration(
                                        "ncache",
                                        config =>
                                        {
                                            config
                                                .WithCacheId(NCacheID)
                                                .WithEndPoint(NCacheHost);
                                        }
                                        )
                                )
                        )
                    )
                );

            var stringCache = container.Resolve<ICacheManager<string>>();

            // testing if we create a singleton instance per type, every Resolve of the same type should return the same instance!
            var stringCacheB = container.Resolve<ICacheManager<string>>();
            stringCache.Put("key1", "something");

            var intCache = container.Resolve<ICacheManager<int>>();
            var intCacheB = container.Resolve<ICacheManager<int>>();
            intCache.Put("key2", 22);

            var boolCache = container.Resolve<ICacheManager<bool>>();
            var boolCacheB = container.Resolve<ICacheManager<bool>>();
            boolCache.Put("key3", false);

            Console.WriteLine("Value type is: " + stringCache.GetType().GetGenericArguments()[0].Name + " test value: " + stringCacheB["key1"]);
            Console.WriteLine("Value type is: " + intCache.GetType().GetGenericArguments()[0].Name + " test value: " + intCacheB["key2"]);
            Console.WriteLine("Value type is: " + boolCache.GetType().GetGenericArguments()[0].Name + " test value: " + boolCacheB["key3"]);

            stringCache.Clear();
            intCache.Clear();
            boolCache.Clear();
        }

        private static void UpdateTest()
        {
            var cache = CacheFactory.Build<string>(
                s =>
                s.WithNCacheHandle("ncache")
                .And
                .WithNCacheConfiguration(
                    "ncache",
                    config =>
                    {
                        config
                        .WithCacheId(NCacheID)
                        .WithEndPoint(NCacheHost);
                    }));

            Console.WriteLine("Testing update...");

            if (!cache.TryUpdate("test", v => "item has not yet been added", out string newValue))
            {
                Console.WriteLine("Value not added?: {0}", newValue == null);
            }

            cache.Add("test", "start");
            Console.WriteLine("Initial value: {0}", cache["test"]);

            cache.AddOrUpdate("test", "adding again?", v => "updating and not adding");
            Console.WriteLine("After AddOrUpdate: {0}", cache["test"]);

            cache.Remove("test");
            try
            {
                var removeValue = cache.Update("test", v => "updated?");
            }
            catch
            {
                Console.WriteLine("Error as expected because item didn't exist.");
            }

            // use try update to not deal with exceptions
            if (!cache.TryUpdate("test", v => v, out string removedValue))
            {
                Console.WriteLine("Value after remove is null?: {0}", removedValue == null);
            }

            cache.Clear();
        }

        private static void UpdateCounterTest()
        {
            var cache = CacheFactory.Build<long>(
                s =>
                s.WithNCacheHandle("ncache")
                .And
                .WithNCacheConfiguration(
                    "ncache",
                    config =>
                    {
                        config
                        .WithCacheId(NCacheID)
                        .WithEndPoint(NCacheHost);
                    }));

            Console.WriteLine("Testing update counter...");

            cache.AddOrUpdate("counter", 0, v => v + 1);

            Console.WriteLine("Initial value: {0}", cache.Get("counter"));

            for (var i = 0; i < 12345; i++)
            {
                cache.Update("counter", v => v + 1);
            }

            Console.WriteLine("Final value: {0}", cache.Get("counter"));

            cache.Clear();
        }

        private static void ParallelUpdateCounterTest()
        {
            var cache = CacheFactory.Build<int>(
                s =>
                s.WithNCacheHandle("ncache")
                .And
                .WithMicrosoftLogging(log => log.AddConsole(LogLevel.Debug))
                .WithNCacheConfiguration(
                    "ncache",
                    config =>
                    {
                        config
                        .WithCacheId(NCacheID)
                        .WithEndPoint(NCacheHost);
                    }));

            cache.Clear();
            cache.Add("key", 0);
            Action test = () => cache.Update("key", val => val + 1, 2000);

            Parallel.Invoke(
                new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 100
                },
                Enumerable.Repeat(test, 100).ToArray());

            Console.WriteLine("Updated counter:" + cache.Get("key"));
        }

        private static void MultiCacheEvictionWithoutCacheHandle()
        {
            var config = new ConfigurationBuilder("NCache with NCache Backplane")
                .WithDictionaryHandle(true)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(5))
                .And
                .WithNCacheBackplane("ncacheConfig")
                .WithNCacheConfiguration(
                        "ncacheConfig",
                        ncacheConfigBuilder =>
                        {
                            ncacheConfigBuilder
                                .WithCacheId(NCacheID)
                                .WithEndPoint(NCacheHost)
                                .WithKeyNotificationsEnabled(true);
                        })
                //.WithMicrosoftLogging(new LoggerFactory().AddConsole(LogLevel.Debug))
                .Build();

            var cacheA = new BaseCacheManager<string>(config);
            var cacheB = new BaseCacheManager<string>(config);

            var key = "someKey";

            cacheA.OnRemove += (s, args) =>
            {
                Console.WriteLine("A triggered remove: " + args.ToString() + " - key still exists? " + cacheA.Exists(key));
            };
            cacheB.OnRemove += (s, args) =>
            {
                Console.WriteLine("B triggered remove: " + args.ToString() + " - key still exists? " + cacheB.Exists(key));
            };

            cacheA.OnRemoveByHandle += (s, args) =>
            {
                cacheA.Remove(args.Key);
                Console.WriteLine("A triggered removeByHandle: " + args.ToString() + " - key still exists? " + cacheA.Exists(key));
            };

            cacheB.OnRemoveByHandle += (s, args) =>
            {
                Console.WriteLine("B triggered removeByHandle: " + args.ToString() + " - key still exists? " + cacheA.Exists(key) + " in A? " + cacheA.Exists(key));
            };

            cacheA.OnAdd += (s, args) =>
            {
                Console.WriteLine("A triggered add: " + args.ToString());
            };

            cacheB.OnAdd += (s, args) =>
            {
                Console.WriteLine("B triggered add: " + args.ToString());
            };

            Console.WriteLine("Add to A: " + cacheA.Add(key, "some value"));
            Console.WriteLine("Add to B: " + cacheB.Add(key, "some value"));

            Thread.Sleep(2000);

            Console.WriteLine($"Removing {key} from cacheA");
            cacheA.Remove(key);

            cacheA.Clear();
            cacheB.Clear();
        }

        private static void MultiCachePubSub()
        {
            var channelName = Guid.NewGuid().ToString();
            var localTriggers = 0;
            var remoteTriggers = 0;

            var item = new CacheItem<object>(
                Guid.NewGuid().ToString(), "something");

            var builder =
                new ConfigurationBuilder()
                .Build()
                .Builder;

            builder
                .WithUpdateMode(CacheUpdateMode.Up)
                // .WithMicrosoftLogging(log => log.AddConsole(LogLevel.Information))
                .WithDictionaryHandle()
                    .EnableStatistics()
                    .EnablePerformanceCounters()
                .And
                .WithMaxRetries(int.MaxValue)
                .WithRetryTimeout(1000)
                .WithNCacheConfiguration(
                    "ncache",
                    config =>
                    {
                        config
                            .WithCacheId(NCacheID)
                            .WithEndPoint(
                                NCacheHost,
                                NCachePort);
                    })
                .WithNCacheBackplane("ncache", channelName)
                .WithNCacheHandle("ncache", true)
                .EnableStatistics()
                .EnablePerformanceCounters();

            var cacheA =
                CacheFactory.FromConfiguration<object>(
                "cacheA",
                builder.Build());

            var cacheB =
                CacheFactory.FromConfiguration<object>(
                "cacheB",
                builder.Build());

            cacheA.Clear();
            cacheB.Clear();

            Thread.Sleep(2000);

            Action IncrementLocal = () => ++localTriggers/*Interlocked.Increment(ref localTriggers)*/;
            Action IncrementRemote = () => ++remoteTriggers/*Interlocked.Increment(ref remoteTriggers)*/;

            RegisterEvents(cacheA, IncrementLocal, IncrementRemote);
            RegisterEvents(cacheB, IncrementLocal, IncrementRemote);

            Console.WriteLine("\nFrom CacheA to CacheB");
            PubSubEvents(cacheA);

            cacheA.Clear();
            cacheB.Clear();

            Console.WriteLine("\nFrom CacheB to CacheA");
            PubSubEvents(cacheB);

            Thread.Sleep(2000);
            Console.WriteLine("\nRemote triggers-" + remoteTriggers);
            Console.WriteLine("Local triggers-" + localTriggers);

            Console.ReadKey();
        }



        private static void RegisterEvents(
            ICacheManager<object> cache,
            Action incrementLocal,
            Action incrementRemote)
        {
            cache.OnAdd += (o, args) =>
            {
                Console.WriteLine($"Add event on {cache.Name}- " + args);
                var key = args.Key;
                var region = args.Region;
                var value = cache.Get(key, region);

                Console.WriteLine($"{cache.Name} Key:{key}-Region:{region ?? "none"}-Value-{value}");

                if (args.Origin == Core.Internal.CacheActionEventArgOrigin.Local)
                {
                    incrementLocal();
                }
                else
                {
                    incrementRemote();
                }
            };


            cache.OnUpdate += (o, args) =>
            {
                Console.WriteLine($"Update event on {cache.Name}- " + args);
                var key = args.Key;
                var region = args.Region;
                var value = cache.Get(key, region);

                Console.WriteLine($"{cache.Name} Key:{key}-Region:{region ?? "none"}-Value-{value}");

                if (args.Origin == Core.Internal.CacheActionEventArgOrigin.Local)
                {
                    incrementLocal();
                }
                else
                {
                    incrementRemote();
                }
            };

            cache.OnPut += (o, args) =>
            {
                Console.WriteLine($"Put event on {cache.Name}- " + args);
                var key = args.Key;
                var region = args.Region;
                var value = cache.Get(key, region);

                Console.WriteLine($"{cache.Name} Key:{key}-Region:{region ?? "none"}-Value-{value}");

                if (args.Origin == Core.Internal.CacheActionEventArgOrigin.Local)
                {
                    incrementLocal();
                }
                else
                {
                    incrementRemote();
                }
            };

            cache.OnRemove += (o, args) =>
            {
                Console.WriteLine($"Remove event on {cache.Name}- " + args);
                var key = args.Key;
                var region = args.Region;

                Console.WriteLine($"{cache.Name} Key:{key}-Region:{region ?? "none"}");

                var exists = region == null ? cache.Exists(key) : cache.Exists(key, region);

                Console.WriteLine(exists ? $"{cache.Name} Key is not removed" : $"{cache.Name} Key is removed");

                if (args.Origin == Core.Internal.CacheActionEventArgOrigin.Local)
                {
                    incrementLocal();
                }
                else
                {
                    incrementRemote();
                }
            };

            cache.OnClear += (o, args) =>
            {
                Console.WriteLine($"Clear event on {cache.Name}- " + args);
                var handles = cache.CacheHandles;

                int count = 0;
                foreach (var handle in handles)
                {
                    count += handle.Count;
                }

                Console.WriteLine($"{cache.Name} Count: " + count);

                if (args.Origin == Core.Internal.CacheActionEventArgOrigin.Remote)
                {
                    incrementRemote();
                }
                else
                {
                    incrementLocal();
                }
            };

            cache.OnClearRegion += (o, args) =>
            {
                Console.WriteLine($"Clear region event on {cache.Name}- " + args);

                Console.WriteLine("Region cleared -" + args.Region);

                var handles = cache.CacheHandles;

                var count = 0L;

                foreach(var handle in handles)
                {
                    count += handle.Stats.GetStatistic(
                        Core.Internal.CacheStatsCounterType.Items, 
                        args.Region);
                }

                Console.WriteLine($"{cache.Name} items in {args.Region}: {count}");

                if (args.Origin == Core.Internal.CacheActionEventArgOrigin.Remote)
                {
                    incrementRemote();
                }
                else
                {
                    incrementLocal();
                }
            };
        }

        private static void PubSubEvents(
            ICacheManager<object> cache)
        {
            var key = "key-";
            var region = "region-";
            var value = 0;

            Console.WriteLine("*********Add Events*************");
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    cache.Add(key + i, ++value, region + j);
                    Thread.Sleep(3000);
                }
            }

            Console.WriteLine("\n\n*********Put Events*************");
            value = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    ++value;
                    cache.Put(key + i, value * 10, region + j);
                    Thread.Sleep(3000);
                }
            }

            Console.WriteLine("\n\n*********Update Events*************");
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    cache.Update(key + i, region + j, val => val + "-updated");
                    Thread.Sleep(3000);
                }
            }

            Console.WriteLine("\n\n*********Clear region Event*************");
            cache.ClearRegion(region + 0);
            Thread.Sleep(3000);

            Console.WriteLine("\n\n*********Clear Event*************");
            cache.Clear();
            Thread.Sleep(3000);
        }
    }



    public class UnityInjectionExampleTarget
    {
        private ICacheManager<object> _cache;

        public UnityInjectionExampleTarget(ICacheManager<object> cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public void GetSomething()
        {
            var value = _cache.Get("myKey");
            var x = value;
            if (value == null)
            {
                throw new InvalidOperationException();
            }
        }

        public void PutSomethingIntoTheCache()
        {
            _cache.Put("myKey", "something");
        }
    }
}
