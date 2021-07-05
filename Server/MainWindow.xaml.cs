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
        public List<Users> ClientList { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            backgroundWork();
        }

        private void btnSendMess_Click(object sender, RoutedEventArgs e)
        {
            if (tbxMess.Text != string.Empty)
            {
                foreach (var item in ClientList)
                {
                    Send(item.IPUser);
                }

                AddMessage(tbxMess.Text);
                tbxMess.Clear();
            }   
        }
        private static BackgroundWorker backgroundWorker;

        /// <summary>
        /// Init Run Process in Background
        /// </summary>
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
                backgroundWorker.CancelAsync();
        }

        /// <summary>
        /// Connect Async
        /// </summary>
        void ConnectAsync()
        {
            try
            {
                ClientList = new List<Users>();
                IP = new IPEndPoint(IPAddress.Any, 8888);
                Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                Server.Bind(IP);

                while (true)
                {
                    Server.Listen(100);
                    Socket client = Server.Accept();
                    //ClientList.Add(client);

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
            Closes();
            MessageBox.Show("ShutDown Server....");
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Run in background process Connect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ConnectAsync();
        }


        /// <summary>
        /// Receive message from client
        /// </summary>
        /// <param name="obj"></param>
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

                    if (message.Contains("[Sum]:["))
                    {
                        string substring_name = message.Substring(message.IndexOf("["));
                        string substrin_message = substring_name.Substring(6, substring_name.Length-6).Replace("[", "").Replace("]", "").Replace(" ", "");
                        int sum = 0;
                        foreach (var item in substrin_message)
                        {
                            int num = int.Parse(item.ToString());
                            if (num % 2 == 0)
                                sum += int.Parse(item.ToString());
                        }
                        client.Send(Serialize("Sum = " + sum.ToString()));
                    }
                    if (message.Contains("[MAX]:["))
                    {
                        string substring_name = message.Substring(message.IndexOf("["));
                        string substrin_message = substring_name.Substring(6, substring_name.Length - 6).Replace("[", "").Replace("]", "").Replace(" ", "");

                        List<int> Max = new List<int>();
                        foreach (var item in substrin_message)
                        {
                            Max.Add(int.Parse(item.ToString()));
                        }

                        client.Send(Serialize("MAX = " + Max.Max().ToString()));
                    }
                    if (message.Contains("exit"))
                    {
                        Users u = new Users();
                        string nickname = message.Replace(":exit", "");
                        if (ClientList.Count != 0)
                        {
                            foreach (var item in ClientList)
                            {
                                if (item.NickName == nickname)
                                {
                                    u = item;
                                }
                            }
                            ClientList.Remove(u);
                        }
                    }
                    else if (!message.Contains("[Authorization]"))
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AddMessage($"{message}");
                        }));
                    }
                    else
                    {
                        string nickname = message.Replace("[Authorization]:", "");
                        Users user = new Users() { IPUser = client, NickName = nickname };

                        int i = 0;
                        if (ClientList.Count != 0)
                        { 
                            foreach (var item in ClientList)
                            {
                                if (item.NickName.Equals(nickname))
                                    i++;
                            }
                            if (i == 0)
                            {
                                ClientList.Add(user);
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    cbxListClient.Items.Add(new ComboBoxItem() { Content = nickname });
                                }));
                            }
                            else client.Send(Serialize("User is exist"));
                        }
                        else
                        {
                            ClientList.Add(user);
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                cbxListClient.Items.Add(new ComboBoxItem() { Content = nickname });
                            }));
                        }
                    }
                }
            }
            catch (Exception)
            {
                foreach (var item in ClientList)
                {
                    if (item.IPUser.Equals(client))
                        ClientList.Remove(item);
                }
                client.Close();
            }
        }

        /// <summary>
        /// Add Message To List View
        /// </summary>
        /// <param name="s"></param>
        void AddMessage(string s)
        {
            lsvMess.Items.Add(new ListViewItem() { Content = s });

            tbxMess.Clear();
        }

        /// <summary>
        /// Send Message as Socket
        /// </summary>
        /// <param name="client"></param>
        void Send(Socket client)
        {
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

    public class Users
    {
        public Socket IPUser { get; set; }
        public string NickName { get; set; }
    }
}
