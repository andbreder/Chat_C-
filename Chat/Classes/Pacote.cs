using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

using JSON = System.Runtime.Serialization.Json.DataContractJsonSerializer;
using System.Net;

namespace Chat
{
    [DataContract]
    public class Pacote
    {
        [DataMember]
        public string action;
        [DataMember]
        public string content;
        [DataMember]
        public string target;
        [DataMember]
        public string nickname;
        [DataMember]
        public string message;
        [DataMember]
        public List<Amigo> users;

        public Pacote()
        {
            users = new List<Amigo>();
        }
        public Pacote(Action act)
        {
            users = new List<Amigo>();
            action = act.ToString();
        }
        public Pacote(Action act, string nick)
        {
            users = new List<Amigo>();
            action = act.ToString();
            nickname = nick;
        }
        public Pacote(Action act, IPAddress target, string nick)
        {
            this.users = new List<Amigo>();
            this.action = act.ToString();
            this.target = target.ToString();
            this.nickname = nick;
        }
        public Pacote(Action act, string target, string nick)
        {
            this.users = new List<Amigo>();
            this.action = act.ToString();
            this.target = target;
            this.nickname = nick;
        }
        public Pacote(Action act, string target, string nick, string content)
        {
            this.users = new List<Amigo>();
            this.action = act.ToString();
            this.target = target;
            this.nickname = nick;
            this.content = content;
        }
        public Pacote(List<Amigo> amigos, string target, string nick)
        {
            this.users = amigos;
            this.action = Action.keepalive.ToString();
            this.target = target;
            this.nickname = nick;
        }
        public Pacote(byte[] msg)
        {
            users = new List<Amigo>();

            JSON ser = new JSON(typeof(Pacote));
            using (var ms = new MemoryStream(msg))
            {
                Pacote novo = (Pacote)ser.ReadObject(ms);

                if (novo.action.ToLower() == "KEEPALIVE")
                    this.action = "";

                this.action = novo.action.ToLower();
                this.content = novo.content;
                this.message = novo.message;
                this.nickname = novo.nickname;
                this.target = novo.target;
                this.users = novo.users;
            }
        }

        public override string ToString()
        {
            JSON jsonSerializer;
            MemoryStream memoryStream;
            StreamReader streamReader;

            memoryStream = new MemoryStream();
            jsonSerializer = new JSON(typeof(Pacote));

            jsonSerializer.WriteObject(memoryStream, this);

            memoryStream.Position = 0;
            streamReader = new StreamReader(memoryStream);

            return streamReader.ReadToEnd();
        }
        public byte[] ToByte()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }
        public int Length()
        {
            return ToByte().Length;
        }
    }
}
