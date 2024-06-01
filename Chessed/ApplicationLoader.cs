using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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