﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TeslaApi
{
    //https://www.teslaapi.io/
    public class Service
    {
        private const string UrlBase = "https://owner-api.teslamotors.com";
        private const string TeslaClientId = "81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384";
        private const string TeslaClientSecret = "c7257eb71a564034f9419ee651c7d0e5f7aa6bfbd18bafb5c5c033b093bb2fa3";
        private static string Email;
        private static string Password;

        public VehicleData Data;
        public string VehicleName { get; private set; }
        public string VehicleState { get; private set; }
        public DateTime LastRefresh = DateTime.MinValue;
        public DateTime LastSleepRefresh = DateTime.MinValue;
        public bool IsInitialized { get; private set; }

        private ConfigurationOptions Options;
        private string AccessToken;
        private DateTime AccessTokenExpires;
        private long VehicleId;
        private DateTime WakeUpSent = DateTime.MinValue;

        public void SetWebProxy(IWebProxy webProxy)
        {
            HttpHelper.Proxy = webProxy;
        }

        public void ClearWebProxy()
        {
            HttpHelper.Proxy = null;
        }

        /// <summary>
        /// Vehicle name is optional, but if not specified and you have multiple cars, it may not choose the one you want.  Either password or access token must be supplied.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="vehicleName"></param>
        public Service(string email, string password, string vehicleName = "")
        {
            Email = email;
            Password = password;
            VehicleName = vehicleName;
            IsInitialized = false;
            HttpHelper.UserAgent = "PintSize.Me TeslaApi";
        }

        public async Task Initialize(ConfigurationOptions configOptions = ConfigurationOptions.None)
        {
            Options = configOptions;
            await Authenticate();
            IsInitialized = true;
        }

        private async Task Authenticate()
        {
            var url = $"{UrlBase}/oauth/token";
            var result = await HttpHelper.HttpPost<TeslaOAuthResult, TeslaOAuthRequest>(url, new TeslaOAuthRequest());
            AccessToken = result.access_token;
            AccessTokenExpires = DateTime.Now.AddSeconds(result.expires_in);
        }

        public async Task GetStatus(bool forceFetch = false)
        {
            if (Data != null && Data.Fetched > DateTime.Now.AddMinutes(-1) && !forceFetch) return;
            if (
                Data != null && 
                LastSleepRefresh > DateTime.Now.AddMinutes(-15) && 
                !Data.charge_state.charging_state.Equals("charging", StringComparison.OrdinalIgnoreCase) && 
                Data.vehicle_state.locked && 
                Data.drive_state.power == 0 &&
                !forceFetch) return;
            
            bool vehicleIsAwake;
            if (string.IsNullOrWhiteSpace(AccessToken) || AccessTokenExpires < DateTime.Now.AddDays(1))
            {
                await Authenticate();
            }

            if (VehicleId == 0)
            {
                var vehicles = await GetVehicles();
                var vehicle = string.IsNullOrWhiteSpace(VehicleName) ? vehicles.FirstOrDefault() : vehicles.FirstOrDefault(v => v.display_name.Equals(VehicleName, StringComparison.CurrentCultureIgnoreCase));
                if (vehicle != null)
                {
                    VehicleName = vehicle.display_name;
                    VehicleId = vehicle.id;
                    vehicleIsAwake = vehicle.state.Equals("online", StringComparison.CurrentCultureIgnoreCase);
                    VehicleState = vehicle.state;
                    LastSleepRefresh = DateTime.Now;
                }
                else
                {
                    VehicleName = null;
                    return;
                }
            }
            else
            {
                var vehicle = await GetVehicle();
                vehicleIsAwake = vehicle.state.Equals("online", StringComparison.CurrentCultureIgnoreCase);
                VehicleState = vehicle.state;
            }

            if (!vehicleIsAwake)
            {
                if (Data == null || Data.Fetched < DateTime.Now.AddHours(-4) || forceFetch)
                    vehicleIsAwake = await WakeVehicle();
                else if (Data != null)
                {
                    Data.state = "asleep";
                    LastSleepRefresh = DateTime.Now;
                }
            }

            if (vehicleIsAwake)
            {
                await GetVehicleData();
                LastRefresh = DateTime.Now;
                LastSleepRefresh = DateTime.Now;
            }
        }

        public async Task<List<Vehicle>> GetVehicles()
        {
            var url = $"{UrlBase}/api/1/vehicles";
            var result = await HttpHelper.HttpGetOAuth<TeslaResult<List<Vehicle>>>(AccessToken, url);
            return result.response;
        }

        private async Task<Vehicle> GetVehicle()
        {
            var url = $"{UrlBase}/api/1/vehicles/{VehicleId}";
            var result = await HttpHelper.HttpGetOAuth<TeslaResult<Vehicle>>(AccessToken, url);
            return result.response;
        }

        private async Task GetVehicleData()
        {
            var resultData = await HttpHelper.HttpGetOAuth<TeslaResult<VehicleData>>(AccessToken, $"{UrlBase}/api/1/vehicles/{VehicleId}/vehicle_data");
            Data = resultData.response;
            LastRefresh = DateTime.Now;
            LastSleepRefresh = DateTime.Now;
        }

        private async Task EnsureAwake()
        {
            var vehicle = await GetVehicle();
            VehicleState = vehicle.state;
            if (!vehicle.state.Equals("online", StringComparison.CurrentCultureIgnoreCase))
                await WakeVehicle();
        }

        private async Task<bool> WakeVehicle()
        {
            if (WakeUpSent > DateTime.Now.AddMinutes(-1)) return false;
            var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/wake_up";
            var result = await HttpHelper.HttpPostOAuth<TeslaResult<Vehicle>, string>(AccessToken, url, "");
            WakeUpSent = DateTime.Now;
            if (result?.response == null) return false; // || !result.response.Any()) return false;
            return result.response.state.Equals("online", StringComparison.CurrentCultureIgnoreCase);
        }

        private async Task VehicleSimpleCommand(string command)
        {
            await EnsureAwake();
            var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/command/{command}";
            await HttpHelper.HttpPostOAuth<JObject, string>(AccessToken, url, "");
        }

        public async Task ChargePortOpen()
        {
            await VehicleSimpleCommand("charge_port_door_open");
        }

        public async Task ChargePortClose()
        {
            await VehicleSimpleCommand("charge_port_door_close");
        }


        public async Task HvacStart()
        {
            await VehicleSimpleCommand("auto_conditioning_start");
        }

        public async Task HvacStop()
        {
            await VehicleSimpleCommand("auto_conditioning_stop");
        }

        public async Task Lock()
        {
            await VehicleSimpleCommand("door_lock");
        }

        public async Task Unlock()
        {
            await VehicleSimpleCommand("door_unlock");
            Data.vehicle_state.locked = false;
        }

        private async Task FrunkTrunk(string which)
        {
            await EnsureAwake();
            var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/command/actuate_trunk";
            await HttpHelper.HttpPostOAuth<JObject, TrunkOption>(AccessToken, url, new TrunkOption(which));
            Data.vehicle_state.locked = false;
        }

        public async Task Trunk()
        {
            await FrunkTrunk("rear");
        }

        public async Task Frunk()
        {
            await FrunkTrunk("front");
        }

        public async Task Start(string password = "")
        {
            if (string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(Password) && Options.HasFlag(ConfigurationOptions.RemoteStartWithoutPassword)) password = Password;
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("A password is required to start.", password);
            await EnsureAwake();
            var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/command/remote_start_drive?password={password}";
            await HttpHelper.HttpPostOAuth<JObject, string>(AccessToken, url, "");
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public class TeslaOAuthRequest
        {
            public string grant_type => "password";
            public string client_id => TeslaClientId;
            public string client_secret => TeslaClientSecret;
            public string email => Email;
            public string password => Password;
        }

        public class TeslaOAuthResult
        {
            public string access_token { get; set; }
            //public string token_type { get; set; }
            public long expires_in { get; set; }
            //public string refresh_token { get; set; }
            //public long created_at{ get; set; }
        }

        public class TeslaResult<T>
        {
            public T response;
        }
    }
}