using ClusterCore.Requests;
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
    public class SocketUtilities
    {
        public static async Task<string> EvaluateClient(WebSocket socket, object[] parameters, string source, Func<Task> callback = null)
        {
            var programObj = new ProgramRequest { Source = source, parameters = parameters };
            string programJson = JsonConvert.SerializeObject(programObj, Formatting.None);
            await SendSocketUntillEnd(socket, programJson);
            string ret = Encoding.UTF8.GetString(await ReadSocketUntillEnd(socket));

            if (callback != null)
            {
                await callback.Invoke();
            }

            return ret;
        }


        public static async Task<byte[]> ReadSocketUntillEnd(WebSocket socket)
        {
            byte[] buffer = new byte[4096];
            await socket.ReceiveAsync(buffer, CancellationToken.None);

            int totalSize = BitConverter.ToInt32(new byte[] { buffer[0], buffer[1], buffer[2], buffer[3] });
            int nullIndex = GetTerminationByte(buffer, 4) - 4;

            MemoryStream bufferStream = new MemoryStream();
            bufferStream.Write(buffer, 4, (nullIndex >= -1 ? nullIndex : 4092));

            int bytesRecieved = (nullIndex >= 0 ? nullIndex : 4092);

            while (totalSize > bytesRecieved)
            {
                await socket.ReceiveAsync(buffer, CancellationToken.None);

                nullIndex = GetTerminationByte(buffer, 4) - 4;
                bufferStream.Write(buffer, 4, (nullIndex >= 0 ? nullIndex : 4092) );
                bytesRecieved += (nullIndex >= 0 ? nullIndex : buffer.Length);
            }

            return bufferStream.ToArray();
        }

        public static async Task SendSocketUntillEnd(WebSocket socket, string data)
        {
            byte[] buffer = new byte[4096];

            byte[] stringBuffer = Encoding.UTF8.GetBytes(data);

            byte[] intBuffer = BitConverter.GetBytes(stringBuffer.Length);

            buffer[0] = intBuffer[0];
            buffer[1] = intBuffer[1];
            buffer[2] = intBuffer[2];
            buffer[3] = intBuffer[3];

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

        public static int GetTerminationByte(byte[] buffer, int offset)
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
