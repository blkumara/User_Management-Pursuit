/* =========================================================
    Item Name: DB Settings-IADDBSettings,ADDBSettings
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Context
{
    public interface IADDBSettings
    {
        string ConnectionString { get; set; }

        string DatabaseName { get; set; }

        string? MSADCollectionName { get; set; }
        string? AzureADCollectionName { get; set; }
        string? GWSADCollectionName { get; set; }
        string? MSADDeltaCollectionName { get; set; }
        string? AzureADDeltaCollectionName { get; set; }
        string? GWSADDeltaCollectionName { get; set; }
    }
    public class ADDBSettings : IADDBSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string? MSADCollectionName { get; set; } = null!;
        public string? AzureADCollectionName { get; set; } = null!;
        public string? GWSADCollectionName { get; set; } = null!;
        public string? MSADDeltaCollectionName { get; set; } = null!;
        public string? AzureADDeltaCollectionName { get; set; } = null!;
        public string? GWSADDeltaCollectionName { get; set; } = null!;
    }
}
