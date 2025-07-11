using System.Collections.Generic;

namespace Data
{
    public interface IScenarioResultRepository
    {
        void AddResult(ScenarioResult result);
        List<ScenarioResult> GetAllResults();
    }

    public class InMemScenarioResultRepository : IScenarioResultRepository
    {
        private readonly List<ScenarioResult> Results = new();

        public void AddResult(ScenarioResult result) => Results.Add(result);

        public List<ScenarioResult> GetAllResults() => new(Results);
    }

    public class ScenarioResult
    {
        public int Rooms { get; set; }
        public int Customers { get; set; }
        public System.TimeSpan Elapsed { get; set; }
        public double AvgItems { get; set; }
        public double AvgRoomTimeMin { get; set; }
        public double AvgWaitTimeMin { get; set; }
    }
}