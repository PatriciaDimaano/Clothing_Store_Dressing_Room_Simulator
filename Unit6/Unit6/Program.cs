using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Business;
using Data;

namespace Unit6
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("\n\nClothing Store Dressing Room Simulator");

            Customer.OnActivity += LogActivity;
            DressingRooms.OnRoomActivity += LogActivity;

            IScenarioResultRepository repo = new InMemScenarioResultRepository();
            var simService = new SimulationService(repo);

            await RunAndDisplayScenario(simService, 3, 10, "Scenario 1: 3 Rooms, 10 Customers");
            await RunAndDisplayScenario(simService, 5, 20, "Scenario 2: 5 Rooms, 20 Customers");
            await RunAndDisplayScenario(simService, 4, 20, "Scenario 3: 4 Rooms, 20 Customers");

            Console.WriteLine("\n\n\nSimulation Complete.");

            ScenarioResult? optimalScenario = simService.GetOptimalByWaitTime();
            if (optimalScenario is not null)
            {
                Console.WriteLine("\n-------------------- Optimal Dressing Room Configuration --------------------");
                DisplayResult(optimalScenario, $"Optimal Scenario (Rooms: {optimalScenario.Rooms}, Customers: {optimalScenario.Customers})");
            }

            Customer.OnActivity -= LogActivity;
            DressingRooms.OnRoomActivity -= LogActivity;
        }

        private static async Task RunAndDisplayScenario(SimulationService service, int rooms, int customers, string title)
        {
            Console.WriteLine($"\n\n\n--------------------Executing {title}--------------------\n");
            ScenarioResult result = await service.RunAndStore(rooms, customers);
            Console.WriteLine($"Scenario Finished\n\n" +
                              $"Rooms = {result.Rooms}, Customers = {result.Customers}");
            DisplayResult(result, string.Empty);
        }

        private static void LogActivity(string message)
        {
            lock (Console.Out)
            {
                Console.WriteLine(message);
            }
        }

        private static void DisplayResult(ScenarioResult result, string title)
        {
            if (!string.IsNullOrEmpty(title))
            {
                Console.WriteLine($"\n{title}\n");
            }

            string FormatTime(TimeSpan ts) => $"{ts.TotalMinutes:F1} minutes";

            Console.WriteLine($"Total Scenario Time: {FormatTime(result.Elapsed)}");
            Console.WriteLine($"Total Customers: {result.Customers}");
            Console.WriteLine($"Average Items per Customer: {result.AvgItems:F2}");

            Console.WriteLine($"Average Time Used in Dressing Room: {result.AvgRoomTimeMin:F1} minutes");
            Console.WriteLine($"Average Time Spent Waiting for Dressing Room: {result.AvgWaitTimeMin:F1} minutes");
        }
    }
}