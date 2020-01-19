using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TeslaApi;
using TeslaApi.Enums;

namespace Tests
{
    public class ServiceTests
    {
        private string Email;
        private string Password;
        private string VehicleName;
        private Service TeslaService;
        private string DeviceId;

        [SetUp]
        public void Setup()
        {
            DeviceId = Environment.MachineName;
            var fileName = "..\\..\\..\\Auth.user"; //bin\debug\netcoreapp2.1
            if (!File.Exists(fileName))
                Assert.Fail("Auth.user not found in Tests folder.");
            var lines = File.ReadAllLines(fileName);
            if(lines.Length < 2)
                Assert.Fail("Auth.user file did not have enough lines.");
            Email = lines[0];
            Password = lines[1];
            if(lines.Length >= 3)
                VehicleName = lines[2];
        }

        private async Task EnsureInitialized()
        {
            if (TeslaService == null)
                await Initialize();
        }

        private async Task EnsureReady()
        {
            await EnsureInitialized();
            await TeslaService.GetStatus();
            Assert.NotNull(TeslaService.Data, "No data retrieved. Car may not be awake yet.");
        }

        [Test, Order(0)]
        public async Task Initialize()
        {
            TeslaService = new Service("TeslaApi.Tests", DeviceId, Email, Password, VehicleName);
            await TeslaService.Initialize();
            Assert.True(TeslaService.IsAuthenticated);
        }

        [Test, Order(1)]
        public async Task WakeVehicle()
        {
            await EnsureInitialized();
            await TeslaService.GetStatus();
            if (!TeslaService.VehicleState.Equals("asleep", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Vehicle is not asleep");
                Assert.Inconclusive("Vehicle is not asleep");
            }
            await TeslaService.WakeVehicle();
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                Thread.Sleep(1000);
                await TeslaService.GetStatus();
                if (sw.Elapsed > TimeSpan.FromSeconds(60)) break;
            } while (TeslaService.VehicleState.Equals("asleep", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("online", TeslaService.VehicleState, "Vehicle is not online after 1 minute.");
            Console.WriteLine($"Vehicle woke after {sw.Elapsed.TotalSeconds} seconds");
        }

        [Test, Order(2)]
        public async Task DoesNotReFetch()
        {
            await EnsureReady();
            var fetched = TeslaService.Data.Fetched;
            await TeslaService.GetStatus();
            Assert.AreEqual(fetched, TeslaService.Data.Fetched, "A refresh should not have happened.");
        }

        [Test, Order(3)]
        public async Task DoesReFetchWhenForced()
        {
            await EnsureReady();
            var fetched = TeslaService.Data.Fetched;
            await TeslaService.GetStatus(true);
            Assert.Less(fetched, TeslaService.Data.Fetched, "A refresh should have happened.");
        }

        [Test, Explicit, Order(100)]
        public async Task DoorsUnlock()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.VehicleState.Locked;
            if (!initialState)
            {
                Console.WriteLine("Already unlocked");
                Assert.Inconclusive("Already unlocked");
            }
            await TeslaService.Unlock();
            await TeslaService.GetStatus(true);
            Assert.True(!TeslaService.Data.VehicleState.Locked, "Unlock failed.");
        }

        [Test, Explicit, Order(101)]
        public async Task DoorsLock()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.VehicleState.Locked;
            if (initialState)
            {
                Console.WriteLine("Already locked");
                Assert.Inconclusive("Already locked");
            }
            await TeslaService.Lock();
            await TeslaService.GetStatus(true);
            Assert.True(TeslaService.Data.VehicleState.Locked, "Lock failed. Are doors, frunk, trunk, or charging port open?");
        }

        [Test, Explicit, Order(102)]
        public async Task PreconditionStart()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.ClimateState.IsPreconditioning;
            if (initialState)
            {
                Console.WriteLine("Already preconditioning");
                Assert.Inconclusive("Already preconditioning");
            }
            await TeslaService.HvacStart();
            await TeslaService.GetStatus(true);
            Assert.True(TeslaService.Data.ClimateState.IsPreconditioning, "State is not changed, the car should be preconditioning.");
        }

