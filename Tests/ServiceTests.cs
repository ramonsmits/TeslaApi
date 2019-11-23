using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using TeslaApi;

namespace Tests
{
    public class ServiceTests
    {
        private string Email;
        private string Password;
        private string VehicleName;
        private Service TeslaService;

        [SetUp]
        public void Setup()
        {
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

        [Test, Order(1)]
        public async Task Initialize()
        {
            TeslaService = new Service(Email, Password, VehicleName);
            await TeslaService.Initialize();
            await TeslaService.GetStatus();
            Assert.NotNull(TeslaService.Data, "No data retrieved. Car may not be awake yet.");
        }

        [Test, Order(2)]
        public async Task DoesNotReFetch()
        {
            var fetched = TeslaService.Data.Fetched;
            await TeslaService.GetStatus();
            Assert.AreEqual(fetched, TeslaService.Data.Fetched, "A refresh should not have happened.");
        }

        [Test, Order(3)]
        public async Task DoesReFetchWhenForced()
        {
            var fetched = TeslaService.Data.Fetched;
            await TeslaService.GetStatus(true);
            Assert.Less(fetched, TeslaService.Data.Fetched, "A refresh should have happened.");
        }

        [Test, Explicit, Order(100)]
        public async Task Unlock()
        {
            await EnsureInitialized();
            var initialState = TeslaService.Data.vehicle_state.locked;
            if (!initialState)
            {
                Console.WriteLine("Already unlocked");
                Assert.Inconclusive("Already unlocked");
            }
            await TeslaService.Unlock();
            await TeslaService.GetStatus(true);
            Assert.True(!TeslaService.Data.vehicle_state.locked, "Unlock failed.");
        }

        [Test, Explicit, Order(101)]
        public async Task Lock()
        {
            await EnsureInitialized();
            var initialState = TeslaService.Data.vehicle_state.locked;
            if (initialState)
            {
                Console.WriteLine("Already locked");
                Assert.Inconclusive("Already locked");
            }
            await TeslaService.Lock();
            await TeslaService.GetStatus(true);
            Assert.True(TeslaService.Data.vehicle_state.locked, "Lock failed. Are doors, frunk, or trunk open?");
        }

        [Test, Explicit, Order(102)]
        public async Task Frunk()
        {
            await EnsureInitialized();
            var initialState = TeslaService.Data.vehicle_state.ft;
            if (initialState != 0) Console.WriteLine("Already open");
            await TeslaService.Frunk();
            await TeslaService.GetStatus(true);
            Assert.AreNotEqual(initialState, TeslaService.Data.vehicle_state.ft, "State is unchanged. It could have been open or will always fail if you have the Model3 with Hansshow kit.");
        }

        [Test, Explicit, Order(103)]
        public async Task Trunk()
        {
            await EnsureInitialized();
            var initialState = TeslaService.Data.vehicle_state.rt;
            await TeslaService.Trunk();
            await TeslaService.GetStatus(true);
            Assert.AreNotEqual(initialState, TeslaService.Data.vehicle_state.rt, "State is unchanged. It could have been open or will always fail if you have the Model3 with Hansshow kit.");
        }

        [Test, Explicit, Order(104)]
        public async Task StartFailsWithoutPassword()
        {
            await EnsureInitialized();
            var initialState = TeslaService.Data.vehicle_state.remote_start;
            if (initialState)
            {
                Console.WriteLine("Already started");
                Assert.Inconclusive("Already started");
            }
            Assert.ThrowsAsync<ArgumentException>(async () => { await TeslaService.Start(); });
            await TeslaService.GetStatus(true);
            Assert.False(TeslaService.Data.vehicle_state.remote_start, "State is changed, the car should not have started.");
        }

        [Test, Explicit, Order(105)]
        public async Task Start()
        {
            await EnsureInitialized();
            var initialState = TeslaService.Data.vehicle_state.remote_start;
            if (initialState)
            {
                Console.WriteLine("Already started");
                Assert.Inconclusive("Already started");
            }
            await TeslaService.Start(Password);
            await TeslaService.GetStatus(true);
            Assert.True(TeslaService.Data.vehicle_state.remote_start, "State is unchanged. The car did not start.");
        }
    }
}