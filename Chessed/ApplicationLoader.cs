using Android.App;
using Android.Runtime;
using Firebase;

namespace Chessed
{
    [Application]
    class ApplicationLoader : Application
    {
        protected ApplicationLoader(System.IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {

        }

        public override void OnCreate()
        {
            if (FirebaseApp.Instance == null) FirebaseApp.InitializeApp(this);
            
            base.OnCreate();
        }


    }
}