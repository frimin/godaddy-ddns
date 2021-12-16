using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Net.Sockets;

namespace GoDaddyDDNS
{
    public static class DDNSRequest
    {
        public static HttpClient CreateHTTPClient(DDNSConfig config)
        {
            HttpClient client = null!;

            
            System.Net.Http.SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler();

            socketsHttpHandler.ConnectCallback = async (SocketsHttpConnectionContext context, CancellationToken cancellationToken) => {
                DnsEndPoint endPoint = new DnsEndPoint(context.DnsEndPoint.Host, context.DnsEndPoint.Port, config.IPv6Mode ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork);

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                try
                {
                    await socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }

                return new NetworkStream(socket, ownsSocket: true);
            };

            client = new HttpClient(socketsHttpHandler);

            return client;
        }

        public static HttpClient CreateAPIHTTPClient(DDNSConfig config)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));;
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"sso-key {config.key}:{config.secret}");

            return client;
        }
        
        public static async Task<string> RequestGetSelfIpAddressAsync(HttpClient client, DDNSConfig config, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await client.GetAsync(config.getIpUrl, cancellationToken))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return (await response.Content.ReadAsStringAsync()).Replace("\n", "");
                }
                else
                    throw new Exception($"request failed, status code = {response.StatusCode}");
            }
        }

        public static async Task RequestUpdateDomainAsync(HttpClient client, DDNSConfig config, string ipAddressStr, CancellationToken cancellationToken)
        {
            if (IPAddress.TryParse(ipAddressStr, out IPAddress? address))
            {
                switch (address.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                    case AddressFamily.InterNetworkV6:
                        break;
                    default:
                        throw new System.NotSupportedException($"invalid address type, address={ipAddressStr}");
                }
            }
            else
            {
                throw new System.NotSupportedException($"invalid address type, address={ipAddressStr}");
            }

            var data = new DDNSPostData()
            {
                name = config.name,
                data = ipAddressStr,
                ttl = config.ttl,
                type = address.AddressFamily == AddressFamily.InterNetwork ? "A" : "AAAA",
            };
            
            using (JsonContent content = JsonContent.Create(new DDNSPostData[] { data }))
            {
                using (HttpResponseMessage response = await client.PutAsync($"https://api.godaddy.com/v1/domains/{config.domain}/records/{data.type}/{config.name}", content, cancellationToken))
                {
                   if (!response.IsSuccessStatusCode)
                   {
                       throw new HttpRequestException($"{(int)response.StatusCode} {response.ReasonPhrase}");
                   }
                }
            }
        }
    }
}
