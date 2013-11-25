using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.ComponentModel;

namespace FEZ_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        delegate void SetTextCallback(String text);
        BackgroundWorker backgroundWorker1 = new BackgroundWorker();


        BackgroundWorker bkw1 = new BackgroundWorker();
        Socket client;
        NetworkStream ns;
        StreamReader sr;
        StreamWriter sw;

        int clientcount = 0;

        public MainWindow()
        {
            InitializeComponent();
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerAsync("Message to Worker");
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            IPAddress ip = IPAddress.Parse("10.10.3.71");   //IPAddress of Server
            TcpListener newsocket = new TcpListener(IPAddress.Any, 9090);  //Create TCP Listener on server
            newsocket.Start();                                  //Open Socket on port 9090

            InsertText("waiting for client");                   //wait for connection
            client = newsocket.AcceptSocket();     //Accept Connection
            ns = new NetworkStream(client);                            //Create Network stream
            sr = new StreamReader(ns);
            sw = new StreamWriter(ns);
            string welcome = "Welcome";
            InsertText("client connected");
            sw.WriteLine(welcome);     //Stream Reader and Writer take away some of the overhead of keeping track of Message size.  By Default WriteLine and ReadLine use Line Feed to delimit the messages
            sw.Flush();
            bkw1 = new BackgroundWorker();
            bkw1.DoWork += new DoWorkEventHandler(client_DoWork);
            bkw1.RunWorkerAsync(clientcount);
            clientcount++;

        }

        private void client_DoWork(object sender, DoWorkEventArgs e)
        {
            int clientnum = (int)e.Argument;

            while (true)
            {
                string inputStream;
                try
                {
                    inputStream = sr.ReadLine();
                    InsertText(inputStream);
                    if (inputStream == "disconnect")
                    {
                        sr.Close();
                        sw.Close();
                        ns.Close();
                        System.Environment.Exit(System.Environment.ExitCode); //close all 
                        break;
                    }


                }
                catch
                {
                    sr.Close();
                    sw.Close();
                    ns.Close();

                    System.Environment.Exit(System.Environment.ExitCode); //close all 
                }
            }



        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < clientcount; i++)
            {
                sw.WriteLine(textBox1.Text);
                sw.Flush();
            }
            if (textBox1.Text == "disconnect")
            {

                sr.Close();
                sw.Close();
                ns.Close();
                System.Environment.Exit(System.Environment.ExitCode); //close all 



            }
            textBox1.Text = "";

        }

        private void InsertText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.listBox1.Dispatcher.CheckAccess())
            {
                this.listBox1.Items.Insert(0, text);

            }
            else
            {
                listBox1.Dispatcher.BeginInvoke(new SetTextCallback(InsertText), text);
            }
        }
    }
}
