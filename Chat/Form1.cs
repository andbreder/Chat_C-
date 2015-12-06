using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Settings = Chat.Properties.Settings;

namespace Chat
{
    public partial class Form1 : Form
    {
        enum Maked
        {
            make,
            take,
            makeW,
            takeW
        }

        Conversa conversa;

        public Form1()
        {
            InitializeComponent();

            conversa = new Conversa();

            conversa.SearchTake += new HandlerCHAT(OnTakeSearch);
            conversa.SearchMake += new HandlerCHAT(OnMakeSearch);
            conversa.SearchComplete += new HandlerCHAT(OnCompleteSearch);

            conversa.KeepAliveTake += new HandlerCHAT(OnTakeKeepAlive);
            conversa.KeepAliveMake += new HandlerCHAT(OnMakeKeepAlive);
            conversa.KeepAliveNull += new HandlerCHAT(OnNullKeepAlive);

            conversa.SayTake += new HandlerCHAT(OnTakeSay);
            conversa.SayMake += new HandlerCHAT(OnMakeSay);

            conversa.WhisperTake += new HandlerCHAT(OnTakeWhisper);
            conversa.WhisperMake += new HandlerCHAT(OnMakeWhisper);

            conversa.LeaveTake += new HandlerCHAT(OnTakeLeave);
            conversa.LeaveMake += new HandlerCHAT(OnMakeLeave);

            conversa.ReportTake += new HandlerCHAT(OnTakeReport);
            conversa.ReportMake += new HandlerCHAT(OnMakeReport);

            txtPorta.Text = Settings.Default.porta.ToString();
        }

    #region LOG

        Color COLOR_MAKED = Color.FromArgb(220, 250, 200);
        Color COLOR_TAKED = Color.White;
        Color COLOR_MAKEW = Color.AliceBlue;
        Color COLOR_TAKEW = Color.Beige;
        private void PrintLog(Maked tipo, Action evento, string sender, string content)
        {
            dgvLog.Rows.Add(
                new string[] { evento.ToString(), DateTime.Now.ToString( "hh:mm:ss.fff"),
                               sender, content });

            DataGridViewRow linha = dgvLog.Rows[dgvLog.RowCount - 1];
            DataGridViewCell cel1 = dgvLog.Rows[dgvLog.RowCount - 1].Cells[0];

            setLineColor(tipo, linha);
            setActionColor(evento, cel1);

            dgvLog.CurrentCell = cel1;
        }
        private void setLineColor(Maked tipo, DataGridViewRow linha)
        {
            switch(tipo)
            {
                case(Maked.make):
                    linha.DefaultCellStyle.BackColor = COLOR_MAKED;
                    linha.DefaultCellStyle.SelectionBackColor = COLOR_MAKED;
                    break;
                case(Maked.take):
                    linha.DefaultCellStyle.BackColor = COLOR_TAKED;
                    linha.DefaultCellStyle.SelectionBackColor = COLOR_TAKED;
                    break;
                case (Maked.makeW):
                    linha.DefaultCellStyle.BackColor = COLOR_MAKEW;
                    linha.DefaultCellStyle.SelectionBackColor = COLOR_MAKEW;
                    break;
                case (Maked.takeW):
                    linha.DefaultCellStyle.BackColor = COLOR_TAKEW;
                    linha.DefaultCellStyle.SelectionBackColor = COLOR_TAKEW;
                    break;
            }
        }
        private void setActionColor(Action evento, DataGridViewCell celAction)
        {
            Color corEvento = Color.Black;
            switch (evento)
            {
                case Action.keepalive:
                    corEvento = Color.Green;
                    break;
                case Action.leave:
                    corEvento = Color.DarkOrange;
                    break;
                case Action.report:
                    corEvento = Color.Red;
                    break;
                case Action.say:
                    corEvento = Color.Black;
                    break;
                case Action.search:
                    corEvento = Color.DodgerBlue;
                    break;
                case Action.whisper:
                    corEvento = Color.Gray;
                    break;
            }

            celAction.Style.ForeColor = corEvento;
            celAction.Style.SelectionForeColor = corEvento;
        }
        
    #endregion

    #region EVENTOS

