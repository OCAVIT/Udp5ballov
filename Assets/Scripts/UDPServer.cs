using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;

public class UDPServer : MonoBehaviour
{
    [Header("Server Settings")]
    public int serverPort = 12345;
    public int minDamage = 10;
    public int maxDamage = 100;

    private UdpClient udpServer;
    private Thread serverThread;
    private bool isServerRunning = false;
    private System.Random random;

    void Start()
    {
        random = new System.Random();
        StartServer();
    }

    void StartServer()
    {
        try
        {
            udpServer = new UdpClient(serverPort);
            serverThread = new Thread(ServerLoop);
            isServerRunning = true;
            serverThread.Start();

            Debug.Log($"UDP Server started on port {serverPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start server: {e.Message}");
        }
    }

    void ServerLoop()
    {
        IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isServerRunning)
        {
            try
            {
                byte[] receivedData = udpServer.Receive(ref clientEndPoint);
                string receivedMessage = Encoding.UTF8.GetString(receivedData);

                Debug.Log($"Received request from {clientEndPoint}: {receivedMessage}");

                var requestPacket = JsonConvert.DeserializeObject<DamagePacket>(receivedMessage);

                if (requestPacket.messageType == "DAMAGE_REQUEST")
                {
                    int generatedDamage = random.Next(minDamage, maxDamage + 1);

                    var responsePacket = new DamagePacket
                    {
                        messageType = "DAMAGE_RESPONSE",
                        damage = generatedDamage,
                        timestamp = DateTime.Now.ToString("HH:mm:ss.fff")
                    };

                    string jsonResponse = JsonConvert.SerializeObject(responsePacket);
                    byte[] responseData = Encoding.UTF8.GetBytes(jsonResponse);
                    udpServer.Send(responseData, responseData.Length, clientEndPoint);

                    Debug.Log($"Sent damage {generatedDamage} to {clientEndPoint}");
                }
            }
            catch (Exception e)
            {
                if (isServerRunning)
                {
                    Debug.LogError($"Server error: {e.Message}");
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        StopServer();
    }

    void OnDestroy()
    {
        StopServer();
    }

    void StopServer()
    {
        isServerRunning = false;

        if (udpServer != null)
        {
            udpServer.Close();
            udpServer = null;
        }

        if (serverThread != null && serverThread.IsAlive)
        {
            serverThread.Join(1000);
            if (serverThread.IsAlive)
            {
                serverThread.Abort();
            }
        }

        Debug.Log("Server stopped");
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"Server Status: {(isServerRunning ? "Running" : "Stopped")}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Port: {serverPort}");
        GUI.Label(new Rect(10, 50, 300, 20), $"Damage Range: {minDamage}-{maxDamage}");
    }
}