using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace Com.Umeng.Message
{
    public partial class UmengDownloadResourceService : global::Android.App.Service
    {
        public partial class DownloadResourceTask : global::Android.OS.AsyncTask
        {
            protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params)
            {
                return A((Java.Lang.Void[])@params);
            }
        }
    }
}