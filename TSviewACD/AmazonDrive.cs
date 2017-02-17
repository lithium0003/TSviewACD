using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TSviewACD
{
    public class ConfigAPI
    {
        public const string client_id = "";
        public const string client_secret = "";
        public const string token_save_password = "cryptpassword";

        public const string App_redirect = "https://lithium03.info/login/redirect";
        public const string LoginSuccess = "https://lithium03.info/login/login_success.html";

        public const string AmazonAPI_login = "https://www.amazon.com/ap/oa";
        public const string AmazonAPI_token = "https://api.amazon.com/auth/o2/token";
        public const string getEndpoint = "https://drive.amazonaws.com/drive/v1/account/endpoint";

        public const long FilenameChangeTrickSize = 10 * 1024 * 1024 * 1024L;
        public const string temporaryFilename = "temporary_filename";

        public const int CopyBufferSize = 64 * 1024 * 1024;
    }

    class AmazonDrive
    {
        AuthKeys key;
        DateTime key_timer;
        CancellationTokenSource ct_source = new CancellationTokenSource();
        public CancellationToken ct { get { return ct_source.Token; } }
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

        public async Task<bool> Login()
        {
            Config.Log.LogOut("\t[Login]");
            if (string.IsNullOrEmpty(Config.refresh_token))
            {
                Authkey = new FormLogin().Login();
                if (Authkey != null && !string.IsNullOrEmpty(Authkey.access_token))
                {
                    key_timer = DateTime.Now;
                    return true;
                }
                return false;
            }
            else
            {
                return await Refresh().ConfigureAwait(false);
            }
        }

        public async Task<bool> Refresh()
        {
            Config.Log.LogOut("\t[Refresh]");
            var newkey = new AuthKeys();
            newkey.refresh_token = Config.refresh_token;
            newkey = await FormLogin.RefreshAuthorizationCode(newkey, ct_source.Token).ConfigureAwait(false);
            if (newkey != null && !string.IsNullOrEmpty(newkey.access_token))
            {
                Authkey = newkey;
                key_timer = DateTime.Now;
                return true;
            }
            Config.refresh_token = "";
            return await Login().ConfigureAwait(false);
        }

        public void Cancel()
        {
            Config.Log.LogOut("\t[Cancel]");
            ct_source.Cancel();
            ct_source = new CancellationTokenSource();
        }

        static T ParseResponse<T>(string response)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(response)))
            {
                return (T)serializer.ReadObject(ms);
            }
        }

        public async Task EnsureToken()
        {
            if (DateTime.Now - key_timer < TimeSpan.FromMinutes(50) && await GetAccountInfo().ConfigureAwait(false)) return;
            await Refresh().ConfigureAwait(false);
        }

        public async Task<bool> GetEndpoint()
        {
            Config.Log.LogOut("\t[GetEndpoint]");
            string error_str;
            if (DateTime.Now - Config.URL_time < TimeSpan.FromDays(3))
            {
                metadataUrl = Config.metadataUrl;
                contentUrl = Config.contentUrl;
                if (await GetAccountInfo().ConfigureAwait(false)) return true;
            }

            using (var client = new HttpClient())
            {
                try
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
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    Config.Log.LogOut("\t[GetEndpoint] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[GetEndpoint] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
            }
            return false;
        }

        public async Task<bool> GetAccountInfo()
        {
            Config.Log.LogOut("\t[GetAccountInfo]");
            string error_str;
            using (var client = new HttpClient())
            {
                try
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
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    Config.Log.LogOut("\t[GetAccountInfo] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[GetAccountInfo] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
            }
            return false;
        }

        public async Task<FileMetadata_Info> GetFileMetadata(string id, bool templink = false)
        {
            Config.Log.LogOut("\t[GetFileMetadata] " + id);
            string error_str;
            using (var client = new HttpClient())
            {
                try
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
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    Config.Log.LogOut("\t[GetFileMetadata] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[GetFileMetadata] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
            }
            throw new SystemException("GetFileMetadata Failed. " + error_str);
        }

        public async Task<FileListdata_Info> ListMetadata(string filters = null, string startToken = null)
        {
            Config.Log.LogOut("\t[ListMetadata]");
            string error_str = "";
            using (var client = new HttpClient())
            {
                Random rnd = new Random();
                var retry = 0;
                while (++retry > 0)
                {
                    try
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
                            var next_info = await ListMetadata(filters, info.nextToken).ConfigureAwait(false);
                            info.data = info.data.Concat(next_info.data).ToArray();
                        }
                        return info;
                    }
                    catch (HttpRequestException ex)
                    {
                        error_str = ex.Message;
                        Config.Log.LogOut("\t[ListMetadata] " + error_str);
                        System.Diagnostics.Debug.WriteLine(error_str);

                        if (ex.Message.Contains("500 (Internal Server Error)") ||
                            ex.Message.Contains("503 (Service Unavailable)"))
                        {
                            var waitsec = rnd.Next((int)Math.Pow(2, Math.Min(retry - 1, 8)));
                            Config.Log.LogOut("\t[ListMetadata] wait " + waitsec.ToString() + " sec");
                            System.Diagnostics.Debug.WriteLine("{0} sec wait", waitsec);
                            await Task.Delay(waitsec * 1000);
                        }
                        else { break; }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        error_str = ex.ToString();
                        Config.Log.LogOut("\t[ListMetadata] " + error_str);
                        System.Diagnostics.Debug.WriteLine(error_str);
                        break;
                    }
                }
            }
            throw new SystemException("ListMetadata Failed. " + error_str);
        }

        public async Task<FileListdata_Info> ListChildren(string id, string startToken = null)
        {
            Config.Log.LogOut("\t[ListChildren] " + id);
            string error_str;
            using (var client = new HttpClient())
            {
                try
                {
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
                        var next_info = await ListChildren(id, info.nextToken).ConfigureAwait(false);
                        info.data = info.data.Concat(next_info.data).ToArray();
                    }
                    return info;
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    Config.Log.LogOut("\t[ListChildren] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[ListChildren] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
            }
            throw new SystemException("ListMetadata Failed. " + error_str);
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

        public async Task<FileMetadata_Info> uploadFile(string filename, string parent_id = null, PoschangeEventHandler process = null)
        {
            Config.Log.LogOut("\t[uploadFile] " + filename);
            string error_str;
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

                    jsonSer.WriteObject(ms, new ItemUpload_Info
                    {
                        name = Path.GetFileName(filename),
                        kind = "FILE",
                        parents = string.IsNullOrEmpty(parent_id) ? null : new string[] { parent_id }
                    });
                    ms.Position = 0;

                    // StreamReader で StringContent (Json) をコンストラクトします。
                    StreamReader sr = new StreamReader(ms);
                    content.Add(new StringContent(sr.ReadToEnd(), System.Text.Encoding.UTF8, "application/json"), "metadata");

                    var f = new PositionStream(File.OpenRead(filename));
                    if (process != null)
                        f.PosChangeEvent += process;
                    //f.PosChangeEvent += (src, e) =>
                    //{
                    //    System.Diagnostics.Debug.WriteLine("HTTP pos :" + e.Log);
                    //};
                    var fileContent = new StreamContent(f);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    fileContent.Headers.ContentLength = f.Length;
                    content.Add(fileContent, "content", Path.GetFileName(filename));

                    var response = await client.PostAsync(
                        Config.contentUrl + "nodes?suppress=deduplication",
                        content,
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
                    Config.Log.LogOut("\t[uploadFile] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[uploadFile] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                    throw;
                }
            }
        }

        public async Task<bool> TrashItem(string id)
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
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[Trash] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
            }
            return false;
        }

        public async Task<Stream> downloadFile(string id, long? from = null, long? to = null)
        {
            Config.Log.LogOut("\t[downloadFile] " + id);
            string error_str;
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromDays(1);
            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authkey.access_token);
                if (from != null || to != null)
                    client.DefaultRequestHeaders.Range = new RangeHeaderValue(from, to);
                string url = Config.contentUrl + "nodes/" + id + "/content?download=false";
                var response = await client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException ex)
            {
                error_str = ex.Message;
                Config.Log.LogOut("\t[downloadFile] " + error_str);
                System.Diagnostics.Debug.WriteLine(error_str);
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
                System.Diagnostics.Debug.WriteLine(error_str);
                throw;
            }
            throw new SystemException("fileDownload failed. " + error_str);
        }

        public async Task<FileMetadata_Info> renameItem(string id, string newname)
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
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[rename] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
            }
            throw new SystemException("rename failed. " + error_str);
        }


        public async Task<FileMetadata_Info> createFolder(string foldername, string parent_id = null)
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
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[createFolder] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
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

        public async Task<bool> moveChild(string childid, string fromParentId, string toParentId)
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
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    Config.Log.LogOut("\t[move] " + error_str);
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
            }
            throw new SystemException("moveChild failed. " + error_str);
        }
    }
}
