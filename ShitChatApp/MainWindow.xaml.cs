using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.ComponentModel;

namespace ShitChatApp
{
    public partial class MainWindow : Window
    {
        private bool isConnected;
        private UdpClient udpClient;
        private IPEndPoint remote;
        private Thread receiveThread;
        private List<string> sentMessages = new List<string>();
        private int currentSentMessage;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void WriteMessage(string msg, bool newLine = true) 
        {
            WriteMessage(msg, Colors.Black, newLine);
        }

        public void WriteMessage(string msg, Color color, bool newLine = true)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                TextRange textRange = new TextRange(txtMessages.Document.ContentEnd,
                    txtMessages.Document.ContentEnd);
                textRange.Text = msg + (newLine ? Environment.NewLine : "");
                textRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            }));
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string msg = txtInput.Text.Trim();
            txtInput.Text = null;

            sentMessages.Add(msg);
            currentSentMessage = sentMessages.Count;

            HandleChatMessage(msg);
        }

        private void txtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtInput.Text.Trim().Length > 0) 
                btnSend.IsEnabled = true;
            else 
                btnSend.IsEnabled = false;
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnSend.IsEnabled) 
                btnSend.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private void txtInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sentMessages.Count < 1) return;
            
            if (e.Key == Key.Up)
            {
                currentSentMessage--;
                if (currentSentMessage < 0) currentSentMessage++;
                txtInput.Text = sentMessages[currentSentMessage];
            }
            else if (e.Key == Key.Down)
            {
                currentSentMessage++;
                if (currentSentMessage > sentMessages.Count - 1) currentSentMessage--;
                txtInput.Text = sentMessages[currentSentMessage];
            }
        }

        private void HandleChatMessage(string msg) 
        {
            if (msg.StartsWith("/")) 
            {
                ParseCommand(msg.Substring(1));
                return;
            }

            if (udpClient == null)
            {
                WriteMessage("You are not connected!", Colors.Red);
                return;
            }

            byte[] message = Encoding.UTF8.GetBytes(msg + Environment.NewLine);
            WriteMessage($"[{GetTimeStamp()}] ", Colors.Brown, false);
            WriteMessage($"YOU: ", Colors.Blue, false);
            WriteMessage(msg);

            try
            {
                udpClient.Send(message, message.Length, remote);
            }
            catch (Exception ex) 
            {
                WriteMessage($"Unable to send the message: {ex}", Colors.Red);
                Disconnect();
            }
        }
        
        private void ParseCommand(string cmdRaw)
        {
            string[] cmdParsed = cmdRaw.Split(new char[] { ' ' }, 2);
            string cmd = cmdParsed[0];
            string[] args = cmdParsed.Length > 1 ? cmdParsed[1].Split('/') : new string[0];
            HandleCommand(cmd, args);
        }

        private void HandleCommand(string cmd, string[] args)
        {
            switch (cmd) 
            {
                case "connect":
                    if (args.Length < 2 || !int.TryParse(args[1], out int port)) 
                    {
                        WriteMessage("Invalid arguments!", Colors.Red);
                        return;
                    }

                    try
                    {
                        WriteMessage($"Connecting to {args[0]}:{port}...", Colors.Green);
                        Connect(args[0], port);
                        WriteMessage($"Connected to {args[0]}:{port}", Colors.Green);
                    }
                    catch (Exception ex) 
                    {
                        WriteMessage($"Unable to connect: {ex}", Colors.Red);
                    }

                    break;
                case "status":
                    WriteMessage(udpClient == null ? "You are not connected!" : 
                        $"You are connected to {remote}", 
                        udpClient == null ? Colors.Red : Colors.Green);
                    break;
                case "clear":
                    txtMessages.Document.Blocks.Clear();
                    sentMessages.Clear();
                    currentSentMessage = 0;
                    break;
                case "disconnect":
                    Disconnect();
                    break;
                case "help":
                    WriteMessage("Available commands:");
                    WriteMessage("- connect ip/port");
                    WriteMessage("- disconnect");
                    WriteMessage("- clear");
                    WriteMessage("- status");
                    break;
                default:
                    WriteMessage("Invalid command!", Colors.Red);
                    break;
            }
        }

        private void Connect(string ip, int port) 
        {
            Disconnect();

            udpClient = new UdpClient();
            remote = new IPEndPoint(IPAddress.Parse(ip), port);
            receiveThread = new Thread(new ThreadStart(() =>
            {
                while (isConnected)
                {
                    try 
                    {
                        byte[] message = udpClient.Receive(ref remote);
                        WriteMessage($"[{GetTimeStamp()}] ", Colors.Brown, false);
                        WriteMessage($"THEM: ", Colors.Red, false);
                        WriteMessage(Encoding.UTF8.GetString(message).Trim());
                    } catch { }
                }
            }));

            isConnected = true;
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            receiveThread.Start();
        }

        private void Disconnect() 
        {
            WriteMessage($"Disconnecting...", Colors.Green);
            isConnected = false;
            if (udpClient != null) udpClient.Close();
            if (receiveThread != null) receiveThread.Abort();
            udpClient = null;
            receiveThread = null;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Disconnect();
        }

        private string GetTimeStamp() 
        {
            return DateTime.Now.ToString("HH:mm:ss");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WriteMessage("Welcome to the shit chat application!", Colors.Green);
            WriteMessage("This is an application I made as my first WPF application lol", Colors.Green);
            WriteMessage("It's just scuffed UDP chatting, i.e any UDP packets are represented as chat messages", Colors.Green);
            WriteMessage("Type /help to get started", Colors.Green);
        }
    }
}
