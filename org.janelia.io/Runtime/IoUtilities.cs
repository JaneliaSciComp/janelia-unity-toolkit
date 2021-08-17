using System;

// Code to support processing messages stored in byte arrays, to avoid code that
// creates a lot of garabage in the form of temporary strings.

namespace Janelia
{
    public static class IoUtilities
    {
        public static long ParseLong(Byte[] b, int iStart, int len, ref bool valid)
        {
            valid = true;
            int iEnd = 0, sign = 0;
            StartEndSign(b, len, ref iStart, ref iEnd, ref sign);
            long pow = 1;
            long x = 0;
            for (int i = iEnd; i >= iStart; i--)
            {
                if ((b[i] < (Byte)'0') || ((Byte)'9' < b[i]))
                {
                    valid = false;
                    return 0;
                }
                long digit = (int)(b[i] - (Byte)'0');
                x += (digit * pow);
                pow *= 10;
            }
            return sign * x;
        }

        // Does not handle scientific notation.
        public static double ParseDouble(Byte[] b, int iStart, int len, ref bool valid)
        {
            valid = true;
            int iEnd = 0, sign = 0;
            StartEndSign(b, len, ref iStart, ref iEnd, ref sign);
            int iDecimal = iStart;
            for (; iDecimal <= iEnd; iDecimal++)
            {
                if (b[iDecimal] == '.')
                    break;
            }
            double denom = (iDecimal < iEnd) ? Math.Pow(10, iEnd - iDecimal) : 1;
            double pow = 1;
            double x = 0;
            for (int i = iEnd; i >= iStart; i--)
            {
                if (i != iDecimal)
                {
                    if ((b[i] < (Byte)'0') || ((Byte)'9' < b[i]))
                    {
                        valid = false;
                        return 0f;
                    }
                    int digit = (int)(b[i] - (Byte)'0');
                    x += (digit * pow);
                    pow *= 10;
                }
            }
            x /= denom;
            x *= sign;
            return x;
        }

        // Similar to `Encoding.ASCII.GetString(b, i0).Split(separator)[n]` without producing
        // extra unused string instances.
        public static void NthSplit(Byte[] b, Byte separator, int i0, int n, ref int i, ref int len)
        {
            int nSoFar = 0;
            i = i0;
            for (; i < b.Length; i++)
            {
                if (b[i] == separator)
                {
                    nSoFar++;
                }
                if (nSoFar == n)
                {
                    break;
                }
            }
            i += (b[i] == separator) ? 1 : 0;
            len = 0;
            for (int j = i; j < b.Length; j++, len++)
            {
                if (b[j] == separator)
                {
                    break;
                }
            }
        }

        // Updates `iStart` and `iEnd` to be the true starting and ending indices, with spaces trimmmed.
        // Also looks for a `-` and returns `sign` as `+1` or `-1`.
        private static void StartEndSign(Byte[] b, int len, ref int iStart, ref int iEnd, ref int sign)
        {
            sign = 1;
            len -= (b[iStart + len - 1] == 0) ? 1 : 0;
            iEnd = iStart + len - 1;
            for (; iStart <= iEnd; iStart++)
            {
                if (b[iStart] == '-')
                    sign = -sign;
                else if (b[iStart] != ' ')
                    break;
            }
            for (; iEnd >= iStart; iEnd--)
            {
                if (b[iEnd] != ' ')
                    break;
            }
        }
    }
}
