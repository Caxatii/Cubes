using System;

namespace Cubes.Presentation.Configuration
{
    internal static class ConfigurationRange
    {
        public static void Require(float value, float minimum, float maximum, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < minimum || value > maximum)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }
}
