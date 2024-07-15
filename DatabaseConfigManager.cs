using NLog;
using System;
using System.Data.Common;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace FastHorse
{
    public class DatabaseConfigManager
    {
        private static readonly string EncryptionKey = "specillove4y****"; // 替换为你的加密密钥
        private static readonly string ConfigFilePath = System.IO.Directory.GetCurrentDirectory() + "\\config.dat";
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void SaveEncryptedConnectionString(string connectionString)
        {
            byte[] encryptedData = EncryptStringToBytes(connectionString, EncryptionKey);
            try
            {
                File.WriteAllBytes(ConfigFilePath, encryptedData);
                MessageBox.Show("保存成功！");
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                MessageBox.Show(e.Message);
            }
        }

        private static byte[] EncryptStringToBytes(string plainText, string key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = new byte[16]; // 初始化向量

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        return msEncrypt.ToArray();
                    }
                }
            }
        }
        public static string LoadDecryptedConnectionString()
        {
            if (!File.Exists(ConfigFilePath))
            {
                return "";
            }

            byte[] encryptedData = File.ReadAllBytes(ConfigFilePath);
            return DecryptStringFromBytes(encryptedData, EncryptionKey);
        }

        public static void LoadAndSetConnectionStringFormat(Action<string, string, string, string> setConnectionStringFields)
        {
            string decryptedConnectionString = DatabaseConfigManager.LoadDecryptedConnectionString();
            if (string.IsNullOrEmpty(decryptedConnectionString))
            {
                setConnectionStringFields("", "", "", "");
            }
            else
            {
                DbConnectionStringBuilder builder = new DbConnectionStringBuilder { ConnectionString = decryptedConnectionString };

                string server = builder["Server"] as string;
                string database = builder["Database"] as string;
                string id = builder["User Id"] as string;
                string pw = builder["password"] as string;

                setConnectionStringFields(server, database, id, pw);
            }
        }

        private static string DecryptStringFromBytes(byte[] cipherText, string key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = new byte[16]; // 初始化向量

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

    }
}
