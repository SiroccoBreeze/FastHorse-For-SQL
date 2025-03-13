using System;
using System.Windows;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SqlScriptRunner
{
    public partial class DatabaseConfigWindow : Window
    {
        public string ServerName { get; private set; }
        public string DatabaseName { get; private set; }
        public bool UseWindowsAuth { get { return false; } } // 始终返回false，表示不使用Windows认证
        public string SqlUsername { get; private set; }
        public string SqlPassword { get; private set; }

        public DatabaseConfigWindow(string serverName, string databaseName, bool useWindowsAuth, string sqlUsername)
        {
            InitializeComponent();
            
            txtServerName.Text = serverName;
            txtDatabaseName.Text = databaseName;
            txtUsername.Text = sqlUsername;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            ServerName = txtServerName.Text.Trim();
            DatabaseName = txtDatabaseName.Text.Trim();
            SqlUsername = txtUsername.Text.Trim();
            SqlPassword = txtPassword.Password;
            
            if (string.IsNullOrEmpty(ServerName))
            {
                MessageBox.Show("请输入服务器名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(SqlUsername))
            {
                MessageBox.Show("请输入用户名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // 测试连接
            try
            {
                // 使用SQL Server认证连接
                ServerConnection conn = new ServerConnection(ServerName, SqlUsername, SqlPassword);
                
                Server server = new Server(conn);
                server.ConnectionContext.ConnectTimeout = 5; // 设置连接超时时间为5秒
                
                // 尝试连接
                server.ConnectionContext.Connect();
                
                // 测试数据库连接
                if (!string.IsNullOrEmpty(DatabaseName))
                {
                    bool databaseExists = false;
                    foreach (Database db in server.Databases)
                    {
                        if (db.Name.Equals(DatabaseName, StringComparison.OrdinalIgnoreCase))
                        {
                            databaseExists = true;
                            break;
                        }
                    }
                    
                    if (!databaseExists)
                    {
                        MessageBox.Show($"数据库 '{DatabaseName}' 不存在", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                
                MessageBox.Show("连接成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
} 