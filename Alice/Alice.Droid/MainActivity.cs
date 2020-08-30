using System;
using Android.App;
using Android.Content.PM;
using Android.Gms.Tasks;
using Android.Widget;
using Android.OS;
using Android.Runtime;
using Firebase.Iid;
using Firebase.Messaging;
using Firebase.Database;
using System.Collections.Generic;
using Alice.Models;
using FFImageLoading.Forms;
using GoogleGson;
using Java.Util;
using Newtonsoft.Json;
using FFImageLoading.Forms.Platform;
using Android;
using Android.Support.V4.App;
using Xamarin.Forms;
using Android.Content;
using Alice.Droid.Services;

namespace Alice.Droid
{
    [Activity(Label = "Alice", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity, IOnCompleteListener
    {
        
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);

            if (!(CheckPermissionGranted(Manifest.Permission.ReadExternalStorage) && !CheckPermissionGranted(Manifest.Permission.WriteExternalStorage)))
            {
                RequestPermission();
            }

            if(!CheckPermissionGranted(Manifest.Permission.RequestIgnoreBatteryOptimizations))
            {
                RequestBatteryPermission();
            }
            LoadApplication(new App());

            StartService(new Intent(Forms.Context, typeof(MyFirebaseMessagingService)));

            if (!GetString(Resource.String.google_app_id).Equals("1:1045837793898:android:a06be197fd59a1591c5813"))
                throw new SystemException("Invalid Json file");
            
            GetTokenFcm();

            CachedImageRenderer.Init(false);


        }

        private void RequestBatteryPermission()
        {
            ActivityCompat.RequestPermissions(this,new string[] { Manifest.Permission.RequestIgnoreBatteryOptimizations},0);
        }
        private void RequestPermission()
        {
            ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage }, 0);
        }

        public bool CheckPermissionGranted(string Permissions)
        {
            // Check if the permission is already available.
            if (ActivityCompat.CheckSelfPermission(this, Permissions) != Permission.Granted)
            {
                return false;
            }
            else
            {
                return true;
            }        
        }

        private void GetTokenFcm()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                var instancedId = FirebaseInstanceId.Instance;
                instancedId.DeleteInstanceId();

                System.Diagnostics.Debug.WriteLine($"---> t1= {instancedId.Token}");
                System.Diagnostics.Debug.WriteLine(
                    $"---> t2= {instancedId.GetToken(GetString(Resource.String.gcm_defaultSenderId), Firebase.Messaging.FirebaseMessaging.InstanceIdScope)}");


                // FirebaseSubscribeToTopic
                FirebaseMessaging.Instance.SubscribeToTopic("chat");
            });
        }
        



        protected override void OnStart()
        {
            base.OnStart();
            App.IsActive = true;
        }
        
        protected override void OnStop()
        {
            App.IsActive = false;
            base.OnStop();
        }

        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            if (task.IsSuccessful)
            {
                ShowToast("Login Success");
            }
            else
            {
                ShowToast($"Error {task.Exception} {task.Result}");
            }
        }



        private void ShowToast(string message)
        {
            Toast.MakeText(this, message, ToastLength.Long).Show();
        }
    }
}

