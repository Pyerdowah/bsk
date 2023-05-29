using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace server
{
    class Program
    {
        public static readonly List<TcpClient> Clients = new List<TcpClient>();
        static void Main(string[] args)
        {
            // create a TCP listener on port 1234
            TcpListener listener = new TcpListener(IPAddress.Any, 1234);
            listener.Start();

            Console.WriteLine("Server started. Listening for incoming connections...");

            while (true)
            {
                // accept an incoming client connection
                TcpClient client = listener.AcceptTcpClient();
                Clients.Add(client);

                Console.WriteLine("Client connected from " + client.Client.RemoteEndPoint.ToString());

                // start a new thread to handle the client
                ClientHandler clientHandler = new ClientHandler(client);
                clientHandler.Start();
            }
        }
    }

    class ClientHandler
    {
        private TcpClient client;
        private Thread thread;
        private Thread availableThread;
        private static int ONE_KB = 4 * 1024; 
        private static int previousClientCount = 0;

        public ClientHandler(TcpClient client)
        {
            this.client = client;
        }

        public void Start()
        {
            thread = new Thread(HandleClient);
            thread.Start();
            availableThread = new Thread(AvailableHandler);
            availableThread.Start();
        }

        private void HandleClient()
        {
            try
            {
                // get the client stream
                NetworkStream stream = client.GetStream();
                byte[] buffer;
                while (client.Connected)
                { 
                   buffer = new byte[ONE_KB];
                   int bytesRead = stream.Read(buffer, 0, buffer.Length);
                   Console.WriteLine("Received message from client: " + buffer);
                    foreach (TcpClient otherClient in Program.Clients)
                    {
                        if (otherClient != client && otherClient.Connected)
                        {
                            otherClient.GetStream().Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                client.Close();
                Program.Clients.Remove(client);
            }
        }
        public void AvailableHandler()
        {
            while (client.Connected)
            {
                int currentClientCount = Program.Clients.Count;
                byte available;

               /* if (currentClientCount == previousClientCount)
                {
                    // Brak zmiany liczby klientów
                    continue;
                }*/
                if (currentClientCount > 1)
                {
                    available = 1;
                }
                else
                {
                    available = 0;
                }
                client.GetStream().WriteByte(available);
                Thread.Sleep(5000);
               // previousClientCount = currentClientCount;
            }
        }
    }

}
