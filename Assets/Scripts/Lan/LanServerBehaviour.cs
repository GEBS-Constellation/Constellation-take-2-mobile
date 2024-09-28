using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class LanServerBehaviour : SingletonObject<LanServerBehaviour>
{
    public static string IP { get; private set; }

    private NetworkDriver driver;
    private NativeList<NetworkConnection> connections;

    public List<Guid> ClientIds = new List<Guid>();

    private UdpClient udpClient;
    private IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, LanCommon.LanPort);
    private bool _udpBroadcastActive = true;
    private float _udpTimerDelay = 3;
    private float _udpTimerCurrentTime = 0;
    private string _udpBroadcastMessage = "Constellation Server";

    public int MaxPlayerCount = 8;

    public Action<Guid> ClientConnected { get; set; }
    public Action<Guid> ClientDisconnected { get; set; }
    public Action<string> ClientDataReceived { get; set; }

    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        MainThreadRunner.EnsureInitialized();

        IP = LanCommon.GetLocalIP();

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
        Instance = null;

        if (driver.IsCreated)
        {
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
        if (_udpBroadcastActive && !string.IsNullOrWhiteSpace(IP))
        {
            _udpTimerCurrentTime += Time.deltaTime;
            if (_udpTimerCurrentTime >= _udpTimerDelay)
            {
                _udpTimerCurrentTime = 0;
                SendUdpBroadcast(_udpBroadcastMessage);
            }
        }

        driver.ScheduleUpdate().Complete();

        // Clean up connections
        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                i--;
            }
        }

        // Accept new connections
        NetworkConnection c;
        while ((c = driver.Accept()) != default)
        {
            connections.Add(c);
            Guid id = Guid.NewGuid();
            ServerLog($"New client connected! Index {connections.Length - 1}, ID {id}");
            ClientIds.Add(id);
            ClientConnected?.Invoke(id);
        }

        // Handle new events
        for (int i = 0; i < connections.Length; i++)
        {
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                switch (cmd)
                {
                    case NetworkEvent.Type.Disconnect:
                        Guid disconnectedId = ClientIds[i];
                        ServerLog($"Client disconnected! Index {i}, ID {disconnectedId}");
                        ClientIds.Remove(disconnectedId);
                        ClientDisconnected?.Invoke(disconnectedId);
                        connections[i] = default;
                        break;

                    case NetworkEvent.Type.Data:
                        string data = stream.ReadFixedString128().ToString();
                        Guid dataClientId = ClientIds[i];
                        ServerLog($"Received string data from client index {i} id {dataClientId}: {data}");
                        ClientDataReceived?.Invoke(data);
                        break;
                }
            }
        }
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

    public void ToggleUdpBroadcast(bool enabled)
    {
        _udpBroadcastActive = enabled;
    }
    public void SendUdpBroadcast(string message)
    {
        if (_udpBroadcastActive)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            udpClient.Send(messageBytes, messageBytes.Length, broadcastEndPoint);
            ServerLog($"UDP broadcast message sent: {message}");
        }
    }

    public void RequestSendData(Guid id, string data)
    {
        int index = ClientIds.FindIndex(x => x == id);
        if (index != -1)
        {
            RequestSendData(index, data);
            return;
        }

        ServerLog($"Requested send data to invalid client id {id}", true);
    }
    private void RequestSendData(int index, string data)
    {
        if (index >= 0 && index < ClientIds.Count)
        {
            ServerLog($"Sending data [{data}] to client index {index}");
            driver.BeginSend(connections[index], out DataStreamWriter writer);
            writer.WriteFixedString128(data);
            driver.EndSend(writer);
            return;
        }

        ServerLog($"Requested send data to invalid client index {index}", true);
    }

    public void RequestKickPlayer(Guid id)
    {
        int index = ClientIds.FindIndex(x => x == id);
        if (index != -1)
        {
            RequestKickPlayer(index);
            return;
        }

        ServerLog($"Requested kicking invalid client id {id}", true);
    }
    private void RequestKickPlayer(int index)
    {
        if (index >= 0 && index < ClientIds.Count)
        {
            ServerLog($"Kicking client index {index}");
            driver.Disconnect(connections[index]);
            return;
        }

        ServerLog($"Requested kicking invalid client index {index}", true);
    }
}
