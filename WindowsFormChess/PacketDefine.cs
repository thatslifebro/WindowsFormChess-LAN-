﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sakk_Alkalmazás_2._0
{
    public class PacketDef
    {
        public const Int16 HeaderSize = 5;
    }

    public class PacketToBytes
    {
        public static byte[] Make(EPacketID packetID, byte[] bodyData)
        {
            byte type = 0;
            var pktID = (Int16)packetID;

            Int16 bodyDataSize = 0;
            if (bodyData != null)
            {
                bodyDataSize = (Int16)bodyData.Length;
            }

            var packetSize = (Int16)(bodyDataSize + PacketDef.HeaderSize);

            var dataSource = new byte[packetSize];
            Buffer.BlockCopy(BitConverter.GetBytes(packetSize), 0, dataSource, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(pktID), 0, dataSource, 2, 2);
            dataSource[4] = type;

            if (bodyData != null)
            {
                Buffer.BlockCopy(bodyData, 0, dataSource, 5, bodyDataSize);
            }

            return dataSource;
        }

        public static byte[] Make(EPacketID packetID)
        {
            Int16 packetSize = 5;
            Int16 pktID = (Int16)packetID;

            var dataSource = new byte[packetSize];
            Buffer.BlockCopy(BitConverter.GetBytes(packetSize), 0, dataSource, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(pktID), 0, dataSource, 2, 2);
            dataSource[4] = 0;

            return dataSource;
        }

    }

    [MessagePackObject]
    public class PKTReqLogin
    {
        [Key(0)]
        public string UserID;
    }

    [MessagePackObject]
    public class PKTResLogin
    {
        [Key(0)]
        public ErrorCode Result;
    }

    [MessagePackObject]
    public class PKTResGameRoomInfos
    {
        [Key(0)]
        public ErrorCode Result;

        [Key(1)]
        public List<(UInt16, UInt16)> GameRoomInfos;
    }

    [MessagePackObject]
    public class PKTReqChat
    {
        [Key(0)]
        public string Chat;
    }

    [MessagePackObject]
    public class PKTNtfChat
    {
        [Key(0)]
        public string Chat;
    }
}
