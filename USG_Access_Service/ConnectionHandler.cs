using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace USG_Access_Service
{
    internal class ConnectionHandler
    {
        private const int _PORT_NUMBER = 9050; // Port number for the connection handler.
        private const int _BROADCAST_PORT_NUMBER = 9049; // Port number for discovery broadcast.
        private const int _SOCKET_TIMEOUT = 2000;
        private static ConnectionHandler _instance;

        private Socket _client;
        private readonly string _localIp;
        private readonly byte[] _intBuffer = new byte[4];
        private readonly byte[] _commandBuffer = new byte[CommandHandler.MAX_COMMAND_LENGTH];
        private readonly CommandHandler _commandHandler;

        /// <summary>
        /// Initializes new <see cref="ConnectionHandler"/> instance.
        /// </summary>
        private ConnectionHandler()
        {
            _localIp = GetLocalIpAddress();
            _commandHandler = new CommandHandler(this);
        }

        /// <summary>
        /// Creates singleton object and starts infinite listening loop.
        /// </summary>
        public static void StartHandler()
        {
            _instance = new ConnectionHandler();
            while (true)
            {
                _instance.StartListening();
            }
        }

        /// <summary>
        /// Waits for PJATK USG client broadcast to make connection.
        /// </summary>
        public void WaitForClient()
        {
            Console.WriteLine("Waiting for UDP client broadcast...");
            var udpClient = new UdpClient(_BROADCAST_PORT_NUMBER);
            var responseData = Encoding.ASCII.GetBytes("PJATK_USG_SERVER_ACK");
            IPEndPoint clientEp = new IPEndPoint(IPAddress.Any, 0); 
            string clientRequest;

            do
            {
                Console.WriteLine("Receive broadcast...");
                var clientRequestData = udpClient.Receive(ref clientEp);
                Console.WriteLine("Received");
                clientRequest = Encoding.ASCII.GetString(clientRequestData);

                Console.WriteLine("Received {0} from {1}.", clientRequest, clientEp.Address);
            } while (clientRequest != "LF_PJATK_USG_SERVER");

            udpClient.Send(responseData, responseData.Length, clientEp);
            udpClient.Close();
        }

        public void StartListening()
        {
            Console.WriteLine("Starting USG network service...");
            try
            {
                WaitForClient();
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Broadcast socket unavailable: {ex.Message}");
                Environment.Exit(1);
            }

            var newsock =
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveTimeout = _SOCKET_TIMEOUT,
                    SendTimeout = _SOCKET_TIMEOUT
                };

            try
            {
                // Accept connection.
                Console.WriteLine($"Waiting client at {_localIp}:{_PORT_NUMBER}...");
                newsock.Bind(new IPEndPoint(IPAddress.Any, _PORT_NUMBER));
                newsock.Listen(10);
                AutoResetEvent accepted = new AutoResetEvent(false);
                newsock.BeginAccept(ar =>
                {
                    if (ar == null || newsock == null)
                    {
                        return;
                    }
                    try
                    {
                        _client = newsock.EndAccept(ar);
                        ar.AsyncWaitHandle.WaitOne();
                        Console.WriteLine(
                            $"Client accepted. Remote: {((IPEndPoint) _client.RemoteEndPoint).Address}:{((IPEndPoint) _client.RemoteEndPoint).Port}");
                        accepted.Set();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Accept exception: {e.Message}\n{e.ToString()}");
                    }
                }, newsock);

                if (!accepted.WaitOne(_SOCKET_TIMEOUT))
                {
                    throw new TimeoutException("Error: Client connection accept timeout.");
                }

                Console.WriteLine("Main routine...");

                // Maintain commands.
                while (IsClientConnected)
                {
                    var command = ReceiveString();
                    if (command == null
                    ) // If we won't get any command for socket receive timeout, we close the connection.
                    {
                        Console.WriteLine("Connection timeout.");
                        break;
                    }

                    _commandHandler.HandleCommand(command);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Socket exception: " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}\n{e.Source}");
            }
            finally
            {
                newsock?.Close();
                newsock = null;
                if (IsClientConnected)
                {
                    Console.WriteLine($"Disconnecting client {((IPEndPoint)_client.RemoteEndPoint).Address}");
                    _client.Shutdown(SocketShutdown.Both);
                    _client.Close();
                    _client = null;
                }
                Console.WriteLine("Socket client closed.");
            }

        }

        private string ReceiveString()
        {
            if (_client.Receive(_intBuffer, 4, SocketFlags.None) < 4)
            {
                return null;
            }
            int length = BitConverter.ToInt32(_intBuffer, 0);
            if (length <=0 || length > _commandBuffer.Length)
            {
                throw new InvalidDataException("Incorrect command length: " + length);
            }

            _client.Receive(_commandBuffer, length, SocketFlags.None);
            return Encoding.ASCII.GetString(_commandBuffer, 0, length);
        }

        public void SendString(string message)
        {
            Console.WriteLine("Sending string: " + message);
            SendByteArray(Encoding.ASCII.GetBytes(message));
        }

        public void SendError(string message)
        {
            Console.WriteLine("Sending error: " + message);
            SendInteger(0);
            SendString(message);
        }

        internal void SendInteger(int valueToSend)
        {
            _client.Send(BitConverter.GetBytes(valueToSend));
        }

        internal void SendByteArray(byte[] data)
        {
            int dataTotal = data.Length;
            int datatSent = 0;

            try
            {
                //Console.WriteLine("Sending array length.");
                SendInteger(dataTotal);

                // Send data content
                //Console.WriteLine($"Sending {dataTotal} bytes.\n" +
                //                  $"First: {data[0]} {data[1]}.\n" +
                //                  $"Last: {data[dataTotal - 1]} {data[dataTotal - 2]}...");
                while (datatSent < dataTotal)
                {
                    datatSent += _client.Send(data, datatSent, dataTotal - datatSent, SocketFlags.None);
                    //Console.WriteLine($"Sent {datatSent}/{dataTotal}...");
                }

                //Console.WriteLine("Send finished.");
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException: " + ex.Message);
            }
        }

        public static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found! - video service");
        }

        public bool IsClientConnected => _client != null && _client.Connected;
        //{
        //    get
        //    {
        //        // true if a) Listen has been called and a connection is pending,
        //        // or b) data is available for reading,
        //        // or c) the connection has been closed, reset, or terminated.
        //        bool readStatus = _client.Poll(1000, SelectMode.SelectRead);
        //        bool hasData = _client.Available > 0;
        //        return !readStatus || hasData;
        //    }
        //}
    }
}
