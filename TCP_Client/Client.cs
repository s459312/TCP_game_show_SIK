using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using TCP_Server;

namespace TCP_Client
{
    class Client
    {
        private static readonly Socket ClientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private static bool IsWaiting = true;


        static void Main()
        {
            Console.Title = "Client";
            ConnectToServer();
            RequestLoop();
            Exit();
        }

        private static void ConnectToServer()
        {
            while (!ClientSocket.Connected)
            {
                try
                {
                    ClientSocket.Connect(IPAddress.Loopback, 888);
                }
                catch (SocketException)
                {
                    Console.WriteLine("Could not connect to the server");
                }
            }
            Console.WriteLine("Connected");
            SendString("hello");
        }

        private static void RequestLoop()
        {
            Console.WriteLine(@"Type 'exit' to properly disconnect client");

            while (true)
            {
                if(!IsWaiting)
                    SendRequest();
                ReceiveResponse();
            }
        }

        private static void Exit()
        {
            SendString("exit"); 
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            Environment.Exit(0);
        }

        private static void SendRequest()
        {
            Console.Write("Send a request: ");
            string request = Console.ReadLine();
            SendString(request);
            IsWaiting = true;

            if (request.ToLower() == "exit")
            {
                Exit();
            }
        }

        private static void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private static void ReceiveResponse()
        {
            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            var deserializedMessage = JsonSerializer.Deserialize<Message>(text);
            Console.WriteLine(deserializedMessage.Text);
            IsWaiting = deserializedMessage.Wait;
        }
    }
}