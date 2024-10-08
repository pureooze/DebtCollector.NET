namespace DebtCollector.NET.Interfaces {
    public interface IHotspotFinder {
        Dictionary<string,int> GetMostCommittedFiles(
            string pathToRepo,
            int daysSince = 1
        );
    }
}