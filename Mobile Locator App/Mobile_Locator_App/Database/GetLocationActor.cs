﻿using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Android.Locations;
using Android.Util;
using Android.OS;
using Android.Runtime;
using Android.App;
using Android.Content;
using Mobile_Locator_App.Droid;
using System.Linq;
using Xamarin.Forms;


namespace Mobile_Locator_App.Code
{
    [Activity(Label = "CurrentLocation", MainLauncher = true, Icon = "@drawable/icon")]
    public class GetLocationActor : UntypedActor
    {
        


        private readonly IActorRef _getLocationActor;

        public IntPtr Handle => throw new NotImplementedException();

        public GetLocationActor(IActorRef getLocationActor, Context mContext)
        {
            Console.WriteLine("*******************************************GetLocationActor");
            Console.WriteLine("//////////////Actor getLocation: " + Self);
            if (!Code.User.CheckInternetConnection())
            {
                throw new Exception();
            }
            getLocation GetLocation = new getLocation(mContext);
            GetLocation.findLocation();

            
        }


        

        protected override void OnReceive(object message)
        {
            Console.WriteLine("**********************************OnRecieve getLocationActor");
        }

        internal class Initialise
        {
            private Activity activity;

            public Initialise(Activity activity)
            {
                this.activity = activity;
                Console.WriteLine("*******************************************GetLocationActor Initiliase");
                getLocation GetLocation = new getLocation(activity);
                GetLocation.findLocation();
            }
        }
    }

    // an object cannot inherit from both Untyped Actor and Java.Lang.Object at the same time, hence the need for two classes
    // one to use a seperate thread and another that will run on the seperate thread to get the location of the user
    public class getLocation : Java.Lang.Object, ILocationListener
    {
        Location currentLocation;
        LocationManager locationManager;
        private readonly Context mContext;
        string locationProvider;
        


        //public IntPtr Handle => throw new NotImplementedException();



        public getLocation(Context mContext)
        {
            
            this.mContext = mContext;
            InitialiseLocationManager();
        }

        private void InitialiseLocationManager()
        {
            locationManager = (LocationManager)Android.App.Application.Context.GetSystemService(Context.LocationService);
            Criteria criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };

            IList<string> acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService, true);

            if(acceptableLocationProviders.Any())
            {
                locationProvider = acceptableLocationProviders.First();
            }
            else
            {
                locationProvider = string.Empty;
            }

            //locationManager = (LocationManager)MainActivity.activity.GetSystemService(Context.LocationService); 
            Console.WriteLine("***************************************** .Location Provider = " + locationProvider);
        }

        public void findLocation()
        {
            locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);
            
            
        }

        public void Stop()
        {
            locationManager.RemoveUpdates(this);
        }
        

        public void OnLocationChanged(Location location)
        {
            string latitude = location.Latitude.ToString();
            string longitude = location.Longitude.ToString();
            string[] locationValues = { longitude, latitude };
            // If the location is found to be changed by some magical genie who keeps track of all this then
            // this method will run, then the longitude and latitude of the newly changed location data will be stored
            // in a string array which will then be passed stored on the server.
            if (string.IsNullOrEmpty(latitude) || string.IsNullOrEmpty(longitude))
            {

                MessagingCenter.Send<getLocation, string[]>(this, "Mobius", locationValues);
            }
            else
            {
                try
                { 
                    Database.DBSupervisor.RedisDB.StringSet(User.Username + "Longtitude", longitude);
                    Database.DBSupervisor.RedisDB.StringSet(User.Username + "Latitude", latitude);
                }
                catch(Exception e)
                {
                    Console.WriteLine("********************* Location Exception" + e);
                }
                User.Latitude = latitude;
                User.Longitude = longitude;
                MessagingCenter.Send<getLocation, string[]>(this, "gotLocation", locationValues);
            }
            
            
            
            
        }




        public void OnProviderDisabled(string provider)
        {
            Console.WriteLine("***OnProviderDisabled");
        }

        public void OnProviderEnabled(string provider)
        {
            Console.WriteLine("***OnProviderEnabled");
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            Console.WriteLine("***OnStatusChanged");
        }

        public void Dispose()
        {
            Console.WriteLine("***Dispose"); ;
        }

    }
}
