using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase;
using Firebase.Auth;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Xamarin.Essentials;

namespace Chessed
{
    [Activity(Label = "main_menu", ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainMenu : Activity
    {
        JsonObject userData = null;

        CustomHttpClient client = new CustomHttpClient()
        {
#if DEBUG
            BaseAddress = new Uri("http://192.168.1.238:8080")
#else
            BaseAddress = new Uri("http://chessed-ac171.oa.r.appspot.com")
#endif
        };

        TextView ELOTv, UsernameTv, WinsTv, DrawsTv, LossesTv;
        LinearLayout matchesContent;

        int width = 0;
        int height = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.main_menu);

            width = Resources.DisplayMetrics.WidthPixels;
            height = Resources.DisplayMetrics.HeightPixels;

            ELOTv = FindViewById<TextView>(Resource.Id.playerElo);
            UsernameTv = FindViewById<TextView>(Resource.Id.playerName);
            WinsTv = FindViewById<TextView>(Resource.Id.winCount);
            DrawsTv = FindViewById<TextView>(Resource.Id.drawCount);
            LossesTv = FindViewById<TextView>(Resource.Id.lossCount);

            matchesContent = FindViewById<LinearLayout>(Resource.Id.matchesContent);
        }

        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back && e.Action == KeyEventActions.Down)
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this, Resource.Style.Dialog);

                AlertDialog alert = builder.Create();
                alert.SetMessage("Are you sure you want to exit?");
                //alert.SetMessage(GetText(Resource.String.draw_confirm));
                alert.SetButton2(GetText(Resource.String.yes), (sender, e) =>
                {
                    //Application.Dispose();
                    FinishAffinity();
                });

                alert.SetButton(GetText(Resource.String.no), (s, e) => { });

                alert.Show();

                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }

        protected override void OnResume()
        {
            base.OnResume();

            FindViewById(Resource.Id.content).Visibility = ViewStates.Gone;

            this.ShowLoadingSpinner(FindViewById<ViewGroup>(Resource.Id.Root), width, height);

            FetchUserData();
        }

        async void FetchUserData()
        {
            try
            {
                userData = await client.MakeHTTPReq("/get_profile", new { uid = Preferences.Get("uid", ""), needMatches = "true" });

                Preferences.Set("elo", userData["elo"].ToString());
                Preferences.Set("username", userData["username"].ToString());
            }
            catch (Exception ex)
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
            List<JsonObject> matchesArr = new List<JsonObject>();

            if (userData["matches"] == null)
            {
                FindViewById(Resource.Id.matchesRoot).Visibility = ViewStates.Gone;
                return;
            }

            JsonObject _matches = userData["matches"].AsObject();

            Dictionary<string, ulong> matches = new Dictionary<string, ulong>();

            foreach (var match in _matches)
            {
                matches.Add(match.Key, ulong.Parse(match.Value["time"].ToString()));
            }

            var sortedMatches = from entry in matches orderby entry.Value ascending select entry;

            RunOnUiThread(() =>
            {
                this.ShowLoadingSpinner(FindViewById<ViewGroup>(Resource.Id.matchesRoot));
                matchesContent.RemoveAllViews();
            });

            foreach (var match in sortedMatches)
            {
                try
                {
                    JsonObject res = await client.MakeHTTPReq("/get_matchdata", new { id = match.Key });

                    string whiteName = res["white"]["id"].ToString() == Preferences.Get("uid", "") ? userData["username"].ToString() : null;
                    string blackName = res["black"]["id"].ToString() == Preferences.Get("uid", "") ? userData["username"].ToString() : null;

                    string toSearch = whiteName == null ? res["white"]["id"].ToString() : res["black"]["id"].ToString();

                    if (!userCache.ContainsKey(toSearch))
                    {
                        JsonObject userDataRes = await client.MakeHTTPReq("/get_profile", new { uid = toSearch });

                        userCache.Add(toSearch, userDataRes["username"].ToString());
                    }

                    res["playedAs"] = whiteName == null ? "black" : "white";

                    whiteName ??= userCache[toSearch];
                    blackName ??= userCache[toSearch];

                    string resultText = "";

                    int resultColor;

                    if (res["winner"].ToString() == "d")
                    {
                        resultText = "Drawn";
                        resultColor = Resource.Color.warning;
                    }
                    else if (res["winner"].ToString() == "w" && res["playedAs"].ToString() == "white" || res["winner"].ToString() == "b" && res["playedAs"].ToString() == "black")
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

                    res["resultColor"] = resultColor.ToString();
                    res["resultText"] = resultText;
                    res["whiteName"] = whiteName;
                    res["blackName"] = blackName;

                    matchesArr.Add(res);

                }
                catch /*(Exception ex)*/ {
                    // Toast.MakeText(this, ex.InnerException.Message, ToastLength.Short).Show();
                };
            }

            matchesArr.Reverse();

            RunOnUiThread(() =>
            {
                foreach (JsonObject match in matchesArr)
                {
                    string me = match["playedAs"].ToString();
                    string opp = me == "white" ? "black" : "white";

                    LinearLayout card = (LinearLayout)LayoutInflater.Inflate(Resource.Drawable.matchCard, null);

                    ((TextView)card.FindViewWithTag("player1Name")).Text = match[$"{me}Name"].ToString();
                    ((TextView)card.FindViewWithTag("player2Name")).Text = match[$"{opp}Name"].ToString();

                    ((TextView)card.FindViewWithTag("player1elo")).Text = $"{GetText(Resource.String.elo)}: {match[me]["elo"]}";
                    ((TextView)card.FindViewWithTag("player2elo")).Text = $"{GetText(Resource.String.elo)}: {match[opp]["elo"]}";


                    ((ImageView)card.FindViewWithTag("player1king")).SetImageDrawable(Resources.GetDrawable(me == "white" ? Resource.Drawable.kw : Resource.Drawable.kb, Theme));
                    ((ImageView)card.FindViewWithTag("player2king")).SetImageDrawable(Resources.GetDrawable(opp == "white" ? Resource.Drawable.kw : Resource.Drawable.kb, Theme));

                    ((TextView)card.FindViewWithTag("reason")).Text = match["resultText"].ToString();
                    ((TextView)card.FindViewWithTag("reason")).SetTextColor(Resources.GetColor(int.Parse(match["resultColor"].ToString()), Theme));


                    LinearLayout.LayoutParams p = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                    p.SetMargins(0, 20, 0, 20);

                    card.LayoutParameters = p;

                    matchesContent.AddView(card);
                }

                this.HideLoadingSpinner();
                matchesContent.Visibility = ViewStates.Visible;
            });
        }

        [Export("StartGameBtn")]
        public void StartGameBtn(View v)
        {
            Intent i = new Intent(this, typeof(WaitingForPlayer));

            StartActivity(i);
        }

        [Export("SignOutBtn")]
        public void SignOutBtn(View v)
        {
            FirebaseAuth auth = FirebaseAuth.GetInstance(FirebaseApp.Instance);
            auth.SignOut();
            Preferences.Clear();

            //SetResult(Android.App.Result.Ok);
            Finish();
        }

        [Export("OpenLbBtn")]
        public void OpenLbBtn(View v)
        {
            Intent intent = new Intent(this, typeof(Leaderboard));

            StartActivity(intent);
        }
    }
}