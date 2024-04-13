namespace DebtCollector.NET.Interfaces {
    public interface IHotspotFinder {
        IEnumerable<KeyValuePair<string, int>> GetMostCommittedFiles(
            string pathToRepo,
            int daysSince = 1
        );
    }
}