        [Test, Explicit, Order(103)]
        public async Task HeatSeat()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.ClimateState.SeatHeaterFrontLeft;
            int level = initialState != 1 ? 1 : 2;
            await TeslaService.SeatHeater(Seat.FrontLeft, level);
            await TeslaService.SeatHeater(Seat.FrontRight, 1);
            await TeslaService.SeatHeater(Seat.RearLeft, 2);
            await TeslaService.SeatHeater(Seat.RearCenter, 3);
            await TeslaService.SeatHeater(Seat.RearRight, 1);
            await TeslaService.GetStatus(true);
            Assert.AreEqual(level, TeslaService.Data.ClimateState.SeatHeaterFrontLeft, "Heat seat failed.");
        }

        [Test, Explicit, Order(104)]
        public async Task HeatSeatStop()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.ClimateState.SeatHeaterFrontLeft;
            if (initialState == 0)
            {
                Console.WriteLine("Already not heating");
                Assert.Inconclusive("Already not heating");
            }

            await TeslaService.SeatHeater(Seat.FrontLeft, 0);
            await TeslaService.SeatHeater(Seat.FrontRight, 0);
            await TeslaService.SeatHeater(Seat.RearLeft, 0);
            await TeslaService.SeatHeater(Seat.RearCenter, 0);
            await TeslaService.SeatHeater(Seat.RearRight, 0);
            await TeslaService.GetStatus(true);
            Assert.AreEqual(0, TeslaService.Data.ClimateState.SeatHeaterFrontLeft, "Heat seat stop failed.");
        }

        [Test, Explicit, Order(105)]
        public async Task PreconditionStop()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.ClimateState.IsPreconditioning;
            if (!initialState)
            {
                Console.WriteLine("Not preconditioning");
                Assert.Inconclusive("Not preconditioning");
            }
            await TeslaService.HvacStop();
            await TeslaService.GetStatus(true);
            Assert.False(TeslaService.Data.ClimateState.IsPreconditioning, "State is not changed, the car should not be preconditioning.");
        }

        [Test, Explicit, Order(106)]
        public async Task ChargePortOpen()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.ChargeState.ChargeDoorOpen;
            if(!initialState.HasValue) Assert.Inconclusive("Null state.");
            if (initialState.Value)
            {
                Console.WriteLine("Already open");
                Assert.Inconclusive("Already open");
            }
            await TeslaService.ChargePortOpen();
            await TeslaService.GetStatus(true);
            Assert.True(TeslaService.Data.ChargeState.ChargeDoorOpen, "Open failed.");
        }

        [Test, Explicit, Order(107)]
        public async Task ChargePortClose()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.ChargeState.ChargeDoorOpen;
            if (!initialState.HasValue) Assert.Inconclusive("Null state.");
            if (!initialState.Value)
            {
                Console.WriteLine("Already closed");
                Assert.Inconclusive("Already closed");
            }
            await TeslaService.ChargePortClose();
            await TeslaService.GetStatus(true);
            Assert.True(TeslaService.Data.ChargeState.ChargeDoorOpen, "Close failed.");
        }

        [Test, Explicit, Order(151)]
        public async Task TrunkFront()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.VehicleState.Frunk;
            if (initialState != 0) Console.WriteLine("Already open");
            await TeslaService.Frunk();
            await TeslaService.GetStatus(true);
            Assert.AreNotEqual(initialState, TeslaService.Data.VehicleState.Frunk, "State is unchanged.");
        }

        [Test, Explicit, Order(152)]
        public async Task TrunkRear()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.VehicleState.Trunk;
            await TeslaService.Trunk();
            await TeslaService.GetStatus(true);
            Assert.AreNotEqual(initialState, TeslaService.Data.VehicleState.Trunk, "State is unchanged. It could have been open or may fail if you have the Model3 with Hansshow kit.");
        }

        [Test, Explicit, Order(200)]
        public async Task StartFailsWithoutPassword()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.VehicleState.StartedRemotely;
            if (initialState)
            {
                Console.WriteLine("Already started");
                Assert.Inconclusive("Already started");
            }
            Assert.ThrowsAsync<ArgumentException>(async () => { await TeslaService.Start(); });
            await TeslaService.GetStatus(true);
            Assert.False(TeslaService.Data.VehicleState.StartedRemotely, "State is changed, the car should not have started.");
        }

        [Test, Explicit, Order(201)]
        public async Task Start()
        {
            await EnsureReady();
            var initialState = TeslaService.Data.VehicleState.StartedRemotely;
            if (initialState)
            {
                Console.WriteLine("Already started");
                Assert.Inconclusive("Already started");
            }
            await TeslaService.Start(Password);
            await TeslaService.GetStatus(true);
            Assert.True(TeslaService.Data.VehicleState.StartedRemotely, "State is unchanged. The car did not start.");
        }
    }
}