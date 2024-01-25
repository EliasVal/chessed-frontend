using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;

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