﻿using System;
using System.Collections.Generic;
using System.Linq;
using Alice.Models;
using Alice.Services;
using FFImageLoading.Forms.Touch;
using Firebase.Auth;
using Firebase.CloudMessaging;
using Firebase.InstanceID;
using Foundation;
using Microsoft.Practices.ObjectBuilder2;
using UIKit;
using UserNotifications;


namespace Alice.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate, IUNUserNotificationCenterDelegate, IMessagingDelegate
    {
        public event EventHandler<UserInfoEventArgs> NotificationReceived;

        // class-level declarations
        //public override UIWindow Window
        //{
        //    get;
        //    set;
        //}

        
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            

            CachedImageRenderer.Init();

            


            // Monitor token generation
            InstanceId.Notifications.ObserveTokenRefresh(TokenRefreshNotification);

            // Register your app for remote notifications.
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                // iOS 10 or later
                var authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;
                UNUserNotificationCenter.Current.RequestAuthorization(authOptions, (granted, error) =>
                {
                    System.Diagnostics.Debug.WriteLine(granted);
                });

                // For iOS 10 display notification (sent via APNS)
                UNUserNotificationCenter.Current.Delegate = this;

                // For iOS 10 data message (sent via FCM)
                Messaging.SharedInstance.RemoteMessageDelegate = this;
            }
            else
            {
                // iOS 9 or before
                var allNotificationTypes = UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound;
                var settings = UIUserNotificationSettings.GetSettingsForTypes(allNotificationTypes, null);
                UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
            }

            UIApplication.SharedApplication.RegisterForRemoteNotifications();
            Firebase.Analytics.App.Configure();

            

            try
            {
                var token = InstanceId.SharedInstance.Token;
                Console.WriteLine($"---->Token: {token}");
                ConnectToFCM();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }


            LoadApplication(new App());
            return base.FinishedLaunching(app, options);
        }
        

        public override void DidEnterBackground(UIApplication application)
        {
            // Use this method to release shared resources, save user data, invalidate timers and store the application state.
            // If your application supports background exection this method is called instead of WillTerminate when the user quits.
            Messaging.SharedInstance.Disconnect();
            System.Diagnostics.Debug.WriteLine("Disconnected from FCM");
        }

        public override void WillEnterForeground(UIApplication application)
        {
            ConnectToFCM(this.Window.RootViewController);
        }

        // To receive notifications in foregroung on iOS 9 and below.
        // To receive notifications in background in any iOS version
        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            // If you are receiving a notification message while your app is in the background,
            // this callback will not be fired 'till the user taps on the notification launching the application.

            // If you disable method swizzling, you'll need to call this method. 
            // This lets FCM track message delivery and analytics, which is performed
            // automatically with method swizzling enabled.
            //Messaging.GetInstance ().AppDidReceiveMessage (userInfo);
            if (NotificationReceived == null)
                return;

            var e = new UserInfoEventArgs { UserInfo = userInfo };
            NotificationReceived(this, e);

            if (application.ApplicationState == UIApplicationState.Active)
            {
                System.Diagnostics.Debug.WriteLine(userInfo);
                var aps_d = userInfo["aps"] as NSDictionary;
                var alert_d = aps_d["alert"] as NSDictionary;
                var body = alert_d["body"] as NSString;
                var title = alert_d["title"] as NSString;

                System.Diagnostics.Debug.WriteLine($"---> push title={title}, body{body}");
            }
        }

        // You'll need this method if you set "FirebaseAppDelegateProxyEnabled": NO in GoogleService-Info.plist
        //public override void RegisteredForRemoteNotifications (UIApplication application, NSData deviceToken)
        //{
        //	InstanceId.SharedInstance.SetApnsToken (deviceToken, ApnsTokenType.Sandbox);
        //}

        // To receive notifications in foreground on iOS 10 devices.
        [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
        public void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            if (NotificationReceived == null)
                return;

            var e = new UserInfoEventArgs { UserInfo = notification.Request.Content.UserInfo };
            NotificationReceived(this, e);

            var title = notification.Request.Content.Title;
            var body = notification.Request.Content.Body;
            
            System.Diagnostics.Debug.WriteLine($"---> push title={title}, body{body}" );
        }

        // Receive data message on iOS 10 devices.
        public void ApplicationReceivedRemoteMessage(RemoteMessage remoteMessage)
        {
            System.Diagnostics.Debug.WriteLine(remoteMessage.AppData);

            var name = remoteMessage.AppData["username"];
            var body = remoteMessage.AppData["message"];
            var photo = remoteMessage.AppData["photo"];
            var attach = remoteMessage.AppData["attach"];

            var message = new ChatMessage()
            {
                IsYourMessage = false,
                Text = body.ToString(),
                UserName = name.ToString(),
                UrlPhoto = photo.ToString(),
                AttachImg = attach.ToString()
            };

            var chatService = ViewModelLocator.Instance.Resolve(typeof(ChatService)) as IChatService;
            chatService.OnMessageReceived(message);
        }

        
        /// WORKAROUND
        #region Workaround for handling notifications in background for iOS 10

        [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
        public void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            if (NotificationReceived == null)
                return;

            var e = new UserInfoEventArgs { UserInfo = response.Notification.Request.Content.UserInfo };
            NotificationReceived(this, e);
            
            System.Diagnostics.Debug.WriteLine("--->get message " + response.Notification.Date);
        }

        #endregion
        /// END OF WORKAROUND
        

        void TokenRefreshNotification(object sender, NSNotificationEventArgs e)
        {
            // This method will be fired everytime a new token is generated, including the first
            // time. So if you need to retrieve the token as soon as it is available this is where that
            // should be done.
            //var refreshedToken = InstanceId.SharedInstance.Token;

            ConnectToFCM();

            // TODO: If necessary send token to application server.
        }

        public static void ConnectToFCM(UIViewController fromViewController)
        {
            Messaging.SharedInstance.Connect(error =>
            {
                if (error != null)
                {
                    System.Diagnostics.Debug.WriteLine("---> Unable to connect to FCM: " + error.LocalizedDescription);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("--->  Success! Connected to FCM");
                    System.Diagnostics.Debug.WriteLine($"Token: {InstanceId.SharedInstance.Token}");

                    //TODO: Change Topic to what is required
                    Messaging.SharedInstance.Subscribe("/topics/chat");
                }
            });
        }

        public static void ConnectToFCM()
        {
            Messaging.SharedInstance.Connect(error =>
            {
                if (error != null)
                {
                    System.Diagnostics.Debug.WriteLine("Unable to connect to FCM " + error.LocalizedDescription);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Success! Connected to FCM");
                    System.Diagnostics.Debug.WriteLine($"Token: {InstanceId.SharedInstance.Token}");

                    //TODO: Change Topic to what is required
                    Messaging.SharedInstance.Subscribe("/topics/chat");
                }
            });
        }
    }

    public class UserInfoEventArgs : EventArgs
    {
        public NSDictionary UserInfo { get; set; }
    }
}
