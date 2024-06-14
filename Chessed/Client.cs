using System.Net.WebSockets;

namespace Chessed
{
    internal class Client
    {
        private static Client instance = null;
        private static readonly object padlock = new object();

        public ClientWebSocket client = new ClientWebSocket();

        public static Client Instance
        {
            get
            {
                // THREAD SAFE (https://csharpindepth.com/articles/singleton)
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Client();
                    }
                    return instance;
                }
            }
        }
    }
}