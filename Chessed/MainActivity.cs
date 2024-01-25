using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Firebase;
using Firebase.Auth;
using Android.Content;
using Android.Widget;
using Java.Interop;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Threading;

namespace Chessed
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {

        private FirebaseAuth mAuth;
        private FirebaseApp mInstance;

        EditText pass, user, email;
        Button actionBtn;
        TextView actionSwitch;

        ClientWebSocket client = Client.Instance.client;

        /// <summary>
        /// false = login
        /// <br>true = signup</br>
        /// </summary>
        bool mode = false;

        CancellationTokenSource cts = new CancellationTokenSource();

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            pass = FindViewById<EditText>(Resource.Id.passwordEt);
            user = FindViewById<EditText>(Resource.Id.usernameEt);
            email = FindViewById<EditText>(Resource.Id.emailEt);
            actionBtn = FindViewById<Button>(Resource.Id.actionBtn);
            actionSwitch = FindViewById<TextView>(Resource.Id.actionSwitch);

            //FirebaseApp.InitializeApp(this);
            //mInstance = FirebaseApp.GetInstance("[DEFAULT]");
            //mAuth = FirebaseAuth.GetInstance(mInstance);

            //signUp.Click += SignUp_Click;


            if (client.State != WebSocketState.Open && client.State != WebSocketState.Connecting)
                await client.ConnectAsync(new Uri("ws://192.168.1.238:8080"), cts.Token);

            //await Task.Run(async () => await ReadMessage());
            Intent i = new Intent(this, typeof(GameScreen));
            StartActivity(i);
        }

        private async void SignUp_Click(object sender, EventArgs e)
        {
            IAuthResult res = await mAuth.CreateUserWithEmailAndPasswordAsync(email.Text, pass.Text);

            Toast.MakeText(this, res.Credential.ToString(), ToastLength.Short).Show();
        }

        [Export("ActionSwitch")]
        public void ActionSwitch_Click(View view)
        {
            if (mode)
            {
                user.Visibility = ViewStates.Visible;
                actionBtn.Text = "Sign Up";
                actionSwitch.Text = "Already a user? Sign in";
            }
            else {
                user.Visibility = ViewStates.Invisible;
                actionBtn.Text = "Sign In";
                actionSwitch.Text = "Not a user? Sign up";
            }

            mode = !mode;
        }
    }
}
