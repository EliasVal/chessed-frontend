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

namespace Chessed
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = Android.Content.PM.ConfigChanges.Locale)]
    public class MainActivity : AppCompatActivity
    {
        EditText pass, user, email;
        Button actionBtn;
        TextView actionSwitch;

        HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri("http://192.168.1.238:3000")
        };

        /// <summary>
        /// false = login
        /// <br>true = signup</br>
        /// </summary>
        bool mode = false;

        CancellationTokenSource cts = new CancellationTokenSource();

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

            //Preferences.Clear();

            bool validToken = true;
            if (Preferences.Get("token", "") == "") validToken = false;

            if (validToken)
            {
                using StringContent stringContent = new StringContent(JsonSerializer.Serialize(new { token = Preferences.Get("token", "") }), Encoding.UTF8, "application/json");
                using HttpResponseMessage res = await client.PostAsync("validate_token", stringContent);

                if (res.StatusCode != HttpStatusCode.OK)
                {
                    if (res.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Preferences.Set("token", "");
                        RunOnUiThread(() => Toast.MakeText(this, "RESET TOKEN", ToastLength.Short).Show());
                    }
                    validToken = false;
                }
            }

            if (validToken)
            {
                RunOnUiThread(() =>
                {
                    Intent i = new Intent(this, typeof(WaitingForPlayer));
                    StartActivity(i);
                });
            }
            else
            {
                RunOnUiThread(() => {
                    SetContentView(Resource.Layout.activity_main);
                    pass = FindViewById<EditText>(Resource.Id.passwordEt);
                    user = FindViewById<EditText>(Resource.Id.usernameEt);
                    email = FindViewById<EditText>(Resource.Id.emailEt);
                    actionBtn = FindViewById<Button>(Resource.Id.actionBtn);
                    actionSwitch = FindViewById<TextView>(Resource.Id.actionSwitch);

                    actionBtn.Click += SignIn_Click;
                });
            }
        }

        private async void SignUp_Click(object sender, EventArgs e)
        {
            actionBtn.Clickable = false;
            using StringContent stringContent = new StringContent(JsonSerializer.Serialize(new { username = user.Text, password = pass.Text, email = email.Text }), Encoding.UTF8, "application/json");
            using HttpResponseMessage res = await client.PostAsync("signup", stringContent);

            Dictionary<string, string> stringRes = JsonSerializer.Deserialize<Dictionary<string, string>>(await res.Content.ReadAsStringAsync());

            if (res.StatusCode == HttpStatusCode.OK)
            {
                Preferences.Set("token", stringRes["token"]);
                Preferences.Set("username", user.Text);
                Preferences.Set("elo", "100");

                RunOnUiThread(() =>
                {
                    Intent i = new Intent(this, typeof(WaitingForPlayer));
                    StartActivity(i);
                });
            }
            else
            {
                RunOnUiThread(() => Toast.MakeText(this, stringRes["error"], ToastLength.Short).Show());
            }
            actionBtn.Clickable = true;
        }

        private async void SignIn_Click(object sender, EventArgs e)
        {
            actionBtn.Clickable = false;

            using StringContent stringContent = new StringContent(JsonSerializer.Serialize(new { password = pass.Text, email = email.Text }), Encoding.UTF8, "application/json");
            using HttpResponseMessage res = await client.PostAsync("login", stringContent);

            Dictionary<string, string> stringRes = JsonSerializer.Deserialize<Dictionary<string, string>>(await res.Content.ReadAsStringAsync());

            if (res.StatusCode == HttpStatusCode.OK)
            {
                Preferences.Set("token", stringRes["token"]);
                Preferences.Set("username", stringRes["username"]);
                Preferences.Set("elo", stringRes["elo"]);

                RunOnUiThread(() =>
                {
                    Intent i = new Intent(this, typeof(WaitingForPlayer));
                    StartActivity(i);
                });
            }
            else
            {
                RunOnUiThread(() => Toast.MakeText(this, stringRes["error"], ToastLength.Short).Show());
            }

            actionBtn.Clickable = true;
        }

        [Export("ActionSwitch")]
        public void ActionSwitch_Click(View view)
        {
            if (mode)
            {
                user.Visibility = ViewStates.Invisible;
                actionBtn.Text = GetText(Resource.String.sign_in);
                actionSwitch.Text = GetText(Resource.String.new_user);

                actionBtn.Click += SignIn_Click;
                actionBtn.Click -= SignUp_Click;
            }
            else {
                user.Visibility = ViewStates.Visible;
                actionBtn.Text = GetText(Resource.String.sign_up);
                actionSwitch.Text = GetText(Resource.String.existing_user);

                actionBtn.Click += SignUp_Click;
                actionBtn.Click -= SignIn_Click;


            }

            mode = !mode;
        }
    }
}
