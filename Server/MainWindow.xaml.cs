using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IPEndPoint IP { get; set; }
        public Socket Server { get; set; }
        public List<Socket> ClientList { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            backgroundWork();
        }

        private void btnSendMess_Click(object sender, RoutedEventArgs e)
        {
            foreach (Socket item in ClientList)
            {
                Send(item);
            }

            AddMessage(tbxMess.Text);
            tbxMess.Clear();
        }
        private static BackgroundWorker backgroundWorker;

        void backgroundWork()
        {
            backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            backgroundWorker.RunWorkerAsync(10000);

            if (backgroundWorker.IsBusy)
            {

                backgroundWorker.CancelAsync();

                Console.ReadLine();

            }
        }
        void ConnectAsync()
        {
            try
            {
                ClientList = new List<Socket>();
                IP = new IPEndPoint(IPAddress.Any, 8888);
                Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                Server.Bind(IP);

                while (true)
                {
                    Server.Listen(100);
                    Socket client = Server.Accept();
                    ClientList.Add(client);
                    //MessageBox.Show("new client connect");
                    string s = client.RemoteEndPoint.ToString();
                    if(!cbxListClient.Items.Contains(s))
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            cbxListClient.Items.Add(new ComboBoxItem() { Content = s });
                        }));
                    }


                    Thread receive = new Thread(Receive);
                    receive.IsBackground = true;
                    receive.Start(client);
                }
            }
            catch (Exception)
            {
                IP = new IPEndPoint(IPAddress.Any, 9999);
                Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ConnectAsync();
        }

        void Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);

                    string message = (string)Deserialize(data);

                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        AddMessage(message);
                    }));
                    
                }
            }
            catch (Exception)
            {
                ClientList.Remove(client);
                client.Close();
            }
        }

        void AddMessage(string s)
        {
            lsvMess.Items.Add(new ListViewItem() { Content = s });

            tbxMess.Clear();
        }

        void Send(Socket client)
        {
            if (tbxMess.Text != string.Empty)
                client.Send(Serialize(tbxMess.Text));
        }

        void Closes()
        {
            Server.Close();
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

        private void Window_Closed(object sender, EventArgs e)
        {
            Closes();
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
