using System;

namespace Cubes.Domain
{
    public static class NumericRange
    {
        public static void Require(float value, float minimum, float maximum, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < minimum || value > maximum)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, $"Expected a value between {minimum} and {maximum}.");
            }
        }
    }
}
