using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System.Net;

namespace Signer.Application.Impl.Actors.Orleans
{
    public sealed class OrleansPipeline
    {
        public static ISiloHost GetHost()
        {
            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "local";
                    options.ServiceId = "hw5-signer";
                })
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SingleHashGrain).Assembly).WithReferences());

            return builder.Build();
        }

        public static IClusterClient GetClient()
        {
            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "local";
                    options.ServiceId = "hw5-signer";
                })
                .Build();

            return client;
        }
    }

}
