using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Chessed
{
    [Activity(Label = "Leaderboard")]
    public class Leaderboard : Activity
    {
        class LichessUser
        {
            public string id, username;

            public Dictionary<string, (int rating, int progress)> perfs;

            public string? title;
        }


        readonly string[] leaderboards = new string[] { "ultraBullet", "bullet", "blitz", "rapid", "classical", "chess960", "crazyhouse", "antichess", "atomic", "horde", "kingOfTheHill", "racingKings", "threeCheck" };
        Dictionary<string, Button> lbButtons = new Dictionary<string, Button>();
        string selected = "";

        LinearLayout lbButtonsContainer;

        Thread th = null;

        CustomHttpClient client = new CustomHttpClient();

        CancellationTokenSource cts;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.public_leaderboard);

            lbButtonsContainer = FindViewById<LinearLayout>(Resource.Id.lbButtons);

            for (int i = 0; i < leaderboards.Length; i++)
            {
                Button btn = new Button(this);

                string button = leaderboards[i];

                btn.BackgroundTintList = ColorStateList.ValueOf(Resources.GetColor(Resource.Color.bg_light, Theme));
                btn.Text = (i == 0 ? "> " : "") + Regex.Replace(button, "([a-z])([A-Z0-9])", "$1 $2");;
                if (i == 0) selected = button;

                btn.Click += LbButton_Click;
                btn.Tag = button;

                lbButtonsContainer.AddView(btn);
                lbButtons.Add(button, btn);
            }
        }

        public void LbButton_Click(object sender, EventArgs v)
        {
            Button btn = (Button)sender;
            if (btn.Tag.ToString() == selected) return;

            foreach (KeyValuePair<string, Button> btnPair in lbButtons)
            {
                btnPair.Value.Text = btnPair.Value.Text.Replace("> ", "");

                if (btn.Tag.ToString() == btnPair.Key) selected = btnPair.Key;
            }
            
            btn.Text = "> " + btn.Text;

            // If an HTTP request is on-going, abort it, refetch new Leaderboard
            if (th != null)
            {
                th.Abort();
                th = null;
            }

            ThreadStart ts = new ThreadStart(FetchLb);
            th = new Thread(ts);
            th.Start();
            
            

        }

        async void FetchLb()
        {
            string t = selected;

            Thread.Sleep(3000);
            JsonObject str = JsonSerializer.Deserialize<JsonObject>(await client.GetStringAsync($"https://lichess.org/api/player/top/10/{t}"));

            RunOnUiThread(() => Toast.MakeText(this, $"Successfully fetched {t}", ToastLength.Short).Show());
            //return;
        }
    }
}