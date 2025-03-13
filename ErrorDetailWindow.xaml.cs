using System.Windows;

namespace SqlScriptRunner
{
    public partial class ErrorDetailWindow : Window
    {
        public ErrorDetailWindow(string errorMessage)
        {
            InitializeComponent();
            
            txtErrorDetail.Text = errorMessage;
            
            // 设置窗口标题包含错误概要
            if (errorMessage.Length > 50)
            {
                Title = $"错误详情: {errorMessage.Substring(0, 47)}...";
            }
            else
            {
                Title = $"错误详情: {errorMessage}";
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 