using System;
using System.Windows;

namespace SqlScriptRunner
{
    public partial class AuthWindow : Window
    {
        public bool IsAuthorized { get; private set; }

        public AuthWindow()
        {
            InitializeComponent();
            IsAuthorized = false;
            txtAuthCode.Focus();
            
            // 注册回车键事件
            txtAuthCode.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    btnAuth_Click(s, e);
                }
            };
        }

        private void btnAuth_Click(object sender, RoutedEventArgs e)
        {
            string inputCode = txtAuthCode.Password;
            string todayCode = GenerateTodayCode();

            if (string.IsNullOrEmpty(inputCode))
            {
                MessageBox.Show("请输入授权码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (inputCode == todayCode)
            {
                IsAuthorized = true;
                Close();
            }
            else
            {
                MessageBox.Show("授权码错误，请重试", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                txtAuthCode.Password = "";
                txtAuthCode.Focus();
            }
        }

        private string GenerateTodayCode()
        {
            // 获取当前日期
            DateTime today = DateTime.Now.Date;
            
            // 使用日期生成种子
            int seed = today.Year * 10001 + today.Month * 100 + today.Day;
            seed = (seed * 31) % 1000000; // 确保是6位数
            
            // 使用固定密钥进行混淆（可以根据需要修改）
            const int secretKey = 6835;
            seed = (seed ^ secretKey) % 1000000;
            
            // 确保生成6位数字
            return seed.ToString("D6");
        }
    }
} 