namespace GoDaddyDDNS
{
    public class DDNSConfig
    {
        public string key = null!;
        public string secret = null!;
        public string name = null!;
        public string domain = null!;
        public int ttl;
        public int interval;
        public string getIpUrl = null!;
        public bool IPv6Mode;
    }
}