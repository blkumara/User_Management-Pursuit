namespace Pursuit.Model
{
    public class Args
    {
        public string? databaseUrl { get; set; }
        public string? collectionName { get; set; }
        public string? cappedMaxSizeMb { get; set; }
        public string? cappedMaxDocuments { get; set; }

    }
}
