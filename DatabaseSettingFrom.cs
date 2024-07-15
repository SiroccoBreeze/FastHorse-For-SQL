using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FastHorse
{
    public partial class DatabaseSettingForm : Form
    {
        public DatabaseSettingForm()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeComponent();
            this.Load += Form2_Load;
            this.SizeChanged += Form2_SizeChanged;
        }

        //窗体的默认宽和高
        int normalWidth = 0;
        int normalHeight = 0;
        //需要记录的控件的位置以及宽和高（X，Y，Widht,Height）
        Dictionary<string, Rect> normalControl = new Dictionary<string, Rect>();

        private void Form2_Load(object sender, EventArgs e)
        {
            // 记录相关对象以及原始尺寸
            normalWidth = this.panel1.Width;
            normalHeight = this.panel1.Height;

            // 通过父控件 Panel 进行控件的遍历
            foreach (Control item in this.panel1.Controls)
            {
                if (!normalControl.ContainsKey(item.Name))
                {
                    normalControl.Add(item.Name, new Rect(item.Left, item.Top, item.Width, item.Height));
                }
            }
            // 读取并解密连接字符串
            DatabaseConfigManager.LoadAndSetConnectionStringFormat((server, database, id, pw) =>
            {
                serverName.Text = server;
                databaseName.Text = database;
                userId.Text = id;
                password.Text = pw;
            });
        }

        private void Form2_SizeChanged(object sender, EventArgs e)
        {
            //根据原始比例进行新尺寸的计算
            int w = this.panel1.Width;
            int h = this.panel1.Height;

            foreach (Control item in this.panel1.Controls)
            {
                int newX = (int)(w * 1.00 / normalWidth * normalControl[item.Name].X);
                int newY = (int)(h * 1.00 / normalHeight * normalControl[item.Name].Y);
                int newW = (int)(w * 1.00 / normalWidth * normalControl[item.Name].Widht);
                int newH = (int)(h * 1.00 / normalHeight * normalControl[item.Name].Height);
                item.Left = newX;
                item.Top = newY;
                item.Width = newW;
                item.Height = newH;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string connectionString = $"Server = {serverName.Text}; Database = {databaseName.Text}; User Id = {userId.Text}; Password = {password.Text}; Connect Timeout = 3;";

            var dataBaseTool = new DataBaseTool();
            var connectTest = dataBaseTool.ConnectTest(connectionString);
            MessageBox.Show(connectTest);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // 校验输入是否为空
            if (string.IsNullOrEmpty(serverName.Text) ||
                string.IsNullOrEmpty(databaseName.Text) ||
                string.IsNullOrEmpty(userId.Text) ||
                string.IsNullOrEmpty(password.Text))
            {
                MessageBox.Show("所有字段都必须填写。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 校验服务器名称是否符合要求（例如，只允许字母、数字和一些特殊字符）
            if (!Regex.IsMatch(serverName.Text, @"^[a-zA-Z0-9-_.,]+$"))
            {
                MessageBox.Show("服务器名称只能包含字母、数字、破折号、下划线和点。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 校验数据库名称是否符合要求（例如，只允许字母、数字和一些特殊字符）
            if (!Regex.IsMatch(databaseName.Text, @"^[a-zA-Z0-9-_.]+$"))
            {
                MessageBox.Show("数据库名称只能包含字母、数字、破折号、下划线和点。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 校验用户ID是否符合要求（例如，只允许字母和数字）
            if (!Regex.IsMatch(userId.Text, @"^[a-zA-Z0-9]+$"))
            {
                MessageBox.Show("用户ID只能包含字母和数字。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string connectionString = $"Server = {serverName.Text}; Database = {databaseName.Text}; User Id = {userId.Text}; Password = {password.Text}; Connect Timeout = 1;";
            // 加密并存储连接字符串
            DatabaseConfigManager.SaveEncryptedConnectionString(connectionString);
        }

        private void password_TextChanged(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
        class Rect
        {
            public Rect(int x, int y, int w, int h)
            {
                this.X = x; this.Y = y; this.Widht = w; this.Height = h;
            }
            public int X { get; set; }
            public int Y { get; set; }
            public int Widht { get; set; }
            public int Height { get; set; }
        }
    }
}
