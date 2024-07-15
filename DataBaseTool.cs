using NLog;
using System.Collections.Generic;
using System;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace FastHorse
{
    internal class DataBaseTool
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        // 读取并解密连接字符串
        string connectionString = DatabaseConfigManager.LoadDecryptedConnectionString();

        public string ExecuteQuery(string sql)
        {
            // 移除脚本最后一行的 "GO"（如果存在）
            sql = RemoveTrailingGo(sql);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    List<string> batches = new List<string>();
                    StringBuilder currentBatch = new StringBuilder();
                    bool inBlockComment = false;

                    using (StringReader reader = new StringReader(sql))
                    {
                        string line;
                        string previousLine = null;

                        while ((line = reader.ReadLine()) != null)
                        {
                            string trimmedLine = line.Trim();

                            if (inBlockComment)
                            {
                                currentBatch.AppendLine(line);
                                if (trimmedLine.EndsWith("*/"))
                                {
                                    inBlockComment = false;
                                }
                            }
                            else if (trimmedLine.StartsWith("/*"))
                            {
                                inBlockComment = true;
                                currentBatch.AppendLine(line);
                            }
                            else if (string.Equals(trimmedLine, "go", StringComparison.OrdinalIgnoreCase))
                            {
                                if (currentBatch.Length > 0)
                                {
                                    batches.Add(currentBatch.ToString().TrimEnd());
                                    currentBatch.Clear();
                                }
                                previousLine = line;
                            }
                            else
                            {
                                currentBatch.AppendLine(line);
                                previousLine = line;
                            }
                        }

                        if (currentBatch.Length > 0)
                        {
                            batches.Add(currentBatch.ToString().TrimEnd());
                        }
                    }

                    foreach (string batch in batches)
                    {
                        if (string.IsNullOrWhiteSpace(batch))
                        {
                            continue;
                        }

                        using (SqlCommand command = new SqlCommand(batch, connection, transaction))
                        {
                            //MessageBox.Show(batch);
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return "成功！";
                }
                catch (SqlException e)
                {
                    try
                    {
                        connection.BeginTransaction().Rollback();
                    }
                    catch
                    {
                        // 处理回滚过程中可能发生的错误
                    }

                    return "【错误】: " + e.Message;
                }
            }
        }

        private string RemoveTrailingGo(string sql)
        {
            // 分割行并检查最后一行是否为 "GO"
            var lines = sql.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();

            // 从后向前查找第一个非空行
            int lastNonEmptyLineIndex = lines.FindLastIndex(line => !string.IsNullOrWhiteSpace(line));

            if (lastNonEmptyLineIndex >= 0 && string.Equals(lines[lastNonEmptyLineIndex].Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                // 移除最后一行
                lines.RemoveAt(lastNonEmptyLineIndex);
            }

            // 重新组合行
            string modifiedSql = string.Join(Environment.NewLine, lines);

            return modifiedSql;
        }


        public string ConnectTest(string connectionString)
        {
            // 创建SqlConnection对象
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    // 打开连接
                    connection.Open();
                    return "连接成功！";
                }
                catch (SqlException ex)
                {
                    // 捕获并显示连接错误信息
                    logger.Error("连接失败：" + ex.Message);
                    return "连接失败：" + ex.Message;
                }
            }
        }
    }
}