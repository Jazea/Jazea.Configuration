using System;
using Jazea.Configuration;

namespace Microsoft.Extensions.Configuration
{
    public static class IConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddConfiguration(
            this IConfigurationBuilder configurationBuilder
            , Action<ZookeeperSource> options)
        {
            var source = new ZookeeperSource();
            options.Invoke(source);

            if (string.IsNullOrEmpty(source.ApplicationName))
                throw new ArgumentNullException(nameof(source.ApplicationName));

            if (string.IsNullOrEmpty(source.ConnectionString))
                throw new ArgumentNullException(nameof(source.ConnectionString));

            configurationBuilder.Add(source);
            return configurationBuilder;
        }

        public static IConfigurationBuilder AddConfiguration(
           this IConfigurationBuilder configurationBuilder
           , string connectionString)
        {
            var applicationName = configurationBuilder.Build().GetValue<string>("applicationName");
            var source = new ZookeeperSource()
            {
                ApplicationName = applicationName,
                ConnectionString = connectionString
            };

            if (string.IsNullOrEmpty(source.ApplicationName))
                throw new ArgumentNullException(nameof(source.ApplicationName));

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(source.ConnectionString));

            configurationBuilder.Add(source);
            return configurationBuilder;
        }
    }
}
