using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TSviewACD
{
    static class Config
    {
        private static string filepath = System.IO.Path.ChangeExtension(System.Windows.Forms.Application.ExecutablePath, "xml");

        public static string Version = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion.ToString();
        public static string SendToHost = "localhost";
        public static int SendToPort = 1240;
        public static int SendPacketNum = 256;
        public static int SendDelay = 16;
        public static int SendRatebySendCount = 3;
        public static int SendRatebyTOTCount = 3;
        public static System.Windows.Forms.Keys SendVK = default(System.Windows.Forms.Keys);
        public static string SendVK_Application = "";
        public static string refresh_token = "";
        public static string contentUrl = "";
        public static string metadataUrl = "";
        public static DateTime URL_time;

        private static byte[] _salt = Encoding.ASCII.GetBytes("TSviewACD");
        private const string password = ConfigAPI.token_save_password;

        public static FormLogWindow Log = new FormLogWindow();
        public static bool LogToFile
        {
            get
            {
                return Log?.LogToFile ?? false;
            }
            set
            {
                (Log ?? (Log = new FormLogWindow())).LogToFile = value;
            }
        }

        public static string enc_refresh_token
        {
            get
            {
                return (string.IsNullOrEmpty(refresh_token)) ? "" : Encrypt(refresh_token, password);
            }
            set
            {
                try
                {
                    refresh_token = Decrypt(value, password);
                }
                catch
                {
                    refresh_token = "";
                }
            }
        }

        static string Encrypt(string plaintxt, string password)
        {
            RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.
            try
            {
                // generate the key from the shared secret and the salt
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(password, _salt);

                // Create a RijndaelManaged object
                aesAlg = new RijndaelManaged();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);

                // Create a decryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // prepend the IV
                    msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plaintxt);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
            finally
            {
                // Clear the RijndaelManaged object.
                if (aesAlg != null)
                    aesAlg.Clear();
            }
        }

        static string Decrypt(string crypttxt, string password)
        {
            // Declare the RijndaelManaged object
            // used to decrypt the data.
            RijndaelManaged aesAlg = null;

            try
            {
                // generate the key from the shared secret and the salt
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(password, _salt);

                // Create the streams used for decryption.                
                byte[] bytes = Convert.FromBase64String(crypttxt);
                using (MemoryStream msDecrypt = new MemoryStream(bytes))
                {
                    // Create a RijndaelManaged object
                    // with the specified key and IV.
                    aesAlg = new RijndaelManaged();
                    aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                    // Get the initialization vector from the encrypted stream
                    byte[] rawLength = new byte[sizeof(int)];
                    if (msDecrypt.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
                    {
                        throw new SystemException("Stream did not contain properly formatted byte array");
                    }
                    byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
                    if (msDecrypt.Read(buffer, 0, buffer.Length) != buffer.Length)
                    {
                        throw new SystemException("Did not read byte array properly");
                    }
                    aesAlg.IV = buffer;
                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            return srDecrypt.ReadToEnd();
                    }
                }
            }
            finally
            {
                // Clear the RijndaelManaged object.
                if (aesAlg != null)
                    aesAlg.Clear();
            }
        }

        static Config()
        {
            var serializer = new DataContractSerializer(typeof(Savedata));
            try
            {
                using (var xmlr = XmlReader.Create(filepath))
                {
                    var data = (Savedata)serializer.ReadObject(xmlr);
                    if(data.LogToFile)
                        LogToFile = data.LogToFile;
                    enc_refresh_token = data.refresh_token;
                    if(!string.IsNullOrWhiteSpace(data.SendToHost))
                        SendToHost = data.SendToHost;
                    if(data.SendToPort != default(int))
                        SendToPort = data.SendToPort;
                    if (data.SendPacketNum != default(int))
                        SendPacketNum = data.SendPacketNum;
                    if (data.SendDelay != default(int))
                        SendDelay = data.SendDelay;
                    if (data.SendRatebySendCount != default(int))
                        SendRatebySendCount = data.SendRatebySendCount;
                    if (data.SendRatebyTOTCount != default(int))
                        SendRatebyTOTCount = data.SendRatebyTOTCount;
                    if (data.SendVK != default(System.Windows.Forms.Keys))
                        SendVK = data.SendVK;
                    if (!string.IsNullOrWhiteSpace(data.SendVK_Application))
                        SendVK_Application = data.SendVK_Application;
                    contentUrl = data.contentUrl;
                    metadataUrl = data.metadataUrl;
                    if (data.URL_time < DateTime.Now)
                        URL_time = data.URL_time;
                }
            }
            catch (FileNotFoundException)
            {
                Save();
            }
        }

        public static void Save()
        {
            var serializer = new DataContractSerializer(typeof(Savedata));
            using (var xmlw = XmlWriter.Create(filepath, new XmlWriterSettings { Indent = true }))
            {
                var data = new Savedata
                {
                    Version = Version,
                    LogToFile = LogToFile,
                    refresh_token = enc_refresh_token,
                    SendToHost = SendToHost,
                    SendToPort = SendToPort,
                    SendPacketNum = SendPacketNum,
                    SendDelay = SendDelay,
                    SendRatebySendCount = SendRatebySendCount,
                    SendRatebyTOTCount = SendRatebyTOTCount,
                    SendVK = SendVK,
                    SendVK_Application = SendVK_Application,
                    contentUrl = contentUrl,
                    metadataUrl = metadataUrl,
                    URL_time = URL_time,
                };
                serializer.WriteObject(xmlw, data);
            }
        }
    }

    [DataContract]
    class Savedata
    {
        [DataMember]
        public string Version;
        [DataMember]
        public bool LogToFile;
        [DataMember]
        public string refresh_token;
        [DataMember]
        public string SendToHost;
        [DataMember]
        public int SendToPort;
        [DataMember]
        public int SendPacketNum;
        [DataMember]
        public int SendDelay;
        [DataMember]
        public int SendRatebySendCount;
        [DataMember]
        public int SendRatebyTOTCount;
        [DataMember]
        public System.Windows.Forms.Keys SendVK;
        [DataMember]
        public string SendVK_Application;
        [DataMember]
        public string contentUrl;
        [DataMember]
        public string metadataUrl;
        [DataMember]
        public DateTime URL_time;
    }
}
