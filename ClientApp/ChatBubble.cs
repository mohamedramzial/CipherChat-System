using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClientApp
{
    public partial class ChatBubble : UserControl
    {
        // الألوان والخطوط
        private Color _bgColor;
        private readonly Color _textColor = Color.White;
        private readonly Color _timeColor = Color.LightGray;
        private readonly Font _messageFont = new Font("Segoe UI", 11); // خط أكبر قليلاً
        private readonly Font _timeFont = new Font("Arial", 8);
        private readonly Font _senderFont = new Font("Arial", 9, FontStyle.Bold);

        private string _message;
        private string _time;
        private string _sender;
        private bool _isMe;
        private Image _image;

        // مربع الصورة الداخلي
        private PictureBox _pbContent;

        // ثوابت الحجم
        private const int ImageSize = 250; // حجم الصورة ثابت وكبير

        public ChatBubble()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.BackColor = Color.Transparent;

            // إلغاء أي تغيير تلقائي للحجم قد يسبب المشاكل
            this.AutoSize = false;

            // إعداد مربع الصورة
            _pbContent = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.StretchImage, // تمدد لملء المربع
                Visible = false,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            // تكبير الصورة عند الضغط
            _pbContent.Click += (s, e) =>
            {
                if (_pbContent.Image != null)
                {
                    Form viewer = new Form();
                    viewer.Size = new Size(800, 600);
                    viewer.StartPosition = FormStartPosition.CenterScreen;
                    viewer.BackgroundImage = _pbContent.Image;
                    viewer.BackgroundImageLayout = ImageLayout.Zoom;
                    viewer.BackColor = Color.Black;
                    viewer.ShowDialog();
                }
            };
            this.Controls.Add(_pbContent);
        }

        public void SetContent(string message, string time, string sender, bool isMe, Image img = null)
        {
            _message = message;
            _time = time;
            _sender = sender;
            _isMe = isMe;
            _image = img;

            // 1. إعداد الألوان (بدون Dock)
            if (_isMe)
            {
                _bgColor = Color.FromArgb(43, 82, 120); // أزرق
                // حذفنا this.Dock = ... لأنه يسبب مشاكل القص
            }
            else
            {
                _bgColor = Color.FromArgb(35, 45, 60); // رمادي
            }

            // 2. حساب الحجم وتطبيقه فوراً
            CalculateAndSetSize();

            this.Invalidate(); // إجبار إعادة الرسم
        }
        private void CalculateAndSetSize()
        {
            if (_image != null)
            {
                // === حالة الصورة ===
                _pbContent.Image = _image;
                _pbContent.Visible = true;
                _pbContent.Location = new Point(12, 12);
                _pbContent.Size = new Size(ImageSize, ImageSize);
                this.Size = new Size(ImageSize + 24, ImageSize + 40); // 40 للوقت والهوامش
            }
            else
            {
                // === حالة النص ===
                _pbContent.Visible = false;

                using (Graphics g = this.CreateGraphics())
                {
                    // عرض النص الأقصى
                    int maxTextWidth = 300;

                    // 1. حساب أبعاد النص الأساسي
                    SizeF msgSize = g.MeasureString(_message, _messageFont, maxTextWidth);

                    // 2. حساب العرض المطلوب
                    int width = (int)msgSize.Width + 35; // 35 هوامش جانبية

                    // 3. حساب الارتفاع (هنا الحل الجذري)
                    // ============================================================
                    // 💡 التعديل هنا: نستخدم شرطاً لتحديد المسافة
                    // ============================================================
                    int height;
                    if (_isMe)
                    {
                        // لرسائلك: نستخدم رقماً صغيراً (مثلاً 35) لتقليل المسافة بين النص والساعة
                        height = (int)msgSize.Height + 35;
                    }
                    else
                    {
                        // للآخرين: نستخدم الرقم الكبير (55) أو (45) لإبقاء المسافة كما هي
                        height = (int)msgSize.Height + 55;
                    }
                    // ============================================================ // 35 للوقت والهوامش العلوية والسفلية

                    // 🛑 إذا كانت الرسالة من شخص آخر، يجب إضافة ارتفاع للاسم
                    if (!_isMe)
                    {
                        SizeF senderSize = g.MeasureString(_sender, _senderFont);

                        // نضيف ارتفاع الاسم + مسافة فاصلة (مثلاً 20 بكسل)
                        int headerHeight = (int)senderSize.Height + 5;
                        height += headerHeight;

                        // نتأكد أن العرض يكفي للاسم أيضاً
                        if (senderSize.Width + 40 > width) width = (int)senderSize.Width + 40;
                    }

                    // تطبيق الحجم مع ضمان حد أدنى
                    this.Size = new Size(Math.Max(width, 140), Math.Max(height, 45));
                }
            }
            this.MinimumSize = this.Size;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. رسم الخلفية
            Rectangle r = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (GraphicsPath path = GetRoundedPath(r, 18))
            using (SolidBrush brush = new SolidBrush(_bgColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // 2. رسم المحتوى النصي
            if (_image == null)
            {
                float currentY = 10; // نبدأ من الأعلى بـ 10 بكسل

                // أ) رسم اسم المرسل (فقط للآخرين)
                if (!_isMe)
                {
                    using (SolidBrush brush = new SolidBrush(Color.Gold))
                    {
                        e.Graphics.DrawString(_sender, _senderFont, brush, 12, currentY);
                    }
                    // 🛑 ننزل للأسفل بمقدار ارتفاع الاسم + مسافة صغيرة
                    currentY += 20;
                }

                // ب) رسم الرسالة
                // نستخدم RectangleF يمتد حتى نهاية الفقاعة لضمان عدم القص
                RectangleF textRect = new RectangleF(12, currentY, this.Width - 24, this.Height - currentY - 20);

                using (SolidBrush brush = new SolidBrush(_textColor))
                {
                    e.Graphics.DrawString(_message, _messageFont, brush, textRect);
                }
            }

            // 3. رسم الوقت
            using (SolidBrush brush = new SolidBrush(_timeColor))
            {
                string t = _time;
                SizeF tSize = e.Graphics.MeasureString(t, _timeFont);
                // نضعه في أقصى الزاوية اليمنى السفلية
                e.Graphics.DrawString(t, _timeFont, brush, this.Width - tSize.Width - 8, this.Height - 18);
            }
        }

        // دالة مساعدة لرسم الزوايا الدائرية
        private GraphicsPath GetRoundedPath(Rectangle rect, int r)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ChatBubble_Load(object sender, EventArgs e)
        {

        }


        public void SetFileContent(string fileName, bool isMe)
        {
            // 1. إعداد الشكل العام (يمين أو يسار)
            SetBubblePosition(isMe); // استخدم دالتك الحالية لتحديد المكان واللون

            // 2. إخفاء الصورة الكبيرة ومربع النص
            // (افترض أن اسم الصورة عندك picImage واسم النص lblMessage)
            if (_pbContent != null) _pbContent.Visible = false;

            // 3. إنشاء تصميم بسيط للملف (أيقونة + اسم)
            PictureBox icon = new PictureBox();
            icon.Image = Properties.Resources.FileIcon; // تأكد من إضافة أيقونة للملفات
            icon.Size = new Size(30, 30);
            icon.SizeMode = PictureBoxSizeMode.Zoom;
            icon.Location = new Point(10, 10);
            icon.BackColor = Color.Transparent;

            Label lblFileName = new Label();
            lblFileName.Text = fileName;
            lblFileName.AutoSize = true;
            lblFileName.ForeColor = isMe ? Color.White : Color.Black;
            lblFileName.Location = new Point(50, 15);
            lblFileName.Font = new Font("Segoe UI", 9, FontStyle.Underline);
            lblFileName.Cursor = Cursors.Hand; // شكل اليد عند المرور

            // 4. إضافة حدث: عند الضغط على الاسم يفتح الملف
            lblFileName.Click += (s, e) => OpenFile(fileName);
            icon.Click += (s, e) => OpenFile(fileName);

            // إضافة الأدوات للفقاعة
            this.Controls.Add(icon);
            this.Controls.Add(lblFileName);

            // تحديد حجم الفقاعة بناء على طول الاسم
            this.Size = new Size(Math.Max(200, lblFileName.Width + 70), 50);
        }

        // دالة لفتح الملف من الجهاز
        private void OpenFile(string fileName)
        {
            try
            {
                // مسار مجلد حفظ الملفات (نفس الذي أنشأناه سابقاً)
                string folderPath = Path.Combine(Application.StartupPath, "Received_Files");
                string fullPath = Path.Combine(folderPath, fileName);

                if (File.Exists(fullPath))
                {
                    // أمر لفتح الملف بالبرنامج الافتراضي (Word, PDF Viewer, etc.)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = fullPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("الملف غير موجود، ربما تم حذفه.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في فتح الملف: " + ex.Message);
            }
        }
        // هذه هي الدالة المفقودة لتحديد لون ومكان الفقاعة
        private void SetBubblePosition(bool isMe)
        {
            if (isMe)
            {
                // 1. تنسيق الرسالة المرسلة (مني)
                // اختر اللون الذي يعجبك (هنا لون سماوي فاتح مثلاً)
                this.BackColor = Color.FromArgb(220, 248, 255);

                // (اختياري) لضبط الهوامش لليمين
                this.Margin = new Padding(50, 5, 5, 5);
            }
            else
            {
                // 2. تنسيق الرسالة المستلمة (من الطرف الآخر)
                this.BackColor = Color.White;

                // (اختياري) لضبط الهوامش لليسار
                this.Margin = new Padding(5, 5, 50, 5);
            }
        }


    }
}