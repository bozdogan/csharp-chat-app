using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Common
{
    public class XmlMessageProtocol : Protocol
    {
        protected override T Decode<T>(byte[] message)
        {
            XmlSerializer x = new XmlSerializer(typeof(T));
            StringReader sr = new StringReader(Encoding.UTF8.GetString(message));
            return (T) x.Deserialize(sr);
        }

        protected override byte[] EncodeBody<T>(T message)
        {
            XmlSerializer x = new XmlSerializer(typeof(T));
            StringWriter sw = new StringWriter();
            x.Serialize(sw, message);

            StringBuilder sb = sw.GetStringBuilder();
            if(sw.ToString() != sb.ToString())
            {
                throw new Exception($"They're different: {sw.ToString()} != {sb.ToString()}");
            }

            return Encoding.UTF8.GetBytes(sw.ToString());
        }
    }
}
