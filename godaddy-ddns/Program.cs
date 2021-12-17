using System.Net;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace GoDaddyDDNS
{
    class Program
    {
        static HttpClient client = null!;
        static HttpClient apiClient = null!;
        static CancellationTokenSource cancelToken = new CancellationTokenSource();

        static async Task<int> Main(string[] args)
        {
            Console.CancelKeyPress += delegate {
                cancelToken.Cancel();
            };

            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "--key",
                    getDefaultValue: () => Environment.GetEnvironmentVariable ("GODADDY_DDNS_KEY") ?? null!,
                    description: "Godaddy API key"),
                new Option<string>(
                    "--secret",
                    getDefaultValue: () => Environment.GetEnvironmentVariable ("GODADDY_DDNS_SECRET") ?? null!,
                    description: "Godaddy API secret"),
                new Option<string>(
                    "--name",
                    getDefaultValue: () => Environment.GetEnvironmentVariable ("GODADDY_DDNS_NAME") ?? null!,
                    description: "Godaddy DNS record name"),
                new Option<string>(
                    "--domain",
                    getDefaultValue: () => Environment.GetEnvironmentVariable ("GODADDY_DDNS_DOMAIN") ?? null!,
                    description: "Godaddy domain name"),
                new Option<string>(
                    "--get-ip-url",
                    getDefaultValue: () => Environment.GetEnvironmentVariable ("GODADDY_DDNS_GET_IP_URL") ?? "https://icanhazip.com",
                    description: "Get host ip address from web service"),
                new Option<int>(
                    "--interval",
                    getDefaultValue: () => 
                    {
                        string val = Environment.GetEnvironmentVariable ("GODADDY_DDNS_INTERVAL") ?? null!;

                        if (string.IsNullOrEmpty(val) || !int.TryParse(val, out int intVal))
                            return 300;

                        return intVal;
                    },
                    description: "Loop check IP address time (second)"),
                new Option<int>(
                    "--ttl",
                    getDefaultValue: () => 
                    {
                        string val = Environment.GetEnvironmentVariable ("GODADDY_DDNS_TLL") ?? null!;

                        if (string.IsNullOrEmpty(val) || !int.TryParse(val, out int intVal))
                            return 600;

                        return intVal;
                    },
                    description: "DNS record Time To Live"),
                new Option<bool>(
                    "--ipv6",
                    getDefaultValue: () => Environment.GetEnvironmentVariable ("GODADDY_DDNS_IPV6") == "1",
                    description: "IPv6 mode"),
                new Option<bool>(
                    "--full-log",
                    getDefaultValue: () => Environment.GetEnvironmentVariable ("GODADDY_DDNS_FULL_LOG") == "1",
                    description: "Print full logs"),
                new Option<bool>(
                    "--without-loop-check",
                    getDefaultValue: () => Environment.GetEnvironmentVariable ("GODADDY_DDNS_WITHOUT_LOOP_CHECK") == "1",
                    "Execute program as simple script"),
            };

            rootCommand.Description = "GoDaddy Dynamic DNS";
            rootCommand.Handler = CommandHandler.Create(ExecDDNS);

            return await rootCommand.InvokeAsync(args);
        }

        static async Task<int> ExecDDNS(string key, string secret, string name, string domain, string getIpUrl, int interval, int ttl, bool ipv6, bool fullLog, bool withoutLoopCheck)
        {
            DDNSConfig config = new DDNSConfig();

            config.key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("No parameter 'key'") : key;
            config.secret = string.IsNullOrWhiteSpace(secret) ? throw new ArgumentException("No parameter 'secret'") : secret;
            config.name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("No parameter 'name'") : name;
            config.domain = string.IsNullOrWhiteSpace(domain) ? throw new ArgumentException("No parameter 'domain'") : domain;
            config.getIpUrl = string.IsNullOrWhiteSpace(getIpUrl) ? throw new ArgumentException("No parameter 'getIpUrl'") : getIpUrl;
            config.IPv6Mode = ipv6;
            config.interval = interval;
            config.ttl = ttl;

            client = DDNSRequest.CreateHTTPClient(config);
            apiClient = DDNSRequest.CreateAPIHTTPClient(config);

            string lastIPAddress = null!;
            int checkCount = 0;

            while (true)
            {
                checkCount++;
                string ipAddress;
                bool succeeded = false;

                if (fullLog)
                    Console.WriteLine($"#{checkCount} Get ip address from {config.getIpUrl}");

                try
                {
                    ipAddress = await DDNSRequest.RequestGetSelfIpAddressAsync(client, config, cancelToken.Token);

                    if (fullLog)
                        Console.WriteLine($"#{checkCount} IP={ipAddress}");
                }
                catch (Exception e)
                {
                    TextWriter errorWriter = Console.Error;
                    errorWriter.WriteLine($"#{checkCount} Get IP failed, try later.");
                    errorWriter.WriteLine(e.Message);
                    ipAddress = null!;
                }

                if (cancelToken.IsCancellationRequested)
                    return 255;

                if (ipAddress != null && lastIPAddress != ipAddress)
                {
                    try
                    {
                        await DDNSRequest.RequestUpdateDomainAsync(apiClient, config, ipAddress, cancelToken.Token);

                        lastIPAddress = ipAddress;
                        Console.WriteLine($"#{checkCount} Update domain record succeeded, {config.name}.{config.domain}={ipAddress}");
                        succeeded = true;
                    }
                    catch (Exception e)
                    {
                        TextWriter errorWriter = Console.Error;
                        errorWriter.WriteLine($"#{checkCount} Update domain record failed, try later");
                        errorWriter.WriteLine(e.Message);
                    }

                    if (cancelToken.IsCancellationRequested)
                        return 255;
                }
                else
                {
                    if (fullLog)
                        Console.WriteLine($"#{checkCount} Get ip {(string.IsNullOrWhiteSpace(ipAddress) ? "(null)" : ipAddress)}, no changes");
                }

                if (withoutLoopCheck)
                    return succeeded ? 0 : 255;

                await Task.Delay(TimeSpan.FromSeconds(config.interval), cancelToken.Token);
                
                if (cancelToken.IsCancellationRequested)
                    return 255;
            }
        }
    }
}