using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Windows.Data;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Document;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Ude;

namespace SqlScriptRunner;

// 字符串长度转换器 - 检查字符串是否超过指定长度
public class StringLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) 
        {
            // 如果值为null，对于Visibility返回Collapsed，对于bool返回false
            return targetType == typeof(Visibility) ? Visibility.Collapsed : false;
        }
        
        string str = value.ToString();
        int maxLength = 50; // 默认最大长度
        
        if (parameter != null)
        {
            int.TryParse(parameter.ToString(), out maxLength);
        }
        
        bool isTooLong = str.Length > maxLength;
        
        // 如果目标类型是Visibility，返回适当的Visibility值
        if (targetType == typeof(Visibility))
        {
            return isTooLong ? Visibility.Visible : Visibility.Collapsed;
        }
        
        // 否则返回布尔值
        return isTooLong;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // 静态构造函数，注册编码提供程序
    static MainWindow()
    {
        // 注册代码页编码提供程序，使程序能够使用GB2312、GBK等编码
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private ObservableCollection<ScriptFile> scriptFiles;
    private Server? sqlServer;
    private string serverName = ".";  // 本地服务器
    private string databaseName = "master";  // 默认数据库
    private bool useWindowsAuth = true;  // 默认使用Windows认证
    private string sqlUsername = "";
    private string sqlPassword = "";

    public MainWindow()
    {
        // 显示授权窗口
        var authWindow = new AuthWindow();
        authWindow.ShowDialog();

        if (!authWindow.IsAuthorized)
        {
            Application.Current.Shutdown();
            return;
        }

        InitializeComponent();
        scriptFiles = new ObservableCollection<ScriptFile>();
        dgScripts.ItemsSource = scriptFiles;
        lvFiles.ItemsSource = scriptFiles;
        
        // 初始化AvalonEdit
        InitializeAvalonEdit();
    }

    private void InitializeAvalonEdit()
    {
        try
        {
            // 设置SQL语法高亮
            txtSqlContent.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(".sql");
            
            // 设置字体和其他属性
            txtSqlContent.FontSize = 13;
            txtSqlContent.FontFamily = new System.Windows.Media.FontFamily("Cascadia Code");
            txtSqlContent.ShowLineNumbers = true;
            txtSqlContent.Options.EnableHyperlinks = false;
            txtSqlContent.Options.EnableEmailHyperlinks = false;
            txtSqlContent.WordWrap = false;
            txtSqlContent.Options.EnableRectangularSelection = true;
            txtSqlContent.Options.EnableTextDragDrop = true;
            txtSqlContent.Options.HighlightCurrentLine = true;
            
            // 设置编辑器背景色和前景色
            txtSqlContent.Background = System.Windows.Media.Brushes.White;
            txtSqlContent.Foreground = System.Windows.Media.Brushes.Black;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"初始化编辑器失败: {ex.Message}");
        }
    }

    private void InitializeSqlConnection()
    {
        try
        {
            // 检查是否提供了用户名和密码
            if (string.IsNullOrEmpty(sqlUsername))
            {
                MessageBox.Show("请先配置数据库连接信息", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // 只使用SQL Server认证
            ServerConnection conn = new ServerConnection(serverName, sqlUsername, sqlPassword);
            
            sqlServer = new Server(conn);
            if (!string.IsNullOrEmpty(databaseName))
            {
                sqlServer.ConnectionContext.DatabaseName = databaseName;
            }
            
            // 移除连接成功提示（因为DatabaseConfigWindow已经有提示）
        }
        catch (Exception ex)
        {
            MessageBox.Show($"连接数据库失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            sqlServer = null;
        }
    }

    private async void btnExecute_Click(object sender, RoutedEventArgs e)
    {
        if (scriptFiles.Count == 0)
        {
            MessageBox.Show("请先加载SQL脚本文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        if (sqlServer == null)
        {
            MessageBox.Show("未连接到数据库，请先配置数据库连接", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        // 禁用所有控件，显示进度层
        EnableControls(false);
        gridProgress.Visibility = Visibility.Visible;
        txtProgressStatus.Text = $"0/{scriptFiles.Count} 已完成";
        
        int completedCount = 0;
        int totalCount = scriptFiles.Count(s => s.Status != "成功"); // 只计算未成功的脚本
        
        foreach (var script in scriptFiles)
        {
            if (script.Status == "成功")
                continue;

            // 更新当前执行的脚本状态
            txtProgressStatus.Text = $"{completedCount}/{totalCount} 已完成 - 正在执行: {script.FileName}";
            
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                
                await Task.Run(() =>
                {
                    string originalSqlScript = ReadFileWithEncoding(script.FilePath);
                    
                    // 在脚本前添加设置
                    string sqlScript = @"
                    SET ANSI_NULLS OFF
                    GO
                    SET QUOTED_IDENTIFIER OFF
                    GO
                    " + originalSqlScript;
                    
                    try
                    {
                        // 为每个脚本创建一个新的连接
                        ServerConnection conn;
                        if (useWindowsAuth)
                        {
                            conn = new ServerConnection(serverName);
                            conn.LoginSecure = true;
                        }
                        else
                        {
                            conn = new ServerConnection(serverName, sqlUsername, sqlPassword);
                        }
                        
                        if (!string.IsNullOrEmpty(databaseName))
                        {
                            conn.DatabaseName = databaseName;
                        }
                        
                        // 设置命令超时时间为5分钟
                        conn.StatementTimeout = 300;
                        
                        // 捕获SQL Server信息和错误消息
                        StringBuilder messageBuilder = new StringBuilder();
                        conn.InfoMessage += (sender, args) => 
                        {
                            messageBuilder.AppendLine(args.Message);
                        };
                        
                        try
                        {
                            conn.ExecuteNonQuery(sqlScript);
                            
                            // 如果有SQL Server返回的消息，添加到输出
                            if (messageBuilder.Length > 0)
                            {
                                Debug.WriteLine($"SQL Server消息: {messageBuilder}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // 提取SQL Server具体错误信息
                            string errorMessage = ex.Message;
                            
                            // ExecutionFailureException通常会包含一个内部异常，其中有具体的SQL错误信息
                            if (ex is ExecutionFailureException sqlEx)
                            {
                                if (sqlEx.InnerException != null)
                                {
                                    // 使用内部异常的消息，这里通常有具体SQL错误
                                    errorMessage = sqlEx.InnerException.Message;
                                    
                                    // SQL Server可能会返回多个错误消息
                                    if (sqlEx.InnerException is SqlException sqlInnerEx)
                                    {
                                        var errors = new StringBuilder();
                                        foreach (SqlError err in sqlInnerEx.Errors)
                                        {
                                            errors.AppendLine($"错误 {err.Number}: {err.Message}, 行 {err.LineNumber}");
                                        }
                                        if (errors.Length > 0)
                                        {
                                            errorMessage = errors.ToString();
                                        }
                                    }
                                }
                            }
                            
                            // 如果有SQL Server返回的信息消息也添加上
                            if (messageBuilder.Length > 0)
                            {
                                errorMessage += Environment.NewLine + messageBuilder.ToString().Trim();
                            }
                            
                            // 抛出包含提取出的具体错误信息的新异常
                            throw new Exception(errorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 确保错误消息直接传递出去
                        throw ex;
                    }
                });
                
                sw.Stop();
                script.Status = "成功";
                script.ExecutionTime = $"{sw.ElapsedMilliseconds}ms";
                script.ErrorMessage = "";
                script.IsSuccess = true;
                script.IsFailed = false;
            }
            catch (Exception ex)
            {
                // 记录完整的异常链用于调试
                Debug.WriteLine($"异常: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"内部异常: {ex.InnerException.Message}");
                }
                
                script.Status = "失败";
                // 直接使用异常信息，不添加任何前缀
                script.ErrorMessage = ex.Message;
                script.IsSuccess = false;
                script.IsFailed = true;
            }
            
            completedCount++;
            txtProgressStatus.Text = $"{completedCount}/{totalCount} 已完成";
        }
        
        // 恢复控件状态，隐藏进度层
        EnableControls(true);
        gridProgress.Visibility = Visibility.Collapsed;
        
        UpdateStatus();
        ApplyFilters();
    }

    private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            try
            {
                string rootPath = dialog.FolderName;
                scriptFiles.Clear();
                
                // 首先尝试只获取当前文件夹中的SQL文件
                var files = Directory.GetFiles(rootPath, "*.sql");
                
                foreach (var file in files)
                {
                    // 使用简单的文件名显示
                    scriptFiles.Add(new ScriptFile
                    {
                        FileName = Path.GetFileName(file),
                        FilePath = file,
                        Status = "待执行",
                        ExecutionTime = "",
                        ErrorMessage = "",
                        IsSuccess = false,
                        IsFailed = false
                    });
                }
                
                // 更新显示和状态
                if (scriptFiles.Count > 0)
                {
                    lvFiles.SelectedIndex = 0;
                }
                
                UpdateStatus();
                
                MessageBox.Show($"已加载 {scriptFiles.Count} 个SQL文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开文件夹出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void btnDbConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DatabaseConfigWindow(serverName, databaseName, useWindowsAuth, sqlUsername);
        if (dialog.ShowDialog() == true)
        {
            // 如果连接成功，更新连接信息
            serverName = dialog.ServerName;
            databaseName = dialog.DatabaseName;
            useWindowsAuth = dialog.UseWindowsAuth;
            sqlUsername = dialog.SqlUsername;
            sqlPassword = dialog.SqlPassword;
            
            // 连接已经在DatabaseConfigWindow中测试过，这里只需更新当前sqlServer对象
            InitializeSqlConnection();
            
        }
    }

    private void lvFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = lvFiles.SelectedItem as ScriptFile;
        if (selectedItem != null)
        {
            try
            {
                string content = ReadFileWithEncoding(selectedItem.FilePath);
                txtSqlContent.Document = new TextDocument(content);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            txtSqlContent.Document = new TextDocument();
        }
    }

    private void chkShowStatus_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var view = dgScripts.Items;
        if (view != null && view.CanFilter)
        {
            view.Filter = item => 
            {
                if (item is ScriptFile script)
                {
                    if (!chkShowSuccess.IsChecked.Value && script.Status == "成功")
                        return false;
                    
                    if (!chkShowFailed.IsChecked.Value && script.Status == "失败")
                        return false;
                    
                    return true;
                }
                return false;
            };
        }
    }

    private void UpdateStatus()
    {
        int total = scriptFiles.Count;
        int success = scriptFiles.Count(s => s.Status == "成功");
        int failed = scriptFiles.Count(s => s.Status == "失败");
        
        txtStatus.Text = $"总执行数: {total} 成功: {success} 失败: {failed}";
    }

    // 修改文件编码检测方法
    private string ReadFileWithEncoding(string filePath)
    {
        try
        {
            // 首先尝试检测BOM并使用相应编码
            byte[] buffer = new byte[4];
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (fs.Length >= 4)
                {
                    fs.Read(buffer, 0, 4);
                    // 检查UTF-8 BOM
                    if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                        return File.ReadAllText(filePath, Encoding.UTF8);
                    // 检查UTF-16 LE BOM
                    if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                        return File.ReadAllText(filePath, Encoding.Unicode);
                    // 检查UTF-16 BE BOM
                    if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                        return File.ReadAllText(filePath, Encoding.BigEndianUnicode);
                    // 检查UTF-32 LE BOM
                    if (buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
                        return File.ReadAllText(filePath, Encoding.UTF32);
                }
            }

            // 尝试使用Ude库进行编码检测
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var detector = new CharsetDetector();
                    detector.Feed(fs);
                    detector.DataEnd();
                    if (detector.Charset != null)
                    {
                        var encoding = Encoding.GetEncoding(detector.Charset);
                        return File.ReadAllText(filePath, encoding);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ude编码检测失败: {ex.Message}");
            }

            // 尝试常用编码
            Encoding[] encodings = new Encoding[]
            {
                Encoding.UTF8,           // UTF-8 (无BOM)
                Encoding.GetEncoding("GB2312"),  // GB2312
                Encoding.GetEncoding("GBK"),     // GBK
                Encoding.GetEncoding("GB18030"), // GB18030
                Encoding.GetEncoding("Big5"),    // Big5
                Encoding.Default,        // 系统默认编码
                Encoding.ASCII           // ASCII
            };

            foreach (var encoding in encodings)
            {
                try
                {
                    string content = File.ReadAllText(filePath, encoding);
                    // 检查是否包含无效字符
                    if (!content.Contains('\uFFFD'))
                    {
                        Debug.WriteLine($"成功使用编码: {encoding.EncodingName}");
                        return content;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"尝试使用编码 {encoding.EncodingName} 失败: {ex.Message}");
                }
            }

            // 如果所有尝试都失败，使用系统默认编码
            return File.ReadAllText(filePath, Encoding.Default);
        }
        catch (Exception ex)
        {
            throw new Exception($"读取文件 {Path.GetFileName(filePath)} 失败: {ex.Message}", ex);
        }
    }

    // 控制界面控件启用状态的辅助方法
    private void EnableControls(bool enable)
    {
        btnOpenFolder.IsEnabled = enable;
        btnDbConfig.IsEnabled = enable;
        btnExecute.IsEnabled = enable;
        lvFiles.IsEnabled = enable;
        dgScripts.IsEnabled = enable;
        chkShowSuccess.IsEnabled = enable;
        chkShowFailed.IsEnabled = enable;
    }

    // 添加DataGrid选择变更事件处理，在窗口加载时绑定
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        dgScripts.SelectionChanged += DgScripts_SelectionChanged;
    }
    
    // 处理DataGrid选择变更事件
    private void DgScripts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgScripts.SelectedItem is ScriptFile selectedFile)
        {
            // 在左侧列表中找到并选中对应的文件
            for (int i = 0; i < lvFiles.Items.Count; i++)
            {
                if (lvFiles.Items[i] is ScriptFile listFile && 
                    listFile.FilePath == selectedFile.FilePath)
                {
                    lvFiles.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    // 添加错误详情按钮点击事件处理程序
    private void btnShowErrorDetail_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string errorMessage)
        {
            // 创建并显示错误详情对话框
            var detailWindow = new ErrorDetailWindow(errorMessage);
            detailWindow.Owner = this;
            detailWindow.ShowDialog();
        }
    }

    private void btnAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new Window
        {
            Title = "关于",
            Width = 400,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = System.Windows.Media.Brushes.White,
            WindowStyle = WindowStyle.ToolWindow
        };

        var stackPanel = new StackPanel
        {
            Margin = new Thickness(20),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var titleBlock = new TextBlock
        {
            Text = "FastHorse-SQL",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var versionBlock = new TextBlock
        {
            Text = "版本 1.0.0",
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var descriptionBlock = new TextBlock
        {
            Text = "一个简单高效的SQL脚本批量执行工具",
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var copyrightBlock = new TextBlock
        {
            Text = "© 2025 FastHorse. 保留所有权利。",
            HorizontalAlignment = HorizontalAlignment.Center
        };

        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(versionBlock);
        stackPanel.Children.Add(descriptionBlock);
        stackPanel.Children.Add(copyrightBlock);

        aboutWindow.Content = stackPanel;
        aboutWindow.ShowDialog();
    }
}

public class ScriptFile : INotifyPropertyChanged
{
    private string _fileName = string.Empty;
    private string _status = string.Empty;
    private string _executionTime = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isSuccess;
    private bool _isFailed;

    public string FileName 
    { 
        get => _fileName; 
        set { _fileName = value; OnPropertyChanged(); } 
    }
    
    public string FilePath { get; set; } = string.Empty;
    
    public string Status 
    { 
        get => _status; 
        set { _status = value; OnPropertyChanged(); } 
    }
    
    public string ExecutionTime 
    { 
        get => _executionTime; 
        set { _executionTime = value; OnPropertyChanged(); } 
    }
    
    public string ErrorMessage 
    { 
        get => _errorMessage; 
        set { _errorMessage = value; OnPropertyChanged(); } 
    }
    
    public bool IsSuccess 
    { 
        get => _isSuccess; 
        set { _isSuccess = value; OnPropertyChanged(); } 
    }
    
    public bool IsFailed 
    { 
        get => _isFailed; 
        set { _isFailed = value; OnPropertyChanged(); } 
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}