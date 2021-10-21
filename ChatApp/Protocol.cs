using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public abstract class Protocol
    {
        const int HEADER_SIZE = 4;

        public async Task<T> ReceiveAsync<T>(NetworkStream stream)
        {
            byte[] header = await ReadAsync(stream, HEADER_SIZE).ConfigureAwait(false);
            int bodyLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header));

            // NOTE(bora): Check for illegal length. If length of the message body
            // returns negative, we may have a memory allocation problem.
            AssertValidMessageLength(bodyLength);

            byte[] body = await ReadAsync(stream, bodyLength).ConfigureAwait(false);
            return Decode<T>(body);
        }

        public async Task SendAsync<T>(NetworkStream stream, T message)
        {
            (byte[] header, byte[] body) = Encode(message);
            await stream.WriteAsync(header, 0, header.Length);
            await stream.WriteAsync(body, 0, body.Length);
        }

        async Task<byte[]> ReadAsync(NetworkStream stream, int numBytesToRead)
        {
            byte[] buffer = new byte[numBytesToRead];
            int numBytesRead = 0;

            while(numBytesRead < numBytesToRead)
            {
                int numRemainingBytes = (numBytesToRead - numBytesRead);
                int numReceivedBytes = await stream.ReadAsync(buffer, numBytesRead, numRemainingBytes).ConfigureAwait(false);

                if(numReceivedBytes != 0)
                {
                    numBytesRead += numReceivedBytes;
                }
                else
                {
                    throw new Exception("Connection Lost");
                }
            }

            return buffer;
        }

        protected (byte[] header, byte[] body) Encode<T>(T message)
        {
            byte[] body = EncodeBody(message);
            byte[] header = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(body.Length));
            return (header, body);
        }

        protected abstract T Decode<T>(byte[] message);

        protected abstract byte[] EncodeBody<T>(T message);

        protected void AssertValidMessageLength(int bodyLength)
        {
            if(bodyLength < 0)
            {
                throw new ArgumentOutOfRangeException("Invalid Message Length");
            }
        }
    }
}
