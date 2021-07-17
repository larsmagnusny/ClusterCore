using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterCore.Utilities
{
    public static class SocketUtilities
    {
        public static async Task<byte[]> ReadSocketUntillEnd(WebSocket socket)
        {
            byte[] buffer = new byte[4096];
            byte[] intBuf = new byte[4];
            await socket.ReceiveAsync(buffer, CancellationToken.None);

            Array.Copy(buffer, intBuf, 4);

            int totalSize = BitConverter.ToInt32(intBuf);
            int nullIndex = GetTerminationByte(buffer, 4) - 4;

            using (var bufferStream = new MemoryStream())
            {
                bufferStream.Write(buffer, 4, (nullIndex >= -1 ? nullIndex : 4092));

                int bytesRecieved = (nullIndex >= 0 ? nullIndex : 4092);

                while (totalSize > bytesRecieved)
                {
                    await socket.ReceiveAsync(buffer, CancellationToken.None);

                    nullIndex = GetTerminationByte(buffer, 4) - 4;
                    bufferStream.Write(buffer, 4, (nullIndex >= 0 ? nullIndex : 4092));
                    bytesRecieved += (nullIndex >= 0 ? nullIndex : 4092);
                }

                return bufferStream.ToArray();
            }
        }

        public static async Task SendSocketUntillEnd(WebSocket socket, string data)
        {
            byte[] buffer = new byte[4096];
            byte[] stringBuffer = Encoding.UTF8.GetBytes(data);

            Array.Copy(BitConverter.GetBytes(stringBuffer.Length), buffer, 4);

            if (stringBuffer.Length + 4 <= buffer.Length)
            {
                Array.Copy(stringBuffer, 0, buffer, 4, stringBuffer.Length);
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            else
            {
                int counter = 0;

                while (counter < stringBuffer.Length)
                {
                    if (stringBuffer.Length - counter > 4092)
                    {
                        Array.Copy(stringBuffer, counter, buffer, 4, 4092);
                        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
                        counter += 4092;
                    }
                    else
                    {
                        int delta = stringBuffer.Length - counter;
                        Array.Copy(stringBuffer, counter, buffer, 4, delta);
                        buffer[delta + 4] = 0x0;
                        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
                        counter += delta;
                    }
                }
            }
        }

        public static async Task SendSocketUntillEnd(WebSocket socket, byte[] data)
        {
            byte[] buffer = new byte[4096];

            Array.Copy(BitConverter.GetBytes(data.Length), buffer, 4);

            if (data.Length + 4 <= buffer.Length)
            {
                Array.Copy(data, 0, buffer, 4, data.Length);
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            else
            {
                int counter = 0;

                while (counter < data.Length)
                {
                    if (data.Length - counter > 4092)
                    {
                        Array.Copy(data, counter, buffer, 4, 4092);
                        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
                        counter += 4092;
                    }
                    else
                    {
                        int delta = data.Length - counter;
                        Array.Copy(data, counter, buffer, 4, delta);
                        buffer[delta + 4] = 0x0;
                        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
                        counter += delta;
                    }
                }
            }
        }

        public static async Task SendSocketUntillEnd(WebSocket socket, object data)
        {
            byte[] buffer = new byte[4096];
            byte[] stringBuffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data, Formatting.None));

            Array.Copy(BitConverter.GetBytes(stringBuffer.Length), buffer, 4);

            if (stringBuffer.Length + 4 <= buffer.Length)
            {
                Array.Copy(stringBuffer, 0, buffer, 4, stringBuffer.Length);
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            else
            {
                int counter = 0;

                while (counter < stringBuffer.Length)
                {
                    if (stringBuffer.Length - counter > 4092)
                    {
                        Array.Copy(stringBuffer, counter, buffer, 4, 4092);
                        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
                        counter += 4092;
                    }
                    else
                    {
                        int delta = stringBuffer.Length - counter;
                        Array.Copy(stringBuffer, counter, buffer, 4, delta);
                        buffer[delta + 4] = 0x0;
                        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
                        counter += delta;
                    }
                }
            }
        }

        private static int GetTerminationByte(byte[] buffer, int offset)
        {
            int nullIndex = -1;
            for (int i = offset; i < buffer.Length; i++)
            {
                if (buffer[i] == '\0')
                {
                    nullIndex = i;
                    i = buffer.Length;
                }
            }

            return nullIndex;
        }
    }
}
