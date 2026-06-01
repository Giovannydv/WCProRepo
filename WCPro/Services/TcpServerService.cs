using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace WCPro.Services
{
    public class TcpServerService
    {
        private TcpListener listener;

        public event Action<string> MessageReceived;

        public async void Start()
        {
            listener =
                new TcpListener(
                    IPAddress.Loopback,
                    5000);

            listener.Start();

            while (true)
            {
                TcpClient client =
                    await listener.AcceptTcpClientAsync();

                _ = HandleClient(client);
            }
        }

        private async Task HandleClient(
            TcpClient client)
        {
            using (client)
            {
                NetworkStream stream =
                    client.GetStream();

                byte[] buffer =
                    new byte[1024];

                int bytesRead =
                    await stream.ReadAsync(
                        buffer,
                        0,
                        buffer.Length);

                string message =
                    Encoding.UTF8.GetString(
                        buffer,
                        0,
                        bytesRead);

                MessageReceived?.Invoke(
                    message);
            }
        }
    }
}
