using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ude;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FastHorse
{
    public partial class MainForm : Form
    {
        private DataTable dt;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly string ConfigFilePath = System.IO.Directory.GetCurrentDirectory() + "\\config.dat";
        public MainForm()
        {

            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeComponent();
            this.Load += Form1_Load;
            this.SizeChanged += Form1_SizeChanged;
        }
        //窗体的默认宽和高
        int normalWidth = 0;
        int normalHeight = 0;
        //需要记录的控件的位置以及宽和高（X，Y，Widht,Height）
        Dictionary<string, Rect> normalControl = new Dictionary<string, Rect>();

        private void Form1_Load(object sender, EventArgs e)
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
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
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

        private async void button1_Click(object sender, EventArgs e)
        {
            // 检查 DataTable 是否为 null 或者没有数据
            if (dt == null || dt.Rows.Count == 0)
            {
                logger.Error("没有可执行的 SQL 文件。");
                MessageBox.Show("没有可执行的 SQL 文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!File.Exists(ConfigFilePath))
            {
                logger.Error("数据库信息未配置！");
                MessageBox.Show("数据库信息未配置！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string serverName = "";
            string databaseName = "";
            string userId = "";
            string password = "";
            // 读取并解密连接字符串
            DatabaseConfigManager.LoadAndSetConnectionStringFormat((server, database, id, pw) =>
            {
                serverName = server;
                databaseName = database;
                userId = id;
                password = pw;
            });

            // 显示一个带有“是”和“否”按钮的消息框
            DialogResult result = MessageBox.Show($"服务器名称：{serverName}\n数据库:{databaseName}\n确认数据库的配置", "确认操作", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // 初始化进度条
                progressBar1.Minimum = 0;
                progressBar1.Maximum = dt.Rows.Count;
                progressBar1.Value = 0;
                progressBar1.Visible = true;

                // 初始化标签
                labelProgress.Text = $"总共需要执行 {dt.Rows.Count} 个文件，正在执行第 0 个文件。";

                int failedCount = 0; // 记录执行失败的脚本数量

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];
                    string sqlText = "SET QUOTED_IDENTIFIER OFF; " + SqlText(row[0].ToString());

                    // 使用异步操作执行查询
                    await Task.Run(() =>
                    {
                        var dataBaseTool = new DataBaseTool();
                        string executeQuery = dataBaseTool.ExecuteQuery(sqlText);
                        if (executeQuery.Contains("成功"))
                        {
                            logger.Info($"文件：【{row[0].ToString()}】------->【成功】");
                        }
                        else
                        {
                            logger.Error($"文件：【{row[0].ToString()}】------->{executeQuery}");
                            // 复制执行失败的 SQL 文件到 Failed 文件夹
                            CopyFailedSqlFile(row[0].ToString());
                            failedCount++; // 增加失败计数
                        }
                    });

                    // 更新进度条
                    progressBar1.Value++;
                    // 更新标签
                    labelProgress.Invoke((MethodInvoker)delegate
                    {
                        labelProgress.Text = $"总共需要执行 {dt.Rows.Count} 个文件，正在执行第 {i + 1} 个文件：\n{row[0].ToString()}";
                        labelProgress.Refresh(); // 强制刷新
                    });
                }

                // 显示完成的提示框，包含执行失败的脚本数量
                MessageBox.Show($"全部执行完成！\n执行失败的脚本数量：{failedCount}", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // 初始化窗口
                ResetWindow();
            }
        }

        private void TraverseDirectory(string path)
        {
            try
            {
                // 获取当前目录下的所有文件
                foreach (string file in Directory.GetFiles(path))
                {
                    string fileName = Path.GetFileName(file);
                    string fileExtension = Path.GetExtension(file);

                    // 只处理扩展名为 .sql 的文件
                    if (fileExtension.Equals(".sql", StringComparison.OrdinalIgnoreCase))
                    {
                        dt.Rows.Add(file);
                    }
                }

                // 获取当前目录下的所有子目录
                foreach (string directory in Directory.GetDirectories(path))
                {
                    TraverseDirectory(directory); // 递归遍历子目录
                }
            }
            catch (UnauthorizedAccessException uae)
            {
                MessageBox.Show($"Access Error: {uae.Message}");
            }
            catch (DirectoryNotFoundException dnfe)
            {
                MessageBox.Show($"Directory Not Found: {dnfe.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"General Error: {ex.Message}");
            }
        }

        private void OpenFolder_Click(object sender, EventArgs e)
        {

            using (CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog())
            {
                commonOpenFileDialog.Title = "选择文件夹";
                commonOpenFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                commonOpenFileDialog.IsFolderPicker = true;

                // 添加这一行以在对话框关闭后还原目录
                commonOpenFileDialog.RestoreDirectory = true;

                CommonFileDialogResult result = commonOpenFileDialog.ShowDialog();

                if (result == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(commonOpenFileDialog.FileName))
                {
                    string selectedFolderPath = commonOpenFileDialog.FileName;
                    ScriptFolder.Text = "打开的文件夹目录：" + selectedFolderPath;
                    dt = new DataTable();
                    dt.Columns.Add("文件", typeof(string));
                    TraverseDirectory(selectedFolderPath);
                }
            }
        }

        private void DataBaseTool_Click(object sender, EventArgs e)
        {
            var form2 = new DatabaseSettingForm();
            form2.ShowDialog();
        }

        public string SqlText(string filePath)
        {
            try
            {
                // 检测文件编码
                Encoding fileEncoding = DetectFileEncoding(filePath);

                // 使用检测到的编码读取文件内容
                using (StreamReader streamReader = new StreamReader(filePath, fileEncoding))
                {
                    string fileContent = streamReader.ReadToEnd();
                    return fileContent;
                }
            }
            catch (Exception ex)
            {
                // 处理异常情况
                logger.Error("读取文件时发生错误: " + ex.Message);
                return "读取文件时发生错误: " + ex.Message;
            }
        }

        private Encoding DetectFileEncoding(string filePath)
        {
            CharsetDetector charsetDetector = new CharsetDetector();

            using (FileStream fileStream = File.OpenRead(filePath))
            {
                charsetDetector.Feed(fileStream);
                charsetDetector.DataEnd();
            }

            if (charsetDetector.Charset != null)
            {
                return Encoding.GetEncoding(charsetDetector.Charset);
            }

            // 如果未能检测到编码，默认使用 UTF-8 编码
            return Encoding.UTF8;
        }

        private void CopyFailedSqlFile(string filePath)
        {
            string failedFolderPath = Path.Combine(Application.StartupPath, "Failed");
            if (!Directory.Exists(failedFolderPath))
            {
                Directory.CreateDirectory(failedFolderPath);
            }

            string fileName = Path.GetFileName(filePath);
            string destinationPath = Path.Combine(failedFolderPath, fileName);
            File.Copy(filePath, destinationPath, true);
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void labelProgress_Click(object sender, EventArgs e)
        {

        }

        private void ScriptFolder_Click(object sender, EventArgs e)
        {

        }

        private void ResetWindow()
        {
            // 重置进度条
            progressBar1.Value = 0;
            progressBar1.Visible = false;

            // 重置标签
            labelProgress.Text = "";
            ScriptFolder.Text = "";

            // 清空 DataTable
            if (dt != null)
            {
                dt.Clear();
            }

            // 其他初始化操作...
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            var about = new About();
            about.ShowDialog();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
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
