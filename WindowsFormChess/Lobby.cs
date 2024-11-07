using MessagePack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
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
        Dictionary<EPacketID, Action<byte[]>> _packetHandler;

        Socket _socket;
        List<(UInt16, UInt16)> RoomInfos = new List<(ushort, ushort)>();
        string UserId;
        string chatBoxText;

        public InGameForm inGameForm;
        Popup popup = new Popup();
        bool InGame = false;

        public Lobby(Socket socket)
        {
            _socket = socket;
            InitializeComponent();
        }

        public void ResEnterGameRoomHandler(byte[] body)
        {
            var resEnterGameRoom = MessagePackSerializer.Deserialize<PKTResEnterGameRoom>(body);
            if (resEnterGameRoom == null)
            {
                return;
            }

            if (resEnterGameRoom.Result == ErrorCode.None)
            {
                InGame = true;

                if(this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        label6.Text = $"현재 참가 방 : {listBox1.SelectedIndex}번 방";
                        popup.UpdateText("게임방 입장 성공");
                        popup.Show();
                    }));
                }
                else
                {
                    label6.Text = $"현재 참가 방 : {listBox1.SelectedIndex}번 방";
                    popup.UpdateText("게임방 입장 성공");
                    popup.Show();
                }
            }
            else
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        popup.UpdateText("게임방 입장 실패 \n이유 : {resEnterGameRoom.Result}");
                        popup.Show();
                    }));
                }
                else
                {
                    popup.UpdateText("게임방 입장 성공");
                    popup.Show();
                }
            }
        }

        public void NTFGameStartHandler(byte[] body)
        {
            var ntfGameStart = MessagePackSerializer.Deserialize<PKTNtfGameStart>(body);
            if (ntfGameStart == null)
            {
                return;
            }

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                    {
                        ShowInGameForm(ntfGameStart.UserInfos[0].Item2 == UserId);
                    }));
            }
            else
            {
                ShowInGameForm(ntfGameStart.UserInfos[0].Item2 == UserId);
            }

            _socket.Send(PacketToBytes.Make(EPacketID.ReqGameRoomInfos));
        }

        void ShowInGameForm(bool amIWhite)
        {
            //if (inGameForm != null)
            //{
            //    inGameForm.Show();
            //    // 안에 다 초기화하기.
            //}
            //else
            //{
            //    inGameForm = new InGameForm(false, _socket, userId);
            //    inGameForm.UpdateComponent(userId == UserId);
            //    inGameForm.UpdateInitLobbyPacketHandler(_packetHandler);
            //    inGameForm.Show();
            //}

            if (inGameForm != null)
            {
                inGameForm.Dispose();
            }
            inGameForm = new InGameForm(false, _socket, amIWhite);
            inGameForm.UpdateInitLobbyPacketHandler(_packetHandler);
            inGameForm.Show();
        }

        public void ResLeaveGameRoomHandler(byte[] body)
        {
            var resLeaveGameRoom = MessagePackSerializer.Deserialize<PKTResLeaveGameRoom>(body);
            if (resLeaveGameRoom == null)
            {
                return;
            }

            if (resLeaveGameRoom.Result == ErrorCode.None)
            {
                InGame = false;

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        label6.Text = "현재 참가 방 : 없음";
                        popup.UpdateText("게임방 퇴장 성공");
                        popup.Show();
                    }));
                }
                else
                {
                    label6.Text = "현재 참가 방 : 없음";
                    popup.UpdateText("게임방 퇴장 성공");
                    popup.Show();
                }
            }
            else
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        popup.UpdateText("게임방 퇴장 실패 \n이유 : {resLeaveGameRoom.Result}");
                        popup.Show();
                    }));
                }
                else
                {
                    popup.UpdateText("게임방 퇴장 실패 \n이유 : {resLeaveGameRoom.Result}");
                    popup.Show();
                }
            }
        }

        public void ResGameRoomInfosHandler(byte[] body)
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
        public void InitLobbyPacketHandler(Dictionary<EPacketID, Action<byte[]>> packetHandler)
        {
            if (packetHandler.ContainsKey(EPacketID.ResGameRoomInfos) == true)
            {
                packetHandler.Remove(EPacketID.ResGameRoomInfos);
            }
            if (packetHandler.ContainsKey(EPacketID.NtfChat) == true)
            {
                packetHandler.Remove(EPacketID.NtfChat);
            }
            if (packetHandler.ContainsKey(EPacketID.ResEnterGameRoom) == true)
            {
                packetHandler.Remove(EPacketID.ResEnterGameRoom);
            }
            if (packetHandler.ContainsKey(EPacketID.NTFGameStart) == true)
            {
                packetHandler.Remove(EPacketID.NTFGameStart);
            }
            if (packetHandler.ContainsKey(EPacketID.ResLeaveGameRoom) == true)
            {
                packetHandler.Remove(EPacketID.ResLeaveGameRoom);
            }

            packetHandler.Add(EPacketID.ResGameRoomInfos, ResGameRoomInfosHandler);
            packetHandler.Add(EPacketID.NtfChat, NtfChatHandler);
            packetHandler.Add(EPacketID.ResEnterGameRoom, ResEnterGameRoomHandler);
            packetHandler.Add(EPacketID.NTFGameStart, NTFGameStartHandler);
            packetHandler.Add(EPacketID.ResLeaveGameRoom, ResLeaveGameRoomHandler);


            _packetHandler = packetHandler;
        }

        public void NtfChatHandler(byte[] body)
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

        private void button1_Click(object sender, EventArgs e)
        {
            if(listBox1.SelectedIndex == -1 || InGame == true)
            {
                return;
            }
            _socket.Send(PacketToBytes.Make(EPacketID.ReqEnterGameRoom, MessagePackSerializer.Serialize(new PKTReqEnterGameRoom { RoomID = (Int16)RoomInfos[listBox1.SelectedIndex].Item1 })));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _socket.Send(PacketToBytes.Make(EPacketID.ReqLeaveGameRoom));
            label6.Text = "현재 참가 방 : 없음";
        }

        private void Lobby_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
