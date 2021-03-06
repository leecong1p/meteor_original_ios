﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Linq;
using UnityEngine;

namespace CoClass
{
    public class TcpProxy
    {
        byte[] Buff;//缓存全部.
        byte[] Buf;//接收每个包
        int Len;//Buff有多长.
        public static int MaxSize = 8192;//
        public static int PacketSize = 4096;//
        public TcpProxy()
        {
            Buf = new byte[PacketSize];
            Buff = new byte[MaxSize];
            Len = 0;
        }

        //返回此包是否出错.
        public bool Analysis(int receivedLength, Dictionary<int, byte[]> packets)
        {
            //缓存爆了，但是解不出包。让套接字断开
            if (Len + receivedLength > MaxSize)
            {
                Len = 0;
                return false;
            }
            Buffer.BlockCopy(Buf, 0, Buff, Len, receivedLength);
            Len += receivedLength;
            int nleft = Len;
            int noffset = 0;
            //把收到的缓存里的全部解到包里去.
            MemoryStream ms = null; 
            while (nleft > 4)
            {
                byte[] sizeHead = new byte[4];
                Buffer.BlockCopy(Buff, noffset, sizeHead, 0, 4);
                //网络字节序，大端.
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(sizeHead);
                ms = new MemoryStream(sizeHead);
                BinaryReader bin = new BinaryReader(ms);
                int nPacketlen = bin.ReadInt32();
                //除去包首部爆缓存.
                if (nPacketlen >= MaxSize - 8)
                {
                    Len = 0;
                    ms.Close();
                    ms = null;
                    bin.Close();
                    bin = null;
                    return false;
                }
                if (nleft < nPacketlen)
                {
                    //当剩下的字节，大于4字节，小于这4字节组成的长度时.这是不完整包
                    if (noffset == 0)
                    {
                        Len = nleft;
                        ms.Close();
                        ms = null;
                        bin.Close();
                        bin = null;
                        break;
                    }
                    else
                    {
                        Buffer.BlockCopy(Buff, noffset, Buff, 0, nleft);//把字节往前移
                        Len = nleft;
                        ms.Close();
                        ms = null;
                        bin.Close();
                        bin = null;
                        break;
                    }
                }
                else
                {
                    ms.Close();
                    byte[] msgBigEndian = new byte[4];
                    Buffer.BlockCopy(Buff, noffset + 4, msgBigEndian, 0, 4);
                    //网络字节序，大端.
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(msgBigEndian);
                    ms = new MemoryStream(msgBigEndian);
                    bin = new BinaryReader(ms);
                    int message = bin.ReadInt32();
                    ms.Close();
                    ms = null;
                    bin.Close();
                    bin = null;
                    //只存在消息时，buff = new byte[0]，类似ping。
                    byte[] buff = new byte[nPacketlen - 8];
                    Buffer.BlockCopy(Buff, noffset + 8, buff, 0, nPacketlen - 8);
                    lock (packets)
                        packets[message] = buff;
                    noffset += nPacketlen;
                    nleft -= nPacketlen;
                }
            }
            if (ms != null)
                ms.Close();
            
            return true;
        }

        public void OnSendComplete(IAsyncResult ar)
        {
            Socket s = ar.AsyncState as Socket;
            s.EndSend(ar);
        }

        public byte[] GetBuffer()
        {
            return Buf;
        }
    }
}
