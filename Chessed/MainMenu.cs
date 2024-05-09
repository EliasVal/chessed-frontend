using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Lights;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xamarin.Essentials;
using static Android.Icu.Text.ListFormatter;

namespace Chessed
{
    [Activity(Label = "main_menu", ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainMenu : Activity
    {
        JsonObject userData = null;

        CustomHttpClient client = new CustomHttpClient()
        {
            BaseAddress = new Uri("http://192.168.1.238:3000")
        };

        TextView ELOTv, UsernameTv, WinsTv, DrawsTv, LossesTv;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.main_menu);

            int width = Resources.DisplayMetrics.WidthPixels;
            int height = Resources.DisplayMetrics.HeightPixels;

            FindViewById(Resource.Id.content).Visibility = ViewStates.Gone;

            this.ShowLoadingSpinner(FindViewById<ViewGroup>(Resource.Id.Root), width, height);

            ELOTv = FindViewById<TextView>(Resource.Id.playerElo);
            UsernameTv = FindViewById<TextView>(Resource.Id.playerName);
            WinsTv = FindViewById<TextView>(Resource.Id.winCount);
            DrawsTv = FindViewById<TextView>(Resource.Id.drawCount);
            LossesTv = FindViewById<TextView>(Resource.Id.lossCount);

            FetchUserData();
        }

        async void FetchUserData()
        {
            try
            {
                userData = await client.MakeHTTPReq("/get_profile", new { uid = Preferences.Get("uid", "") });
            }
            catch
            {
                Toast.MakeText(this, "Failed to fetch user data, please try again later", ToastLength.Short);
                Preferences.Clear();
                Finish();
            }

            ShowMatches();

            // Update UI with user's data
            RunOnUiThread(() =>
            {
                ELOTv.Text = $"{userData["elo"]} {GetText(Resource.String.elo)}";
                UsernameTv.Text = $"{GetText(Resource.String.welcome_back)}, {userData["username"]}";
                WinsTv.Text = $"{userData["wins"]} {GetText(Resource.String.wins)}";
                DrawsTv.Text = $"{userData["draws"]} {GetText(Resource.String.draws)}";
                LossesTv.Text = $"{userData["losses"]} {GetText(Resource.String.losses)}";
                this.HideLoadingSpinner();
                FindViewById(Resource.Id.content).Visibility = ViewStates.Visible;
            });
        }

        Dictionary<string, string> userCache = new Dictionary<string, string>();

        async void ShowMatches()
        {
            JsonObject matches = userData["matches"].AsObject();

            RunOnUiThread(() =>
            {
                this.ShowLoadingSpinner(FindViewById<ViewGroup>(Resource.Id.matchesRoot));
            });

            foreach (var match in matches)
            {
                try
                {
                    JsonObject res = await client.MakeHTTPReq("/get_matchdata", new { id = match.Key });

                    string whiteName = res["white"]["id"].ToString() == Preferences.Get("uid", "") ? userData["username"].ToString() : null;
                    string blackName = res["black"]["id"].ToString() == Preferences.Get("uid", "") ? userData["username"].ToString() : null;

                    string toSearch = whiteName == null ? res["white"]["id"].ToString() : res["black"]["id"].ToString();

                    string resultText = "";

                    int resultColor;

                    if (res["winner"].ToString() == "d")
                    {
                        resultText = "Drawn";
                        resultColor = Resource.Color.warning;
                    }
                    else if (res["winner"].ToString() == "w" && whiteName != null || res["winner"].ToString() == "b" && blackName != null)
                    {
                        resultText = "Won";
                        resultColor = Resource.Color.success;
                    }
                    else
                    {
                        resultText = "Lost";
                        resultColor = Resource.Color.danger;
                    }

                    resultText += $" by {res["reason"]}";


                    if (!userCache.ContainsKey(toSearch))
                    {
                        JsonObject userDataRes = await client.MakeHTTPReq("/get_profile", new { uid = toSearch });

                        userCache.Add(toSearch, userDataRes["username"].ToString());
                    }

                    

                    RunOnUiThread(() =>
                    {
                        LinearLayout card = (LinearLayout)LayoutInflater.Inflate(Resource.Drawable.matchCard, null);

                        ((TextView)card.FindViewWithTag("player1Name")).Text = whiteName == null ? userCache[res["white"]["id"].ToString()] : whiteName;
                        ((TextView)card.FindViewWithTag("player1elo")).Text = $"{GetText(Resource.String.elo)}: {res["white"]["elo"]}";
                        ((TextView)card.FindViewWithTag("player2Name")).Text = blackName == null ? userCache[res["black"]["id"].ToString()] : blackName;
                        ((TextView)card.FindViewWithTag("player2elo")).Text = $"{GetText(Resource.String.elo)}: {res["black"]["elo"]}";
                        ((TextView)card.FindViewWithTag("reason")).Text = resultText;
                        ((TextView)card.FindViewWithTag("reason")).SetTextColor(Resources.GetColor(resultColor, Theme));

                        FindViewById<LinearLayout>(Resource.Id.matchesContent).AddView(cgard);
                    });

                }
                catch /*(Exception ex)*/ {
                    // Toast.MakeText(this, ex.InnerException.Message, ToastLength.Short).Show();
                };
            }

            RunOnUiThread(() =>
            {
                this.HideLoadingSpinner();
                FindViewById(Resource.Id.matchesContent).Visibility = ViewStates.Visible;
            });
        }

        [Export("StartGameBtn")]
        public void StartGameBtn(View v)
        {
            Intent i = new Intent(this, typeof(WaitingForPlayer));

            StartActivity(i);
        }
    }
}