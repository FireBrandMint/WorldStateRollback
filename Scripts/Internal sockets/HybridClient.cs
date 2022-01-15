using Godot;
using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

public class HybridClient
{
    Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    UdpClient udp;

    ConcurrentBag<byte[]> messages = new ConcurrentBag<byte[]>();

    public bool CouldConnect = false;

    public bool Functioning = true;

    public HybridClient (string ip, int port)
    {
        StartClient(ip, port);
    }

    void StartClient (string ip, int port)
    {
        int attempts = 30;

        for (int i = 0; i<attempts; ++i)
        {
            try
            {
                clientSocket.Connect(IPAddress.Parse(ip), port);
                udp = new UdpClient(((IPEndPoint)clientSocket.LocalEndPoint).Port);
                udp.Connect(IPAddress.Parse(ip), port);
                CouldConnect = true;
                GD.Print("Client connected!");
                break;
            }
            catch { GD.Print("Client couldn't connect!");}
        }

        if (CouldConnect)
        {
            var thread = new System.Threading.Thread(ReceiveMessages);
            //thread.Priority = ThreadPriority.Lowest;
            thread.Start();
            
            var thread2 = new System.Threading.Thread(ReceiveMessagesUDP);
            //thread.Priority = ThreadPriority.Lowest;
            thread2.Start();
        }
    }

    public void Send (byte[] message)
    {
        try
        {
            Int32 sizee = message.Length;
            clientSocket.Send(BitConverter.GetBytes(sizee));
            clientSocket.Send(message);
        }
        catch {}
    }

    public void SendUDP (byte[] message)
    {
        udp.Send(message, message.Length);
    }

    public int MessageCount () => messages.Count;

    public byte[] GetMessage ()
    {
        byte[] message;
        if (messages.TryTake(out message)) return message;
        return null;
    }

    public void Close ()
    {
        clientSocket.Close();
        Functioning = false;
    }

    void ReceiveMessages ()
    {
        while(Functioning)
        {
                try
                {
                var rece = new byte[4];
                /*if (clientSocket.Available == 0)
                {
                    if (Sleep)
                    {
                        Sleep = false;
                        System.Threading.Thread.Sleep(4);
                    }
                    continue;
                }*/

                clientSocket.Receive(rece);

                int toReceive = BitConverter.ToInt32(rece, 0);

                byte[] receiveBuffer = new byte[toReceive];

                clientSocket.Receive(receiveBuffer);
            
                messages.Add(receiveBuffer);
            }
            catch
            {
                break;
            }
        }
    }

    void ReceiveMessagesUDP ()
    {
        while(Functioning)
        {
            try
            {
                var rEndPoint = new IPEndPoint(IPAddress.Any, 0);

                byte[] message = udp.Receive(ref rEndPoint);

                messages.Add(message);
            }
            catch
            {
                break;
            }
        }
    }

}