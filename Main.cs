using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace EcoBand
{
    [Activity (Label = "EcoBand", MainLauncher = true)]
    public class Main : Activity
    {
        int count = 1;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

			// LayoutInflater inflater = Application.Context.GetSystemService(Context.LayoutInflaterService) as LayoutInflater;
			// View layout = inflater.Inflate(Resource.Layout.Main, null);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button> (Resource.Id.btnConnect);
            
            button.Click += delegate {
                button.Text = string.Format("Thanks! {0} clicks.", count++); };
        }
    }
}


