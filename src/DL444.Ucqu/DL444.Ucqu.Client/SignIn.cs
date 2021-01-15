using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DL444.Ucqu.Models;

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
        public async Task<SignInResult> SignInAsync(string username, string passwordHash)
        {
            string sessionId = GetRandomSessionId();
            cookieContainer.SetSessionId(sessionId);

            HttpResponseMessage response = await httpClient.GetAsync("_data/index_login.aspx");
            string responseString = await response.Content.ReadAsStringAsync();
            string? viewState = GetSignInPageProperty(responseString, "__VIEWSTATE");
            string? viewStateGen = GetSignInPageProperty(responseString, "__VIEWSTATEGENERATOR");
            if (string.IsNullOrEmpty(viewState) || string.IsNullOrEmpty(viewStateGen))
            {
                throw new FormatException("Unable to extract view state parameters.");
            }
            var signInRequest = new HttpRequestMessage(HttpMethod.Post, "_data/index_login.aspx");
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
            if (responseString.Contains("您尚未报到注册成功，请到学院咨询并办理相关手续！", StringComparison.Ordinal))
            {
                return SignInResult.NotRegistered;
            }
            else if (responseString.Contains("账号或密码不正确！请重新输入", StringComparison.Ordinal))
            {
                return SignInResult.InvalidCredentials;
            }
            else
            {
                signedInUser = username;
                return SignInResult.Success;
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

        private string signedInUser = string.Empty;
    }
}
