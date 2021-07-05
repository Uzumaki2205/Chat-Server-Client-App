using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        public IPEndPoint IP { get; set; }
        public Socket Client { get; set; }
        public string Nickname { get; set; }

        void ConnectToServer()
        {
            try
            {
                IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
                Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Client.Connect(IP);
                MessageBox.Show("Connect Success!!");
            }
            catch (Exception)
            {
                MessageBox.Show("Not connect to server....");
            }

            Thread listen = new Thread(ReceiveMessage);
            listen.IsBackground = true;
            listen.Start();
        }

        void SendNickname()
        {
            if (tbxNickName.Text == string.Empty)
                Nickname = "Anonymous";
            else Nickname = tbxNickName.Text;

            Client.Send(Serialize($"[Authorization]:{Nickname}"));
        }

        void ReceiveMessage()
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    Client.Receive(data, 0, data.Length, SocketFlags.None);

                    string message = (string)Deserialize(data);
                    this.Dispatcher.BeginInvoke(new Action(() => { AddMessage(message); }));
                }
            }
            catch (Exception)
            {
                Closes();
            }
        }

        void AddMessage(string s)
        {
            lsvMess.Items.Add(new ListViewItem() { Content = s });
            tbxMess.Clear();
        }

        void Send()
        {
            try
            {
                Client.Send(Serialize($"{Nickname}:{tbxMess.Text}"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSendMess_Click(object sender, RoutedEventArgs e)
        {
            if (tbxMess.Text != string.Empty)
            {
                Send();
                lsvMess.Items.Add(new ListViewItem() { Content = tbxMess.Text });
                tbxMess.Clear();
            }   
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Closes();
        }

        void Closes()
        {
            Client.Close();
        }

        byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);

            return stream.ToArray();
        }

        object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        private void tbxMess_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                btnSendMess_Click(sender, null);
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(() => ConnectToServer());
            thread.IsBackground = true;
            thread.Start();
        }

        private void btnNickname_Click(object sender, RoutedEventArgs e)
        {
            SendNickname();
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Client.Send(Serialize($"{Nickname}:exit"));
        }
    }
}