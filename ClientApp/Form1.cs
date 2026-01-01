using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace ClientApp
{
    public partial class Form1 : Form
    {
        TcpClient client;
        NetworkStream stream;
        string userName = "";
        Dictionary<string, Image> userAvatars = new Dictionary<string, Image>();

        // استدعاء دالة من نظام ويندوز لإخفاء الأشرطة
        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        // دالة نستخدمها لإخفاء الشريط
        private void HideScrollBar()
        {
            // الرقم 1 يعني الشريط العمودي (Vertical)
            // false تعني "إخفاء"
            ShowScrollBar(pnlChat.Handle, 1, false);
        }

        // عداد لحساب ترتيب دخول المستخدمين
        int usersCounter = 0;

        public Form1()
        {
            InitializeComponent();
            userName = "Designer"; // قيمة افتراضية
            ApplyModernStyle();
        }
        // التعديل 1: الكونستركتور يستقبل الاسم
        public Form1(string nameFromLogin)
        {
            InitializeComponent();
            userName = nameFromLogin; // حفظ الاسم القادم من الـ Login
            ApplyModernStyle(); // <--- تشغيل التصميم الجديد
            this.MouseDown += Form1_MouseDown; // تفعيل التحريك

        }


        // التعديل 2: الاتصال يتم تلقائياً عند تحميل الفورم
        private async void Form1_Load(object sender, EventArgs e)
        {
            // هذا السطر هو الحل: إذا كان المصمم هو من يشغل الفورم، توقف هنا
            if (userName == "Designer" || DesignMode)
            {
                return;
            }
            HideScrollBar();

            // الكود الطبيعي للبرنامج
            this.Text = $"Secure Chat - {userName}";
            await ConnectToServer();
        }

        // دالة الاتصال (فصلناها لتكون مرتبة)
        // دالة الاتصال (محدثة لتتحكم بالأزرار)
        private async Task ConnectToServer()
        {
            try
            {
                // إذا كان متصلاً بالفعل، لا تفعل شيئاً
                if (client != null && client.Connected) return;

                client = new TcpClient();
                await client.ConnectAsync("192.168.8.110", 5000);
                stream = client.GetStream();

                var endPoint = (System.Net.IPEndPoint)client.Client.LocalEndPoint;
                lblConnectionInfo.Text = $"IP: {endPoint.Address} | Port: {endPoint.Port}";

                // --- تحديث حالة الأزرار ---
                btnConnect.Enabled = false;      // نعطل زر الاتصال لأننا اتصلنا خلاص
                btnDisconnect.Enabled = true;    // نفعل زر قطع الاتصال
                                                 // --------------------------

                AddMessageBubble($"Welcome back {userName}! Connected.", "System", false);

                _ = ReceiveMessagesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل الاتصال: " + ex.Message);

                // في حال الفشل، نتيح زر الاتصال ليحاول المستخدم مرة أخرى
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
            }
        }

        // دالة قطع الاتصال (محدثة)
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (stream != null) stream.Close();
                if (client != null) client.Close();

                lblConnectionInfo.Text = "Not Connected";

                // --- تحديث حالة الأزرار ---
                btnConnect.Enabled = true;       // نفعل زر الاتصال لكي يستطيع العودة
                btnDisconnect.Enabled = false;   // نعطل زر القطع
                                                 // --------------------------

                AddMessageBubble("You disconnected.", "System", false);
            }
            catch { }
        }

        // --- باقي الكود كما هو تماماً ---
        private async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[1024 * 1024 * 10]; // 10 MB Buffer
            try
            {
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    // 1. فك التشفير وتحليل البيانات
                    string encryptedJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string json = CryptoHelper.Decrypt(encryptedJson);
                    var packet = JsonConvert.DeserializeObject<MessagePacket>(json);

                    // 2. تجاهل الرسائل التي أرسلتها أنا
                    if (packet.SenderName == userName) continue;

                    // 3. معالجة الرسالة حسب نوعها
                    if (packet.Type == "Text") // === رسالة نصية ===
                    {
                        AddMessageBubble(packet.Content, packet.SenderName, false);
                    }
                    else if (packet.Type == "File") // === ملف أو صورة ===
                    {
                        // حفظ الملف في مجلد Received_Files باستخدام الدالة المساعدة
                        // (إذا لم تكن أضفت الدالة المساعدة، هذا الكود سيقوم بالمهمة)
                        SaveReceivedFile(packet.Content, packet.FileData);

                        // هل الملف صورة؟
                        if (IsImageFile(packet.Content))
                        {
                            Image img = BytesToImage(packet.FileData);
                            if (img != null)
                                AddMessageBubble("", packet.SenderName, false, img);
                            else
                                AddMessageBubble($"📁 Shared File: {packet.Content}", packet.SenderName, false);
                        }
                        else
                        {
                            // ========================================================
                            // 🛑 التغيير الجديد هنا: عرض الملف كأيقونة قابلة للنقر
                            // ========================================================
                            this.Invoke((MethodInvoker)delegate
                            {
                                ChatBubble bubble = new ChatBubble();
                                // false يعني أن الرسالة مستلمة (ليست مني)
                                bubble.SetFileContent(packet.Content, false);

                                pnlChat.Controls.Add(bubble);
                                pnlChat.ScrollControlIntoView(bubble);
                            });
                            // ========================================================
                        }
                    }
                }
            }
            catch
            {
                HandleDisconnection("Disconnected from server.");
            }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (client == null || !client.Connected) return;
            if (string.IsNullOrWhiteSpace(txtMessage.Text)) return;

            AddMessageBubble(txtMessage.Text, "Me", true);

            var packet = new MessagePacket
            {
                SenderName = userName,
                Type = "Text",
                Content = txtMessage.Text
            };

            await SendPacketAsync(packet);
            txtMessage.Clear();
        }

        private async void btnSendFile_Click(object sender, EventArgs e)
        {
            if (client == null || !client.Connected) return;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All Files|*.*|Images|*.jpg;*.jpeg;*.png;*.bmp|Documents|*.txt;*.docx;*.pdf;*.xlsx";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(ofd.FileName);
                    string fileName = Path.GetFileName(ofd.FileName);

                    // هل الملف صورة؟
                    if (IsImageFile(fileName))
                    {
                        // محاولة تحويل البايتات إلى صورة لعرضها عندي
                        Image img = BytesToImage(fileBytes);

                        if (img != null)
                        {
                            // ========================================================
                            // 🛑 الحل هنا: يجب أن يكون النص فارغاً "" عند إرسال صورة
                            // ========================================================
                            // لاحظ الـ "" في المعامل الأول
                            AddMessageBubble("", "Me", true, img);
                            // ========================================================
                        }
                        else
                        {
                            // في حال فشل تحميل الصورة محلياً، نعرضها كملف عادي
                            AddMessageBubble($"📁 Sending File: {fileName}", "Me", true);
                        }
                    }
                    else
                    {
                        // ملف عادي وليس صورة
                        AddMessageBubble($"📁 Sending File: {fileName}", "Me", true);
                    }

                    // تجهيز وإرسال الباكت للسيرفر (هذا الجزء سليم لأن الآخرين يستقبلونها)
                    var packet = new MessagePacket
                    {
                        SenderName = userName,
                        Type = "File",
                        Content = fileName,
                        FileData = fileBytes
                    };

                    await SendPacketAsync(packet);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading/sending file: " + ex.Message);
                }
            }
        }



        // الدوال المساعدة (نفسها تماماً)
        private Image BytesToImage(byte[] bytes)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    // هذه الطريقة تنشئ نسخة جديدة تماماً من الصورة في الذاكرة
                    // وتمنع مشاكل اختفاء الصورة
                    Image loadedImage = Image.FromStream(ms);
                    return new Bitmap(loadedImage);
                }
            }
            catch { return null; }
        }

        private bool IsImageFile(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            return (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp");
        }

        private async Task SendPacketAsync(MessagePacket packet)
        {
            try
            {
                // 1. فحص مبدئي
                if (client == null || !client.Connected || stream == null)
                {
                    HandleDisconnection("You are not connected!");
                    return;
                }

                string json = JsonConvert.SerializeObject(packet);
                string encryptedJson = CryptoHelper.Encrypt(json);
                byte[] data = Encoding.UTF8.GetBytes(encryptedJson);

                // 2. محاولة الإرسال (هنا يحدث الخطأ عادة)
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (System.IO.IOException)
            {
                // 3. صيد الخطأ المحدد (Server Kicked You)
                HandleDisconnection("Connection lost. The server may have disconnected you.");
            }
            catch (ObjectDisposedException)
            {
                // 4. صيد خطأ إذا كانت الذاكرة مغلقة
                HandleDisconnection("Connection closed.");
            }
            catch (Exception ex)
            {
                // 5. أي خطأ آخر
                MessageBox.Show("Error sending data: " + ex.Message);
            }
        }

        private void AddMessageBubble(string message, string sender, bool isMe, Image img = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, string, bool, Image>(AddMessageBubble), message, sender, isMe, img);
                return;
            }

            // 1. الحاوية الرئيسية للصف (FlowLayoutPanel)
            FlowLayoutPanel rowPanel = new FlowLayoutPanel();
            rowPanel.AutoSize = true;
            rowPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            rowPanel.BackColor = Color.Transparent;
            rowPanel.Padding = new Padding(0, 5, 0, 5);
            rowPanel.WrapContents = false;

            // 2. تجهيز صورة المستخدم (Avatar) - مربعةs
            PictureBox pbAvatar = new PictureBox();
            // ... (الكود السابق لتعريف pbAvatar)
            pbAvatar.Size = new Size(45, 45);
            pbAvatar.SizeMode = PictureBoxSizeMode.StretchImage;

            // =========================================================
            // 💡 التعديل هنا: استخدام الدالة الجديدة لاختيار الصورة
            // =========================================================
            pbAvatar.Image = GetUserAvatar(sender);
            // =========================================================

            pbAvatar.Margin = new Padding(5, 0, 5, 0);
            // ... (باقي الكود كما هو)5, 0);

            // 3. تجهيز الفقاعة
            ChatBubble bubble = new ChatBubble();
            string time = DateTime.Now.ToString("HH:mm");
            bubble.SetContent(message, time, (isMe ? sender : ""), isMe, img);

            // 4. الترتيب (يمين أو يسار)
            if (isMe)
            {
                rowPanel.FlowDirection = FlowDirection.RightToLeft;
                // هنا نجعل المحاذاة لليمين داخل الصف نفسه
                rowPanel.Controls.Add(pbAvatar);
                rowPanel.Controls.Add(bubble);
            }
            else
            {
                rowPanel.FlowDirection = FlowDirection.LeftToRight;

                // حاوية عمودية للاسم والرسالة
                FlowLayoutPanel messageContainer = new FlowLayoutPanel();
                messageContainer.FlowDirection = FlowDirection.TopDown;
                messageContainer.AutoSize = true;
                messageContainer.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                messageContainer.BackColor = Color.Transparent;
                messageContainer.WrapContents = false;
                messageContainer.Margin = new Padding(0);

                Label lblName = new Label();
                lblName.Text = sender;
                lblName.ForeColor = Color.LightGray; // لون فاتح ليظهر على الخلفية الداكنة
                lblName.Font = new Font("Segoe UI", 8, FontStyle.Regular);
                lblName.AutoSize = true;
                lblName.Margin = new Padding(5, 0, 0, 2);

                messageContainer.Controls.Add(lblName);
                messageContainer.Controls.Add(bubble);

                rowPanel.Controls.Add(pbAvatar);
                rowPanel.Controls.Add(messageContainer);
            }

            // ================================================================
            // 💡 الإصلاح الحقيقي هنا: تحديد المكان والإضافة للوحة الصحيحة
            // ================================================================

            // 5. استخدام لوحة الشات الأصلية دائماً
            Panel targetPanel = pnlChat;

            // 6. حاوية للصف لضبط العرض وتسهيل الحسابات
            Panel rowWrapper = new Panel();
            rowWrapper.Width = targetPanel.ClientSize.Width - 35; // عرض كامل ناقص هوامش
            rowWrapper.Height = rowPanel.PreferredSize.Height + 10; // ارتفاع حسب المحتوى
            rowWrapper.BackColor = Color.Transparent;

            // وضع محتوى الصف داخل الغلاف
            if (isMe)
            {
                rowPanel.Location = new Point(rowWrapper.Width - rowPanel.PreferredSize.Width, 0); // محاذاة يمين
            }
            else
            {
                rowPanel.Location = new Point(0, 0); // محاذاة يسار
            }
            rowWrapper.Controls.Add(rowPanel);

            // 7. حساب الموقع العمودي (Y) لكي تأتي الرسالة تحت السابقة
            int nextY = 0;
            if (targetPanel.Controls.Count > 0)
            {
                // نأخذ آخر رسالة ونضيف ارتفاعها لموقعها لنحصل على مكان الرسالة الجديدة
                Control lastControl = targetPanel.Controls[targetPanel.Controls.Count - 1];
                nextY = lastControl.Bottom + 10; // 10 بكسل مسافة بين الرسائل
            }

            rowWrapper.Location = new Point(0, nextY); // وضع الرسالة في المكان الصحيح

            // 8. الإضافة والتمرير
            targetPanel.Controls.Add(rowWrapper);

            // تمرير الشات لأسفل
            targetPanel.ScrollControlIntoView(rowWrapper);
            // في آخر سطر داخل دالة AddMessageBubble
            HideScrollBar(); // إجبار الشريط على الاختفاء بعد إضافة الرسالة
        }
        private void lblConnectionInfo_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            // ببساطة نستدعي دالة الاتصال الموجودة لدينا مسبقاً
            await ConnectToServer();
        }
        // دالة موحدة للتعامل مع انقطاع الاتصال وتنظيف الواجهة
        private void HandleDisconnection(string reason)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(HandleDisconnection), reason);
                return;
            }

            if (btnConnect.Enabled == true) return;

            try
            {
                if (stream != null) stream.Close();
                if (client != null) client.Close();
            }
            catch { }

            // تحديث الأزرار
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            lblConnectionInfo.Text = "Not Connected";

            // --- قمنا بحذف السطر التالي لأنه لم يعد موجوداً ---
            // txtName.Enabled = true;  <-- تم الحذف
            // ------------------------------------------------

            AddMessageBubble($"[System]: {reason}", "System", false);
            MessageBox.Show(reason, "Disconnected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void pnlChat_Paint(object sender, PaintEventArgs e)
        {

        }

        // ================================================================
        //  DESIGN: Cyber Dark Theme for Client
        // ================================================================
        private void ApplyModernStyle()
        {
            // 1. إعدادات النافذة
            this.BackColor = Color.FromArgb(32, 33, 36);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(900, 700);

            // =========================================================
            // 💡 إضافة الايموجي وضبط الأماكن
            // =========================================================
            int margin = 15;
            int inputAreaHeight = 60;

            // 1. زر الإرسال (Send) - أقصى اليمين
            btnSend.Size = new Size(80, 40);
            btnSend.Location = new Point(this.Width - btnSend.Width - margin, this.Height - inputAreaHeight);

            // 2. زر إرفاق ملف (Send File) - بجانب زر الإرسال
            btnSendFile.Size = new Size(40, 40);
            btnSendFile.Location = new Point(btnSend.Left - btnSendFile.Width - 10, btnSend.Top);

            // 3. 🆕 إعداد وتصميم أزرار الايموجي (يسار مربع النص)
            CreateEmojiButton("emojiBtn1", Properties.Resources.emoji1, new Point(margin, btnSend.Top));
            CreateEmojiButton("emojiBtn2", Properties.Resources.emoji2, new Point(margin + 45, btnSend.Top));
            CreateEmojiButton("emojiBtn3", Properties.Resources.emoji4, new Point(margin + 90, btnSend.Top));
            CreateEmojiButton("emojiBtn4", Properties.Resources.emoji5, new Point(margin + 135, btnSend.Top));

            // 4. مربع النص (Text Box) - يأخذ المساحة المتبقية
            // لاحظ: حركنا بدايته لليمين (90 بكسل) لترك مساحة للايموجي
            txtMessage.Height = 30;
            txtMessage.Location = new Point(margin + 180, btnSend.Top + 5);
            txtMessage.Width = btnSendFile.Left - (margin + 180) - 10;

            // 5. ضبط لوحة الشات (الحاوية والقناع)
            Panel pnlContainer;
            if (this.Controls.ContainsKey("pnlContainer")) pnlContainer = (Panel)this.Controls["pnlContainer"];
            else
            {
                pnlContainer = new Panel { Name = "pnlContainer" };
                this.Controls.Add(pnlContainer);
            }

            pnlContainer.Location = new Point(margin, 50);
            pnlContainer.Size = new Size(this.Width - (margin * 2), btnSend.Top - 60);
            pnlContainer.BackColor = Color.FromArgb(40, 42, 45);
            pnlContainer.AutoScroll = false;

            pnlChat.Parent = pnlContainer;
            pnlChat.Location = new Point(0, 0);
            int scrollBarWidth = 30;
            pnlChat.Width = pnlContainer.Width + scrollBarWidth;
            pnlChat.Height = pnlContainer.Height;
            pnlChat.AutoScroll = true;
            pnlChat.BackColor = Color.Transparent;
            pnlChat.Padding = new Padding(0, 0, scrollBarWidth + 10, 0);

            // =========================================================
            //  تنسيق الألوان
            // =========================================================
            txtMessage.BackColor = Color.FromArgb(60, 64, 67);
            txtMessage.ForeColor = Color.White;
            txtMessage.BorderStyle = BorderStyle.FixedSingle;
            txtMessage.Font = new Font("Segoe UI", 11);

            StyleButton(btnSend, Color.FromArgb(0, 122, 204));
            btnSend.Text = "Send";
            StyleButton(btnSendFile, Color.FromArgb(40, 167, 69));
            btnSendFile.Text = "📎";

            // أزرار الاتصال وزر الإغلاق (كما هي)
            int topBtnY = 10;
            if (btnConnect != null) { btnConnect.Location = new Point(15, topBtnY); StyleButton(btnConnect, Color.SeaGreen); }
            if (btnDisconnect != null) { btnDisconnect.Location = new Point(110, topBtnY); StyleButton(btnDisconnect, Color.Crimson); }

            if (Controls.ContainsKey("btnClose"))
            {
                Button btnClose = (Button)Controls["btnClose"];
                btnClose.Location = new Point(this.Width - 40, 5);
                btnClose.Size = new Size(35, 35);
                StyleButton(btnClose, Color.Transparent);
                btnClose.ForeColor = Color.White;
                btnClose.Font = new Font("Arial", 14, FontStyle.Bold);
                btnClose.Click += (s, e) => Application.Exit();
                btnClose.BringToFront();
            }
            lblConnectionInfo.Location = new Point(this.Width / 2 - 80, 15);
            lblConnectionInfo.ForeColor = Color.Gray;
        }

        // دالة مساعدة لإنشاء زر الايموجي برمجياً
        private void CreateEmojiButton(string name, Image img, Point loc)
        {
            if (Controls.ContainsKey(name)) return; // عدم التكرار

            PictureBox pb = new PictureBox();
            pb.Name = name;
            pb.Image = img;
            pb.SizeMode = PictureBoxSizeMode.Zoom; // لضبط حجم الصورة
            pb.Size = new Size(40, 40);
            pb.Location = loc;
            pb.Cursor = Cursors.Hand;
            pb.BackColor = Color.Transparent;

            // عند الضغط، أرسل الايموجي
            pb.Click += async (s, e) => await SendEmojiAsImage(img, name);

            this.Controls.Add(pb);
            pb.BringToFront();
        }
        private void StyleButton(Button btn, Color color)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            // جعل الحواف دائرية قليلاً (خدعة بصرية)
            btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 10, 10));
        }

        // دالة لاستيراد رسم الحواف الدائرية (اختياري لجمال الأزرار)
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        // ================================================================
        //  تحريك النافذة (Drag & Drop)
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

        private void pnlChat_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void pnlChat_Paint_2(object sender, PaintEventArgs e)
        {

        }
        // دالة لإرسال الايموجي كصورة
        private async Task SendEmojiAsImage(Image img, string emojiName)
        {
            if (client == null || !client.Connected)
            {
                MessageBox.Show("Please connect first!");
                return;
            }

            try
            {
                // 1. تحويل الصورة إلى بايتات (Byte Array)
                byte[] imgBytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    // نحفظها بصيغة PNG للحفاظ على الشفافية
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    imgBytes = ms.ToArray();
                }

                // 2. عرضها عندي في الشات فوراً
                AddMessageBubble("", "Me", true, img);

                // 3. تجهيز الباكت
                var packet = new MessagePacket
                {
                    SenderName = userName,
                    Type = "File", // نرسلها كملف
                    Content = emojiName + ".png", // اسم وهمي ينتهي بـ png ليفهمه السيرفر والطرف الآخر
                    FileData = imgBytes
                };

                // 4. الإرسال
                await SendPacketAsync(packet);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending emoji: " + ex.Message);
            }
        }
        private Image GetAvatarForUser(string name)
        {
            // هذه دالة بسيطة تختار صورة بناءً على طول الاسم أو حروفه
            // لضمان أن نفس الشخص يحصل دائماً على نفس الصورة

            if (name == userName) // إذا كنت أنا
            {
                return Properties.Resources.user1; // صورتي (توم مثلاً)
            }
            else
            {
                // صور الآخرين (يمكنك التنويع هنا)
                return Properties.Resources.user2; // صورة الطرف الآخر (جيري)
            }
        }

        // دالة لاختيار الافاتار بناءً على الاسم
        private Image GetUserAvatar(string name)
        {
            // 1. هل هذا الاسم مر علينا من قبل وله صورة محفوظة؟
            // إذا نعم، نرجع صورته المحفوظة فوراً
            if (userAvatars.ContainsKey(name))
            {
                return userAvatars[name];
            }

            // 2. إذا كان هذا "اسم جديد" (أول مرة يرسل رسالة):
            usersCounter++; // نزيد عدد المستخدمين واحداً

            Image selectedAvatar;

            // معادلة التدوير الرياضية
            // (العدد - 1) % 3 
            // النتيجة ستكون دائماً: 0 أو 1 أو 2
            int index = (usersCounter - 1) % 3;

            if (index == 0)
            {
                selectedAvatar = Properties.Resources.user1; // المستخدم رقم 1، 4، 7...
            }
            else if (index == 1)
            {
                selectedAvatar = Properties.Resources.user2; // المستخدم رقم 2، 5، 8...
            }
            else
            {
                selectedAvatar = Properties.Resources.user3; // المستخدم رقم 3، 6، 9...
            }

            // 3. نحفظ الصورة لهذا الاسم في الذاكرة للمرات القادمة
            userAvatars.Add(name, selectedAvatar);

            return selectedAvatar;
        }

        private void pnlChat_SizeChanged(object sender, EventArgs e)
        {

            HideScrollBar();

        }
        // دالة مساعدة لحفظ الملفات المستلمة
        private void SaveReceivedFile(string fileName, byte[] data)
        {
            string folderPath = Path.Combine(Application.StartupPath, "Received_Files");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string fullPath = Path.Combine(folderPath, fileName);

            // إذا كان الملف موجوداً، نغير اسمه قليلاً لتجنب الاستبدال
            if (File.Exists(fullPath))
            {
                string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                fileName = $"{nameNoExt}_{DateTime.Now.Ticks}{ext}";
                fullPath = Path.Combine(folderPath, fileName);
            }

            File.WriteAllBytes(fullPath, data);
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // 1. مهم جداً: هذا السطر يمنع صوت "التنبيه" ويمنع النزول لسطر جديد
                e.SuppressKeyPress = true;

                // 2. الضغط برمجياً على زر الإرسال (وكأنك نقرت عليه بالماوس)
                btnSend.PerformClick();
            }
        }
    }
    public class SmoothFlowLayoutPanel : FlowLayoutPanel
    {
        public SmoothFlowLayoutPanel()
        {
            // تفعيل خاصية الرسم المزدوج لمنع الوميض والتقطيع
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;

                // 1. إخفاء شريط التمرير العمودي (كما فعلنا سابقاً)
                cp.Style &= ~0x00200000;

                // 2. تفعيل خاصية "WS_EX_COMPOSITED"
                // هذه الخاصية هي الحل السحري لمشكلة "التمطط" عند السحب
                // فهي تجبر ويندوز على معالجة الرسم بالكامل قبل عرضه
                cp.ExStyle |= 0x02000000;

                return cp;
            }
        }

        // دالة إضافية لمنع مشاكل الرسم عند السحب السريع
        protected override void OnScroll(ScrollEventArgs se)
        {
            this.Invalidate();
            base.OnScroll(se);
        }
    }
    // أداة FlowLayoutPanel معدلة: تخفي الشريط + تمنع التمطط والتقطيع

}