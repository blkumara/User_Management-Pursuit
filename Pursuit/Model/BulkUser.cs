/* =========================================================
    Item Name: BulkUser model-used in Bulk user upsert
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
    public class BulkUser
    {
        public int? SlNo { get; set; }
        public bool? Err { get; set; }
        public string? ErrDescription { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? LoginId { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }

    }
}
