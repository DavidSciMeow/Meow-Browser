namespace MeowBrowserExtern
{
    public interface IPageContext
    {
        string Url { get; }
        void ExecuteScript(string script);
        void Reload();
    }
}