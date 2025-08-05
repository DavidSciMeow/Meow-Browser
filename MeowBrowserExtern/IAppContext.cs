namespace MeowBrowserExtern
{
    public interface IAppContext
    {
        void AddNewTab(string url);
        void CloseActiveTab();
    }
}