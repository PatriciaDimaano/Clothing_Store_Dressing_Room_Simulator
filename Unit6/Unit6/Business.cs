using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data;

namespace Business
{
    public class DressingRooms
    {
        private readonly SemaphoreSlim Semaphore;
        public static event Action<string>? OnRoomActivity;

        public DressingRooms(int numRooms = 3)
        {
            Semaphore = new(numRooms, numRooms);
        }

        public async Task RequestRoom(Customer customer)
        {
            customer.WaitTimer.Start();
            await Semaphore.WaitAsync();
            customer.WaitTimer.Stop();

            OnRoomActivity?.Invoke(
                $"Customer {customer.Id} entered a dressing room." +
                (customer.WaitTimer.Elapsed.TotalMinutes > 0 ?
                 $" Wait time: {customer.WaitTimer.Elapsed.TotalMinutes:F1} minutes." : string.Empty)
            );
        }

        public void ReleaseRoom(Customer customer)
        {
            Semaphore.Release();
            OnRoomActivity?.Invoke($"Customer {customer.Id} left the dressing room.");
        }
    }

    public class Customer
    {
        private static int NextId;
        private const int MaxItems = 6;

        private static readonly ThreadLocal<Random> ThreadRandom = new(() => new(Guid.NewGuid().GetHashCode()));

        public int Id { get; }
        public int Items { get; }
        public Stopwatch RoomTimer { get; } = new();
        public Stopwatch WaitTimer { get; } = new();

        public static event Action<string>? OnActivity;

        public static void ResetIdCounter() => Interlocked.Exchange(ref NextId, 0);

        public Customer(int numItems = 0)
        {
            Id = Interlocked.Increment(ref NextId);
            Items = (numItems == 0)
                ? ThreadRandom.Value!.Next(1, MaxItems + 1)
                : Math.Clamp(numItems, 1, MaxItems);
        }

        public async Task EnterAndUseDressingRoom(DressingRooms rooms)
        {
            await rooms.RequestRoom(this);

            RoomTimer.Start();
            for (int i = 0; i < Items; i++)
            {
                double tryOnTimeMin = ThreadRandom.Value!.Next(10, 31) / 10.0;
                OnActivity?.Invoke($"Customer {Id} is trying on item {i + 1}/{Items}. (Est. {tryOnTimeMin:F1} minutes)");
                await Task.Delay(TimeSpan.FromMinutes(tryOnTimeMin));
            }
            RoomTimer.Stop();

            OnActivity?.Invoke(
                $"Customer {Id} has finished trying on {Items} items in {RoomTimer.Elapsed.TotalMinutes:F1} minutes.");

            rooms.ReleaseRoom(this);
        }
    }

    public class Scenario
    {
        private readonly int NumRooms;
        private readonly int NumCustomers;
        private readonly Stopwatch Timer = new();
        private readonly List<Customer> Customers = new();

        public Scenario(int numRooms, int numCustomers)
        {
            NumRooms = numRooms;
            NumCustomers = numCustomers;
        }

        public async Task<ScenarioResult> Execute()
        {
            Customer.ResetIdCounter();
            Timer.Start();

            DressingRooms rooms = new(NumRooms);

            Customers.AddRange(Enumerable.Range(0, NumCustomers).Select(_ => new Customer()));

            await Task.WhenAll(Customers.Select(c => c.EnterAndUseDressingRoom(rooms)));

            Timer.Stop();

            return new()
            {
                Rooms = NumRooms,
                Customers = Customers.Count,
                Elapsed = Timer.Elapsed,
                AvgItems = Customers.Average(c => c.Items),
                AvgRoomTimeMin = Customers.Average(c => c.RoomTimer.Elapsed.TotalMinutes),
                AvgWaitTimeMin = Customers.Average(c => c.WaitTimer.Elapsed.TotalMinutes)
            };
        }
    }

    public class SimulationService
    {
        private readonly IScenarioResultRepository Repo;

        public SimulationService(IScenarioResultRepository repo) => Repo = repo;

        public async Task<ScenarioResult> RunAndStore(int rooms, int customers)
        {
            var scenario = new Scenario(rooms, customers);
            var result = await scenario.Execute();
            Repo.AddResult(result);
            return result;
        }

        public List<ScenarioResult> GetAllResults() => Repo.GetAllResults();

        public ScenarioResult? GetOptimalByWaitTime()
        {
            var allResults = Repo.GetAllResults();
            return allResults.OrderBy(r => r.AvgWaitTimeMin).FirstOrDefault();
        }
    }
}