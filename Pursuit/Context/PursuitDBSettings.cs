/* =========================================================
    Item Name: DB Settings-IPursuitDBSettings,PursuitDBSettings
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Context
{
    public interface IPursuitDBSettings
    {
        string? ConnectionDomain { get; set; }
        string? ConnectionPort { get; set; }
        string? ConnectionUsername { get; set; }
        string? ConnectionPassword { get; set; }
        string? ConnectionString { get; set; } 

        string? DatabaseName { get; set; } 

        string? CollectionName { get; set; }

        string? AdminCollectionName { get; set; }

        string? EmailCollectionName { get; set; }

        string? ConnectionCollectionName { get; set; }
        string? LogCollectionName { get; set; }



    }
    public class PursuitDBSettings : IPursuitDBSettings
    {
        public string? ConnectionDomain { get; set; } = null!;
        public string? ConnectionPort { get; set; } = null!;
        public string? ConnectionUsername { get; set; } = null!;
        public string? ConnectionPassword { get; set; } = null!;
        public string? ConnectionString { get; set; } = null!;

        public string? DatabaseName { get; set; } = null!;

        public string? CollectionName { get; set; } = null!;
        public string? AdminCollectionName { get; set; } = null!;
        public string? EmailCollectionName { get; set; } = null!;

        public string? ConnectionCollectionName { get; set; } = null!;
        public string? LogCollectionName { get; set; } = null!;
    }
}