        public void OnTakeSearch(object sender, EventArgs e)
        {
            Amigo amigo = (Amigo)sender;
            PrintLog(Maked.take, Action.search, amigo.nickname, "Search recebido");
        }
        public void OnMakeSearch(object sender, EventArgs e)
        {
            PrintLog(Maked.make, Action.search, "Eu mesmo", "Search enviado");
        }
        public void OnCompleteSearch(object sender, EventArgs e)
        {
            picSearchWait.Visible = false;
            btnLeave.Enabled = true;
            PrintLog(Maked.make, Action.search, "Eu mesmo", "Search completado. A sala está aberta");

            pnlChat.Enabled = true;

            txtMensagem.Focus();
        }

        private void OnTakeKeepAlive(object sender, EventArgs e)
        {
            Amigo amigo = (Amigo)sender;
            PrintLog(Maked.take, Action.keepalive, amigo.nickname, "KeepAlive recebido");
        }
        private void OnMakeKeepAlive(object sender, EventArgs e)
        {
            PrintLog(Maked.make, Action.keepalive, "Eu mesmo", "KeepAlive enviado");
            Listar();
        }
        private void OnNullKeepAlive(object sender, EventArgs e)
        {
            PrintLog(Maked.make, Action.keepalive, "Eu mesmo", "A sala está vazia");
            Listar();

            pnlChat.Enabled = false;

            txtNick.Enabled = true;
            txtPorta.Enabled = true;
            btnEntrar.Enabled = true;

            btnLeave.Enabled = false;
        }

        public void OnTakeSay(object sender, EventArgs e)
        {
            Pacote pacote = (Pacote)sender;
            PrintLog(Maked.take, Action.say, pacote.nickname, "Say Recebido, content => " + pacote.content);

            dgvChat.Rows.Add("S<=", DateTime.Now.ToString("hh:mm"), pacote.nickname, pacote.content);

            DataGridViewRow linha = dgvChat.Rows[dgvChat.RowCount - 1];
            DataGridViewCell cel1 = dgvChat.Rows[dgvChat.RowCount - 1].Cells[0];
            cel1.ToolTipText = "Msg Pública Recebida";

            setLineColor(Maked.take, linha);
            setActionColor(Action.say, cel1);

            dgvChat.CurrentCell = cel1;
        }
        public void OnMakeSay(object sender, EventArgs e)
        {
            Pacote pacote = (Pacote)sender;
            PrintLog(Maked.make, Action.say, "Eu mesmo", "Say Enviado, content => " + pacote.content);
            
            txtMensagem.Clear();

            dgvChat.Rows.Add("S=>", DateTime.Now.ToString("hh:mm"), "Eu mesmo", pacote.content);

            DataGridViewRow linha = dgvChat.Rows[dgvChat.RowCount - 1];
            DataGridViewCell cel1 = dgvChat.Rows[dgvChat.RowCount - 1].Cells[0];
            cel1.ToolTipText = "Msg Pública Enviada";

            setLineColor(Maked.make, linha);
            setActionColor(Action.say, cel1);

            dgvChat.CurrentCell = cel1;
        }

        public void OnTakeWhisper(object sender, EventArgs e)
        {
            Pacote pacote = (Pacote)sender;
            PrintLog(Maked.take, Action.whisper, pacote.nickname, "Whisper recebido, content => " + pacote.content);

            dgvChat.Rows.Add("W<=", DateTime.Now.ToString("hh:mm"), pacote.nickname, pacote.content);

            DataGridViewRow linha = dgvChat.Rows[dgvChat.RowCount - 1];
            DataGridViewCell cel1 = dgvChat.Rows[dgvChat.RowCount - 1].Cells[0];
            cel1.ToolTipText = "Msg Privada Recebida";

            setLineColor(Maked.takeW, linha);
            setActionColor(Action.whisper, cel1);

            dgvChat.CurrentCell = cel1;
        }
        public void OnMakeWhisper(object sender, EventArgs e)
        {
            Pacote pacote = (Pacote)sender;
            PrintLog(Maked.make, Action.whisper, "Eu mesmo", "Whisper Enviado, content => " + pacote.content);

            txtMensagem.Clear();

            dgvChat.Rows.Add("W=>", DateTime.Now.ToString("hh:mm"), "Eu mesmo", pacote.content);

            DataGridViewRow linha = dgvChat.Rows[dgvChat.RowCount - 1];
            DataGridViewCell cel1 = dgvChat.Rows[dgvChat.RowCount - 1].Cells[0];
            cel1.ToolTipText = "Msg Privada Enviada";

            setLineColor(Maked.makeW, linha);
            setActionColor(Action.whisper, cel1);

            dgvChat.CurrentCell = cel1;
        }

