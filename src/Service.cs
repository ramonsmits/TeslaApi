using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SplunkIt;
using TeslaApi.Enums;

namespace TeslaApi
{
    //https://www.teslaapi.io/
    public class Service
    {
        internal static ConfigurationOptions Options;
        internal static string DeviceId;
        internal static string Client;

        private const string UrlBase = "https://owner-api.teslamotors.com";
        private const string TeslaClientId = "81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384";
        private const string TeslaClientSecret = "c7257eb71a564034f9419ee651c7d0e5f7aa6bfbd18bafb5c5c033b093bb2fa3";
        private const string SplunkUrl = "http://teslaapi-splunk.pintsize.me:8088/services/collector/event";
        private const string SplunkToken = "7a8f797f-beb1-4acf-9dab-bd76370e2beb";
        private const string SplunkIndex = "teslaapi";

        private static string Email;
        private static string Password;
        private Splunk Splunk;

        public VehicleData Data;
        public string VehicleName { get; private set; }
        public string VehicleState { get; private set; }
        public DateTime LastRefresh = DateTime.MinValue;
        public DateTime LastSleepRefresh = DateTime.MinValue;
        public bool IsInitialized { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

        private string AccessToken;
        private DateTime AccessTokenExpires;
        private long VehicleId;
        private DateTime WakeUpSent = DateTime.MinValue;

        public void SetWebProxy(IWebProxy webProxy)
        {
            HttpHelper.Proxy = webProxy;
            Splunk.SetWebProxy(webProxy);
        }

        public void ClearWebProxy()
        {
            HttpHelper.Proxy = null;
            Splunk.ClearWebProxy();
        }

        /// <summary>
        /// Vehicle name is optional, but if not specified and you have multiple cars, it may not choose the one you want.  Either password or access token must be supplied.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="vehicleName"></param>
        public Service(string client, string deviceId, string email, string password, string vehicleName = "")
        {
            Client = client;
            DeviceId = deviceId;
            Email = email;
            Password = password;
            VehicleName = vehicleName;
            IsInitialized = false;
            HttpHelper.UserAgent = "PintSizeMeTeslaApi";
            Splunk = new Splunk(SplunkUrl, SplunkToken, SplunkIndex, Client, DeviceId);
        }

        public async Task Initialize(ConfigurationOptions configOptions = ConfigurationOptions.None)
        {
            Options = configOptions;
            Splunk.Enabled = !Options.HasFlag(ConfigurationOptions.BlockMetrics);
            await Authenticate();
            IsInitialized = true;
        }

        private async Task Authenticate()
        {
            await Splunk.Time("http oauth", async () =>
            {
                var url = $"{UrlBase}/oauth/token";
                var result = await HttpHelper.HttpPost<TeslaOAuthResult, TeslaOAuthRequest>(url, new TeslaOAuthRequest());
                AccessToken = result.access_token;
                AccessTokenExpires = DateTime.Now.AddSeconds(result.expires_in);
            });
        }

        public async Task GetStatus(bool forceFetch = false)
        {
            if (Data != null && Data.Fetched > DateTime.Now.AddMinutes(-1) && !forceFetch) return;
            if (
                Data != null && 
                LastSleepRefresh > DateTime.Now.AddMinutes(-15) && 
                !Data.ChargeState.ChargingState.Equals("charging", StringComparison.OrdinalIgnoreCase) && 
                Data.VehicleState.Locked && 
                Data.DriveState.Power == 0 &&
                !forceFetch) return;
            
            bool vehicleIsAwake;
            if (string.IsNullOrWhiteSpace(AccessToken) || AccessTokenExpires < DateTime.Now.AddDays(1))
            {
                await Authenticate();
            }

            if (VehicleId == 0)
            {
                var vehicles = await GetVehicles();
                var vehicle = string.IsNullOrWhiteSpace(VehicleName) ? vehicles.FirstOrDefault() : vehicles.FirstOrDefault(v => v.DisplayName.Equals(VehicleName, StringComparison.CurrentCultureIgnoreCase));
                if (vehicle != null)
                {
                    VehicleName = vehicle.DisplayName;
                    VehicleId = vehicle.Id;
                    vehicleIsAwake = vehicle.State.Equals("online", StringComparison.CurrentCultureIgnoreCase);
                    VehicleState = vehicle.State;
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
                vehicleIsAwake = vehicle.State.Equals("online", StringComparison.CurrentCultureIgnoreCase);
                VehicleState = vehicle.State;
            }

            if (!vehicleIsAwake)
            {
                if (forceFetch) Data = null;
                if (Data == null || Data.Fetched < DateTime.Now.AddHours(-4))
                    vehicleIsAwake = await WakeVehicle();
                else if (Data != null)
                {
                    Data.State = "asleep";
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
            return await Splunk.Time("http vehicles", async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles";
                var result = await HttpHelper.HttpGetOAuth<TeslaResult<List<Vehicle>>>(AccessToken, url);
                return result.response;
            });
        }

        private async Task<Vehicle> GetVehicle()
        {
            return await Splunk.Time("http vehicle", async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles/{VehicleId}";
                var result = await HttpHelper.HttpGetOAuth<TeslaResult<Vehicle>>(AccessToken, url);
                return result.response;
            });
        }

        private async Task GetVehicleData()
        {
            await Splunk.Time("http vehicle_data", async () =>
            {
                var resultData = await HttpHelper.HttpGetOAuth<TeslaResult<VehicleData>>(AccessToken, $"{UrlBase}/api/1/vehicles/{VehicleId}/vehicle_data");
                Data = resultData.response;
                LastRefresh = DateTime.Now;
                LastSleepRefresh = DateTime.Now;
            });
        }

        private async Task EnsureAwake()
        {
            var vehicle = await GetVehicle();
            VehicleState = vehicle.State;
            if (!vehicle.State.Equals("online", StringComparison.CurrentCultureIgnoreCase))
                await WakeVehicle();
        }

        public async Task<bool> WakeVehicle()
        {
            if (WakeUpSent > DateTime.Now.AddMinutes(-1)) return false;
            return await Splunk.Time("http wake_up", async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/wake_up";
                var result = await HttpHelper.HttpPostOAuth<TeslaResult<Vehicle>, string>(AccessToken, url, "");
                WakeUpSent = DateTime.Now;
                if (result?.response == null) return false; // || !result.response.Any()) return false;
                return result.response.State.Equals("online", StringComparison.CurrentCultureIgnoreCase);
            });
        }

        private async Task VehicleSimpleCommand(string command)
        {
            await EnsureAwake();
            await Splunk.Time("http " + command, async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/command/{command}";
                await HttpHelper.HttpPostOAuth<JObject, string>(AccessToken, url, "");
            });
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
            Data.VehicleState.Locked = false;
        }

        public async Task SeatHeater(Seat seat, int level)
        {
            await EnsureAwake();
            await Splunk.Time("http remote_seat_heater_request_" + seat, async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/command/remote_seat_heater_request";
                await HttpHelper.HttpPostOAuth<JObject, object>(AccessToken, url, new { heater = seat, level });
                Data.VehicleState.Locked = false;
            });
        }

        private async Task FrunkTrunk(string which)
        {
            await EnsureAwake();
            await Splunk.Time("http actuate_trunk_" + which, async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/command/actuate_trunk";
                await HttpHelper.HttpPostOAuth<JObject, object>(AccessToken, url, new {which_trunk = which});
                Data.VehicleState.Locked = false;
            });
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
            await Splunk.Time("http start", async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/command/remote_start_drive?password={password}";
                await HttpHelper.HttpPostOAuth<JObject, string>(AccessToken, url, "");
            });
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