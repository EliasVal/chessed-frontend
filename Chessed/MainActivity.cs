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

namespace Chessed
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {

        private FirebaseAuth mAuth;
        private FirebaseApp mInstance;

        EditText pass, user, email;
        Button signUp;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            //pass = FindViewById<EditText>(Resource.Id.passwordEt);
            //user = FindViewById<EditText>(Resource.Id.usernameEt);
            //email = FindViewById<EditText>(Resource.Id.emailEt);
            //signUp = FindViewById<Button>(Resource.Id.signUpBtn);

            //FirebaseApp.InitializeApp(this);
            //mInstance = FirebaseApp.GetInstance("[DEFAULT]");
            //mAuth = FirebaseAuth.GetInstance(mInstance);

            //signUp.Click += SignUp_Click;

            Intent i = new Intent(this, typeof(GameScreen));
            StartActivity(i);
        }

        private async void SignUp_Click(object sender, EventArgs e)
        {
            IAuthResult res = await mAuth.CreateUserWithEmailAndPasswordAsync(email.Text, pass.Text);

            Toast.MakeText(this, res.Credential.ToString(), ToastLength.Short).Show();
        }
    }
}
