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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Chessed
{
    [Activity(Label = "GameScreen")]
    internal class WaitingForPlayer : Activity
    {

        ClientWebSocket client = Client.Instance.client;


        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            RunOnUiThread(() => SetContentView(Resource.Layout.waitingForPlayer));

            await client.ConnectAsync(new Uri($"ws://192.168.1.238:8080?token={Preferences.Get("token", "")}"), CancellationToken.None);

#pragma warning disable CS4014
            Task.Run(async () => await ReadMessage());
#pragma warning restore CS4014

            if (client.State != WebSocketState.Open)
            {
               Finish();
            }

        }

        async Task ReadMessage()
        {
            while (client.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                var message = new ArraySegment<byte>(new byte[4096]);
                do
                {
                    result = await client.ReceiveAsync(message, CancellationToken.None);
                    if (result.MessageType != WebSocketMessageType.Text)
                        break;
                    var messageBytes = message.Skip(message.Offset).Take(result.Count).ToArray();
                    string receivedMessage = Encoding.UTF8.GetString(messageBytes);
                    Dictionary<string, string> res = JsonSerializer.Deserialize<Dictionary<string, string>>(receivedMessage);

                    if (res["type"] == "match_start")
                    {
                        RunOnUiThread(() => {
                            Intent i = new Intent(this, typeof(GameScreen));
                            i.PutExtra("data", receivedMessage);
                            StartActivity(i);
                        });

                        return;
                    }
                }
                while (!result.EndOfMessage);
            }
        }
    }
}