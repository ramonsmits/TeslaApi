using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TeslaApi.Enums;

namespace TeslaApi
{
    //https://www.teslaapi.io/
    public class Service
    {
        internal static ConfigurationOptions Options;

        private const string UrlBase = "https://owner-api.teslamotors.com";
        private const string TeslaClientId = "81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384";
        private const string TeslaClientSecret = "c7257eb71a564034f9419ee651c7d0e5f7aa6bfbd18bafb5c5c033b093bb2fa3";

        private static string Email;
        private static string Password;
        private ITelemetryProvider TelemetryProvider;

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
            TelemetryProvider?.SetWebProxy(webProxy);
        }

        public void ClearWebProxy()
        {
            HttpHelper.Proxy = null;
            TelemetryProvider.ClearWebProxy();
        }

        /// <summary>
        /// Vehicle name is optional, but if not specified and you have multiple cars, it may not choose the one you want.  Either password or access token must be supplied.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="vehicleName"></param>
        public Service(string email, string password, string vehicleName = "", ITelemetryProvider telemetryProvider = null)
        {
            Email = email;
            Password = password;
            VehicleName = vehicleName;
            IsInitialized = false;
            HttpHelper.UserAgent = "PintSizeMeTeslaApi";
            TelemetryProvider = telemetryProvider;
        }

        public async Task Initialize(ConfigurationOptions configOptions = ConfigurationOptions.None)
        {
            Options = configOptions;
            await Authenticate();
            IsInitialized = true;
        }

        private async Task Authenticate()
        {
            await Time("http oauth", async () =>
            {
                var url = $"{UrlBase}/oauth/token";
                var result = await HttpHelper.HttpPost<TeslaOAuthResult, TeslaOAuthRequest>(url, new TeslaOAuthRequest());
                AccessToken = result.access_token;
                AccessTokenExpires = DateTime.UtcNow.AddSeconds(result.expires_in);
            });
        }

        public async Task GetStatus(bool forceFetch = false)
        {
            var now = DateTime.UtcNow;
            if (Data != null && Data.Fetched > now.AddMinutes(-1) && !forceFetch) return;
            if (
                Data != null &&
                LastSleepRefresh > now.AddMinutes(-15) &&
                !Data.ChargeState.ChargingState.Equals("charging", StringComparison.OrdinalIgnoreCase) &&
                Data.VehicleState.Locked &&
                Data.DriveState.Power == 0 &&
                !forceFetch) return;

            bool vehicleIsAwake;
            if (string.IsNullOrWhiteSpace(AccessToken) || AccessTokenExpires < now.AddDays(1))
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
                    LastSleepRefresh = now;
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
                if (Data == null || Data.Fetched < now.AddHours(-4))
                    vehicleIsAwake = await WakeVehicle();
                else if (Data != null)
                {
                    Data.State = "asleep";
                    LastSleepRefresh = now;
                }
            }

            if (vehicleIsAwake)
            {
                await GetVehicleData();
                LastRefresh = now;
                LastSleepRefresh = now;
            }
        }

        public async Task<List<Vehicle>> GetVehicles()
        {
            return await Time("http vehicles", async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles";
                var result = await HttpHelper.HttpGetOAuth<TeslaResult<List<Vehicle>>>(AccessToken, url);
                return result.response;
            });
        }

        private async Task<Vehicle> GetVehicle()
        {
            return await Time("http vehicle", async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles/{VehicleId}";
                var result = await HttpHelper.HttpGetOAuth<TeslaResult<Vehicle>>(AccessToken, url);
                return result.response;
            });
        }

        private async Task GetVehicleData()
        {
            var now = DateTime.UtcNow;
            await Time("http vehicle_data", async () =>
            {
                var resultData = await HttpHelper.HttpGetOAuth<TeslaResult<VehicleData>>(AccessToken, $"{UrlBase}/api/1/vehicles/{VehicleId}/vehicle_data");
                Data = resultData.response;
                LastRefresh = now;
                LastSleepRefresh = now;
            });
        }

        public void SetVehicle(long vehicleId, string vehicleName)
        {
            VehicleId = vehicleId;
            VehicleName = vehicleName;
            Data = null;
            VehicleState = "";
            LastRefresh = DateTime.MinValue;
            LastSleepRefresh = DateTime.MinValue;
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
            var now = DateTime.UtcNow;
            if (WakeUpSent > now.AddMinutes(-1)) return false;
            return await Time("http wake_up", async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/wake_up";
                var result = await HttpHelper.HttpPostOAuth<TeslaResult<Vehicle>, string>(AccessToken, url, "");
                WakeUpSent = now;
                if (result?.response == null) return false; // || !result.response.Any()) return false;
                return result.response.State.Equals("online", StringComparison.CurrentCultureIgnoreCase);
            });
        }

        private async Task VehicleSimpleCommand(string command)
        {
            await EnsureAwake();
            await Time("http " + command, async () =>
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

        public async Task HonkHorn()
        {
            await VehicleSimpleCommand("honk_horn");
        }

        public async Task FlashLights()
        {
            await VehicleSimpleCommand("flash_lights");
        }

        public async Task SeatHeater(Seat seat, int level)
        {
            await EnsureAwake();
            await Time("http remote_seat_heater_request_" + seat, async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/command/remote_seat_heater_request";
                await HttpHelper.HttpPostOAuth<JObject, object>(AccessToken, url, new { heater = seat, level });
                Data.VehicleState.Locked = false;
            });
        }

        private async Task FrunkTrunk(string which)
        {
            await EnsureAwake();
            await Time("http actuate_trunk_" + which, async () =>
            {
                var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/command/actuate_trunk";
                await HttpHelper.HttpPostOAuth<JObject, object>(AccessToken, url, new { which_trunk = which });
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
            await Time("http start", async () =>
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

        private async Task Time(string key, Func<Task> function)
        {
            if (TelemetryProvider != null)
                await TelemetryProvider.Time(key, function);
            else
                await function();
        }

        private async Task<T> Time<T>(string key, Func<Task<T>> function)
        {
            if (TelemetryProvider != null)
                return await TelemetryProvider.Time(key, function);
            else
                return await function();
        }

        public async Task SunRoofClose()
        {
            var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/command/sun_roof_control";
            await HttpHelper.HttpPostOAuth<JObject, object>(AccessToken, url, new { state = "close" });
        }

        public async Task SunRoofVent()
        {
            var url = $"{UrlBase}/api/1/vehicles/{VehicleId}/command/sun_roof_control";
            await HttpHelper.HttpPostOAuth<JObject, object>(AccessToken, url, new { state = "vent" });
        }
    }
}