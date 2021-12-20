using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
////////////////////////////
using System.Collections;
using System.Threading;
using System.Net.Sockets;

namespace TCP_Private_Server
{
    public partial class Form_Main : Form
    {
        const int PORT_NUM = 2021;
        private Hashtable clients = new Hashtable();
        private TcpListener listener;
        private Thread listenerThread;
        public Form_Main()
        {
            //This call is required by the Windows Form Designer.
            InitializeComponent();
            //Add any initialization after the InitializeComponent() call
            // So that we only need to set the title of the application once,
            // we use the AssemblyInfo class (defined in the AssemblyInfo.cs file)
            // to read the AssemblyTitle attribute.
            AssemblyInfo ainfo = new AssemblyInfo();
            this.Text = ainfo.Title;
            this.aboutToolStripMenuItem.Text = string.Format("&About {0} ...", ainfo.Title);// nó sẽ tự động tạo một tệp AssemblyInfo.cs cho chúng ta. Trong một dự án thực tế, chúng ta phải sửa đổi nội dung của tệp
        }

        // Start the background listener thread.
        private void Form_Server_Load(object sender, EventArgs e)
        {
            listenerThread = new Thread(new ThreadStart(DoListen));//khởi tạo luồng
            listenerThread.Start();
            UpdateStatus("Ready to connect");
        }

        /*
         This subroutine is used a background listener thread to allow reading incoming
         messages without lagging the user interface.
         */
        private void DoListen()
        {
            try
            {
                // Lăngs nghe kết nối mới
                //Cung cấp một địa chỉ IP (thường là 0.0.0.0) để chỉ ra rằng Server phải lắng nghe
                //các hoạt động của Client trên tất cả các Card mạng (sử dụng khi xây dựng Server).
                //Thuộc tính này chỉ đọc.
                listener = new TcpListener(System.Net.IPAddress.Any, PORT_NUM);
                listener.Start();//bắt đầu lắng nghe các yêu cầu kết nối
                do
                {
                    // Tạo một kết nối mới sử dụng TcpClient được trả về bởi TcpListener.AcceptTcpClient()
                    //Chấp nhận một yêu cầu kết nối đang chờ.
                    //(Ứng dụng sẽ dừng tại lệnh này cho đến khi nào có một kết nối đến – “Blocking”).
                    UserConnection client = new UserConnection(listener.AcceptTcpClient());
                    // // Tạo một trình xử lý sự kiện để cho phép UserConnection giao tiếp với cửa sổ.
                    client.LineReceived += new LineReceive(OnLineReceived);
                    //AddHandler client.LineReceived, AddressOf OnLineReceived;
                    UpdateStatus("An user is connecting.Please wait for few second...");
                } while (true);
            }
            catch
            {
            }
        }

        /* 
         Chương trình con này thêm dòng vào hộp danh sách Trạng thái.
         */
        private void UpdateStatus(string statusMessage)
        {
            listBox_Status.Items.Add(statusMessage);
        }

        /*
         Đây là trình xử lý sự kiện cho UserConnection khi nó nhận được một dòng đầy đủ.
         Phân tích cú pháp lệnh và các tham số và thực hiện hành động thích hợp.
         */
        private void OnLineReceived(UserConnection sender, string data)
        {
            string[] dataArray;
            //Các phần tin nhắn được chia bởi "|". Ngắt chuỗi thành một mảng tương ứng.
            dataArray = data.Split((char)124);

            // dataArray(0) is the command.
            switch (dataArray[0])
            {
                case "CONNECT":
                    ConnectUser(dataArray[1], sender);
                    break;
                case "CHAT":
                    SendChat(dataArray[1], sender);
                    break;
                case "DISCONNECT":
                    DisconnectUser(sender);
                    break;
                case "REQUESTUSERS":
                    ListUsers(sender);
                    break;
                default:
                    UpdateStatus("Unknown message:" + data);
                    break;
            }
        }

