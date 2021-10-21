using System;
using System.Net;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server");

            Server server = new Server(IPAddress.Parse("0.0.0.0"), 5001);
            server.Start();
            while(Console.ReadLine() != "quit")
                ;
        }
    }
}
