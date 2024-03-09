using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Chessed
{
    [Activity(Label = "main_menu", ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainMenu : Activity
    {
        Dictionary<string, string> userData = null;

        HttpClient client = new HttpClient()
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
            using StringContent stringContent = new StringContent(JsonSerializer.Serialize(new { uid = Preferences.Get("uid", "") }), Encoding.UTF8, "application/json");
            HttpResponseMessage res = await client.PostAsync("/get_profile", stringContent);
            string t = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                userData = JsonSerializer.Deserialize<Dictionary<string, string>>(await res.Content.ReadAsStringAsync());

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
            else
            {
                Toast.MakeText(this, "Failed to fetch user data, please try again later", ToastLength.Short);
                Preferences.Clear();
                Finish();
            }
        }

        [Export("StartGameBtn")]
        public void StartGameBtn(View v)
        {
            Intent i = new Intent(this, typeof(WaitingForPlayer));

            StartActivity(i);
        }
    }
}