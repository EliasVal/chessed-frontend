using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chessed.src
{
    internal class Sounds
    {
        static MediaPlayer mp;

        static Dictionary<string, int> sounds = new Dictionary<string, int>()
        {
            { "check", Resource.Raw.check },
            { "castle", Resource.Raw.castle },
            { "capture", Resource.Raw.capture },
            { "promote", Resource.Raw.promote },
            { "move", Resource.Raw.move },
        };

        public static void PlaySound(Context c, string sound)
        {
            if (mp != null) mp.Release();
            mp = MediaPlayer.Create(c, sounds[sound]);
            mp.Start();
        }
    }
}