using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TSviewACD
{
    public class ConfigAPI
    {
        readonly static string client_id_enc = "";
        readonly static string client_secret_enc = "";

        public static string client_id
        {
            get { return Encoding.ASCII.GetString(Convert.FromBase64String(client_id_enc)); }
        }
        public static string client_secret
        {
            get { return Encoding.ASCII.GetString(Convert.FromBase64String(client_secret_enc)); }
        }
        public const string token_save_password = "cryptpassword";

        public const string App_redirect = "https://lithium03.info/login/redirect";
        public const string LoginSuccess = "https://lithium03.info/login/login_success.html";

        public const string AmazonAPI_login = "https://www.amazon.com/ap/oa";
        public const string AmazonAPI_token = "https://api.amazon.com/auth/o2/token";
        public const string getEndpoint = "https://drive.amazonaws.com/drive/v1/account/endpoint";

        public const long FilenameChangeTrickSize = 9500 * 1000 * 1000L; //9.5GB
        public const string temporaryFilename = "temporary_filename";

        public const int CopyBufferSize = 64 * 1024 * 1024;
    }

    public class AmazonDriveUploadException : Exception
    {
        public string Hash;

        public AmazonDriveUploadException() : base()
        {
        }
        public AmazonDriveUploadException(string message) : base(message)
        {
        }
        public AmazonDriveUploadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    class AmazonDrive
    {
        AuthKeys key;
        DateTime key_timer;
        string contentUrl;
        string metadataUrl;

        AuthKeys Authkey
        {
            get
            {
                if (DateTime.Now - key_timer < TimeSpan.FromMinutes(30)) return key;
                if (Refresh().Result) return key;
                Config.Log.LogOut("\t[Authkey] autokey refresh failed.");
                return key;
            }
            set { key = value; }
        }

        public async Task<bool> Login(CancellationToken ct = default(CancellationToken))
        {
            Config.Log.LogOut("\t[Login]");
            if (string.IsNullOrEmpty(Config.refresh_token))
            {
                Thread t = new Thread(new ThreadStart(() =>
                {
                    Authkey = new FormLogin().Login(ct);
                }));
                t.SetApartmentState(System.Threading.ApartmentState.STA);
                t.IsBackground = true;
                t.Start();
                while (t.IsAlive) await Task.Delay(1000).ConfigureAwait(false);
                if (Authkey != null && !string.IsNullOrEmpty(Authkey.access_token))
                {
                    key_timer = DateTime.Now;
                    DriveData.RemoveCache();
                    return true;
                }
                return false;
            }
            else
            {
                return await Refresh(ct).ConfigureAwait(false);
            }
        }

        public async Task<bool> Refresh(CancellationToken ct = default(CancellationToken))
        {
            Config.Log.LogOut("\t[Refresh]");
            var newkey = new AuthKeys();
            newkey.refresh_token = Config.refresh_token;
            newkey = await FormLogin.RefreshAuthorizationCode(newkey, ct).ConfigureAwait(false);
            if (newkey != null && !string.IsNullOrEmpty(newkey.access_token))
            {
                Authkey = newkey;
                key_timer = DateTime.Now;
                return true;
            }
            Config.refresh_token = "";
            return await Login(ct).ConfigureAwait(false);
        }

        static T ParseResponse<T>(string response)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(response)))
            {
                return (T)serializer.ReadObject(ms);
            }
        }

        static T ParseResponse<T>(Stream response)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            return (T)serializer.ReadObject(response);
        }

        delegate Task<T> DoConnection<T>();
        private async Task<T> DoWithRetry<T>(DoConnection<T> func, string LogPrefix = "DoWithRetry")
        {
            Random rnd = new Random();
            var retry = 0;
            string error_str = "";
            while (++retry < 30)
            {
                try
                {
                    return await func();
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    Config.Log.LogOut("\t[" + LogPrefix + "] " + error_str);

                    if (ex.Message.Contains("401 (Unauthorized)") ||
                        ex.Message.Contains("429 (Too Many Requests)") ||
                        ex.Message.Contains("500 (Internal Server Error)") ||
                        ex.Message.Contains("503 (Service Unavailable)"))
                    {
                        var waitsec = rnd.Next((int)Math.Pow(2, Math.Min(retry - 1, 8)));
                        Config.Log.LogOut("\t[" + LogPrefix + "] wait " + waitsec.ToString() + " sec");
                        await Task.Delay(waitsec * 1000);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[" + LogPrefix + "] " + error_str);
                    break;
                }
            }
            throw new SystemException(LogPrefix + " Failed. " + error_str);
        }


        public async Task EnsureToken(CancellationToken ct = default(CancellationToken))
        {
            if (DateTime.Now - key_timer < TimeSpan.FromMinutes(50) && await GetAccountInfo(ct).ConfigureAwait(false)) return;
            await Refresh(ct).ConfigureAwait(false);
        }

        public async Task<bool> GetEndpoint(CancellationToken ct = default(CancellationToken))
        {
            Config.Log.LogOut("\t[GetEndpoint]");
            if (DateTime.Now - Config.URL_time < TimeSpan.FromDays(3))
            {
                metadataUrl = Config.metadataUrl;
                contentUrl = Config.contentUrl;
                if (await GetAccountInfo(ct).ConfigureAwait(false)) return true;
            }
            try
            {
                using (var client = new HttpClient())
                {
                    return await DoWithRetry(async () =>
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);
                        var response = await client.GetAsync(
                            ConfigAPI.getEndpoint,
                            ct
                        ).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        // Above three lines can be replaced with new helper method in following line
                        // string body = await client.GetStringAsync(uri);
                        var data = ParseResponse<getEndpoint_Info>(responseBody);
                        contentUrl = data.contentUrl;
                        metadataUrl = data.metadataUrl;
                        Config.contentUrl = contentUrl;
                        Config.metadataUrl = metadataUrl;
                        Config.URL_time = DateTime.Now;
                        Config.Save();
                        return true;
                    }, "GetEndpoint");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> GetAccountInfo(CancellationToken ct = default(CancellationToken))
        {
            Config.Log.LogOut("\t[GetAccountInfo]");
            try
            {
                using (var client = new HttpClient())
                {
                    return await DoWithRetry(async () =>
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key.access_token);
                        var response = await client.GetAsync(
                            metadataUrl + "account/info",
                            ct
                        ).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        // Above three lines can be replaced with new helper method in following line
                        // string body = await client.GetStringAsync(uri);
                        var data = ParseResponse<getAccountInfo_Info>(responseBody);
                        return true;
                    }, "GetAccountInfo");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<FileMetadata_Info> GetFileMetadata(string id, CancellationToken ct = default(CancellationToken), bool templink = false)
        {
            Config.Log.LogOut("\t[GetFileMetadata] " + id);
            using (var client = new HttpClient())
            {
                return await DoWithRetry(async () =>
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);
                    var response = await client.GetAsync(
                        metadataUrl + "nodes/" + id + ((templink) ? "?tempLink=true" : ""),
                        ct
                    ).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    // Above three lines can be replaced with new helper method in following line
                    // string body = await client.GetStringAsync(uri);
                    return ParseResponse<FileMetadata_Info>(responseBody);
                }, "GetFileMetadata");
            }
        }

        public async Task<FileListdata_Info> ListMetadata(string filters = null, string startToken = null, CancellationToken ct = default(CancellationToken))
        {
            Config.Log.LogOut("\t[ListMetadata]");
            using (var client = new HttpClient())
            {
                return await DoWithRetry(async () =>
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);
                    var url = metadataUrl + "nodes?";
                    if (!string.IsNullOrEmpty(filters)) url += "filters=" + filters + '&';
                    if (!string.IsNullOrEmpty(startToken)) url += "startToken=" + startToken + '&';
                    url = url.Trim('?', '&');
                    var response = await client.GetAsync(
                        url,
                        ct
                    ).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    // Above three lines can be replaced with new helper method in following line
                    // string body = await client.GetStringAsync(uri);
                    var info = ParseResponse<FileListdata_Info>(responseBody);
                    if (!string.IsNullOrEmpty(info.nextToken))
                    {
                        var next_info = await ListMetadata(filters, info.nextToken, ct: ct).ConfigureAwait(false);
                        info.data = info.data.Concat(next_info.data).ToArray();
                    }
                    return info;
                }, "ListMetadata");
            }
        }

        public async Task<FileListdata_Info> ListChildren(string id, string startToken = null, CancellationToken ct = default(CancellationToken))
        {
            Config.Log.LogOut("\t[ListChildren] " + id);
            using (var client = new HttpClient())
            {
                return await DoWithRetry(async () =>
                {
                    ct.ThrowIfCancellationRequested();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);
                    var response = await client.GetAsync(
                        metadataUrl + "nodes/" + id + "/children" + (string.IsNullOrEmpty(startToken) ? "" : "?startToken=" + startToken),
                        ct
                    ).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    // Above three lines can be replaced with new helper method in following line
                    // string body = await client.GetStringAsync(uri);
                    var info = ParseResponse<FileListdata_Info>(responseBody);
                    if (!string.IsNullOrEmpty(info.nextToken))
                    {
                        var next_info = await ListChildren(id, info.nextToken, ct: ct).ConfigureAwait(false);
                        info.data = info.data.Concat(next_info.data).ToArray();
                    }
                    return info;
                }, "ListChildren");
            }
        }

        [DataContract]
        public class ItemUpload_Info
        {
            [DataMember(EmitDefaultValue = false)]
            public string name;

            [DataMember(EmitDefaultValue = false)]
            public string kind;

            [DataMember(EmitDefaultValue = false)]
            public string[] parents;
        }

        Task DelayUploadReset;

        public async Task<FileMetadata_Info> uploadFile(string filename, string parent_id = null, string uploadname = null, string uploadkey = null, PoschangeEventHandler process = null, CancellationToken ct = default(CancellationToken))
        {
            int transbufsize = Config.UploadBufferSize;
            if (Config.UploadLimit < transbufsize) transbufsize = (int)Config.UploadLimit;
            if (transbufsize < 1 * 1024) transbufsize = 1 * 1024;
            if (Config.UploadTrick1) transbufsize = 256 * 1024;


            Config.Log.LogOut("\t[uploadFile] " + filename);
            string error_str;
            string HashStr = "";
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromDays(1);
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);
                    var content = new MultipartFormDataContent();

                    DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(ItemUpload_Info));

                    // Serializerを使ってオブジェクトをMemoryStream に書き込み
                    MemoryStream ms = new MemoryStream();
                    var short_filename = Path.GetFileName(filename);

                    if (uploadname == null)
                    {
                        uploadname = (Config.UseEncryption) ? short_filename + ".enc" : short_filename;
                        if (uploadkey == null)
                            uploadkey = short_filename;
                    }
                    else
                    {
                        if (uploadkey == null)
                            uploadkey = Path.GetFileNameWithoutExtension(uploadname);
                    }
                    jsonSer.WriteObject(ms, new ItemUpload_Info
                    {
                        name = uploadname,
                        kind = "FILE",
                        parents = string.IsNullOrEmpty(parent_id) ? null : new string[] { parent_id }
                    });
                    ms.Position = 0;

                    // StreamReader で StringContent (Json) をコンストラクトします。
                    StreamReader sr = new StreamReader(ms);
                    content.Add(new StringContent(sr.ReadToEnd(), System.Text.Encoding.UTF8, "application/json"), "metadata");

                    using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 256*1024))
                    {
                        HashStream contStream = null;
                        Stream cryptStream = null;
                        StreamContent fileContent = null;
                        IHashStream hasher = null;
                        if (Config.UseEncryption)
                        {
                            if (Config.CryptMethod == CryptMethods.Method1_CTR)
                            {
                                cryptStream = new CryptCTR.AES256CTR_CryptStream(file, uploadkey);
                                contStream = new HashStream(cryptStream, new MD5CryptoServiceProvider());
                                hasher = contStream;
                            }
                            if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                            {
                                cryptStream = new CryptCarotDAV.CryptCarotDAV_CryptStream(file);
                                contStream = new HashStream(cryptStream, new MD5CryptoServiceProvider());
                                hasher = contStream;
                            }
                        }
                        else
                        {
                            contStream = new HashStream(file, new MD5CryptoServiceProvider());
                            hasher = contStream;
                        }

                        using (cryptStream)
                        using (contStream)
                        using (var thstream = new ThrottleUploadStream(contStream, ct))
                        using (var f = new PositionStream(thstream))
                        {
                            if (process != null)
                                f.PosChangeEvent += process;

                            fileContent = new StreamContent(f, transbufsize);
                            fileContent.Headers.ContentLength = f.Length;
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            content.Add(fileContent, "content", Path.GetFileName(filename));

                            using (fileContent)
                            {
                                if (Config.UploadTrick1)
                                {
                                    if(DelayUploadReset != null)
                                    {
                                        if (Interlocked.CompareExchange(ref Config.UploadLimitTemp, Config.UploadLimit, 10 * 1024) == 10 * 1024)
                                        {
                                            DelayUploadReset = Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith((task) =>
                                            {
                                                Interlocked.Exchange(ref Config.UploadLimit, Config.UploadLimitTemp);
                                                DelayUploadReset = null;
                                            });
                                        }
                                    }
                                }
                                var response = await client.PostAsync(
                                    Config.contentUrl + "nodes?suppress=deduplication",
                                    content,
                                    ct).ConfigureAwait(false);
                                HashStr = hasher.Hash.ToLower();
                                response.EnsureSuccessStatusCode();
                                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                // Above three lines can be replaced with new helper method in following line
                                // string body = await client.GetStringAsync(uri);
                                var ret = ParseResponse<FileMetadata_Info>(responseBody);
                                if (ret.contentProperties?.md5 != HashStr)
                                    throw new AmazonDriveUploadException(HashStr);
                                return ret;
                            }
                        }
                    }
                }
                catch (AmazonDriveUploadException)
                {
                    throw;
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    Config.Log.LogOut("\t[uploadFile] " + error_str);
                    throw new AmazonDriveUploadException(HashStr, ex);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[uploadFile] " + error_str);
                    throw;
                }
            }
        }

        public async Task<bool> TrashItem(string id, CancellationToken ct = default(CancellationToken))
        {
            Config.Log.LogOut("\t[Trash] " + id);
            string error_str;
            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);
                    DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(ItemUpload_Info));

                    // Serializerを使ってオブジェクトをMemoryStream に書き込み
                    MemoryStream ms = new MemoryStream();

                    jsonSer.WriteObject(ms, new ItemUpload_Info
                    {
                        name = null,
                        kind = "FILE",
                        parents = null,
                    });
                    ms.Position = 0;

                    // StreamReader で StringContent (Json) をコンストラクトします。
                    StreamReader sr = new StreamReader(ms);

                    var response = await client.PutAsync(
                        metadataUrl + "trash/" + id,
                        new StringContent(sr.ReadToEnd(), System.Text.Encoding.UTF8, "application/json"),
                        ct
                    ).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    // Above three lines can be replaced with new helper method in following line
                    // string body = await client.GetStringAsync(uri);
                    return true;
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    Config.Log.LogOut("\t[Trash] " + error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[Trash] " + error_str);
                }
            }
            return false;
        }

        public async Task<Stream> downloadFile(FileMetadata_Info target, long? from = null, long? to = null, string enckey = null, bool autodecrypt = true, CancellationToken ct = default(CancellationToken))
        {
            string id = target.id;
            string filename = target.name;
            CryptMethods Encrypted = CryptMethods.Method0_Plain;
            if (enckey != null)
            {
                Encrypted = CryptMethods.Method1_CTR;
            }
            else
            {
                if (filename.StartsWith(Config.CarotDAV_CryptNameHeader))
                {
                    Encrypted = CryptMethods.Method2_CBC_CarotDAV;
                    enckey = "";
                }
                else if (Regex.IsMatch(filename, ".*?\\.[a-z0-9]{8}\\.enc$"))
                {
                    Encrypted = CryptMethods.Method1_CTR;
                    enckey = Path.GetFileNameWithoutExtension(filename);
                }
                else if (Regex.IsMatch(filename, "^[\u2800-\u28ff]+$"))
                {
                    enckey = DriveData.DecryptFilename(target);
                    if (enckey != null) Encrypted = CryptMethods.Method1_CTR;
                }

                if (enckey == null) Encrypted = CryptMethods.Method0_Plain;
            }
            if (!autodecrypt) Encrypted = CryptMethods.Method0_Plain;
            Config.Log.LogOut("\t[downloadFile] " + id);
            string error_str = "";
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromDays(1);
            try
            {
                long? fix_from = from, fix_to = to;

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);
                if (from != null || to != null)
                {
                    if (Encrypted == CryptMethods.Method2_CBC_CarotDAV)
                    {
                        if (fix_from != null)
                        {
                            // ひとつ前のブロックから要求する
                            fix_from -= CryptCarotDAV.BlockSizeByte;
                            if (fix_from < CryptCarotDAV.BlockSizeByte)
                            {
                                // 先頭ブロックを取得するときはファイルの先頭から
                                fix_from = 0;
                            }
                            else
                            {
                                // ブロックにアライメントを合わせる
                                fix_from -= ((fix_from - 1) % CryptCarotDAV.BlockSizeByte + 1);
                                // 途中のブロックを要求された場合は、ヘッダをスキップ
                                fix_from += CryptCarotDAV.CryptHeaderByte;
                            }
                        }
                        if (fix_to != null)
                        {
                            if (fix_to >= target.OrignalLength)
                            {
                                // 末尾まで読み込むときは、ハッシュチェックのために最後まで読み込む
                                fix_to = null;
                            }
                            else
                            {
                                // オリジナルの位置を、暗号化済みの位置に変更
                                fix_to += CryptCarotDAV.CryptHeaderByte;
                            }
                        }
                        if (fix_from != null || fix_to != null)
                            client.DefaultRequestHeaders.Range = new RangeHeaderValue(fix_from, fix_to);
                    }
                    else
                        client.DefaultRequestHeaders.Range = new RangeHeaderValue(from, to);
                }
                string url = Config.contentUrl + "nodes/" + id + "/content?download=false";
                var response = await client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                if (Encrypted == CryptMethods.Method1_CTR)
                {
                    return new CryptCTR.AES256CTR_CryptStream(new ThrottleDownloadStream(new HashStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), new MD5CryptoServiceProvider()), ct),
                        enckey,
                        from ?? 0);
                }
                else if (Encrypted == CryptMethods.Method2_CBC_CarotDAV)
                {
                    return new CryptCarotDAV.CryptCarotDAV_DecryptStream(new ThrottleDownloadStream(new HashStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), new MD5CryptoServiceProvider()), ct),
                        from ?? 0,
                        fix_from ?? 0,
                        target.contentProperties?.size ?? -1
                        );
                }
                else
                    return new ThrottleDownloadStream(new HashStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), new MD5CryptoServiceProvider()), ct);
            }
            catch (HttpRequestException ex)
            {
                error_str = ex.Message;
                Config.Log.LogOut("\t[downloadFile] " + error_str);
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                error_str = ex.ToString();
                Config.Log.LogOut("\t[downloadFile] " + error_str);
                throw;
            }
            throw new SystemException("fileDownload failed. " + error_str);
        }

        public async Task<FileMetadata_Info> renameItem(string id, string newname, CancellationToken ct = default(CancellationToken))
        {
            Config.Log.LogOut("\t[rename] " + id + ' ' + newname);
            string error_str;
            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);
                    string url = Config.metadataUrl + "nodes/" + id;
                    var req = new HttpRequestMessage(new HttpMethod("PATCH"), url);
                    string data = "{ \"name\" : \"" + newname + "\" }";
                    var content = new StringContent(data, Encoding.UTF8, "application/json");
                    content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                    req.Content = content;
                    var response = await client.SendAsync(req, ct).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    // Above three lines can be replaced with new helper method in following line
                    // string body = await client.GetStringAsync(uri);
                    return ParseResponse<FileMetadata_Info>(responseBody);
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    Config.Log.LogOut("\t[rename] " + error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[rename] " + error_str);
                }
            }
            throw new SystemException("rename failed. " + error_str);
        }


        public async Task<FileMetadata_Info> createFolder(string foldername, string parent_id = null, CancellationToken ct = default(CancellationToken))
        {
            Config.Log.LogOut("\t[createFolder] " + foldername);
            string error_str;
            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);

                    DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(ItemUpload_Info));

                    // Serializerを使ってオブジェクトをMemoryStream に書き込み
                    MemoryStream ms = new MemoryStream();

                    jsonSer.WriteObject(ms, new ItemUpload_Info
                    {
                        name = foldername,
                        kind = "FOLDER",
                        parents = string.IsNullOrEmpty(parent_id) ? null : new string[] { parent_id }
                    });
                    ms.Position = 0;

                    // StreamReader で StringContent (Json) をコンストラクトします。
                    StreamReader sr = new StreamReader(ms);

                    var response = await client.PostAsync(
                        Config.metadataUrl + "nodes",
                        new StringContent(sr.ReadToEnd(), System.Text.Encoding.UTF8, "application/json"),
                        ct).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    // Above three lines can be replaced with new helper method in following line
                    // string body = await client.GetStringAsync(uri);
                    return ParseResponse<FileMetadata_Info>(responseBody);
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    Config.Log.LogOut("\t[createFolder] " + error_str);
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[createFolder] " + error_str);
                }
            }
            throw new SystemException("mkFolder failed. " + error_str);
        }

        [DataContract]
        public class MoveChild_Info
        {
            [DataMember(EmitDefaultValue = false)]
            public string fromParent;

            [DataMember(EmitDefaultValue = false)]
            public string childId;
        }

        public async Task<bool> moveChild(string childid, string fromParentId, string toParentId, CancellationToken ct = default(CancellationToken))
        {
            Config.Log.LogOut("\t[move]");
            string error_str;
            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);

                    DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(MoveChild_Info));

                    // Serializerを使ってオブジェクトをMemoryStream に書き込み
                    MemoryStream ms = new MemoryStream();

                    jsonSer.WriteObject(ms, new MoveChild_Info
                    {
                        fromParent = fromParentId,
                        childId = childid
                    });
                    ms.Position = 0;

                    // StreamReader で StringContent (Json) をコンストラクトします。
                    StreamReader sr = new StreamReader(ms);

                    var response = await client.PostAsync(
                        Config.metadataUrl + "nodes/" + toParentId + "/children",
                        new StringContent(sr.ReadToEnd(), System.Text.Encoding.UTF8, "application/json"),
                        ct).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    // Above three lines can be replaced with new helper method in following line
                    // string body = await client.GetStringAsync(uri);
                    return true;
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    Config.Log.LogOut("\t[move] " + error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[move] " + error_str);
                }
            }
            throw new SystemException("moveChild failed. " + error_str);
        }


        [DataContract]
        public class changesreq_Info
        {
            [DataMember(EmitDefaultValue = false)]
            public string checkpoint;

            [DataMember(EmitDefaultValue = false)]
            public int? chunkSize;

            [DataMember(EmitDefaultValue = false)]
            public int? maxNodes;

            [DataMember(EmitDefaultValue = false)]
            public string includePurged;
        }

        public async Task<Changes_Info[]> changes(string checkpoint = null, int? chankSize = null, CancellationToken ct = default(CancellationToken))
        {
            Config.Log.LogOut("\t[changes]");
            using (var handler = new HttpClientHandler())
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromDays(1);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);

                    return await DoWithRetry(async () =>
                    {
                        DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(changesreq_Info));

                        MemoryStream ms = new MemoryStream();

                        jsonSer.WriteObject(ms, new changesreq_Info
                        {
                            checkpoint = checkpoint,
                            chunkSize = chankSize,
                            maxNodes = null,
                            includePurged = "true",
                        });
                        ms.Position = 0;

                        // StreamReader で StringContent (Json) をコンストラクトします。
                        StreamReader sr = new StreamReader(ms);

                        var res = new List<Changes_Info>();

                        var req = new HttpRequestMessage(HttpMethod.Post, Config.metadataUrl + "changes");
                        req.Content = new StringContent(sr.ReadToEnd(), System.Text.Encoding.UTF8, "application/json");
                        var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();

                        var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        byte[] buf = new byte[16 * 1024 * 1024];
                        int len = 0;
                        int offset = 0;
                        while (true)
                        {
                            using (var mem = Stream.Synchronized(new MemoryStream()))
                            {
                                try
                                {
                                    bool reading = true;
                                    while (reading)
                                    {
                                        ct.ThrowIfCancellationRequested();
                                        if (offset > 0)
                                        {
                                            for (int i = offset; i < len; i++)
                                            {
                                                if (buf[i] == '\n')
                                                {
                                                    mem.Write(buf, offset, i - offset);
                                                    offset = i + 1;
                                                    reading = false;
                                                    break; // for
                                                }
                                            }
                                            if (!reading)
                                                break; // reading while
                                            mem.Write(buf, offset, len - offset);
                                            len = 0;
                                            offset = 0;
                                        }
                                        var task = responseStream.ReadAsync(buf, 0, buf.Length, ct).ContinueWith((t) =>
                                        {
                                            reading = false;
                                            if (t.Wait(-1, ct))
                                            {
                                                len = t.Result;
                                                if (len == 0) return;
                                                for (int i = 0; i < len; i++)
                                                {
                                                    if (buf[i] == '\n')
                                                    {
                                                        mem.Write(buf, 0, i);
                                                        offset = i + 1;
                                                        return;
                                                    }
                                                }
                                                mem.Write(buf, 0, len);
                                                offset = 0;
                                                reading = true;
                                            }
                                        }, ct);
                                        await task.ConfigureAwait(false);
                                    }
                                }
                                catch
                                {
                                    break;
                                }
                                if (mem.Position == 0) break;
                                mem.Position = 0;
                                try
                                {
                                    res.Add(ParseResponse<Changes_Info>(mem));
                                }
                                catch { }
                            }
                        }
                        return res.ToArray();
                    }, "changes").ConfigureAwait(false);
                }
            }
        }
    }
}
