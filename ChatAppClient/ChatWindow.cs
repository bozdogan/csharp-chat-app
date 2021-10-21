using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Common;

namespace ChatAppClient
{
    public partial class ChatWindow : Form
    {
        // NOTE(bora): This is used for invoke pattern to make "cross-thread operation"
        private delegate void SafeCallDelegateMessage(string senderName, string messageText);
        private delegate void SafeCallDelegateStatus(string statusMessage);
        private string nickname;
        private NetworkStream networkStream;


        public ChatWindow(string nickname, NetworkStream networkStream)
        {
            InitializeComponent();
            this.nickname = nickname;
            this.networkStream = networkStream;
        }

        private void ChatWindow_Load(object sender, EventArgs e)
        {
            Task.Run(() => ListenLoop());
        }

        private void sendBt_Click(object sender, EventArgs e)
        {
            string message = messageTx.Text;
            Task.Run(() => SendMessage(
                Encoding.UTF8.GetBytes($"{nickname}: {message}")));
            messageTx.Clear();
        }

        void AppendToChatBoxSafe(string senderName, string messageText)
        {
            if(chatTx.InvokeRequired)
            {
                SafeCallDelegateMessage d = new SafeCallDelegateMessage(AppendToChatBox);
                chatTx.Invoke(d, new object[] { senderName, messageText});
            }
            else
            {
                AppendToChatBox(senderName, messageText);
            }
        }

        void AppendToChatBox(string senderName, string messageText)
        {
            string nameTag = $"\n{senderName}: ";

            chatTx.SelectionFont = new Font(chatTx.SelectionFont, FontStyle.Bold);
            chatTx.AppendText(nameTag);

            chatTx.SelectionFont = new Font(chatTx.SelectionFont, FontStyle.Regular);
            chatTx.AppendText(messageText);
        }

        void AppendToChatBoxSafe(string statusMessage)
        {
            if(chatTx.InvokeRequired)
            {
                SafeCallDelegateStatus d = new SafeCallDelegateStatus(AppendToChatBox);
                chatTx.Invoke(d, new object[] { statusMessage });
            }
            else
            {
                AppendToChatBox(statusMessage);
            }
        }

        void AppendToChatBox(string statusMessage)
        {
            chatTx.SelectionFont = new Font(chatTx.SelectionFont, FontStyle.Italic);
            chatTx.AppendText("\n" + statusMessage);
        }

        private async void SendMessage(byte[] message)
        {
            await networkStream.WriteAsync(message, 0, message.Length).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[SendMessage]: {message.Length} bytes sent to the server.");
            System.Diagnostics.Debug.WriteLine(Encoding.UTF8.GetString(message));
        }

        private async void ListenLoop()
        {
            while(true)
            {
                try
                {
                    byte[] buffer = new byte[Common.Message.BUFFER_LENGTH];
                    int messageLen = await networkStream.ReadAsync(buffer, 0, buffer.Length)
                        .ConfigureAwait(false);

                    string message = Encoding.UTF8.GetString(buffer).Substring(0, messageLen);
                    string[] message_tokens = message.Split(":", 2);

                    System.Diagnostics.Debug.WriteLine($"[{message_tokens.Length}] :: {message_tokens[0]}:{message_tokens[1]}");
                    AppendToChatBoxSafe(message_tokens[0], message_tokens[1]);
                }
                catch(IOException)
                {
                    // NOTE(bora): Server disconnected. Maybe try to reconnect?
                    AppendToChatBoxSafe("Disconnected.");
                    break;
                }
            }
        }
    }
}
