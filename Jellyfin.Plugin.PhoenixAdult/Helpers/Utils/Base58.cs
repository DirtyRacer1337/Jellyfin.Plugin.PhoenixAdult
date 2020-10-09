using System;
using System.Linq;
using System.Numerics;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class Base58
    {
        private const string DIGITS = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public static string EncodePlain(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            var intData = data.Aggregate<byte, BigInteger>(0, (current, t) => (current * 256) + t);

            var result = string.Empty;
            while (intData > 0)
            {
                var remainder = (int)(intData % 58);
                intData /= 58;
                result = DIGITS[remainder] + result;
            }

            for (var i = 0; i < data.Length && data[i] == 0; i++)
            {
                result = '1' + result;
            }

            return result;
        }

        public static byte[] DecodePlain(string data)
        {
            if (data == null)
            {
                return null;
            }

            BigInteger intData = 0;
            for (var i = 0; i < data.Length; i++)
            {
                var digit = DIGITS.IndexOf(data[i].ToString(), StringComparison.Ordinal);

                if (digit < 0)
                {
                    throw new FormatException($"Invalid Base58 character `{data[i]}` at position {i}");
                }

                intData = (intData * 58) + digit;
            }

            var leadingZeroCount = data.TakeWhile(c => c == '1').Count();
            var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
            var bytesWithoutLeadingZeros = intData.ToByteArray().Reverse().SkipWhile(b => b == 0);
            var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();

            return result;
        }
    }
}
