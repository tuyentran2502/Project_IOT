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
using System.IO;

namespace TCP_Private_Client
{
    public partial class Form_Main : Form
    {
        const int READ_BUFFER_SIZE = 255;
        const int PORT_NUM = 2021;
        private TcpClient client;
        public string user;
        private byte[] readBuffer = new byte[READ_BUFFER_SIZE];
        string str = "noname";
        private int p, q, n, e, d, phi_n;// tham ssoos mã hóa RSA
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
            this.aboutToolStripMenuItem.Text = string.Format("&About {0} ...", ainfo.Title);   
        }

        /* 
       Khi biểu mẫu bắt đầu, chương trình con này sẽ kết nối với máy chủ và cố gắng đăng nhập.
         */
        private void Form_Main_Load(object sender, EventArgs e)
        {
            Form_Login frmLogin = new Form_Login();
            try
            {
                // TcpClient là một lớp con của Socket, cung cấp chức năng cấp cao hơn như phát trực tuyến.
                client = new TcpClient("localhost", PORT_NUM);
                //Bắt đầu đọc không đồng bộ gọi DoRead để tránh làm chậm giao diện người dùng.
                client.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(DoRead), null);
                //Đảm bảo rằng cửa sổ đang hiển thị trước khi bật lên hộp thoại kết nối.
                this.Show();
                AttemptLogin();
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Can not connect to server. Please try logging in again.",
                       this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                this.Dispose();
            }
        }
        // Đây là hàm callback cho TcpClient. GetStream.Begin để nhận được một lượt đọc không đồng bộ.
        private void DoRead(IAsyncResult ar)
        {
            int BytesRead;
            string strMessage;

            try
            {
                // Kết thúc đọc không đồng bộ vào Bộ đệm đọc và trả về số byte đã đọc.
                BytesRead = client.GetStream().EndRead(ar);
                if (BytesRead < 1)
                {
                    //nếu không có byte nào được đọc thì máy chủ đã đóng.
                   MarkAsDisconnected();
                    return;
                }
                // Chuyển đổi mảng byte mà tin nhắn đã được lưu thành, trừ đi hai ký tự Chr (13) và Chr (10)
                strMessage = Encoding.ASCII.GetString(readBuffer, 0, BytesRead - 2);
                ProcessCommands(strMessage);
                // Bắt đầu đọc không đồng bộ mới vào readBuffer.
                client.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(DoRead), null);

            }
            catch (Exception e)
            {
                MarkAsDisconnected();
            }
        }
        // Khi máy chủ ngắt kết nối, ngăn không cho gửi thêm tin nhắn trò chuyện.
        private void MarkAsDisconnected()
        {
            textBox_Send.ReadOnly = true;
            button_Send.Enabled = false;
        }
        // Xử lý lệnh nhận được từ máy chủ và thực hiện hành động thích hợp.
        private void ProcessCommands(string strMessage)
        {
            string[] dataArray;
            //Các phần tin nhắn được chia bởi "|" .Ngắt chuỗi thành một mảng tương ứng.
            dataArray = strMessage.Split((char)124);//(char)124 : |, (char)13, (char)10 : CR,LF
            // dataArray(0) is the command.
            switch (dataArray[0])
            {
                case "JOIN":
                    // Máy chủ đã xác nhận đăng nhập.
                    DisplayText("Successful login. Are you ready for the conversation?" + (char)13 + (char)10);
                    break;
                case "CHAT":
                    // Đã nhận tin nhắn trò chuyện, hiển thị nó.
                    if (dataArray[1].Substring(0, 10) == "Waiting...")
                        DisplayText(dataArray[1] + (char)13 + (char)10);
                    else
                    {
                        string data = dataArray[1];
                        string subStringYes = data.Substring(data.Length - user.Length, user.Length);
                        string subStringNo = data.Substring(data.Length - "noname".Length, "noname".Length);
                        if (subStringYes == user)
                            DisplayText(getString(data.Substring(0, data.Length - user.Length)) + (char)13 + (char)10);
                        else if (subStringNo == "noname")
                            DisplayText(getString(data.Substring(0, data.Length - "noname".Length)) + (char)13 + (char)10);
                    }
                    break;
                case "REFUSE":
                    // Máy chủ từ chối đăng nhập với tên người dùng này, hãy thử đăng nhập bằng tên khác.
                    AttemptLogin();
                    break;
                case "LISTUSERS":
                    // Máy chủ đã gửi danh sách người dùng.
                    ListUsers(dataArray);
                    break;
                case "BROAD":
                    // Máy chủ đã gửi một tin nhắn quảng bá
                    DisplayText("Máy chủ: " + dataArray[1] + (char)13 + (char)10);
                    break;
            }
        }
        // Writes text to the output textbox.
        private void DisplayText(string text)
        {
            textBox_Status.AppendText(text);
        }
        // Bật hộp thoại Kết nối người dùng và gửi thông báo yêu cầu người dùng đăng nhập để trò chuyện.
        void AttemptLogin()
        {
            Form_Login frmLogin = new Form_Login();
            frmLogin.StartPosition = FormStartPosition.CenterParent;
            frmLogin.ShowDialog(this);
            SendData("CONNECT|" + frmLogin.textBox_UserFormLogin.Text);
            frmLogin.Dispose();
            user = frmLogin.textBox_UserFormLogin.Text;
            label_UserName.Text = user;
        }
        //Dùng RSA
        private string getString(string str)
        {
            int bre = 0;
            string s1 = null;
            string s2 = null;
            string s = null;
            char[] getChar = new char[str.Length];
            getChar = str.ToCharArray();
            for (int i = 0; i < str.Length; i++)
            {
                if (getChar[i] == ':')
                {
                    bre = i + 1;
                    break;
                }
            }
            for (int k = 0; k < bre; k++)
            {
                s1 += getChar[k].ToString();
            }
            for (int j = bre; j < str.Length; j++)
            {
                s2 += getChar[j].ToString();
            }
            s2 = Decipher(s2);
            s = s1 + " " + s2;
            return s;
        }
        private string Decipher(string str)
        {
            //str = getString(str);
            string rtbChuoiKiTu = str.Trim() + " ";
            int chieuDaiChuoi = rtbChuoiKiTu.Length;
            char[] rtbMangKiTu;
            rtbMangKiTu = new char[chieuDaiChuoi];
            int[] rtbMangSo;
            rtbMangSo = new int[chieuDaiChuoi];
            rtbMangKiTu = rtbChuoiKiTu.ToCharArray();
            string s = "";
            int count = 0;
            int i = 0;
            for (i = 0; i < chieuDaiChuoi; i++)
            {
                if (rtbMangKiTu[i] != ' ')
                {
                    s += rtbMangKiTu[i];
                }
                else
                {
                    rtbMangSo[count] = int.Parse(s);
                    count++;
                    s = "";
                }
            }
            char[] rtbMang;
            rtbMang = new char[chieuDaiChuoi];
            int dd = rtbMangSo[0];
            int ee = rtbMangSo[1];
            int nn = rtbMangSo[2];
            for (i = 3; i < count; i++)
            {
                rtbMangSo[i] = (rtbMangSo[i] ^ dd) % nn;
                rtbMangSo[i] = (rtbMangSo[i] ^ ee) % nn;
                rtbMangSo[i] = (rtbMangSo[i] ^ dd) % nn;
                rtbMang[i] = (char)rtbMangSo[i];
                s += rtbMang[i];
            }
            return s;
        }
        // Chương trình con này thêm danh sách người dùng vào hộp danh sách.
        private void ListUsers(string[] users)
        {
            int I;

            for (I = 1; I <= (users.Length - 1); I++)
            {
                listBox_UserOnline.Items.Add(users[I]);
            }
        }

        /* 
        Gửi nội dung của hộp văn bản Gửi nếu nó không trống.
         */
        private void button_Send_Click(object sender, EventArgs e)
        {
            if (textBox_Send.Text != "")
            {
                DisplayText(user + ": " + textBox_Send.Text + (char)13 + (char)10);
                SendData("CHAT|" + Ecrypt(textBox_Send.Text) + str);
                textBox_Send.Text = string.Empty;
            }
        }
        // Sử dụng StreamWriter để gửi tin nhắn tới máy chủ.
        private void SendData(string data)
        {
            StreamWriter writer = new StreamWriter(client.GetStream());
            writer.Write(data + (char)13);
            writer.Flush();
        }
        private string Ecrypt(string str)
        {
            taoKhoa();
            int len = str.Length;
            char[] mangKiTu = new char[len];
            mangKiTu = str.ToCharArray();
            int[] mangAscii = new int[len];
            for (int i = 0; i < len; i++)
                mangAscii[i] = (int)mangKiTu[i];
            for (int i = 0; i < len; i++)

                mangAscii[i] = (mangAscii[i] ^ e) % n;			//Mã hóa từng kí tự trong chuỗi

            string str1 = "";					// Gán vào một chuỗi số khác
            for (int i = 0; i < len; i++)
                str1 += (mangAscii[i] + " ");
            return d.ToString() + " " + e.ToString() + " " + n.ToString() + " " + str1;
        }
        //Hàm tạo các khóa
        private void taoKhoa()
        {
            //Tạo hai số nguyên tố ngẫu nhine6 khác nhau
            do
            {
                p = soNgauNhien();
                q = soNgauNhien();
            }
            while (p == q || !kiemTraNguyenTo(p) || !kiemTraNguyenTo(q));

            //Tinh n=p*q
            n = p * q;

            //Tính Phi(n)=(p-1)*(q-1)
            phi_n = (p - 1) * (q - 1);

            //Tính e là một số ngẫu nhiên có giá trị 0< e <phi(n) và là số nguyên tố cùng nhau với Phi(n)
            do
            {
                Random rd = new Random();
                e = rd.Next(2, phi_n);
            }
            while (!nguyenToCungNhau(e, phi_n));

            //Tính d
            d = 0;
            int i = 2;
            while (((1 + i * phi_n) % e) != 0 || d <= 0)
            {
                i++;
                d = (1 + i * phi_n) / e;
            }
        }
        //Hàm tạo số ngẫu nhiên từ 2->10000
        private int soNgauNhien()
        {
            Random rd = new Random();
            return rd.Next(11, 101);
        }

        //Hàm kiểm tra nguyên tố
        private bool kiemTraNguyenTo(int i)
        {
            bool kiemtra = true;
            for (int j = 2; j < i; j++)
                if (i % j == 0)
                    kiemtra = false;
            return kiemtra;
        }

        //Hàm kiểm tra hai số nguyên tố cùng nhau
        private bool nguyenToCungNhau(int a, int b)
        {
            bool kiemtra = true;
            for (int i = 2; i < a; i++)
                if (a % i == 0 && b % i == 0)
                    kiemtra = false;
            return kiemtra;
        }

        private void button_CapNhatDanhSach_Click(object sender, EventArgs e)
        {
            listBox_UserOnline.Items.Clear();
            SendData("REQUESTUSERS");
        }

        private void textBox_Status_TextChanged(object sender, EventArgs e)
        {

        }

        private void button_PublicChat_Click(object sender, EventArgs e)
        {
             str = "noname";
            label_Private.Text = "You are public chatting !!!";
        }

     
        // This code will close the form.
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Close the current form
            this.Close();
        }

        // This code simply shows the About form.
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open the About form in Dialog Mode
            Form_About frm = new Form_About();
            frm.ShowDialog(this);
            frm.Dispose();
        }

        // Send the server a disconnect message  
        private void Form_Main_Closing(object sender, System.ComponentModel.CancelEventArgs e) //base.Closing;
        {
            // Send only if server is still running.
            if (button_Send.Enabled == true)
            {
                SendData("DISCONNECT");
            }
        }

        private void listBox_UserOnline_SelectedIndexChanged(object sender, EventArgs e)
        {
            str = listBox_UserOnline.Text;
            label_Private.Text = "You are private chatting with :  " + str;
        }

       
    }
}