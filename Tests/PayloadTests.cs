using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using TeslaApi;

namespace Tests
{
    public class PayloadTests
    {
        [TestCase("After Charging.json")]
        [TestCase("AP On.json")]
        [TestCase("Charging.json")]
        [TestCase("Driving.json")]
        [TestCase("Hvac On.json")]
        [TestCase("Parked away from home before drive.json")]
        [TestCase("Parked away from home.json")]
        [TestCase("Software 2 Minute Countdown.json")]
        [TestCase("Software Downloaded.json")]
        [TestCase("Stop Hold.json")]
        [TestCase("Stopped.json")]
        [TestCase("Updating.json")]
        public void DeserializeFile(string fileName)
        {
            var content = File.ReadAllText("..\\..\\..\\Payloads\\" + fileName);
            var result = JsonConvert.DeserializeObject<Service.TeslaResult<VehicleData>>(content);
            Assert.Pass();
        }
    }
}