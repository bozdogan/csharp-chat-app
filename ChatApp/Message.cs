using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class Message
    {
        public const int BUFFER_LENGTH = 2048;

        public int Code { get; set; }
        public MessageBody Body { get; set; }

        public Message(int code, string sender, string text)
        {
            Code = code;
            Body = new MessageBody(sender, text);
        }

        public override string ToString()
        {
            return $"Message(Code={Code}, Body=\"{Body}\")";
        }
    }
    
    public class MessageBody
    {
        public string Sender { get; set; }
        public string Text { get; set; }

        public MessageBody(string sender, string text)
        {
            this.Sender = sender;
            this.Text = text;
        }

        public override string ToString()
        {
            return $"{Text} -{Sender}";
        }
    }
}
