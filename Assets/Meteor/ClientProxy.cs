﻿using CoClass;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

//处理与服务器的连接.没有p2p
class ClientProxy
{
    public static bool quit = false;
    public static AutoResetEvent logicEvent = new AutoResetEvent(false);//负责收到服务器后的响应线程的激活.
    public static IPEndPoint server;
    public static TcpProxy proxy;
    public static Dictionary<int, byte[]> Packet = new Dictionary<int, byte[]>();//消息ID和字节流
    static Timer tConn;

    public static void Init()
    {
        quit = false;
        InitServerCfg();
        if (tConn == null)
            tConn = new Timer(TryConn, null, 0, 5000);
    }

    public static void TryConn(object param)
    {
        if (sProxy == null)
            sProxy = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sProxy.BeginConnect(server, OnTcpConnect, sProxy);
        if (tConn != null)
            tConn.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public static void OnTcpConnect(IAsyncResult ret)
    {
        LocalMsg result = new LocalMsg();
        try
        {
            sProxy.EndConnect(ret);
            if (tConn != null)
                tConn.Change(Timeout.Infinite, Timeout.Infinite);
        }
        catch (Exception exp)
        {
            Log.LogInfo(exp.Message);
            result.Message = (int)LocalMsgType.Connect;
            result.Result = 0;
            ProtoHandler.PostMessage(result);
            if (tConn != null)
                tConn.Change(5000, 5000);
            return;
        }

        result.Message = (int)LocalMsgType.Connect;
        result.Result = 1;
        ProtoHandler.PostMessage(result);
        if (proxy == null)
            proxy = new TcpProxy();
        try
        {
            sProxy.BeginReceive(proxy.GetBuffer(), 0, TcpProxy.PacketSize, SocketFlags.None, OnReceivedData, sProxy);
        }
        catch
        {
            result.Message = (int)LocalMsgType.DisConnect;
            result.Result = 0;
            ProtoHandler.PostMessage(result);
            sProxy.Close();
            sProxy = null;
            proxy = null;
            if (tConn != null)
                tConn.Change(5000, 5000);
        }
    }

    static void OnReceivedData(IAsyncResult ar)
    {
        int len = 0;
        try
        {
            len = sProxy.EndReceive(ar);
        }
        catch
        {
                
        }
        if (len <= 0)
        {
            if (!quit)
            {
                LocalMsg msg = new LocalMsg();
                msg.Message = (int)LocalMsgType.DisConnect;
                msg.Result = 1;
                ProtoHandler.PostMessage(msg);
                sProxy.Close();
                sProxy = null;
                proxy = null;
                if (tConn != null)
                    tConn.Change(5000, 5000);
            }
            return;
        }

        lock (Packet)
        {
            if (!proxy.Analysis(len, Packet))
            {
                sProxy.Close();
                sProxy = null;
                return;
            }
        }
        logicEvent.Set();

        if (!quit)
        {
            try
            {
                sProxy.BeginReceive(proxy.GetBuffer(), 0, TcpProxy.PacketSize, SocketFlags.None, OnReceivedData, sProxy);
            }
            catch
            {
                LocalMsg msg = new LocalMsg();
                msg.Message = (int)LocalMsgType.DisConnect;
                msg.Result = 1;
                ProtoHandler.PostMessage(msg);
                sProxy.Close();
                sProxy = null;
                proxy = null;
                if (tConn != null)
                    tConn.Change(5000, 5000);
            }
        }
    }

    static void InitServerCfg()
    {
        if (server == null)
        {
            IPAddress[] addr = Dns.GetHostAddresses(GameData.Domain);
            if (addr.Length != 0)
                server = new IPEndPoint(addr[0], GameData.GatePort);
        }
    }

    public static void OnLogout(uint userid, Action<RBase> cb)
    {
    }

    public static Socket sProxy = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public static void Exit()
    {
        quit = true;
        if (sProxy != null && sProxy.Connected)
            sProxy.Close();
        sProxy = null;
        if (tConn != null)
        {
            tConn.Dispose();
            tConn = null;
        }
    }

    //发出的.
    //网关服务器从中心服务器取得游戏服务器列表.
    public static void UpdateGameServer()
    {
        Common.SendUpdateGameServer();
    }

    public static void JoinRoom(int roomId)
    {
        Common.SendJoinRoom(roomId);
    }

    //public static void SendBattleResult(bool result, int battleId, List<int> monster, Action<RBase> cb = null)
    //{
    //    try
    //    {
    //        InitServerCfg();
    //        Common.SendBattleResult(result, battleId, monster, cb);
    //    }
    //    catch (Exception exp)
    //    {
    //        Console.WriteLine("socket error");
    //    }
    //}
}