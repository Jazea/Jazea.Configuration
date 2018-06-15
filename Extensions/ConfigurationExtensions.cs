using System;
using System.Linq;
using Jazea.Configuration;

namespace Jazea.Configuration
{
    public enum Scope
    {
        Global,
        Service
    }

    public enum ChangeType
    {
        Data,
        Children
    }
    public class ValueChangedArgs<T> : EventArgs
    {
        public ValueChangedArgs(string key, T value, ChangeType type)
        {
            Key = key;
            Value = value;
            Type = type;
        }
        public string Key { get; }
        public T Value { get; }
        public ChangeType Type { get; }
    }
}

namespace Microsoft.Extensions.Configuration
{
    public static class IConfigurationExtensions
    {
        public static T Get<T>(
            this IConfiguration configuration,
            Scope scope)
            where T : IConfigItem
        {
            return Get<T>(configuration, scope, null);
        }
        public static T Get<T>(
            this IConfiguration configuration,
            Scope scope,
            Action<ValueChangedArgs<T>> changeArgs)
            where T : IConfigItem
        {
            var key = $"{typeof(T).Name}";
            return Get<T>(configuration, key, scope, changeArgs);
        }

        public static T Get<T>(
            this IConfiguration configuration,
            string key,
            Scope scope)
            where T : IConfigItem
        {
            return Get<T>(configuration, key, scope, null);
        }

        public static T Get<T>(
            this IConfiguration configuration,
            string key,
            Scope scope,
            Action<ValueChangedArgs<T>> changeArgs = null)
            where T : IConfigItem
        {
            var nodeName = scope == Scope.Global ? Defaults.PATH_GLOBAL : $"{Defaults.PATH_SERVICES}:{configuration.GetValue<string>("applicationName")}";
            key = $"{Defaults.PATH_ROOT}:{nodeName}:{key}";
            var value = configuration.GetSection(key).Get<T>();

            SubscribeChange(configuration as IConfigurationRoot, key, value, changeArgs);

            return value;
        }

        public static T GetValue<T>(
            this IConfiguration configuration,
            string key,
            Scope scope,
            Action<ValueChangedArgs<T>> changeArgs = null)
        {
            var nodeName = scope == Scope.Global ? Defaults.PATH_GLOBAL : $"{Defaults.PATH_SERVICES}:{configuration.GetValue<string>("applicationName")}";
            key = $"{Defaults.PATH_ROOT}:{nodeName}:{key}";
            T value = configuration.GetValue<T>(key);

            SubscribeChange(configuration as IConfigurationRoot, key, value, changeArgs);

            return value;
        }

        private static void SubscribeChange<T>(
            IConfigurationRoot configuration,
            string key, T value,
            Action<ValueChangedArgs<T>> changeArgs)
        {
            var provider = configuration.Providers.FirstOrDefault(x => x.GetType() == typeof(ZookeeperProvider)) as ZookeeperProvider;
            if (provider != null)
            {
                provider.SubscribeDataChange(key, args =>
                {
                    value = configuration.GetSection(key).Get<T>();
                    if (changeArgs != null)
                        changeArgs.Invoke(new ValueChangedArgs<T>(key, value, ChangeType.Data));
                }).GetAwaiter();

                provider.SubscribeChildrenChange(key, args =>
                {
                    value = configuration.GetSection(key).Get<T>();
                    if (changeArgs != null)
                        changeArgs.Invoke(new ValueChangedArgs<T>(key, value, ChangeType.Children));
                }).GetAwaiter();
            }
        }
    }
}
