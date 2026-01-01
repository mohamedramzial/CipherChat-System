using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json; //  √ﬂœ „‰ ÊÃÊœ „ﬂ »… Json

namespace ServerApp
{
    public partial class Form1 : Form
    {

        // ﬂ·«” · Œ“Ì‰ »Ì«‰«  «·⁄„Ì· «·„ ’·
        public class ClientInfo
        {
            public TcpClient TcpClient { get; set; }
            public string Name { get; set; }
            public string Ip { get; set; }
            public int Port { get; set; }
            public DateTime ConnectTime { get; set; }
            public int GridRowIndex { get; set; } // ·„⁄—›… „ﬂ«‰Â ›Ì «·ÃœÊ·
        }

        TcpListener listener;
        // ﬁ«∆„… · Œ“Ì‰ «·⁄„·«¡ «·„ ’·Ì‰ Õ«·Ì«
        List<ClientInfo> connectedClients = new List<ClientInfo>();
        // ﬁ«∆„… · Œ“Ì‰ «·¬Ì»ÌÂ«  «·„ÕŸÊ—…
        List<string> bannedIps = new List<string>();

        public Form1()
        {
            InitializeComponent();

            SetupDataGridView(); // œ«· ﬂ «·ﬁœÌ„… ·≈⁄œ«œ «·√⁄„œ…
            ApplyModernStyle();  // <--- «” œ⁄«¡ œ«·… «· ’„Ì„ «·ÃœÌœ… Â‰«

            // —»ÿ ÕœÀ «· Õ—Ìﬂ »«·›Ê—„
            this.MouseDown += Form1_MouseDown;
        }

        private void SetupDataGridView()
        {
            dgvClients.ColumnCount = 5;
            dgvClients.Columns[0].Name = "Username";
            dgvClients.Columns[1].Name = "IP Address";
            dgvClients.Columns[2].Name = "Port";
            dgvClients.Columns[3].Name = "Connect Time";
            dgvClients.Columns[4].Name = "Status";

            //  ·ÊÌ‰ «·ÂÌœ— ·ÌﬂÊ‰ «Õ —«›Ì
            dgvClients.EnableHeadersVisualStyles = false;
            dgvClients.ColumnHeadersDefaultCellStyle.BackColor = Color.Navy;
            dgvClients.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 5000);
                listener.Start();
                btnStart.Enabled = false;
                Log("Server Started on Port 5000...");
                Log("Waiting for connections...");

