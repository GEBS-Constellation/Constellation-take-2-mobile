using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;

public class LanServerBehaviour : MonoBehaviour
{
    public static LanServerBehaviour Instance;

    private NetworkDriver driver;
    private NativeList<NetworkConnection> connections;
    private JobHandle serverJobHandle;

    private UdpClient udpClient;
    private IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, LanCommon.LanPort);

    public int MaxPlayerCount = 8;

    private struct ServerUpdateConnectionsJob : IJob
    {
        public NetworkDriver Driver;
        public NativeList<NetworkConnection> Connections;

        public void Execute()
        {
            // Clean up connections.
            for (int i = 0; i < Connections.Length; i++)
            {
                if (!Connections[i].IsCreated)
                {
                    Connections.RemoveAtSwapBack(i);
                    i--;
                }
            }

            // Accept new connections.
            NetworkConnection c;
            while ((c = Driver.Accept()) != default)
            {
                Connections.Add(c);
                ServerLog("Accepted new client connection.");
            }
        }
    }

    private struct ServerUpdateJob : IJobParallelForDefer
    {
        public NetworkDriver.Concurrent Driver;
        public NativeArray<NetworkConnection> Connections;

        public void Execute(int i)
        {
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = Driver.PopEventForConnection(Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                switch (cmd)
                {
                    case NetworkEvent.Type.Connect:
                        ServerLog($"Client {i} connected!");

                        RequestSendData(ref Driver, Connections[i], 123);
                        break;

                    case NetworkEvent.Type.Data:
                        uint number = stream.ReadUInt();
                        ServerLog($"Got uint {number} from client {i}");
                        break;

                    case NetworkEvent.Type.Disconnect:
                        ServerLog($"Client {i} disconnected!");
                        Connections[i] = default;
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
        connections = new NativeList<NetworkConnection>(MaxPlayerCount, Allocator.Persistent);

        NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4.WithPort(LanCommon.LanPort);
        if (driver.Bind(endpoint) != 0)
        {
            ServerLog($"Failed to bind to port: {LanCommon.LanPort}", true);
            return;
        }
        driver.Listen();

        udpClient = new()
        {
            EnableBroadcast = true
        };
    }

    void OnDestroy()
    {
        if (driver.IsCreated)
        {
            serverJobHandle.Complete();
            driver.Dispose();
            connections.Dispose();
        }

        if (udpClient != null)
        {
            udpClient.Close();
        }
    }

    void Update()
    {
        serverJobHandle.Complete();

        ServerUpdateConnectionsJob connectionJob = new ServerUpdateConnectionsJob
        {
            Driver = driver,
            Connections = connections
        };

        ServerUpdateJob serverUpdateJob = new ServerUpdateJob
        {
            Driver = driver.ToConcurrent(),
            Connections = connections.AsDeferredJobArray()
        };

        serverJobHandle = driver.ScheduleUpdate();
        serverJobHandle = connectionJob.Schedule(serverJobHandle);
        serverJobHandle = serverUpdateJob.Schedule(connections, 1, serverJobHandle);
    }

    public void SendUdpBroadcast(string message)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        udpClient.Send(messageBytes, messageBytes.Length, broadcastEndPoint);
        ServerLog($"UDP broadcast message sent: {message}");
    }

    public void RequestSendData(int index, uint value)
    {
        driver.BeginSend(connections[index], out DataStreamWriter writer);
        writer.WriteUInt(value);
        driver.EndSend(writer);
    }
    public static void RequestSendData(ref NetworkDriver.Concurrent driver, NetworkConnection connection, uint value)
    {
        driver.BeginSend(connection, out DataStreamWriter writer);
        writer.WriteUInt(value);
        driver.EndSend(writer);
    }

    private static void ServerLog(string message, bool warning = false)
    {
        if (warning)
        {
            Debug.LogError($"Server warning: {message}");
        }
        else
        {
            Debug.Log($"Server: {message}");
        }
    }
}
