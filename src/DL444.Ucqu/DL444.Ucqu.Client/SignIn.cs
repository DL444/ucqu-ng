using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DL444.Ucqu.Client
{
    public partial class UcquClient
    {
        public async Task<SignInResult> SignInAsync(string username, string passwordHash)
        {
            string sessionId = GetRandomSessionId();
            cookieContainer.SetSessionId(sessionId);

            HttpRequestMessage initRequest = CreateSignInRequest(HttpMethod.Get);
            try
            {
                HttpResponseMessage response = await httpClient.SendAsync(initRequest);
                string responseString = await response.Content.ReadAsStringAsync();
                string? viewState = GetSignInPageProperty(responseString, "__VIEWSTATE");
                string? viewStateGen = GetSignInPageProperty(responseString, "__VIEWSTATEGENERATOR");
                if (string.IsNullOrEmpty(viewState) || string.IsNullOrEmpty(viewStateGen))
                {
                    return new SignInResult(false, "应用或教务系统发生未知异常");
                }
                HttpRequestMessage signInRequest = CreateSignInRequest(HttpMethod.Post);
                Dictionary<string, string> content = new Dictionary<string, string>()
                {
                    { "__VIEWSTATE", viewState },
                    { "__VIEWSTATEGENERATOR", viewStateGen },
                    { "Sel_Type", "STU" },
                    { "txt_dsdsdsdjkjkjc", username },
                    { "efdfdfuuyyuuckjg", passwordHash }
                };
                signInRequest.Content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string?, string?>>)content);
                response = await httpClient.SendAsync(signInRequest);
                responseString = await response.Content.ReadAsStringAsync();
                if (responseString.Contains("您尚未报到注册成功，请到学院咨询并办理相关手续！"))
                {
                    return new SignInResult(false, "本学期教务系统暂未开放，将展示最近的缓存信息");
                }
                else if (responseString.StartsWith("", StringComparison.OrdinalIgnoreCase))
                {
                    signedInUser = username;
                    return new SignInResult(true, "登录成功");
                }
                else
                {
                    return new SignInResult(false, "帐户信息不正确");
                }
            }
            catch (HttpRequestException)
            {
                return new SignInResult(false, "教务系统发生网络异常");
            }
            catch (TaskCanceledException)
            {
                return new SignInResult(false, "教务系统发生网络异常");
            }

        }

        public string GetPasswordHash(string username, string password)
        {
            MD5 md5 = MD5.Create();
            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            byte[] hash = md5.ComputeHash(passwordBytes);
            StringBuilder builder = new StringBuilder(username.Length + 30 + schoolCode.Length);
            builder.Append(username);
            for (int i = 0; i < 15; i++)
            {
                builder.Append(hash[i].ToString("X2"));
            }
            builder.Append(schoolCode);
            string hashStr = builder.ToString();
            passwordBytes = Encoding.ASCII.GetBytes(hashStr);
            hash = md5.ComputeHash(passwordBytes);
            builder = new StringBuilder(30);
            for (int i = 0; i < 15; i++)
            {
                builder.Append(hash[i].ToString("X2"));
            }
            return builder.ToString();
        }

        private string GetRandomSessionId()
        {
            StringBuilder builder = new StringBuilder(24);
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[24];
            rng.GetBytes(randomBytes);
            foreach (var b in randomBytes)
            {
                int transformed = b % 52 + 65;
                if (transformed > 90)
                {
                    transformed += 6;
                }
                builder.Append((char)transformed);
            }
            return builder.ToString();
        }

        private HttpRequestMessage CreateSignInRequest(HttpMethod method)
        {
            string signInUri = "_data/index_login.aspx";
            HttpRequestMessage request = new HttpRequestMessage(method, signInUri);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.Referrer = new Uri($"http://{host}/home.aspx");
            return request;
        }

        private string? GetSignInPageProperty(string page, string name)
        {
            Regex regex = new Regex($"name=\"{name}\" value=\"(.*)\"", RegexOptions.CultureInvariant);
            Match match = regex.Match(page);
            if (!match.Success)
            {
                return null;
            }
            return match.FirstGroupValue();
        }

        private string signedInUser = string.Empty;
    }

    public class SignInResult
    {
        public SignInResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
