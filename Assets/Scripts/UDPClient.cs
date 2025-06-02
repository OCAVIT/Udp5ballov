using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public class UDPClient : MonoBehaviour
{
    [Header("Client Settings")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 12345;
    public float requestTimeout = 5f;

    private UdpClient udpClient;
    private Thread clientThread;
    private bool isClientRunning = false;
    private Queue<string> messageQueue = new Queue<string>();
    private object queueLock = new object();
    private bool isConnected = false;

    void Start()
    {
        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            udpClient = new UdpClient();
            clientThread = new Thread(ClientLoop);
            isClientRunning = true;
            clientThread.Start();
            isConnected = true;

            Debug.Log($"UDP Client connected to {serverIP}:{serverPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to server: {e.Message}");
            isConnected = false;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && isConnected)
        {
            RequestDamage();
        }

        ProcessMessageQueue();
    }

    void RequestDamage()
    {
        try
        {
            var requestPacket = new DamagePacket
            {
                messageType = "DAMAGE_REQUEST",
                damage = 0,
                timestamp = DateTime.Now.ToString("HH:mm:ss.fff")
            };

            string jsonRequest = JsonConvert.SerializeObject(requestPacket);
            byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);

            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
            udpClient.Send(requestData, requestData.Length, serverEndPoint);

            Debug.Log("Damage request sent to server");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send damage request: {e.Message}");
        }
    }

    void ClientLoop()
    {
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);

        while (isClientRunning)
        {
            try
            {
                if (udpClient.Available > 0)
                {
                    byte[] receivedData = udpClient.Receive(ref serverEndPoint);
                    string receivedMessage = Encoding.UTF8.GetString(receivedData);

                    lock (queueLock)
                    {
                        messageQueue.Enqueue(receivedMessage);
                    }
                }

                Thread.Sleep(10);
            }
            catch (Exception e)
            {
                if (isClientRunning)
                {
                    Debug.LogError($"Client error: {e.Message}");
                }
            }
        }
    }

    void ProcessMessageQueue()
    {
        lock (queueLock)
        {
            while (messageQueue.Count > 0)
            {
                string message = messageQueue.Dequeue();
                ProcessServerResponse(message);
            }
        }
    }

    void ProcessServerResponse(string jsonResponse)
    {
        try
        {
            var responsePacket = JsonConvert.DeserializeObject<DamagePacket>(jsonResponse);

            if (responsePacket.messageType == "DAMAGE_RESPONSE")
            {
                Debug.Log($"Урон: {responsePacket.damage}");

                Debug.Log($"[{responsePacket.timestamp}] Получен урон от сервера: {responsePacket.damage}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to process server response: {e.Message}");
        }
    }

    void OnApplicationQuit()
    {
        DisconnectFromServer();
    }

    void OnDestroy()
    {
        DisconnectFromServer();
    }

    void DisconnectFromServer()
    {
        isClientRunning = false;
        isConnected = false;

        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }

        if (clientThread != null && clientThread.IsAlive)
        {
            clientThread.Join(1000);
            if (clientThread.IsAlive)
            {
                clientThread.Abort();
            }
        }

        Debug.Log("Client disconnected");
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"Client Status: {(isConnected ? "Connected" : "Disconnected")}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Server: {serverIP}:{serverPort}");
        GUI.Label(new Rect(10, 50, 300, 20), "Left Click - Request Damage");

        if (!isConnected)
        {
            if (GUI.Button(new Rect(10, 70, 100, 30), "Reconnect"))
            {
                ConnectToServer();
            }
        }
    }
}