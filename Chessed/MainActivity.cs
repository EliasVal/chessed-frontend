using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Android.Content;
using Android.Widget;
using Java.Interop;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Xamarin.Essentials;
using Java.Util;
using System.Collections.Generic;
using Android.Content.PM;
using System.Text.Json.Nodes;

namespace Chessed
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.Locale, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity
    {
        EditText pass, user, email;
        Button actionBtn;
        TextView actionSwitch;

        CustomHttpClient client = new CustomHttpClient()
        {
            BaseAddress = new Uri("http://192.168.1.238:3000")
        };

        /// <summary>
        /// false = login
        /// <br>true = signup</br>
        /// </summary>
        bool mode = false;

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (Client.Instance.client.State == WebSocketState.Open)
            {
                Client.Instance.client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", default);
            }
        }

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                if (Preferences.Get("token", "") == "") throw new Exception();
                await client.MakeHTTPReq("/validate_token", new { token = Preferences.Get("token", "") });

                // On success
                RunOnUiThread(() =>
                {
                    Intent i = new Intent(this, typeof(MainMenu));
                    StartActivity(i);
                });
            }
            catch (Exception ex)
            {
                if (ex.Message == "401")
                {
                    Preferences.Set("token", "");
                    RunOnUiThread(() => Toast.MakeText(this, "RESET TOKEN", ToastLength.Short).Show());
                }

                RunOnUiThread(() => {
                    SetContentView(Resource.Layout.activity_main);
                    pass = FindViewById<EditText>(Resource.Id.passwordEt);
                    user = FindViewById<EditText>(Resource.Id.usernameEt);
                    email = FindViewById<EditText>(Resource.Id.emailEt);
                    actionBtn = FindViewById<Button>(Resource.Id.actionBtn);
                    actionSwitch = FindViewById<TextView>(Resource.Id.actionSwitch);

                    actionBtn.Click += Auth_Click;
                });
            }
        }

        private async void Auth_Click(object sender, EventArgs e)
        {
            actionBtn.Clickable = false;

            try
            {
                JsonObject res;

                if (mode) res = await client.MakeHTTPReq("/signup", new { username = user.Text, password = pass.Text, email = email.Text });
                else res = await client.MakeHTTPReq("/login", new { password = pass.Text, email = email.Text });

                Preferences.Set("token", res["token"].ToString());
                Preferences.Set("username", res["username"].ToString());
                Preferences.Set("elo", res["elo"].ToString());
                Preferences.Set("uid", res["uid"].ToString());

                RunOnUiThread(() =>
                {
                    Intent i = new Intent(this, typeof(MainMenu));
                    StartActivity(i);
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => Toast.MakeText(this, ex.InnerException.Message, ToastLength.Short).Show());
            }
            finally
            {
                actionBtn.Clickable = true;
            }
        }

        [Export("ActionSwitch")]
        public void ActionSwitch_Click(View view)
        {
            user.Visibility = mode ? ViewStates.Invisible : ViewStates.Invisible;
            actionBtn.Text = mode ? GetText(Resource.String.sign_in) : GetText(Resource.String.sign_up);
            actionSwitch.Text = mode ? GetText(Resource.String.new_user) : GetText(Resource.String.existing_user);

            mode = !mode;
        }
    }
}
