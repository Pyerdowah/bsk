using System;
using System.Collections.Generic;
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

        public ClientHandler(TcpClient client)
        {
            this.client = client;
        }

        public void Start()
        {
            thread = new Thread(HandleClient);
            thread.Start();
            availableThread = new Thread(AvailableHandler);
           // availableThread.Start();
        }

        private void HandleClient()
        {
            try
            {
                // get the client stream
                NetworkStream stream = client.GetStream();

                // send a welcome message to the client
                string welcomeMessage = "Welcome to the server!";
                byte[] welcomeMessageBytes = Encoding.ASCII.GetBytes(welcomeMessage);
               // stream.Write(welcomeMessageBytes, 0, welcomeMessageBytes.Length);

                while (client.Connected)
                { 
                   byte[] buffer = new byte[1024 * 1024]; // 1 MB
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
                if (Program.Clients.Count > 1)
                {
                    string availableMess = "Drugi dostepny";
                    byte[] available = Encoding.ASCII.GetBytes(availableMess);
                    client.GetStream().Write(available, 0, available.Length);
                }
                else
                {
                    string availableMess = "Drugi niedostepny";
                    byte[] available = Encoding.ASCII.GetBytes(availableMess);
                    client.GetStream().Write(available, 0, available.Length);
                }
                Thread.Sleep(8000);
            }
        }
    }

}
