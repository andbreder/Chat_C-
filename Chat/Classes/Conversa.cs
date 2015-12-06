//#define CHAT_RECURSIVO

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using System.Threading;

namespace Chat
{
    public class Conversa
    {
        const int REMOVER_EM = 60; // segundos
        private bool CHAT_ABERTO = false;

        int port;
        IPEndPoint endP;
        UdpClient sock;

        public string name;
        public string ipv4;
        public List<Amigo> amigos;



        public Conversa()
        {
            amigos = new List<Amigo>();
        }

        private IPAddress getIP()
        {
            try
            {
                return Dns.GetHostEntry(Dns.GetHostName()).AddressList.Last(c => c.AddressFamily == AddressFamily.InterNetwork);
            }
            catch
            {
                throw new Exception("Local IP Address Not Found!");
            }
        }
        private Action getAction(string act)
        {
            switch (act.ToLower())
            {
                case "keepalive":
                    return Action.keepalive;
                case "leave":    
                    return Action.leave;
                case "report":   
                    return Action.report;
                case "say":      
                    return Action.say;
                case "search":   
                    return Action.search;
                case "whisper":  
                    return Action.whisper;
                default:
                    return Action.erro;
            }
        }



        private void PacoteEnviar(Pacote pacote)
        {
            if(pacote.target != "127.0.0.1")
                sock.Send(pacote.ToByte(), pacote.Length(), new IPEndPoint(IPAddress.Parse(pacote.target), port));
        }

        private void PacoteRecebido()
        {
            sock.BeginReceive(new AsyncCallback(PacoteLer), null);
        }
        private void PacoteLer(IAsyncResult e)
        {
            try
            {
                Pacote pacote = new Pacote(sock.EndReceive(e, ref endP));

#if !CHAT_RECURSIVO
                //Não peguei meu próprio pacote (O IPv4 de saída do pacote é igual ao meu? Se sim, fui eu mesmo que mandei)
                if (endP.Address.ToString() != ipv4)
                {
#endif
                    switch (getAction(pacote.action))
                    {
                        case Action.keepalive:
                            TakeKeepAlive(pacote, endP.Address);
                            break;
                        case Action.leave:
                            TakeLeave(pacote, endP.Address);
                            break;
                        case Action.report:
                            TakeReport(pacote, endP.Address);
                            break;
                        case Action.say:
                            TakeSay(pacote, endP.Address);
                            break;
                        case Action.search:
                            TakeSearch(pacote, endP.Address);
                            break;
                        case Action.whisper:
                            TakeWhisper(pacote, endP.Address);
                            break;
                        case Action.erro:
                            Report(endP.Address.ToString(), String.Format("?[{0}]? - Action não recohecida", pacote.action));
                            break;

                    }
#if !CHAT_RECURSIVO
                }
#endif
                if (CHAT_ABERTO)
                    PacoteRecebido();
                else
                    sock.Close();
            }
            catch
            {

            }
        }

        public void Search(string nome, int porta)
        {

            name = nome;
            port = porta;
            ipv4 = getIP().ToString();
            endP = new IPEndPoint(IPAddress.Broadcast, port);

            if (sock != null)
                sock.Close();
            sock = new UdpClient(new IPEndPoint(IPAddress.Parse(ipv4), port));

            CHAT_ABERTO = true;

            PacoteRecebido();
            MakeSearch();
        }
        public void Leave()
        {
            CHAT_ABERTO = false;
            MakeLeave();
        }

        public void Say(string msg)
        {
            MakeSay(msg);
        }
        public void Whisper(string msg)
        {
            MakeWhisper(msg);
        }

        public void Report(Amigo amigo, string msg)
        {
            MakeReport(amigo.address.ToString(), msg);
        }
        public void Report(string destino, string msg)
        {
            MakeReport(destino, msg);
        }

    #region SEARCH

        const int SEND_SEARCH_TIME = 2000;

        private void MakeSearch()
        {
            MakeSearch(false);
        }
        private void MakeSearch(bool again)
        {
            if (this.name == null)
                throw new Exception("Cliente sem Nickname");

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) =>
            {
                if (amigos.Count == 0)
                {
                    if (again)
                        Thread.Sleep(SEND_SEARCH_TIME);
                    PacoteEnviar(new Pacote(Action.search, IPAddress.Broadcast, this.name));
                }
            };
            bw.RunWorkerCompleted += (sender, e) =>
            {
                OnMakeSearch(new EventArgs());
                if (amigos.Count == 0)
                    MakeSearch(true);
                else
                    CompleteSearch();
            };
            bw.RunWorkerAsync();

        }

