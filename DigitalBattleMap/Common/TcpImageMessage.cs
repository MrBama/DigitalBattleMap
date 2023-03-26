using DigitalBattleMap.Common.DigitalBattleMap.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.Common
{
    public class TcpImageMessage
    {
        public TcpImageMessage(string action, Bitmap bitmap)
        {
            Action = action;
            Bitmap = new Bitmap(bitmap);
        }

        public string Action { get; set; }
        public Bitmap Bitmap { get; set; }

        public byte[] GetBytes()
        {
            // Format: "UpdateMap<:>imageBytes<EOM>";

            using (var memoryStream = new MemoryStream())
            {
                Bitmap.Save(memoryStream, ImageFormat.Png);
                var actionBytes = Encoding.UTF8.GetBytes(Action + TcpConstants.ActionSeparator);
                var bitmapBytes = memoryStream.ToArray();
                var stringBytes = Encoding.UTF8.GetBytes(TcpConstants.EndOfMessage);
                return ByteArray.Combine(new List<byte[]> { actionBytes, bitmapBytes, stringBytes });
            }
        }

        public static TcpImageMessage Parse(TcpMessage tcpMessage)
        {
            var bytes = tcpMessage.GetBytes();
            var rawString = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            var action = rawString.Substring(0, rawString.IndexOf(TcpConstants.ActionSeparator));

            var prefixLenght = $"{action}{TcpConstants.ActionSeparator}".Length;
            var imageBytes = new byte[bytes.Length - prefixLenght];
            Buffer.BlockCopy(bytes, prefixLenght, imageBytes, 0, imageBytes.Length);

            using (var memoryStream = new MemoryStream(imageBytes))
            {
                using (var bitmap = new Bitmap(memoryStream))
                {
                    return new TcpImageMessage(action, bitmap);
                }
            }
        }

        public static bool TryParse(TcpMessage tcpMessage, out TcpImageMessage tcpImageMessage)
        {
            try
            {
                tcpImageMessage = Parse(tcpMessage);
            }
            catch (Exception)
            {
                tcpImageMessage = null;
                return false;
            }

            return true;
        }
    }
}
