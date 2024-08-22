using NLog;
using System.Collections.Generic;
using System;
using System.Data.SqlClient;
using System.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FastHorse
{
    internal class DataBaseTool
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        // 读取并解密连接字符串
        string connectionString = DatabaseConfigManager.LoadDecryptedConnectionString();

        public string ExecuteQuery(string sql)
        {
            var batches = SplitSqlBatches(sql);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlTransaction transaction = null;
                try
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();

                    foreach (var batch in batches)
                    {
                        if (string.IsNullOrWhiteSpace(batch))
                        {
                            continue;
                        }

                        using (SqlCommand command = new SqlCommand(batch, connection, transaction))
                        {
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return "执行成功！";
                }
                catch (SqlException e)
                {
                    try
                    {
                        transaction?.Rollback();
                    }
                    catch (Exception rollbackEx)
                    {
                        logger.Error(rollbackEx, "回滚失败：" + rollbackEx.Message);
                    }
                    return "【错误】: " + e.Message;
                }
            }
        }

        private List<string> SplitSqlBatches(string sql)
        {
            List<string> batches = new List<string>();
            StringBuilder currentBatch = new StringBuilder();
            bool inBlockComment = false;
            bool inString = false;
            char stringDelimiter = '\0';

            using (StringReader reader = new StringReader(sql))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmedLine = line.Trim();

                    // 处理块注释和字符串的开始和结束
                    int index = 0;
                    while (index < trimmedLine.Length)
                    {
                        if (!inString && !inBlockComment && trimmedLine.Substring(index).StartsWith("/*"))
                        {
                            inBlockComment = true;
                            index += 2;
                        }
                        else if (inBlockComment && trimmedLine.Substring(index).StartsWith("*/"))
                        {
                            inBlockComment = false;
                            index += 2;
                        }
                        else if (!inBlockComment && (trimmedLine[index] == '\'' || trimmedLine[index] == '\"'))
                        {
                            if (inString && trimmedLine[index] == stringDelimiter)
                            {
                                // 检查是否为转义字符
                                if (index + 1 < trimmedLine.Length && trimmedLine[index + 1] == stringDelimiter)
                                {
                                    index += 2;
                                    continue;
                                }
                                else
                                {
                                    inString = false;
                                    stringDelimiter = '\0';
                                }
                            }
                            else if (!inString)
                            {
                                inString = true;
                                stringDelimiter = trimmedLine[index];
                            }
                        }
                        index++;
                    }

                    // 检查 "GO" 语句后是否有注释
                    if (!inBlockComment && !inString && trimmedLine.StartsWith("GO", StringComparison.OrdinalIgnoreCase))
                    {
                        string afterGO = trimmedLine.Substring(2).Trim();
                        if (string.IsNullOrEmpty(afterGO) || afterGO.StartsWith("--"))
                        {
                            if (currentBatch.Length > 0)
                            {
                                batches.Add(currentBatch.ToString().TrimEnd());
                                currentBatch.Clear();
                            }
                            continue;  // 继续下一行
                        }
                    }

                    currentBatch.AppendLine(line);
                }

                // 处理最后一个批次，如果最后一行是GO，则忽略
                if (currentBatch.Length > 0)
                {
                    string lastBatch = currentBatch.ToString().TrimEnd();
                    if (!lastBatch.EndsWith("GO", StringComparison.OrdinalIgnoreCase))
                    {
                        batches.Add(lastBatch);
                    }
                }
            }

            return batches;
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