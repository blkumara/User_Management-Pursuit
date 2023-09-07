using System;

namespace Pursuit.Helpers
{
    [Serializable]
    public class EvolveConfigurationException : Exception
    {
        public EvolveConfigurationException() { }

        public EvolveConfigurationException(string exception) : base(exception) { }
    }
}
