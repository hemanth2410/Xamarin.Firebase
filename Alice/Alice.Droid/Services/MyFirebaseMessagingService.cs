using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alice.Models;
using Alice.Services;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Firebase.Messaging;
using Xamarin.Forms;

namespace Alice.Droid.Services
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    class MyFirebaseMessagingService : FirebaseMessagingService
    {
        // id message, if you remove messages will be replaced by new ones
        private static int idPush = 1;
        private Context mContext;
        private NotificationCompat.Builder notificationBuilder;
        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            try
            {
                

                if (message.Data.Count > 0)
                {
                    long l = 0;
                    if (message.Data["date_message"] != null)
                    {
                        var str = message.Data["date_message"];
                        int result = 0;
                        Int32.TryParse(str, out result);
                        l = result;
                    }

                    var messagePush = new ChatMessage()
                    {
                        IsYourMessage = false,
                        UserName = message.Data["username"],
                        Text = message.Data["message"],
                        UrlPhoto = message.Data["photo"],
                        AttachImg = message.Data["attach"],
                        DateMessageTimeSpan = l,
                    };

                    SendNotification(messagePush);
                }
                else
                {
                    if (message.GetNotification() != null)
                    {
                        var messageAdmin = new ChatMessage()
                        {
                            IsYourMessage = false,
                            UserName = "Admin",
                            Text = message.GetNotification().Body,
                        };

                        SendNotification(messageAdmin);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void SendNotification(ChatMessage message)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);

            var defaultSoundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);
            notificationBuilder = new NotificationCompat.Builder(this)
                .SetSmallIcon(Resource.Drawable.icon)   // Display this icon
                .SetContentTitle(message.UserName)      // Set its title
                .SetContentText(message.Text)           // The message to display.
                .SetAutoCancel(true)                    // Dismiss from the notif. area when clicked
                .SetSound(defaultSoundUri)              // Sound of message
                .SetContentIntent(pendingIntent)
                .SetChannelId("10023")
                .SetPriority(1)
                .SetVisibility((int)NotificationVisibility.Public)
                .SetVibrate(new long[0]);

            if (!App.IsActive)
            {
                var alarmAttributes = new AudioAttributes.Builder().SetContentType(AudioContentType.Sonification).SetUsage(AudioUsageKind.Notification).Build();
                NotificationManager notificationManager = mContext.GetSystemService(Context.NotificationService) as NotificationManager;
                if(global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                {
                    NotificationImportance importance = global::Android.App.NotificationImportance.High;
                    NotificationChannel notificationChannels = new NotificationChannel("10023", message.UserName, importance);
                    notificationChannels.EnableLights(true);
                    notificationChannels.EnableVibration(true);
                    notificationChannels.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification), alarmAttributes);
                    notificationChannels.Importance = NotificationImportance.Max;
                    notificationChannels.SetVibrationPattern(new long[] { 100, 100, 100, 300, 300, 100, 100, 100 });
                    if(notificationManager != null)
                    {
                        notificationBuilder.SetChannelId("10023");
                        notificationManager.CreateNotificationChannel(notificationChannels);

                    }
                    notificationManager.Notify(0, notificationBuilder.Build());
                }
                else
                {
                    var notificationManager1 = NotificationManager.FromContext(this);
                    notificationManager1.Notify(idPush++, notificationBuilder.Build());
                }
                //remove after debug
            }
            else
            {
                var chatService = ViewModelLocator.Instance.Resolve(typeof(ChatService)) as IChatService;
                chatService.OnMessageReceived(message);
            }

            
        }
    }
}