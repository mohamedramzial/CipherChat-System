using System;
using System.Windows.Forms;

namespace ClientApp
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter your name first!");
                return;
            }

            // 1. إخفاء نافذة الدخول
            this.Hide();

            // 2. فتح نافذة الشات وتمرير الاسم لها
            // (سنعدل Form1 بعد قليل ليقبل الاسم)
            Form1 chatForm = new Form1(txtName.Text);
            chatForm.Closed += (s, args) => this.Close(); // لإغلاق البرنامج بالكامل عند إغلاق الشات
            chatForm.Show();
        }
        private void LoginForm_Load(object sender, EventArgs e)
        {
            // هذه الدالة فارغة لإرضاء المصمم
        }
    }
}