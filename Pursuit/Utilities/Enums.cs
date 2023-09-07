/* =========================================================
    Item Name: Enums
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Utilities
{
    public abstract class Enums
    {
        public enum UserStatus
        {
            PENDING_APPROVAL=1,
            PENDING_VERIFICATION = 2,
            ADMIN_APPROVED =3,
            FIRST_LOGIN=4,
            ACTIVE=5,
            DEACTIVE=6,
            REJECTED=7,
        }

       public enum UserOrigin
        {
           BULK=1,
           ADMIN=2,
           SELF=3
        }
    }
}