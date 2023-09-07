/* =========================================================
    Item Name: Regex validations - Validations
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Utilities
{
    public abstract class Validations
    {
        //TODO: you may want to load the patterns supported from resource, file, settings etc.
        private static string[] p_phone
                    = new string[] {
                                         @"^[0-9]{10}$",
                                         @"^\+[0-9]{2}\s+[0-9]{2}[0-9]{8}$",
                                         @"^[0-9]{3}-[0-9]{4}-[0-9]{4}$",
                                    };
           
        private static string[] p_email
            = new string[] { @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
                            @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                                @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                                @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                            };

        public static string PhonePatterns
        {
            get {
              return  string.Join("|", p_phone
              .Select(item => "(" + item + ")"));
            }
        }

        public static string EmailPattern
        {
            get
            {
                return string.Join("|", p_email
               .Select(item => "(" + item + ")"));
            }
        }
    }
}
