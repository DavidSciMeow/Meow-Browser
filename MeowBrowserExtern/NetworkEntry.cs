namespace MeowBrowserExtern
{
    // 网络请求信息类
    public struct NetworkEntry
    {
        public string Method { get; set; }
        public string Url { get; set; }
        public int StatusCode { get; set; }
        public string ContentType { get; set; }
    }
}
