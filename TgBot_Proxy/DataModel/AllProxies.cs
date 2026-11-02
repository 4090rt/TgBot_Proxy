namespace TgBot_Proxy.DataModel
{
    public struct AllProxies
    {
        public IReadOnlyList<MTProtoData> listMTPRoto { get; set; }
        public IReadOnlyList<SOCKSData> listSocks5 { get; set; }
    }
}
