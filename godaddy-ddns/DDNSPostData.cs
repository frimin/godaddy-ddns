namespace GoDaddyDDNS
{
    class DDNSPostData
    {
        public string name { get; set; } = null!;
        public string type { get; set; } = null!;
        public string data { get; set; } = null!;
        public int ttl { get; set; }
    }
}
