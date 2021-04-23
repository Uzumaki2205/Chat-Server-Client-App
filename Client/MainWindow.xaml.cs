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

            Thread thread = new Thread(() => ConnectToServer());
            //ConnectToServer();
            thread.IsBackground = true;
            thread.Start();
        }
        public IPEndPoint IP { get; set; }
        public Socket Client { get; set; }

        void ConnectToServer()
        {
            IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                Client.Connect(IP);
            }
            catch (Exception)
            {
                MessageBox.Show("Not connect to server....");
            }

            Thread listen = new Thread(ReceiveMessage);
            listen.IsBackground = true;
            listen.Start();
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
            if (tbxMess.Text != string.Empty)
                Client.Send(Serialize(tbxMess.Text));
        }

        private void btnSendMess_Click(object sender, RoutedEventArgs e)
        {
            Send();
            lsvMess.Items.Add(new ListViewItem() { Content = tbxMess.Text });
            tbxMess.Clear();
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
    }
}
