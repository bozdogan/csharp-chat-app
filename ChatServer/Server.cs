using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer
{
    class Server
    {
        private readonly IPEndPoint endpoint;
        private readonly Socket socket;

        private Dictionary<int, (Socket, NetworkStream)> clientList;
        private readonly object _clientListLock = new object();
        private int nMaxClient = 10;
        private int _lastClientId = 0;
        

        public Server(IPAddress ipAddress, int port)
        {
            this.endpoint = new IPEndPoint(ipAddress, port);
            socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            clientList = new Dictionary<int, (Socket, NetworkStream)>();
        }

        public void Start()
        {
            socket.Bind(endpoint);
            socket.Listen(128);

            Console.WriteLine("SERVER ONLINE");

            Task.Run(() => ListenLoop());
        }

        private async void ListenLoop()
        {
            while(true)
            {
                if(clientList.Count < nMaxClient)
                {
                    int clientId = -1;
                    try
                    {
                        Socket clientSocket = await Task.Factory.FromAsync(
                            new Func<AsyncCallback, object, IAsyncResult>(socket.BeginAccept),
                            new Func<IAsyncResult, Socket>(socket.EndAccept),
                            null).ConfigureAwait(false);

                        Console.WriteLine("[ListenLoop] New connection");

                        NetworkStream stream = new NetworkStream(clientSocket, true);
                        // NOTE(bora): As listen loop is async, two instances may be appending
                        // to the list at the same time. Use a lock here to make sure there is
                        // no collision.
                        lock(_clientListLock)
                        {
                            clientId = GenerateNextClientId();
                            clientList.Add(clientId, (clientSocket, stream));
                        }

                        Task.Run(() => ListenOneClient(clientId));
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"[ListenLoop] ERROR({e.GetType()}): {e.Message}");
                        if(clientId != -1)
                        {
                            Console.WriteLine($"[ListenLoop] Connection lost on Client #{clientId}");
                            DestroyClientConnection(clientId);
                        }
                    }

                }
                else
                {
                    // NOTE(bora): Prevent CPU burning
                    Thread.Sleep(500);
                }
            }
        }

        private async void ListenOneClient(int clientId)
        {
            (_, NetworkStream stream) = clientList[clientId];

            while(true)
            {
                try
                {
                    byte[] buffer = new byte[Common.Message.BUFFER_LENGTH];
                    int nBytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)
                    .ConfigureAwait(false);
                    if(nBytesRead == 0)
                    {
                        // NOTE(bora): Potentially lost connection.
                        Console.WriteLine($"[ListenOneClient] Client data is empty. ID: {clientId}");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer).Substring(0, nBytesRead);
                    Console.WriteLine($"MESSAGE({nBytesRead}) ::\n{message}\n");

                    // NOTE(bora): Broadcast everyone (echo back to this client as well)
                    Broadcast(Encoding.UTF8.GetBytes(message), -1);
                }
                catch(IOException)
                {
                    Console.WriteLine($"[ListenOneClient] Client #{clientId} hanged up");
                    break;
                }
            }

            DestroyClientConnection(clientId);
        }

        private async void Broadcast(byte[] buffer, int ignoredClietId)
        {
            foreach((int clientId, (_, NetworkStream stream)) in clientList)
            {
                if(clientId != ignoredClietId)
                {
                    await stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                }
            }
        }

        private void DestroyClientConnection(int clientId)
        {
            (_, NetworkStream networkStream) = clientList[clientId];

            // NOTE(bora): Network stream owns the socket so it will handle
            // closing the socket.
            networkStream.Close();
            lock(_clientListLock)
            {
                clientList.Remove(clientId);
            }
        }

        private int GenerateNextClientId()
        {
            return ++_lastClientId;
        }
    }
}