        public void OnTakeLeave(object sender, EventArgs e)
        {
            Amigo amigo = (Amigo)sender;
            PrintLog(Maked.take, Action.leave, amigo.nickname, amigo.nickname + " saiu do chat");
            Listar();
        }
        public void OnMakeLeave(object sender, EventArgs e)
        {
            PrintLog(Maked.make, Action.leave, "Eu mesmo", "Eu mesmo sai do chat");
        }

        public void OnTakeReport(object sender, EventArgs e)
        {
            Pacote pacote = (Pacote)sender;
            PrintLog(Maked.take, Action.report, pacote.nickname, "Erro Recebido, message => " + pacote.message);
        }
        public void OnMakeReport(object sender, EventArgs e)
        {
            Pacote pacote = (Pacote)sender;
            PrintLog(Maked.make, Action.report, "Eu mesmo", "Erro Enviado, message => " + pacote.message);
        }

    #endregion



    #region Inicializar Chat

        private void txtNick_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                Login();
        }

        private void btnEntrar_Click(object sender, EventArgs e)
        {
            Login();
        }
        private void btnLeave_Click(object sender, EventArgs e)
        {
            conversa.Leave();
        }
        
        private void Login()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(txtNick.Text))
                    throw new Exception("Preencha o nickname");
                if (Settings.Default.porta <= 0)
                    throw new Exception("Porta não definida");

                conversa.Search(txtNick.Text, Settings.Default.porta);

                btnEntrar.Enabled = false;
                txtNick.Enabled = false;
                txtPorta.Enabled = false;
                picSearchWait.Visible = true;
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Erro Login", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtMensagem.Focus();
            }
        }

        private void txtPorta_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!String.IsNullOrEmpty(txtPorta.Text))
                {
                    int porta = Settings.Default.porta;
                    if (int.TryParse(txtPorta.Text, out porta))
                    {
                        Settings.Default.porta = porta;
                        Settings.Default.Save();
                        MessageBox.Show(this, "Número da porta alterado", "Config Porta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                txtPorta.Text = Settings.Default.porta.ToString();
            }
        }

    #endregion

    #region Lista de Amigos

        private void Listar()
        {
            dgvAmigos.Rows.Clear();

            foreach (Amigo amigo in conversa.amigos)
                dgvAmigos.Rows.Add(amigo.confident, amigo.nickname, amigo.address, amigo.timestamp.ToString("hh:mm:ss"));
        
        }

        private void dgvAmigos_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 0)
                {
                    conversa.amigos.Single(x => x.address == dgvAmigos.Rows[e.RowIndex].Cells[2].Value.ToString()).confident = !(bool)dgvAmigos.Rows[e.RowIndex].Cells[0].Value;
                    Listar();
                }
            }
            catch
            { 
            
            }
        }
        private void dgvAmigos_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }
        private void dgvAmigos_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

    #endregion

    #region Chat

        private void txtMensagem_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
                if (e.Control) 
                    { EnviarMsgPrivada(txtMensagem.Text); }
                else
                    { EnviarMsgPublica(txtMensagem.Text); }
        }

        private void btnSay_Click(object sender, EventArgs e)
        {
            EnviarMsgPublica(txtMensagem.Text);
        }
        private void EnviarMsgPublica(string texto)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(texto))
                    conversa.Say(texto);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Erro Say", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnWhisper_Click(object sender, EventArgs e)
        {
            EnviarMsgPrivada(txtMensagem.Text);
        }
        private void EnviarMsgPrivada(string texto)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(texto))
                    conversa.Whisper(texto);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Erro Whisper", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

    #endregion

    #region Log Eventos

        private void lblCloseLog_Click(object sender, EventArgs e)
        {
            if (pnlLog.Height != 16)
            { lblCloseLog.Text = "p"; pnlLog.Height = 016; }
            else
            { lblCloseLog.Text = "q"; pnlLog.Height = 158; }
        }

    #endregion



        


    }
}
