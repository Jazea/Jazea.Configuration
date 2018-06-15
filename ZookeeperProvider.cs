using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Rabbit.Zookeeper;
using Rabbit.Zookeeper.Implementation;
using org.apache.zookeeper;
using static org.apache.zookeeper.ZooDefs;

namespace Jazea.Configuration
{
    public class ZookeeperProvider : ConfigurationProvider
    {
        private readonly IZookeeperClient _client;
        private readonly ZookeeperSource _source;

        public ZookeeperProvider(ZookeeperSource source)
        {
            _source = source;
            _client = new ZookeeperClient(new ZookeeperClientOptions(source.ConnectionString)
            {
                BasePath = "/",
                ConnectionTimeout = TimeSpan.FromSeconds(10),
                SessionTimeout = TimeSpan.FromMilliseconds(source.SessionTimeout),
                OperatingTimeout = TimeSpan.FromSeconds(60),
                ReadOnly = false,
                SessionId = 0,
                SessionPasswd = null,
                EnableEphemeralNodeRestore = true
            });
        }

        public override void Load() => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        private async Task LoadAsync()
        {
            try
            {
                Data.Clear();
                await GetTree($"/{Defaults.PATH_ROOT}/{Defaults.PATH_GLOBAL}");
                await GetTree($"/{Defaults.PATH_ROOT}/{Defaults.PATH_SERVICES}/{_source.ApplicationName}");
            }
            catch (KeeperException.NoNodeException ex)
            {
                CreateNode($"/{Defaults.PATH_ROOT}");
                CreateNode(ex.getPath());
                await LoadAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task GetTree(string parent)
        {
            var nodes = _client.GetChildrenAsync(parent).GetAwaiter().GetResult();
            foreach (var item in nodes)
            {
                var path = $"{parent}/{item}";
                await AddItem(path);
                await GetTree(path);
            }
        }

        private void CreateNode(string node)
        {
            if (!_client.ExistsAsync(node).GetAwaiter().GetResult())
            {
                _client.CreateAsync(node, null, Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT).GetAwaiter().GetResult();
            }
        }

        private async Task AddItem(string path)
        {
            var data = await _client.GetDataAsync(path);
            if (data.Any())
            {
                var key = path.Substring(1).Replace("/", ":");
                var value = data.Any() ? Encoding.UTF8.GetString(data.ToArray()) : string.Empty;
                Data[key] = value;
            }
        }

        public async Task SubscribeDataChange(string key, Action<NodeDataChangeArgs> action)
        {
            var keys = Data.Keys.Where(x => x.StartsWith(key, StringComparison.InvariantCultureIgnoreCase));
            foreach (var k in keys)
            {
                var path = $"/{k.Replace(":", "/")}";
                await _client.SubscribeDataChange(path, (client, args) =>
                {
                    if (args.Type != Watcher.Event.EventType.NodeDeleted)
                        Data[k] = Encoding.UTF8.GetString(args.CurrentData.ToArray());
                    else
                        Data.Remove(k);

                    action.Invoke(args);
                    return Task.CompletedTask;
                });
            }

        }

        public async Task SubscribeChildrenChange(string key, Action<NodeChildrenChangeArgs> action)
        {
            var keys = Data.Keys.Where(x => x.StartsWith(key, StringComparison.InvariantCultureIgnoreCase));
            foreach (var k in keys)
            {
                var path = $"/{k.Replace(":", "/")}";
                await _client.SubscribeChildrenChange(path, (client, args) =>
                {
                    if (args.Type != Watcher.Event.EventType.NodeDeleted)
                        Data[k] = null;
                    else
                        Data.Remove(k);

                    action.Invoke(args);
                    return Task.CompletedTask;
                });
            }
        }

    }
}
