using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace TSviewACD
{
    static class Config
    {
        public readonly static string[] CarotDAV_crypt_names = new string[]
        {
            "^_",
            ":D",
            ";)",
            "T-T",
            "orz",
            "ノシ",
            "（´・ω・）"
        };

        public const long SmallFileSize = 10 * 1024 * 1024;
        private static string GetFileSystemPath(Environment.SpecialFolder folder)
        {
            // パスを取得
            string path = string.Format(@"{0}\{1}\{2}",
              Environment.GetFolderPath(folder),  // ベース・パス
              Application.CompanyName,            // 会社名
              Application.ProductName);           // 製品名

            // パスのフォルダを作成
            lock (typeof(Application))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            return path;
        }
        private static readonly bool IsInstalled = File.Exists(Path.Combine(Application.StartupPath, "_installed.txt"));
        private static readonly string _config_basepath = (IsInstalled) ? GetFileSystemPath(Environment.SpecialFolder.ApplicationData) : "";
        public static string Config_BasePath
        {
            get { return _config_basepath; }
        }
        private static readonly string _filepath = Path.Combine(_config_basepath, Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".xml");
        private static string filepath {
            get { return _filepath; }
        }

        private static string _MasterPassword = "crypt for password";
        public static string MasterPassword
        {
            get { return _MasterPassword; }
            set
            {
                try
                {
                    if (IsMasterPasswordCorrect || Decrypt(enc_Check_drive_password, value) == Check_pass_password)
                    {
                        _MasterPassword = value;
                        if (string.IsNullOrEmpty(value)) _MasterPassword = "crypt for password";
                        enc_Check_drive_password = Encrypt(Check_pass_password, _MasterPassword);
                    }
                }
                catch { }
            }
        }
        private const string Check_pass_password = "check for dirve password";

        public static readonly string Version = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion.ToString();
        public static string SendToHost = "localhost";
        public static int SendToPort = 1240;
        public static int SendPacketNum = 32;
        public static int SendDelay = 0;
        public static int SendLongOffset = 2000;
        public static int SendRatebySendCount = 5;
        public static int SendRatebyTOTCount = 1;
        public static System.Windows.Forms.Keys SendVK = default(System.Windows.Forms.Keys);
        public static string SendVK_Application = "";
        public static double UploadLimit = double.PositiveInfinity;
        public static double DownloadLimit = double.PositiveInfinity;
        public static string refresh_token = "";
        public static string contentUrl = "";
        public static string metadataUrl = "";
        public static DateTime URL_time;
        public static FFmoduleKeybindsClass FFmoduleKeybinds = new FFmoduleKeybindsClass() {
            { ffmodule.FFplayerKeymapFunction.FuncPlayExit,         new FFmoduleKeysClass{System.Windows.Forms.Keys.Escape } },
            { ffmodule.FFplayerKeymapFunction.FuncSeekPlus10sec,    new FFmoduleKeysClass{System.Windows.Forms.Keys.Right } },
            { ffmodule.FFplayerKeymapFunction.FuncSeekMinus10sec,   new FFmoduleKeysClass{System.Windows.Forms.Keys.Left } },
            { ffmodule.FFplayerKeymapFunction.FuncSeekPlus60sec,    new FFmoduleKeysClass{System.Windows.Forms.Keys.Up} },
            { ffmodule.FFplayerKeymapFunction.FuncSeekMinus60sec,   new FFmoduleKeysClass{System.Windows.Forms.Keys.Down} },
            { ffmodule.FFplayerKeymapFunction.FuncVolumeUp,         new FFmoduleKeysClass{System.Windows.Forms.Keys.Insert} },
            { ffmodule.FFplayerKeymapFunction.FuncVolumeDown,       new FFmoduleKeysClass{System.Windows.Forms.Keys.Delete} },
            { ffmodule.FFplayerKeymapFunction.FuncToggleFullscreen, new FFmoduleKeysClass{System.Windows.Forms.Keys.F} },
            { ffmodule.FFplayerKeymapFunction.FuncToggleDisplay,    new FFmoduleKeysClass{System.Windows.Forms.Keys.D} },
            { ffmodule.FFplayerKeymapFunction.FuncToggleMute,       new FFmoduleKeysClass{System.Windows.Forms.Keys.M} },
            { ffmodule.FFplayerKeymapFunction.FuncCycleChannel,     new FFmoduleKeysClass{System.Windows.Forms.Keys.C} },
            { ffmodule.FFplayerKeymapFunction.FuncCycleAudio,       new FFmoduleKeysClass{System.Windows.Forms.Keys.A} },
            { ffmodule.FFplayerKeymapFunction.FuncCycleSubtitle,    new FFmoduleKeysClass{System.Windows.Forms.Keys.T} },
            { ffmodule.FFplayerKeymapFunction.FuncForwardChapter,   new FFmoduleKeysClass{System.Windows.Forms.Keys.PageUp} },
            { ffmodule.FFplayerKeymapFunction.FuncRewindChapter,    new FFmoduleKeysClass{System.Windows.Forms.Keys.PageDown} },
            { ffmodule.FFplayerKeymapFunction.FuncTogglePause,      new FFmoduleKeysClass{System.Windows.Forms.Keys.P} },
            { ffmodule.FFplayerKeymapFunction.FuncResizeOriginal,   new FFmoduleKeysClass{System.Windows.Forms.Keys.D0} },
            { ffmodule.FFplayerKeymapFunction.FuncSrcVolumeUp,      new FFmoduleKeysClass{System.Windows.Forms.Keys.F1} },
            { ffmodule.FFplayerKeymapFunction.FuncSrcVolumeDown,    new FFmoduleKeysClass{System.Windows.Forms.Keys.F2} },
            { ffmodule.FFplayerKeymapFunction.FuncSrcAutoVolume,    new FFmoduleKeysClass{System.Windows.Forms.Keys.F4} },
        };
        public static string FontFilepath = "ipaexg.ttf";
        public static int FontPtSize = 48;
        public static double FFmodule_TransferLimit = 128;
        public static bool FFmodule_AutoResize = true;
        static string _DrivePassword = null;
        public static bool IsMasterPasswordCorrect
        {
            get {
                try
                {
                    return string.IsNullOrEmpty(_Encrypted_DirvePasswordCheck) || Decrypt(_Encrypted_DirvePasswordCheck, MasterPassword) == Check_pass_password;
                }
                catch
                {
                    return false;
                }
            }
        }
        public static string DrivePassword
        {
            get {
                if (_DrivePassword != null) return _DrivePassword;
                try
                {
                    if(string.IsNullOrEmpty(_Encrypted_DirvePasswordCheck) || Decrypt(_Encrypted_DirvePasswordCheck, MasterPassword) == Check_pass_password)
                    {
                        _DrivePassword = Decrypt(_Encrypted_DrivePassword, MasterPassword);
                    }
                }
                catch
                {
                    _DrivePassword = null;
                }
                return _DrivePassword ?? "";
            }
            set
            {
                _DrivePassword = value;
                CryptCarotDAV.Password = _DrivePassword ?? "";
                CryptCTR.password = _DrivePassword ?? "";
            }
        }
        public static bool LockPassword = false;
        public static bool UseEncryption = false;
        public static bool UseFilenameEncryption = false;
        public static string Language = "";
        public static bool debug = false;
        public static CryptMethods CryptMethod = CryptMethods.Method1_CTR;
        public static bool AutoDecode = true;
        public static string CarotDAV_CryptNameHeader = CarotDAV_crypt_names[0];
        public static int UploadBufferSize = 16 * 1024 * 1024;
        public static int DownloadBufferSize = 16 * 1024 * 1024;
        public static bool UploadTrick1 = false;
        public static int ParallelDownload = 3;
        public static int ParallelUpload = 3;
        public static bool FFmodule_fullscreen = false;
        public static bool FFmodule_display = false;
        public static double FFmodule_volume = 50;
        public static bool FFmodule_mute = false;
        public static int FFmodule_width = 0;
        public static int FFmodule_hight = 0;
        public static int FFmodule_x = 0;
        public static int FFmodule_y = 0;
        public static System.Drawing.Point? Main_Location;
        public static System.Drawing.Size? Main_Size;
        // temporary
        public static int AmazonDriveTempCount = 0;
        public static double UploadLimitTemp = 100 * 1024;

        private static byte[] _salt = Encoding.ASCII.GetBytes("TSviewACD");
        private const string token_password = ConfigAPI.token_save_password;

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

        private static bool _IsClosing = false;
        public static bool IsClosing
        {
            get { return _IsClosing; }
            set
            {
                if (_IsClosing) return;
                _IsClosing = value;
                if (IsClosing)
                {
                    Save();
                }
            }
        }

        public static string enc_refresh_token
        {
            get
            {
                return (string.IsNullOrEmpty(refresh_token)) ? "" : Encrypt(refresh_token, token_password);
            }
            set
            {
                try
                {
                    refresh_token = Decrypt(value, token_password);
                }
                catch
                {
                    refresh_token = "";
                }
            }
        }

        private static string _Encrypted_DirvePasswordCheck;
        public static string enc_Check_drive_password
        {
            get
            {
                if (!IsMasterPasswordCorrect)
                    return _Encrypted_DirvePasswordCheck;
                return Encrypt(Check_pass_password, MasterPassword);
            }
            set
            {
                _Encrypted_DirvePasswordCheck = value;
            }
        }

        private static string _Encrypted_DrivePassword;
        public static string enc_drive_password
        {
            get
            {
                if (!IsMasterPasswordCorrect)
                    return _Encrypted_DrivePassword;
                return (string.IsNullOrEmpty(DrivePassword)) ? "" : Encrypt(DrivePassword, MasterPassword);
            }
            set
            {
                _Encrypted_DrivePassword = value;
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
            DrivePassword = null;
            var serializer = new DataContractSerializer(typeof(Savedata));
            try
            {
                using (var xmlr = XmlReader.Create(filepath))
                {
                    var data = (Savedata)serializer.ReadObject(xmlr);
                    if (data.LogToFile)
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
                    if (data.SendLongOffset != default(int))
                        SendLongOffset = data.SendLongOffset;
                    if (data.SendRatebySendCount != default(int))
                        SendRatebySendCount = data.SendRatebySendCount;
                    if (data.SendRatebyTOTCount != default(int))
                        SendRatebyTOTCount = data.SendRatebyTOTCount;
                    if (data.SendVK != default(System.Windows.Forms.Keys))
                        SendVK = data.SendVK;
                    if (!string.IsNullOrWhiteSpace(data.SendVK_Application))
                        SendVK_Application = data.SendVK_Application;
                    if (data.UploadBandwidthLimit != default(double))
                        UploadLimit = data.UploadBandwidthLimit;
                    if (data.DownloadBandwidthLimit != default(double))
                        DownloadLimit = data.DownloadBandwidthLimit;
                    if (data.FFmoduleKeybinds != null)
                    {
                        if (data.FFmoduleKeybinds.Count() >= FFmoduleKeybinds.Count())
                            FFmoduleKeybinds = data.FFmoduleKeybinds;
                        else
                            data.FFmoduleKeybinds.ToList().ForEach(x => FFmoduleKeybinds[x.Key] = x.Value);
                    }
                    if (!string.IsNullOrWhiteSpace(data.FontFilepath))
                        FontFilepath = data.FontFilepath;
                    if (data.FontPtSize != default(int))
                        FontPtSize = data.FontPtSize;
                    if (data.FFmodule_TransferLimit != default(double))
                        FFmodule_TransferLimit = data.FFmodule_TransferLimit;
                    if (data.FFmodule_AutoResize != true)
                        FFmodule_AutoResize = data.FFmodule_AutoResize;
                    enc_drive_password = data.DrivePassword;
                    enc_Check_drive_password = data.DrivePasswordCheck;
                    if (data.LockPassword != default(bool))
                        LockPassword = data.LockPassword;
                    if (data.UseEncryption != default(bool))
                        UseEncryption = data.UseEncryption;
                    if (data.UseFilenameEncryption != default(bool))
                        UseFilenameEncryption = data.UseFilenameEncryption;
                    if (data.Language != default(string))
                        Language = data.Language;
                    if (data.CryptMethod != default(CryptMethods))
                        CryptMethod = data.CryptMethod;
                    if (data.AutoDecode != true)
                        AutoDecode = data.AutoDecode;
                    if (data.CarotDAV_CryptNameHeader != default(string))
                        CarotDAV_CryptNameHeader = data.CarotDAV_CryptNameHeader;
                    if (data.UploadBufferSize != default(int))
                        UploadBufferSize = data.UploadBufferSize;
                    if (data.DownloadBufferSize != default(int))
                        DownloadBufferSize = data.DownloadBufferSize;
                    if (data.UploadTrick1 != default(bool))
                        UploadTrick1 = data.UploadTrick1;
                    if (data.ParallelDownload != default(int))
                        ParallelDownload = data.ParallelDownload;
                    if (data.ParallelUpload != default(int))
                        ParallelUpload = data.ParallelUpload;
                    if (data.FFmodule_fullscreen != default(bool))
                        FFmodule_display = data.FFmodule_display;
                    if (data.FFmodule_volume != default(double))
                        FFmodule_volume = data.FFmodule_volume;
                    if (data.FFmodule_mute != default(bool))
                        FFmodule_mute = data.FFmodule_mute;
                    if (data.FFmodule_Size != null)
                    {
                        FFmodule_width = data.FFmodule_Size.Value.Width;
                        FFmodule_hight = data.FFmodule_Size.Value.Height;
                    }
                    if (data.FFmodule_Location != null)
                    {
                        FFmodule_x = data.FFmodule_Location.Value.X;
                        FFmodule_y = data.FFmodule_Location.Value.Y;
                    }
                    if (data.Main_Size != null)
                        Main_Size = data.Main_Size;
                    if (data.Main_Location != null)
                        Main_Location = data.Main_Location;
                    contentUrl = data.contentUrl;
                    metadataUrl = data.metadataUrl;
                    if (data.URL_time < DateTime.Now)
                        URL_time = data.URL_time;
                }
            }
            catch (Exception)
            {
                Save();
            }
        }

        public static void Save()
        {
            lock (filepath)
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
                        SendLongOffset = SendLongOffset,
                        SendRatebySendCount = SendRatebySendCount,
                        SendRatebyTOTCount = SendRatebyTOTCount,
                        SendVK = SendVK,
                        SendVK_Application = SendVK_Application,
                        UploadBandwidthLimit = UploadLimit,
                        DownloadBandwidthLimit = DownloadLimit,
                        contentUrl = contentUrl,
                        metadataUrl = metadataUrl,
                        URL_time = URL_time,
                        FFmoduleKeybinds = FFmoduleKeybinds,
                        FontPtSize = FontPtSize,
                        FontFilepath = FontFilepath,
                        FFmodule_TransferLimit = FFmodule_TransferLimit,
                        FFmodule_AutoResize = FFmodule_AutoResize,
                        DrivePassword = enc_drive_password,
                        DrivePasswordCheck = enc_Check_drive_password,
                        LockPassword = LockPassword,
                        UseEncryption = UseEncryption,
                        UseFilenameEncryption = UseFilenameEncryption,
                        Language = Language,
                        CryptMethod = CryptMethod,
                        AutoDecode = AutoDecode,
                        CarotDAV_CryptNameHeader = CarotDAV_CryptNameHeader,
                        UploadBufferSize = UploadBufferSize,
                        DownloadBufferSize = DownloadBufferSize,
                        UploadTrick1 = UploadTrick1,
                        ParallelDownload = ParallelDownload,
                        ParallelUpload = ParallelUpload,
                        FFmodule_fullscreen = FFmodule_fullscreen,
                        FFmodule_display = FFmodule_display,
                        FFmodule_volume = FFmodule_volume,
                        FFmodule_mute = FFmodule_mute,
                        FFmodule_Size = new System.Drawing.Size(FFmodule_width, FFmodule_hight),
                        FFmodule_Location = new System.Drawing.Point(FFmodule_x, FFmodule_y),
                        Main_Size = Program.MainForm?.Size ?? Main_Size,
                        Main_Location = Program.MainForm?.Location ?? Main_Location,
                    };
                    serializer.WriteObject(xmlw, data);
                }
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
        public int SendLongOffset;
        [DataMember]
        public int SendRatebySendCount;
        [DataMember]
        public int SendRatebyTOTCount;
        [DataMember]
        public System.Windows.Forms.Keys SendVK;
        [DataMember]
        public string SendVK_Application;
        [DataMember]
        public double UploadBandwidthLimit;
        [DataMember]
        public double DownloadBandwidthLimit;
        [DataMember]
        public string contentUrl;
        [DataMember]
        public string metadataUrl;
        [DataMember]
        public DateTime URL_time;
        [DataMember]
        public FFmoduleKeybindsClass FFmoduleKeybinds;
        [DataMember]
        public string FontFilepath;
        [DataMember]
        public int FontPtSize;
        [DataMember]
        public double FFmodule_TransferLimit;
        [DataMember]
        public bool FFmodule_AutoResize;
        [DataMember]
        public string DrivePassword;
        [DataMember]
        public string DrivePasswordCheck;
        [DataMember]
        public bool LockPassword;
        [DataMember]
        public bool UseEncryption;
        [DataMember]
        public bool UseFilenameEncryption;
        [DataMember]
        public string Language;
        [DataMember]
        public CryptMethods CryptMethod;
        [DataMember]
        public bool AutoDecode;
        [DataMember]
        public string CarotDAV_CryptNameHeader;
        [DataMember]
        public int UploadBufferSize;
        [DataMember]
        public int DownloadBufferSize;
        [DataMember]
        public bool UploadTrick1;
        [DataMember]
        public int ParallelDownload;
        [DataMember]
        public int ParallelUpload;
        [DataMember]
        public bool FFmodule_fullscreen;
        [DataMember]
        public bool FFmodule_display;
        [DataMember]
        public double FFmodule_volume;
        [DataMember]
        public bool FFmodule_mute;
        [DataMember]
        public System.Drawing.Size? FFmodule_Size;
        [DataMember]
        public System.Drawing.Point? FFmodule_Location;
        [DataMember]
        public System.Drawing.Size? Main_Size;
        [DataMember]
        public System.Drawing.Point? Main_Location;
    }

    [CollectionDataContract
    (Name = "FFmoduleKeyBinds",
    ItemName = "entry",
    KeyName = "command",
    ValueName = "keys")]
    public class FFmoduleKeybindsClass : Dictionary<ffmodule.FFplayerKeymapFunction, FFmoduleKeysClass> { }

    [CollectionDataContract(Name = "bindkeys")]
    public class FFmoduleKeysClass : Collection<System.Windows.Forms.Keys>{ }
}
