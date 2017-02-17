using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace TSviewACD
{
    public partial class FormLogin : Form
    {
        public FormLogin()
        {
            InitializeComponent();
        }

        AuthKeys key;
        CancellationTokenSource ct_soruce;
        string error_str;

        public AuthKeys Login()
        {
            webBrowser1.Navigate(
                ConfigAPI.AmazonAPI_login + "?" +
                "client_id=" + ConfigAPI.client_id + "&" +
                "scope=" + "clouddrive%3Aread_all+clouddrive%3Awrite" + "&" +
                "response_type=" + "code" + "&" +
                "redirect_uri=" + ConfigAPI.App_redirect
                );
            ct_soruce = new CancellationTokenSource();
            ShowDialog();
            return key;
        }

        private void FormLogin_FormClosing(object sender, FormClosingEventArgs e)
        {
            ct_soruce.Cancel();
            if(key == null)
                Environment.Exit(-1);
        }

        private async void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            Text = e.Url.ToString();
            var path = e.Url.AbsoluteUri;
            if (path.StartsWith(ConfigAPI.App_redirect))
            {
                const string code_str = "?code=";
                var i = path.IndexOf(code_str);
                if (i < 0) return;

                string code = path.Substring(i + code_str.Length, path.IndexOf('&', i) - i - code_str.Length);
                await GetAuthorizationCode(code);

                if(key != null && key.access_token != "")
                {
                    webBrowser1.Navigate(ConfigAPI.LoginSuccess);
                    timer1.Enabled = true;
                }
            }
        }

        private async Task GetAuthorizationCode(string access_code)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.PostAsync(
                        ConfigAPI.AmazonAPI_token,
                        new FormUrlEncodedContent(new Dictionary<string, string>{
                            {"grant_type","authorization_code"},
                            {"code",access_code},
                            {"client_id",ConfigAPI.client_id},
                            {"client_secret",ConfigAPI.client_secret},
                            {"redirect_uri",Uri.EscapeUriString(ConfigAPI.App_redirect)},
                        }),
                        ct_soruce.Token
                    );
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Above three lines can be replaced with new helper method in following line
                    // string body = await client.GetStringAsync(uri);
                    key = ParseResponse(responseBody);

                    // Save refresh_token
                    Config.refresh_token = key.refresh_token;
                    Config.Save();
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
            }
        }

        static private AuthKeys ParseResponse(string response)
        {
            AuthKeys key = new AuthKeys();
            var serializer = new DataContractJsonSerializer(typeof(Authentication_Info));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(response)))
            {
                var data = (Authentication_Info)serializer.ReadObject(ms);
                key.access_token = data.access_token;
                key.refresh_token = data.refresh_token;
            }
            return key;
        }

        static public async Task<AuthKeys> RefreshAuthorizationCode(AuthKeys key, CancellationToken ct = default(CancellationToken))
        {
            string error_str;
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.PostAsync(
                        ConfigAPI.AmazonAPI_token,
                        new FormUrlEncodedContent(new Dictionary<string, string>{
                            {"grant_type","refresh_token"},
                            {"refresh_token",key.refresh_token},
                            {"client_id",ConfigAPI.client_id},
                            {"client_secret",ConfigAPI.client_secret},
                        }),
                        ct
                    );
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Above three lines can be replaced with new helper method in following line
                    // string body = await client.GetStringAsync(uri);
                    key = ParseResponse(responseBody);

                    // Save refresh_token
                    Config.refresh_token = key.refresh_token;
                    Config.Save();
                }
                catch (HttpRequestException ex)
                {
                    error_str = ex.Message;
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error_str = ex.ToString();
                    System.Diagnostics.Debug.WriteLine(error_str);
                }
            }
            return key;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            Close();
        }
    }

    [DataContract]
    public class Authentication_Info
    {
        [DataMember]
        public string token_type;
        [DataMember]
        public int expires_in;
        [DataMember]
        public string access_token;
        [DataMember]
        public string refresh_token;
    }

    public class AuthKeys
    {
        public string access_token;
        public string refresh_token;
    }
}
