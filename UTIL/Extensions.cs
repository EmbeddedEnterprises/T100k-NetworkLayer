using System.Collections.Generic;

namespace Utils
{
    /// <summary>
    /// This class represents all kinds of extension methods for convenience.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Tests whether the given bit is set in a byte.
        /// </summary>
        /// <returns><c>true</c>, if bit set was set, <c>false</c> otherwise.</returns>
        /// <param name="val">The value to check.</param>
        /// <param name="bit">The bit to check.</param>
        public static bool IsBitSet(this byte val, int bit)
        {
            return (val & (1 << bit)) != 0;
        }

        /// <summary>
        /// Creates an enumerable from a single value.
        /// </summary>
        /// <param name="value">The value to enumerate.</param>
        /// <typeparam name="T">The type of the value to enumerate.</typeparam>
        public static IEnumerable<T> Enumerate<T>(this T value)
        {
            yield return value;
        }
    }
}
