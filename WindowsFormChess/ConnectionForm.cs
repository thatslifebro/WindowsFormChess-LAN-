using MessagePack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sakk_Alkalmazás_2._0
{
    public partial class ConnectionForm : Form
    {
        private Thread receiveThread;       // Receive() 작업
        private Socket socket;        // 연결된 클라이언트 소켓
        string UserId = string.Empty;
        Lobby lobby;

        bool IsLogin = false;

        Dictionary<EPacketID, Action<byte[]>> packetHandler = new Dictionary<EPacketID, Action<byte[]>>();

        public ConnectionForm()
        {
            InitPacketHandler();

            InitializeComponent();

            // 서버연결
            ConnectToServer();

            receiveThread = new Thread(new ThreadStart(Receive));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            lobby = new Lobby(socket);
        }

        void InitPacketHandler()
        {
            packetHandler.Add(EPacketID.ResLogin, ResLoginHandler);
        }

        void ConnectToServer()
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            var endpoint = new IPEndPoint(ip, 32452);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endpoint);
        }

        void Receive()
        {
            while(true)
            {
                byte[] receiveBuffer = new byte[1024];
                int length = socket.Receive(receiveBuffer,
                    receiveBuffer.Length, SocketFlags.None);

                Int16 packetSize = BitConverter.ToInt16(receiveBuffer, 0);
                Int16 packetID = BitConverter.ToInt16(receiveBuffer, 2);
                byte type = receiveBuffer[4];
                byte[] bodyData = new byte[packetSize - 5];
                Buffer.BlockCopy(receiveBuffer, 5, bodyData, 0, packetSize - 5);

                if(packetHandler.TryGetValue((EPacketID)packetID, out var action))
                {
                    action(bodyData);
                }

            }
        }

        void ResLoginHandler(byte[] body)
        {
            var resLogin = MessagePackSerializer.Deserialize<PKTResLogin>(body);
            if (resLogin == null)
            {
                return;
            }

            if (resLogin.Result == ErrorCode.None || resLogin.Result == ErrorCode.AlreadyLoginUser)
            {
                IsLogin = true;

                lobby.UpdateUserID(UserId);
                if (!lobby.IsDisposed)
                    lobby.ShowDialog();
                
            }
        }

        

        private void button1_Click(object sender, EventArgs e)
        {
            UserId = textBox1.Text;

            socket.Send(PacketToBytes.Make(EPacketID.ReqLogin,
                               MessagePackSerializer.Serialize(new PKTReqLogin { UserID = UserId } )));

        }
    }
}