                _ = AcceptClientsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting server: " + ex.Message);
            }
        }

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Log(">> New Client Connected!");
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            int clientPort = ((IPEndPoint)client.Client.RemoteEndPoint).Port;

            // 1. ›Õ’ «·ÕŸ— (Ban Check)
            if (bannedIps.Contains(clientIp))
            {
                Log($"Blocked connection attempt from banned IP: {clientIp}");
                client.Close();
                return;
            }

            NetworkStream stream = client.GetStream();
            ClientInfo currentClient = null;

            try
            {
                byte[] buffer = new byte[1024 * 1024 * 5];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)

                {
                    Log($"Relayed encrypted packet ({bytesRead} bytes).");
                    string encryptedJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Log($" Encrypted Data: {encryptedJson}");
                    string json = CryptoHelper.Decrypt(encryptedJson); // ›ﬂ «· ‘›Ì—
                    var packet = JsonConvert.DeserializeObject<MessagePacket>(json);


                    // ⁄‰œ √Ê· —”«·…° ‰”Ã· »Ì«‰«  «·⁄„Ì· ›Ì «·ÃœÊ·
                    if (currentClient == null)
                    {
                        currentClient = new ClientInfo
                        {
                            TcpClient = client,
                            Name = packet.SenderName,
                            Ip = clientIp,
                            Port = clientPort,
                            ConnectTime = DateTime.Now
                        };

                        // ≈÷«›… ··ÃœÊ· (Thread Safe)
                        Invoke(new Action(() =>
                        {
                            int rowIndex = dgvClients.Rows.Add(currentClient.Name, currentClient.Ip, currentClient.Port, currentClient.ConnectTime.ToShortTimeString(), "Connected");
                            currentClient.GridRowIndex = rowIndex;
                            dgvClients.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen; // ·Ê‰ √Œ÷— ··„ ’·
                            connectedClients.Add(currentClient);
                        }));

                        Log($"{packet.SenderName} connected from {clientIp}");
                        BroadcastMessage($"Server: {packet.SenderName} joined the chat.");
                    }

                    // ≈–« ﬂ«‰  —”«·… ⁄«œÌ… √Ê „·›° ‰—”·Â« ··Ã„Ì⁄
                    BroadcastPacket(packet);
                }
            }
            catch
            {
                // «‰ﬁÿ⁄ «·« ’«·
            }
            finally
            {
                //  ‰ŸÌ› »⁄œ Œ—ÊÃ «·⁄„Ì·
                if (currentClient != null)
                {
                    Invoke(new Action(() =>
                    {
                        UpdateClientStatus(currentClient, "Disconnected", Color.LightGray);
                    }));

                    connectedClients.Remove(currentClient);
                    Log($"{currentClient.Name} disconnected.");
                    BroadcastMessage($"Server: {currentClient.Name} left the chat.");
                }
                client.Close();
            }
        }

        // œ«·… · ÕœÌÀ Õ«·… «·⁄„Ì· ›Ì «·ÃœÊ·
        private void UpdateClientStatus(ClientInfo client, string status, Color color)
        {
            if (client.GridRowIndex < dgvClients.Rows.Count)
            {
                dgvClients.Rows[client.GridRowIndex].Cells[4].Value = status;
                dgvClients.Rows[client.GridRowIndex].DefaultCellStyle.BackColor = color;
            }
        }

        // ≈⁄«œ… ≈—”«· «·Õ“„… ··Ã„Ì⁄
        private async void BroadcastPacket(MessagePacket packet)
        {
            string json = JsonConvert.SerializeObject(packet);
            string encryptedJson = CryptoHelper.Encrypt(json);
            byte[] data = Encoding.UTF8.GetBytes(encryptedJson);

            foreach (var c in connectedClients.ToArray()) // ToArray · Ã‰» «·√Œÿ«¡ √À‰«¡ «· ⁄œÌ·
            {
                try
                {
                    if (c.TcpClient.Connected)
                    {
                        await c.TcpClient.GetStream().WriteAsync(data, 0, data.Length);
                    }
                }
                catch { }
            }
        }

        private void BroadcastMessage(string msg)
        {
            var packet = new MessagePacket { SenderName = "System", Type = "Text", Content = msg };
            BroadcastPacket(packet);
        }

        private void Log(string msg)
        {
            // «· √ﬂœ „‰ √‰‰« ‰ﬂ » „‰ «·‹ Thread «·’ÕÌÕ
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Log), msg);
                return;
            }

            // 1. ≈÷«›… «·—”«·… „⁄ «·Êﬁ  «·Õ«·Ì
            string time = DateTime.Now.ToString("HH:mm:ss");
            lbLog.Items.Add($"[{time}] {msg}");

            // 2. «·‰“Ê·  ·ﬁ«∆Ì« ·¬Œ— ”ÿ— (Auto Scroll)
            lbLog.TopIndex = lbLog.Items.Count - 1;
        }

        // ========================================================
        // 4. «· Õﬂ„ (ﬂ·Ìﬂ Ì„Ì‰) - Ban, Disconnect, Unban
        // ========================================================

        // “— «·ÿ—œ (Disconnect)
        private void toolStripMenuItemDisconnect_Click(object sender, EventArgs e)
        {
            if (dgvClients.SelectedRows.Count > 0)
            {
                string ip = dgvClients.SelectedRows[0].Cells[1].Value.ToString();
                var target = connectedClients.Find(c => c.Ip == ip);

                if (target != null)
                {
                    // ≈€·«ﬁ «·« ’«· „‰ ÿ—› «·”Ì—›—
                    target.TcpClient.Close();
                    UpdateClientStatus(target, "Kicked", Color.Orange);
                    Log($"Admin kicked {target.Name}");
                }
            }
        }

        // “— «·ÕŸ— (Ban)
        private void toolStripMenuItemBan_Click(object sender, EventArgs e)
        {
            if (dgvClients.SelectedRows.Count > 0)
            {
                string ip = dgvClients.SelectedRows[0].Cells[1].Value.ToString();

                if (!bannedIps.Contains(ip))
                {
                    bannedIps.Add(ip);
                    MessageBox.Show($"IP {ip} has been BANNED.");
                }

                // ÿ—œÂ ›Ê—« ≈–« ﬂ«‰ „ ’·«
                var target = connectedClients.Find(c => c.Ip == ip);
                if (target != null)
                {
                    target.TcpClient.Close();
                    UpdateClientStatus(target, "Banned", Color.Red);
                }
            }
        }

        // “— ›ﬂ «·ÕŸ— (Unban)
        private void toolStripMenuItemUnban_Click(object sender, EventArgs e)
        {
            if (dgvClients.SelectedRows.Count > 0)
            {
                string ip = dgvClients.SelectedRows[0].Cells[1].Value.ToString();

                if (bannedIps.Contains(ip))
                {
                    bannedIps.Remove(ip);
                    MessageBox.Show($"IP {ip} has been UNBANNED. They can connect now.");
                    //  €ÌÌ— ·Ê‰Â ›Ì «·ÃœÊ· ··≈‘«—… √‰Â „”„ÊÕ ·Â »«·⁄Êœ…
                    dgvClients.SelectedRows[0].DefaultCellStyle.BackColor = Color.White;
                    dgvClients.SelectedRows[0].Cells[4].Value = "Offline (Allowed)";
                }
                else
                {
                    MessageBox.Show("This user is not banned.");
                }
            }
        }

        private void ctxMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // ================================================================
        // œ«·… · ÿ»Ìﬁ «· ’„Ì„ «·«Õ —«›Ì (Dark Cyber Theme)
        // ================================================================
        private void ApplyModernStyle()
        {
            // 1. “Ì«œ… ÕÃ„ «·‰«›–… («·⁄—÷ 1300 »œ·« „‰ 900)
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1300, 750); // <--- Ã⁄·‰« «·⁄—÷ ﬂ»Ì—« Ê„—ÌÕ«

            // 2.  ‰”Ìﬁ «·ÃœÊ·
            dgvClients.BackgroundColor = Color.FromArgb(40, 40, 40);
            dgvClients.BorderStyle = BorderStyle.None;
            dgvClients.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvClients.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvClients.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvClients.EnableHeadersVisualStyles = false;

            dgvClients.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 122, 204);
            dgvClients.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvClients.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvClients.ColumnHeadersHeight = 40;

            dgvClients.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
            dgvClients.DefaultCellStyle.ForeColor = Color.White;
            dgvClients.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 60, 60);
            dgvClients.DefaultCellStyle.SelectionForeColor = Color.WhiteSmoke;
            dgvClients.DefaultCellStyle.Font = new Font("Segoe UI", 9);

            // 3.  ‰”Ìﬁ «··ÊÃ
            lbLog.BackColor = Color.Black;
            lbLog.ForeColor = Color.LimeGreen;
            lbLog.Font = new Font("Consolas", 10);
            lbLog.BorderStyle = BorderStyle.FixedSingle;

            // 4.  ‰”Ìﬁ “— «· ‘€Ì·
            StyleButton(btnStart, Color.SeaGreen);

            if (Controls.ContainsKey("btnClose"))
            {
                Button btnClose = (Button)Controls["btnClose"];
                StyleButton(btnClose, Color.Crimson);
                btnClose.Text = "X";
            }

            // =========================================================
            //  ﬂÊœ  ﬁ”Ì„ «·‘«‘… «·„⁄œ· (·÷„«‰ √‰ «·‹ ListBox ÌŸÂ— ﬂ«„·«)
            // =========================================================
            int margin = 20;
            int topSpace = 80; // „”«Õ… ﬂ«›Ì… ··⁄‰Ê«‰ Ê«·√“—«— ›Ì «·√⁄·Ï

            // Õ”«» «·⁄—÷ «·„ «Õ
            int availableWidth = this.Width - (margin * 3);

            // ”‰⁄ÿÌ 60% ··ÃœÊ· Ê 40% ··ÊÃ (√Ê 50/50 Õ”» —€» ﬂ)
            int tableWidth = (int)(availableWidth * 0.55); // «·ÃœÊ· Ì√Œ– 55%
            int logWidth = availableWidth - tableWidth;    // «··ÊÃ Ì√Œ– «·»«ﬁÌ

            // ÷»ÿ «·ÃœÊ· (Ì”«—)
            dgvClients.Location = new Point(margin, topSpace);
            dgvClients.Size = new Size(tableWidth, this.Height - topSpace - margin);

            // ÷»ÿ «··ÊÃ (Ì„Ì‰)
            lbLog.Location = new Point(margin + tableWidth + margin, topSpace);
            lbLog.Size = new Size(logWidth, this.Height - topSpace - margin);
        }
        // œ«·… „”«⁄œ… · ‰”Ìﬁ «·√“—«—
        private void StyleButton(Button btn, Color color)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
        }

        // ================================================================
        // ﬂÊœ · Õ—Ìﬂ «·‰«›–… (·√‰‰« √·€Ì‰« «·‘—Ìÿ «· ﬁ·ÌœÌ)
        // ================================================================
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

    }


    // ﬂ·«” «·Õ“„ (ÌÃ» √‰ ÌﬂÊ‰ „ÿ«»ﬁ« ··⁄„Ì·)


    // ﬂ·«” „”«⁄œ ·»Ì«‰«  «·ÃœÊ·

}