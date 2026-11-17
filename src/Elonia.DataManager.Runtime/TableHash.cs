using System.Security.Cryptography;
using System.Text;

namespace Elonia.DataManager.Runtime
{
    public static class TableHash
    {
        private const string HEX_DIGITS = "0123456789abcdef";

        public static string Compute(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return ToHex(sha256.ComputeHash(bytes));
            }
        }

        public static string Compute(string text)
        {
            return Compute(Encoding.UTF8.GetBytes(text));
        }

        private static string ToHex(byte[] hash)
        {
            char[] chars = new char[hash.Length * 2];
            for (int index = 0; index < hash.Length; index++)
            {
                chars[index * 2] = HEX_DIGITS[hash[index] >> 4];
                chars[index * 2 + 1] = HEX_DIGITS[hash[index] & 0xF];
            }

            return new string(chars);
        }
    }
}
