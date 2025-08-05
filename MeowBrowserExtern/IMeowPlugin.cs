using System;

namespace MeowBrowserExtern
{
    public interface IMeowPlugin
    {
        string Name { get; }
        void OnNetworkRequest(NetworkEntry entry);
        void OnAppLoaded(IAppContext appContext);
    }
}