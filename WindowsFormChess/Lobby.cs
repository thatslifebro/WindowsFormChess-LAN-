using MessagePack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Sakk_Alkalmazás_2._0
{
    public partial class Lobby : Form
    {
        private Thread receiveThread;
        Dictionary<EPacketID, Action<byte[]>> packetHandler = new Dictionary<EPacketID, Action<byte[]>>();

        Socket _socket;
        List<(UInt16, UInt16)> RoomInfos = new List<(ushort, ushort)>();
        string UserId;
        string chatBoxText;

        Thread getGameRoomInfosThread;

        public Lobby(Socket socket)
        {
            _socket = socket;
            InitializeComponent();
            InitPacketHandler();
            StartReceiveThread();
        }

        void StartReceiveThread()
        {
            receiveThread = new Thread(new ThreadStart(Receive));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        void Receive()
        {
            while (true)
            {
                byte[] receiveBuffer = new byte[1024];
                int length = _socket.Receive(receiveBuffer,
                    receiveBuffer.Length, SocketFlags.None);

                Int16 packetSize = BitConverter.ToInt16(receiveBuffer, 0);
                Int16 packetID = BitConverter.ToInt16(receiveBuffer, 2);
                byte type = receiveBuffer[4];
                byte[] bodyData = new byte[packetSize - 5];
                Buffer.BlockCopy(receiveBuffer, 5, bodyData, 0, packetSize - 5);

                if (packetHandler.TryGetValue((EPacketID)packetID, out var action))
                {
                    action(bodyData);
                }

            }
        }

        void InitPacketHandler()
        {
            packetHandler.Add(EPacketID.ResGameRoomInfos, ResGameRoomInfosHandler);
            packetHandler.Add(EPacketID.NtfChat, NtfChatHandler);
        }

        void ResGameRoomInfosHandler(byte[] body)
        {
            var resGameRoomInfos = MessagePackSerializer.Deserialize<PKTResGameRoomInfos>(body);
            if (resGameRoomInfos == null)
            {
                return;
            }

            if (resGameRoomInfos.Result == ErrorCode.None)
            {
                UpdateGameRoomList(resGameRoomInfos.GameRoomInfos);
            }
        }

        void NtfChatHandler(byte[] body)
        {
            var chatData = MessagePackSerializer.Deserialize<PKTNtfChat>(body);
            if (chatData == null)
            {
                return;
            }

            if (chatData.Chat != null)
            {
                if (listBox2.InvokeRequired == true)
                {
                    listBox2.Invoke(new Action(() =>
                    {
                        listBox2.Items.Add(chatData.Chat);
                    }));
                }
                else
                {
                    listBox2.Items.Add(chatData.Chat);
                }
            }
        }

        public void UpdateGameRoomList(List<(UInt16, UInt16)> roomInfos)
        {
            RoomInfos = roomInfos;

            if(listBox1.InvokeRequired == true)
            {
                listBox1.Invoke(new Action(() =>
                {
                    listBox1.Items.Clear();
                    foreach (var roomInfo in RoomInfosToStringList())
                    {
                        listBox1.Items.Add(roomInfo);
                    }
                }));
            }
            else
            {
                listBox1.Items.Clear();
                foreach (var roomInfo in RoomInfosToStringList())
                {
                    listBox1.Items.Add(roomInfo);
                }
            }
        }

        public void UpdateUserID(string userId)
        {
            UserId = userId;
            label5.Text = userId;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Lobby_Load(object sender, EventArgs e)
        {
            label5.Text = UserId;

            _socket.Send(PacketToBytes.Make(EPacketID.ReqGameRoomInfos));
        }

        private List<string> RoomInfosToStringList()
        {
            List<string> strRoomInfos = new List<string>();
            foreach (var roomInfo in RoomInfos)
            {
                strRoomInfos.Add($"{roomInfo.Item1}번 게임 - 참가인원 : ({roomInfo.Item2}/2)");
            }

            return strRoomInfos;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _socket.Send(PacketToBytes.Make(EPacketID.ReqGameRoomInfos));
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            chatBoxText = textBox1.Text;
        }

        private void searchTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                _socket.Send(PacketToBytes.Make(EPacketID.ReqChat, MessagePackSerializer.Serialize(new PKTReqChat { Chat = chatBoxText })));
                textBox1.Text = "";
                chatBoxText = "";
            }
        }
    }
}
