using Microsoft.Extensions.Configuration;

namespace Jazea.Configuration
{
    public class ZookeeperSource : IConfigurationSource
    {
        public string ConnectionString { get; set; }
        public string ApplicationName { get; set; }
        public int SessionTimeout { get; set; } = 30000;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ZookeeperProvider(this);
        }
    }
}
