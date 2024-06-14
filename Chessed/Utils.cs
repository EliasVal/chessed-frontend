using Android.App;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;

namespace Chessed
{
    static class Utils
    {
        static bool isShown = false;

        static ImageView spinner;

        public static ViewGroup ShowLoadingSpinner(this Activity activity, ViewGroup parent, int width = 0, int height = 0)
        {
            ViewGroup spinnerContainer = (ViewGroup)activity.LayoutInflater.Inflate(Resource.Layout.spinner, null);
            spinner = (ImageView)spinnerContainer.GetChildAt(0);

            if (width == 0) parent.AddView(spinnerContainer);
            else parent.AddView(spinnerContainer, width, height);

            isShown = true;
            Spin(activity);

            return spinnerContainer;
        }

        public static void HideLoadingSpinner(this Activity activity)
        {
            isShown = false;
            ViewGroup spinnerContainer = activity.FindViewById<ViewGroup>(Resource.Id.loading_spinner);
            ((ViewGroup)spinnerContainer.Parent).RemoveView(spinnerContainer);
        }

        async static void Spin(Activity a)
        {
            while (isShown)
            {
                try
                {
                    a.RunOnUiThread(() => spinner.Animate().SetInterpolator(null).RotationBy(360).SetDuration(2500).Start());
                    await Task.Delay(2500);
                }
                catch
                {
                    isShown = false;
                }
            }
        }
    }
}