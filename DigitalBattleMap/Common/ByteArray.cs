using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.Common
{
    public static class ByteArray
    {
        public static byte[] Combine(List<byte[]> byteArrays)
        {
            var resultByteArray = new byte[byteArrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (var array in byteArrays)
            {
                Buffer.BlockCopy(array, 0, resultByteArray, offset, array.Length);
                offset += array.Length;
            }
            return resultByteArray;
        }

        public static byte[] Extract(this byte[] byteArray, int startIndex, int lenght)
        {
            var resultByteArray = new byte[lenght];
            Buffer.BlockCopy(byteArray, startIndex, resultByteArray, 0, lenght);
            return resultByteArray;
        }

        public static int GetIndexOfBytes(byte[] haystack, string needle)
        {
            return GetIndexOfBytes(haystack, Encoding.UTF8.GetBytes(needle));
        }

        private static int GetIndexOfBytes(byte[] haystack, byte[] needle)
        {
            var lenght = needle.Length;
            var limit = haystack.Length - lenght;
            for (var i = 0; i <= limit; i++)
            {
                var k = 0;
                for (; k < lenght; k++)
                {
                    if (needle[k] != haystack[i + k]) break;
                }
                if (k == lenght) return i;
            }
            return -1;
        }
    }
}
