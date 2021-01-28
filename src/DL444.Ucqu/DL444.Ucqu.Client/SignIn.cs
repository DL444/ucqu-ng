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
        /// <summary>
        /// Sign a user in.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="passwordHash">The hashed password.</param>
        /// <returns>Sign in result.</returns>
        public async Task<SignInContext> SignInAsync(string username, string passwordHash)
        {
            string sessionId = GetRandomSessionId();
            HttpRequestMessage initRequest = new HttpRequestMessage(HttpMethod.Get, "_data/index_login.aspx").AddSessionCookie(sessionId);
            HttpResponseMessage response = await httpClient.SendAsync(initRequest);
            string responseString = await response.Content.ReadAsStringAsync();
            string? viewState = GetSignInPageProperty(responseString, "__VIEWSTATE");
            string? viewStateGen = GetSignInPageProperty(responseString, "__VIEWSTATEGENERATOR");
            if (string.IsNullOrEmpty(viewState) || string.IsNullOrEmpty(viewStateGen))
            {
                throw new FormatException("Unable to extract view state parameters.");
            }
            var signInRequest = new HttpRequestMessage(HttpMethod.Post, "_data/index_login.aspx").AddSessionCookie(sessionId);
            Dictionary<string, string> content = new Dictionary<string, string>()
            {
                { "__VIEWSTATE", viewState },
                { "__VIEWSTATEGENERATOR", viewStateGen },
                { "Sel_Type", "STU" },
                { "txt_dsdsdsdjkjkjc", username },
                { "efdfdfuuyyuuckjg", passwordHash }
            };
            signInRequest.Content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string?, string?>>)content);
            try
            {
                response = await httpClient.SendAsync(signInRequest);
            }
            catch (HttpRequestException)
            {
                // Sometimes upstream server returns empty response. Retry for once.
                await Task.Delay(2000);
                signInRequest = new HttpRequestMessage(HttpMethod.Post, "_data/index_login.aspx").AddSessionCookie(sessionId);
                signInRequest.Content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string?, string?>>)content);
                response = await httpClient.SendAsync(signInRequest);
            }
            responseString = await response.Content.ReadAsStringAsync();
            if (responseString.Contains("您尚未报到注册成功，请到学院咨询并办理相关手续！", StringComparison.Ordinal))
            {
                return new SignInContext(SignInResult.NotRegistered, null, null);
            }
            else if (responseString.Contains("账号或密码不正确！请重新输入", StringComparison.Ordinal))
            {
                return new SignInContext(SignInResult.InvalidCredentials, null, null);
            }
            else
            {
                return new SignInContext(SignInResult.Success, sessionId, username);
            }
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
    }
}
