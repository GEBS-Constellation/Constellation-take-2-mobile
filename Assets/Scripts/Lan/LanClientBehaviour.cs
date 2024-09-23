using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;

public class LanClientBehaviour : MonoBehaviour
{
    public static LanClientBehaviour Instance;

    private NetworkDriver driver;
    private NativeArray<NetworkConnection> connection;
    private JobHandle clientJobHandle;

    private UdpClient udpClient;
    private IPEndPoint receiveEndPoint = new IPEndPoint(IPAddress.Any, LanCommon.LanPort);

    public Action<string, string> ServerUDPMessageReceived { get; set; }

    [Header("Server info (DoNotEdit)")]
    public static string ServerIP;

    private struct ClientUpdateJob : IJob
    {
        public NetworkDriver Driver;
        public NativeArray<NetworkConnection> Connection;

        public void Execute()
        {
            if (!Connection[0].IsCreated)
            {
                return;
            }

            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = Connection[0].PopEvent(Driver, out stream)) != NetworkEvent.Type.Empty)
            {
                switch (cmd)
                {
                    case NetworkEvent.Type.Connect:
                        ClientLog($"Successfully connected to IP: {ServerIP}");

                        RequestSendData(ref Driver, Connection[0], 321);
                        break;

                    case NetworkEvent.Type.Data:
                        uint value = stream.ReadUInt();
                        ClientLog($"Got the uint value {value} back from IP: {ServerIP}");
                        break;

                    case NetworkEvent.Type.Disconnect:
                        ClientLog($"Disconnected from IP: {ServerIP}");
                        Connection[0] = default;
                        break;
                }
            }
        }
    }

    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        driver = NetworkDriver.Create();
        connection = new NativeArray<NetworkConnection>(1, Allocator.Persistent);

        udpClient = new(receiveEndPoint);
        udpClient.BeginReceive(ReceiveUdpMessageCallback, null);
    }

    void OnDestroy()
    {
        clientJobHandle.Complete();
        driver.Dispose();
        connection.Dispose();

        if (udpClient != null)
        {
            udpClient.Close();
        }
    }

    void Update()
    {
        clientJobHandle.Complete();

        ClientUpdateJob job = new ClientUpdateJob
        {
            Driver = driver,
            Connection = connection,
        };
        clientJobHandle = driver.ScheduleUpdate();
        clientJobHandle = job.Schedule(clientJobHandle);
    }

    public bool TryStartConnect(string ip)
    {
        if (NetworkEndpoint.TryParse(ip, LanCommon.LanPort, out NetworkEndpoint endpoint))
        {
            ClientLog($"Trying to connect to IP {ip}, port {LanCommon.LanPort}");
            ServerIP = ip;
            clientJobHandle.Complete();
            connection[0] = driver.Connect(endpoint);
            return true;
        }

        ClientLog($"Failed to connect to {ip}!", true);
        return false;
    }

    public void RequestSendData(uint value)
    {
        driver.BeginSend(connection[0], out DataStreamWriter writer);
        writer.WriteUInt(value);
        driver.EndSend(writer);
    }
    public static void RequestSendData(ref NetworkDriver driver, NetworkConnection connection, uint value)
    {
        driver.BeginSend(connection, out DataStreamWriter writer);
        writer.WriteUInt(value);
        driver.EndSend(writer);
    }

    private void ReceiveUdpMessageCallback(IAsyncResult asyncResult)
    {
        byte[] receivedBytes = udpClient.EndReceive(asyncResult, ref receiveEndPoint);
        string receivedMessage = Encoding.UTF8.GetString(receivedBytes);
        ClientLog($"Received UDP broadcast! Message '{receivedMessage}' from IP: {receiveEndPoint.Address}");
        ServerUDPMessageReceived.Invoke(receiveEndPoint.Address.ToString(), receivedMessage);

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
}
