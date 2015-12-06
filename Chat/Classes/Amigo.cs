using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Net;

namespace Chat
{
    [DataContract]
    public class Amigo
    {
        [DataMember]
        public string address;
        [DataMember]
        public string nickname;

        public DateTime timestamp;
        public bool confident;

        public Amigo(string ip, string nome)
        {
            nickname = nome;
            address = ip;
            timestamp = DateTime.Now;
            confident = false;
        }
        public Amigo(IPAddress ip, string nome)
        {
            nickname = nome;
            address = ip.ToString();
            timestamp = DateTime.Now;
            confident = false;
        }

        public override string ToString()
        {
            return String.Format("name=\"{0}\"; ipv4=\"{1}\"; alive=\"{2}\"; confident=\"{3}\"", nickname, address, timestamp.ToString("hh:mm"), confident);
        }
    }
}
