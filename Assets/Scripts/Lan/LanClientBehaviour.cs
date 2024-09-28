using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class LanClientBehaviour : SingletonObject<LanClientBehaviour>
{
    private NetworkDriver driver;
    private NetworkConnection connection;

    private UdpClient udpClient;
    private IPEndPoint receiveEndPoint = new IPEndPoint(IPAddress.Any, LanCommon.LanPort);
    private bool parseUdpBroadcasts = true;

    public Action<string, string> ServerUDPMessageReceived { get; set; }
    public Action ConnectedToServer { get; set; }
    public Action DisconnectedFromServer { get; set; }
    public Action<string> ServerDataReceived { get; set; }

    [Header("Server info (DoNotEdit)")]
    public static string ServerIP;

    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        MainThreadRunner.EnsureInitialized();

        driver = NetworkDriver.Create();

        udpClient = new(receiveEndPoint);
        udpClient.BeginReceive(ReceiveUdpMessageCallback, null);
        ClientLog("Started listening for UDP broadcasts");
    }

    void OnDestroy()
    {
        Instance = null;
        driver.Dispose();

        if (udpClient != null)
        {
            udpClient.Close();
        }
    }

    void Update()
    {
        driver.ScheduleUpdate().Complete();

        if (IsConnectedToServer())
        {
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
            {
                switch (cmd)
                {
                    case NetworkEvent.Type.Connect:
                        OnConnected();
                        break;

                    case NetworkEvent.Type.Disconnect:
                        OnDisconnected();
                        break;

                    case NetworkEvent.Type.Data:
                        OnDataReceived(ref stream);
                        break;
                }
            }
        }
    }

    private void ReceiveUdpMessageCallback(IAsyncResult asyncResult)
    {
        byte[] receivedBytes = udpClient.EndReceive(asyncResult, ref receiveEndPoint);

        if (parseUdpBroadcasts)
        {
            string receivedMessage = Encoding.UTF8.GetString(receivedBytes);
            ClientLog($"Received UDP broadcast! Message '{receivedMessage}' from IP: {receiveEndPoint.Address}");
            ServerUDPMessageReceived.Invoke(receiveEndPoint.Address.ToString(), receivedMessage);
        }

        udpClient.BeginReceive(ReceiveUdpMessageCallback, null);
    }

    private static void ClientLog(string message, bool warning = false)
    {
        if (warning)
        {
            Debug.LogError($"Client warning: {message}");
        }
        else
        {
            Debug.Log($"Client: {message}");
        }
    }

    public bool IsConnectedToServer()
        => connection != null && connection.IsCreated;

    public bool TryStartConnect(string ip)
    {
        if (IsConnectedToServer())
        {
            ClientLog($"Already connected to server at {ServerIP}!", true);
            return false;
        }

        if (NetworkEndpoint.TryParse(ip, LanCommon.LanPort, out NetworkEndpoint endpoint))
        {
            ClientLog($"Trying to connect to IP {ip}, port {LanCommon.LanPort}");
            parseUdpBroadcasts = false;
            ServerIP = ip;

            connection = driver.Connect(endpoint);
            return true;
        }

        ClientLog($"Failed to connect to {ip}!", true);
        return false;
    }

    public void RequestSendData(string data)
    {
        if (IsConnectedToServer())
        {
            ClientLog($"Sending string data to server: {data}");
            driver.BeginSend(connection, out DataStreamWriter writer);
            writer.WriteFixedString128(data);
            driver.EndSend(writer);
            return;
        }

        ClientLog($"Cannot send data to server while disconnected!", true);
    }

    public void RequestDisconnect()
    {
        if (IsConnectedToServer())
        {
            ClientLog($"Requested disconnect from server at {ServerIP}");
            driver.Disconnect(connection);
            OnDisconnected();
        }
    }

    private void OnConnected()
    {
        ClientLog($"Connected to server at {ServerIP}");
        ConnectedToServer?.Invoke();
    }
    private void OnDataReceived(ref DataStreamReader stream)
    {
        string data = stream.ReadFixedString128().ToString();
        ClientLog($"Received string data from server: {data}");
        ServerDataReceived?.Invoke(data);
    }
    private void OnDisconnected()
    {
        ClientLog($"Disconnected from server at {ServerIP}");
        connection = default;
        parseUdpBroadcasts = true;
        DisconnectedFromServer?.Invoke();
    }
}