        private void CompleteSearch()
        {
            OnCompleteSearch(new EventArgs());
            MakeKeepAlive();
        }

        private void TakeSearch(Pacote pacote, IPAddress requester)
        {
            if (!(getAction(pacote.action) == Action.search))
                throw new Exception("Action inválida para search (Action recebida: \"" + pacote.action + "\")");
            
            string dest = null;
            string erro = null;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) => 
                {
                    if (amigos.Count != 0)
                        foreach (Amigo amigo in amigos)
                        {
                            //Algum amigo meu tem esse ip? Se sim atualizar o nickname dele.
                            if (amigo.address == requester.ToString())
                            {
                                dest = amigo.address;
                                erro = "Você já está na minha lista, atualizei teu nickname de \"" + amigo.nickname + "\" para \"" + pacote.nickname + "\".";
                                break;
                            }

                            //Algum amigo meu tem esse nickname? Se sim reporto nickname duplicado.
                            if (amigo.nickname == pacote.nickname)
                            {
                                dest = requester.ToString();
                                erro = "Esse nickname já está presente na minha lista, tente outro.";
                                break;
                            }
                        }

                };
            bw.RunWorkerCompleted += (sender, e) =>
                {
                    if (dest != null)
                    {
                        Report(dest, erro);
                        return;
                    }

                    Amigo novo = new Amigo(requester, pacote.nickname);
                    amigos.Add(novo);

                    if (novo != null)
                        OnTakeSearch(novo);
                };
            bw.RunWorkerAsync();
        }

        public event HandlerCHAT SearchMake;
        public virtual void OnMakeSearch(EventArgs e)
        {
            if (SearchMake != null)
                SearchMake(this, e);
        }
        public event HandlerCHAT SearchTake;
        public virtual void OnTakeSearch(Amigo cliente)
        {
            if (SearchTake != null)
                SearchTake(cliente, new EventArgs());
        }
        public event HandlerCHAT SearchComplete;
        public virtual void OnCompleteSearch(EventArgs e)
        {
            if (SearchComplete != null)
                SearchComplete(this, e);
        }

    #endregion

    #region KEEPALIVE

        const int SEND_KEEPALIVE_TIME = 10000;

        private void MakeKeepAlive()
        {
            if (this.amigos == null)
                throw new Exception("Lista de amigos não definida");
            if (this.amigos.Count == 0)
            {
                OnNullKeepAlive(new EventArgs());
                return;
            }

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) =>
            {
                foreach (Amigo amigo in amigos)
                    PacoteEnviar(new Pacote(amigos, amigo.address, this.name));
            };
            bw.RunWorkerCompleted += (sender, e) =>
            {
                OnMakeKeepAlive(new EventArgs());
                KeepAliveWait();
            };
            bw.RunWorkerAsync();
        }
        private void KeepAliveWait()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) =>
            {
                List<Amigo> sairam = new List<Amigo>();
                foreach (Amigo amigo in amigos)
                    if (DateTime.Compare(DateTime.Now, amigo.timestamp.AddSeconds(REMOVER_EM)) == 1)
                        sairam.Add(amigo);

                if (sairam.Count != 0)
                    foreach (Amigo amigo in sairam)
                        amigos.Remove(amigo);

                Thread.Sleep(SEND_KEEPALIVE_TIME);
            };
            bw.RunWorkerCompleted += (sender, e) =>
            {
                MakeKeepAlive();
            };
            bw.RunWorkerAsync();
        }
        private void TakeKeepAlive(Pacote pacote, IPAddress requester)
        {
            if (!(getAction(pacote.action) == Action.keepalive))
                throw new Exception("Action inválida para keepalive (Action recebida: \"" + pacote.action + "\")");

            Amigo keepTaked = null;
            
            string dest = null;
            string erro = null;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) => 
                {
                    if (this.amigos.Count == 0)
                        amigos.Add(new Amigo(requester, pacote.nickname));
                    else
                    {
                        foreach (Amigo amigo in amigos)
                        {
                            //Algum amigo meu tem esse ip?
                            if (amigo.address == requester.ToString())
                            {
                                //Esse amigo tem esse nickname? Se sim atualizar o timestamp dele.
                                if (amigo.nickname == pacote.nickname)
                                {
                                    amigo.timestamp = DateTime.Now;
                                    keepTaked = amigo;
                                    return;
                                }
                                else
                                {
                                    dest = amigo.address;
                                    erro = "Você mandou um keepalive, mas teu nickname era outro, atualizei aqui de \"" + amigo.nickname + "\" para \"" + pacote.nickname + "\".";
                                    return;
                                }
                            }
                        }

                        dest = requester.ToString();
                        erro = "Você mandou um keepalive mas estava fora da minha lista de amigos, verfique o tempo que você leva para enviar keepalive";
                        amigos.Add(new Amigo(requester, pacote.nickname));
                    }
                };
             bw.RunWorkerCompleted += (sender, e) =>
                {
                    ControleDeAmigos(pacote);

                    if (dest != null)
                        Report(dest, erro);
                    
                    if (keepTaked != null)
                        OnTakeKeepAlive(keepTaked);
                };
             bw.RunWorkerAsync();
        }

        private void ControleDeAmigos(Pacote pacote)
        {

            List<Amigo> amigosAtuais = pacote.users;
            if (pacote.users != null) //O cara tem amigos?
                foreach (Amigo amigoDoAmigo in amigosAtuais) //Quem são os amigos
                    if (amigoDoAmigo.address != this.ipv4) //Esse amigo... sou eu?
                        if (!amigos.Any(x => x.address == amigoDoAmigo.address)) //Eu conheço?
                            amigos.Add(new Amigo(amigoDoAmigo.address, amigoDoAmigo.nickname)); //Então me apresente
            pacote.users = amigosAtuais;
        }

        public event HandlerCHAT KeepAliveMake;
        public virtual void OnMakeKeepAlive(EventArgs e)
        {
            if (KeepAliveMake != null)
                KeepAliveMake(this, e);
        }
        public event HandlerCHAT KeepAliveTake;
        public virtual void OnTakeKeepAlive(Amigo cliente)
        {
            if (KeepAliveTake != null)
                KeepAliveTake(cliente, new EventArgs());
        }
        public event HandlerCHAT KeepAliveNull;
        public virtual void OnNullKeepAlive(EventArgs e)
        {
            if (KeepAliveNull != null)
                KeepAliveNull(this, e);
        }

    #endregion

    #region SAY

        private void MakeSay(string texto)
        {
            if (this.amigos == null)
                throw new Exception("Lista de amigos não definida");
            if (this.amigos.Count == 0)
                throw new Exception("Cliente sem amigos");

            Pacote pacote = new Pacote();

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) =>
            {
                pacote = new Pacote(Action.say, "", this.name, texto);
                foreach (Amigo amigo in amigos)
                {
                    pacote.target = amigo.address;
                    PacoteEnviar(pacote);
                }
            };
            bw.RunWorkerCompleted += (sender, e) =>
            {
                OnMakeSay(pacote);
            };
            bw.RunWorkerAsync();
        }
        private void TakeSay(Pacote pacote, IPAddress requester)
        {
            if (!(getAction(pacote.action) == Action.say))
                throw new Exception("Action inválida para say (Action recebida: \"" + pacote.action + "\")");

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) =>
            {
                if (String.IsNullOrEmpty(pacote.nickname))
                    pacote.nickname = this.amigos.Single(x => x.address == requester.ToString()).nickname;
            };
            bw.RunWorkerCompleted += (sender, e) =>
            {
                OnTakeSay(pacote);
            };
            bw.RunWorkerAsync();

        }

        public event HandlerCHAT SayMake;
        public virtual void OnMakeSay(Pacote pacote)
        {
            if (SayMake != null)
                SayMake(pacote, new EventArgs());
        }
        public event HandlerCHAT SayTake;
        public virtual void OnTakeSay(Pacote pacote)
        {
            if (SayTake != null)
                SayTake(pacote, new EventArgs());
        }

    #endregion

    #region WHISPER

        private void MakeWhisper(string texto)
        {
            if (this.amigos == null)
                throw new Exception("Lista de amigos não definida");
            if (this.amigos.Count == 0)
                throw new Exception("Cliente sem amigos");
            if (!(this.amigos.Any(c => c.confident)))
                throw new Exception("Você não tem confidentes, selecione-os na sua lista de amigos");

            Pacote pacote = new Pacote();

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) =>
            {
                foreach (Amigo amigo in amigos)
                {
                    if (amigo.confident)
                    {
                        pacote = new Pacote(Action.whisper, amigo.address, this.name, texto);
                        PacoteEnviar(pacote);
                    }
                }
            };
            bw.RunWorkerCompleted += (sender, e) =>
            {
                OnMakeWhisper(pacote);
            };
            bw.RunWorkerAsync();
        }
        private void TakeWhisper(Pacote pacote, IPAddress requester)
        {
            if (!(getAction(pacote.action) == Action.whisper))
                throw new Exception("Action inválida para whisper (Action recebida: \"" + pacote.action + "\")");

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) =>
            {
                if (String.IsNullOrEmpty(pacote.nickname))
                    pacote.nickname = this.amigos.Single(x => x.address == requester.ToString()).nickname;
            };
            bw.RunWorkerCompleted += (sender, e) =>
            {
                OnTakeWhisper(pacote);
            };
            bw.RunWorkerAsync();

        }

        public event HandlerCHAT WhisperMake;
        public virtual void OnMakeWhisper(Pacote pacote)
        {
            if (WhisperMake != null)
                WhisperMake(pacote, new EventArgs());
        }
        public event HandlerCHAT WhisperTake;
        public virtual void OnTakeWhisper(Pacote pacote)
        {
            if (WhisperTake != null)
                WhisperTake(pacote, new EventArgs());
        }

    #endregion

    #region LEAVE

        private void MakeLeave()
        {
            if (this.amigos == null)
                throw new Exception("Lista de amigos não definida");
            //if (this.amigos.Count == 0)
            //    throw new Exception("Cliente sem amigos");

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) =>
            {
                foreach (Amigo amigo in amigos)
                    PacoteEnviar(new Pacote(Action.leave, amigo.address, this.name));
                amigos.Clear();
            };
            bw.RunWorkerCompleted += (sender, e) =>
            {
                OnMakeLeave(new EventArgs());
                OnNullKeepAlive(new EventArgs());
            };
            bw.RunWorkerAsync();
        }
        private void TakeLeave(Pacote pacote, IPAddress requester)
        {
            if (!(getAction(pacote.action) == Action.leave))
                throw new Exception("Action inválida para leave (Action recebida: \"" + pacote.action + "\")");

            if (this.amigos == null)
                throw new Exception("Lista de amigos não definida");
            if (this.amigos.Count == 0)
                return;

            Amigo quemSaiu = null;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) =>
            {
                quemSaiu = amigos.Single(c => c.address == requester.ToString() && c.nickname == pacote.nickname);
                if (quemSaiu != null)
                    amigos.Remove(quemSaiu);
            };
            bw.RunWorkerCompleted += (sender, e) =>
            {
                if (quemSaiu != null)
                    OnTakeLeave(quemSaiu);
            };
            bw.RunWorkerAsync();

        }

        public event HandlerCHAT LeaveMake;
        public virtual void OnMakeLeave(EventArgs e)
        {
            if (LeaveMake != null)
                LeaveMake(this, e);
        }
        public event HandlerCHAT LeaveTake;
        public virtual void OnTakeLeave(Amigo cliente)
        {
            if (LeaveTake != null)
                LeaveTake(cliente, new EventArgs());
        }

    #endregion

    #region REPORT

        private void MakeReport(string taget, string texto)
        {
            if (this.amigos == null)
                throw new Exception("Lista de amigos não definida");
            if (this.amigos.Count == 0)
                throw new Exception("Cliente sem amigos");

            Pacote pacote = new Pacote(Action.report);
            pacote.message = texto;
            pacote.nickname = this.name;
            pacote.target = taget;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) =>
            {
                PacoteEnviar(pacote);
            };
            bw.RunWorkerCompleted += (sender, e) =>
            {
                OnMakeReport(pacote);
            };
            bw.RunWorkerAsync();
        }
        private void TakeReport(Pacote pacote, IPAddress requester)
        {
            if (!(getAction(pacote.action) == Action.report))
                throw new Exception("Action inválida para Report (Action recebida: \"" + pacote.action + "\")");

            if (this.amigos == null)
                throw new Exception("Lista de amigos não definida");
            if (this.amigos.Count == 0)
            {
                amigos.Add(new Amigo(requester, pacote.nickname));
            }

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) =>
            {
                if (String.IsNullOrEmpty(pacote.nickname))
                    pacote.nickname = this.amigos.Single(x => x.address == requester.ToString()).nickname;
            };
            bw.RunWorkerCompleted += (sender, e) =>
            {
                OnTakeReport(pacote);
            };
            bw.RunWorkerAsync();

        }

        public event HandlerCHAT ReportMake;
        public virtual void OnMakeReport(Pacote pacote)
        {
            if (ReportMake != null)
                ReportMake(pacote, new EventArgs());
        }
        public event HandlerCHAT ReportTake;
        public virtual void OnTakeReport(Pacote pacote)
        {
            if (ReportTake != null)
                ReportTake(pacote, new EventArgs());
        }

    #endregion

    }
}
