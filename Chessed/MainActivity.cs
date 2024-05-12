using System;
using Android.App;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using Android.Content;
using Android.Widget;
using Java.Interop;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Android.Content.PM;
using Firebase;
using Firebase.Auth;
using Android.Gms.Extensions;
using Android.Runtime;

namespace Chessed
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.Locale, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity
    {
        EditText pass, user, email;
        Button actionBtn;
        TextView actionSwitch;

        FirebaseAuth auth;

        CustomHttpClient client = new CustomHttpClient()
        {
            BaseAddress = new Uri("http://192.168.1.238:3000")
        };

        /// <summary>
        /// false = login
        /// <br>true = signup</br>
        /// </summary>
        bool mode = false;

        bool initialized = false;

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (Client.Instance.client.State == WebSocketState.Open)
            {
                Client.Instance.client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", default);
            }
        }

        void InitMenu()
        {
            SetContentView(Resource.Layout.activity_main);
            pass = FindViewById<EditText>(Resource.Id.passwordEt);
            user = FindViewById<EditText>(Resource.Id.usernameEt);
            email = FindViewById<EditText>(Resource.Id.emailEt);
            actionBtn = FindViewById<Button>(Resource.Id.actionBtn);
            actionSwitch = FindViewById<TextView>(Resource.Id.actionSwitch);

            actionBtn.Click += Auth_Click;

            initialized = true;
        }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (FirebaseApp.Instance == null) FirebaseApp.InitializeApp(this);

            auth = FirebaseAuth.GetInstance(FirebaseApp.Instance);

            if (auth.CurrentUser == null)
            {
                Preferences.Clear();
                RunOnUiThread(InitMenu);
            }
            else
            {
                Preferences.Set("uid", auth.CurrentUser.Uid);

                GetTokenResult t = await auth.CurrentUser.GetIdToken(true).AsAsync<GetTokenResult>();
                Preferences.Set("token", t.Token);

                RunOnUiThread(() =>
                {
                    Intent i = new Intent(this, typeof(Leaderboard));
                    //Intent i = new Intent(this, typeof(MainMenu));
                    StartActivityForResult(i, 0);
                });
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (FirebaseApp.Instance == null) return;

            if (initialized)
            {
                user.Text = "";
                pass.Text = "";
                email.Text = "";
            }
            else InitMenu();
        }

        private async Task SignIn()
        {
            try
            {
                IAuthResult res = await auth.SignInWithEmailAndPasswordAsync(email.Text, pass.Text);

                Preferences.Set("uid", res.User.Uid);

                GetTokenResult t = await res.User.GetIdToken(true).AsAsync<GetTokenResult>();

                Preferences.Set("token", t.Token);

                RunOnUiThread(() =>
                {
                    Intent i = new Intent(this, typeof(MainMenu));
                    StartActivity(i);
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => Toast.MakeText(this, ex.Message, ToastLength.Short).Show());
            }
        }

        private async Task SignUp()
        {
            try
            {
                await client.MakeHTTPReq("/signup", new { username = user.Text, password = pass.Text, email = email.Text });

                await SignIn();
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => Toast.MakeText(this, ex.InnerException.Message, ToastLength.Short).Show());
            }
        }

        private async void Auth_Click(object sender, EventArgs e)
        {
            RunOnUiThread(() => actionBtn.Clickable = false);

            if (mode) await SignUp();
            else await SignIn();

            RunOnUiThread(() => actionBtn.Clickable = true);
        }

        [Export("ActionSwitch")]
        public void ActionSwitch_Click(View view)
        {
            user.Visibility = mode ? ViewStates.Invisible : ViewStates.Visible;
            actionBtn.Text = mode ? GetText(Resource.String.sign_in) : GetText(Resource.String.sign_up);
            actionSwitch.Text = mode ? GetText(Resource.String.new_user) : GetText(Resource.String.existing_user);

            mode = !mode;
        }
    }
}
