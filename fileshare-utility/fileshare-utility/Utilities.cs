using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{
    class Utilities
    {

        /// <summary>Performs a case insensitive string compare and returns whether the two strings are equal or not.
        /// </summary>
        /// <param name="str1">First string to compare.</param>
        /// <param name="str2">Second string to compare.</param>
        /// <returns>whether the two strings are equal or not.</returns>
        internal static bool StrCompareCaseInsensitive(string str1, string str2)
        {
            return String.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