        /* Chương trình con này kiểm tra xem tên người dùng đã tồn tại trong Hashtable của máy khách hay chưa.
         * nếu có, hãy gửi một thông báo REFUSE, nếu không, hãy xác nhận JOIN
         */
        private void ConnectUser(string userName, UserConnection sender)
        {
            if (clients.Contains(userName))
            {
                ReplyToSender("REFUSE", sender);
            }
            else
            {
                sender.Name = userName;
                UpdateStatus(userName + " connected.");
                clients.Add(userName, sender);
                //Gửi JOIN cho người gửi và thông báo cho tất cả các users khác rằng người gửi đã tham gia
                ReplyToSender("JOIN", sender);
                SendToClients("CHAT|" + "Waiting..." + sender.Name + " just connected.", sender);
            }
        }
        // Chương trình con gửi lại phản hồi cho sender.
        private void ReplyToSender(string strMessage, UserConnection sender)
        {
            sender.SendData(strMessage);
        }
        // Chương trình con này gửi một thông báo đến tất cả các user được đính kèm ngoại trừ người gửi.
        private void SendToClients(string strMessage, UserConnection sender)
        {
            UserConnection client;
            
            foreach (DictionaryEntry entry in clients)
            {
                client = (UserConnection)entry.Value;
                // Exclude the sender.
                if (client.Name != sender.Name)
                {
                    client.SendData(strMessage);
                }
            }
        }

       /* 
         Gửi tin nhắn cho các user đc chỉ định ngoại trừ người gửi.
       */
        private void SendChat(string message, UserConnection sender)
        {
            UpdateStatus(sender.Name + ": " + message);
            SendToClients("CHAT|" + sender.Name + ": " + message, sender);
        }

        /* Chương trình con này thông báo cho các user khác rằng người gửi đã rời khỏi cuộc trò chuyện
         * và xóa tên từ Hashtable của user.
        */
        private void DisconnectUser(UserConnection sender)
        {
            UpdateStatus("Waiting..." + sender.Name + "log out.");
            SendToClients("CHAT|" + "Waiting..." + sender.Name + "log out.", sender);
            clients.Remove(sender.Name);
        }

        /* 
         Ghép tất cả các tên khách hàng và gửi chúng đến người dùng đã yêu cầu danh sách người dùng
         */
        private void ListUsers(UserConnection sender)
        {
            UserConnection client;
            string strUserList;
            UpdateStatus("Sending to  " + sender.Name + " list of online users.");
            strUserList = "LISTUSERS";
            // Tất cả các mục nhập trong Hashtable của máy khách đều là UserConnection
            // nên có thể gán nó một cách an toàn..

            foreach (DictionaryEntry entry in clients)
            {
                client = (UserConnection)entry.Value;
                strUserList = strUserList + "|" + client.Name;
            }

            // Send the list to the sender.
            ReplyToSender(strUserList, sender);
        }

        private void button_Send_Click(object sender, EventArgs e)
        {
            if (textBox_Send.Text != "")
            {
                UpdateStatus("Server: " + textBox_Send.Text);
                Send("BROAD|" + textBox_Send.Text);
                textBox_Send.Text = string.Empty;
            }
        }
        // Chương trình con này sẽ gửi một thông báo đến tất cả các user được đính kèm
        private void Send(string strMessage)
        {
            UserConnection client;
            /// Tất cả các mục nhập trong Hashtable của máy khách đều là UserConnection
            // nên có thể gán nó một cách an toàn..
            foreach (DictionaryEntry entry in clients)
            {
                client = (UserConnection)entry.Value;
                client.SendData(strMessage);
            }
        }

        // Mã này sẽ đóng biểu mẫu.
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Mã này chỉ hiển thị biểu mẫu About form.
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Mở biểu mẫu About form trong  Dialog Mode
            Form_About frm = new Form_About();
            frm.ShowDialog(this);
            frm.Dispose();
        }

        // Khi cửa sổ đóng, ta dừng lắng nghe.
        private void Form_Server_Closing(object sender, System.ComponentModel.CancelEventArgs e) //base.Closing;
        {
            listener.Stop();
        }

        private void listBox_Status_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}