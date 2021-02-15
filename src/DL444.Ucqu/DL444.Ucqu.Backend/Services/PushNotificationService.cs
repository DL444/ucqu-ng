using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend.Services
{
    public interface IPushNotificationService<T>
    {
        PushPlatform Platform { get; }
        Task SendNotificationAsync(string username, Func<T> notification, ILogger log);
    }

    public enum PushPlatform
    {
        Apns,
        Wns
    }

    internal class WindowsPushNotificationService : IPushNotificationService<WindowsPushNotification>
    {
        public WindowsPushNotificationService(HttpClient httpClient, IPushDataAccessService dataService, IConfiguration config)
        {
            this.httpClient = httpClient;
            this.dataService = dataService;
            packageId = config.GetValue<string>("Notification:Windows:PackageSid");
            secret = config.GetValue<string>("Notification:Windows:Secret");
        }

        public PushPlatform Platform => PushPlatform.Wns;

        public async Task SendNotificationAsync(string username, Func<WindowsPushNotification> notificationFunc, ILogger log)
        {
            Task<string?> tokenTask = GetTokenAsync(log);
            DataAccessResult<NotificationChannelCollection> channelResult = await dataService.GetPushChannelsAsync(username, Platform);
            if (channelResult.StatusCode == 404)
            {
                return;
            }
            else if (!channelResult.Success)
            {
                log.LogError("Failed to fetch WNS channels from databse. Username: {username}", username);
            }
            string? token = await tokenTask;
            if (token == null)
            {
                return;
            }

            List<string> channelsToRemove = new List<string>();
            for (int i = 0; i < channelResult.Resource.Channels.Count; i++)
            {
                string channel = channelResult.Resource.Channels[i].ChannelIdentifier;
                if (!new Uri(channel).Host.EndsWith(validChannelHost, StringComparison.Ordinal))
                {
                    log.LogWarning("Invalid channel found. Channel URI: {channelUri}", channel);
                    channelsToRemove.Add(channel);
                    continue;
                }

                WindowsPushNotification notification = notificationFunc();
                if (notification.Type == WindowsNotificationType.Raw)
                {
                    throw new NotSupportedException($"Notification type {notification.Type} is not currently supported.");
                }
                HttpRequestMessage request = CreateRequest(token, channel, notification);
                HttpResponseMessage response = await httpClient.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    token = await RenewTokenAsync(log);
                    if (token == null)
                    {
                        return;
                    }
                    i--;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    channelsToRemove.Add(channel);
                }
                else if (!response.IsSuccessStatusCode)
                {
                    log.LogWarning("Failed to send notification to WNS server. Username: {username}", username);
                }
            }
        }

        private HttpRequestMessage CreateRequest(string token, string channel, WindowsPushNotification notification)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, channel);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            string typeHeader = $"wns/{notification.Type.ToString().ToLowerInvariant()}";
            request.Headers.Add("X-WNS-Type", typeHeader);
            request.Content = new StringContent(notification.Payload, Encoding.UTF8, "text/xml");
            return request;
        }
        private async Task<string?> GetTokenAsync(ILogger log)
        {
            DataAccessResult<PushAccessToken> result = await dataService.GetPushAccessTokenAsync(Platform);
            if (result.Success)
            {
                return result.Resource.Token;
            }
            else if (result.StatusCode == 404)
            {
                return await RenewTokenAsync(log);
            }
            else
            {
                log.LogWarning("Unable to fetch WNS token from database. Status code: {statusCode}", result.StatusCode);
                return null;
            }
        }
        private async Task<string?> RenewTokenAsync(ILogger log)
        {
            if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(secret))
            {
                return null;
            }

            Dictionary<string, string> content = new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" },
                { "client_id", packageId },
                { "client_secret", secret },
                { "scope", tokenRequestScope }
            };
            HttpResponseMessage response = await httpClient.PostAsync(tokenRequestUri, new FormUrlEncodedContent(content));
            if (!response.IsSuccessStatusCode)
            {
                log.LogError("WNS service returned error status while renewing tokens. Status code: {statusCode}", response.StatusCode);
                return null;
            }
            try
            {
                PushAccessToken token = await JsonSerializer.DeserializeAsync<PushAccessToken>(await response.Content.ReadAsStreamAsync());
                token.Platform = PushPlatform.Wns;
                DataAccessResult updateResult = await dataService.SetPushAccessTokenAsync(token);
                if (!updateResult.Success)
                {
                    log.LogWarning("Unable to write new WNS token to database. Status code: {statusCode}", updateResult.StatusCode);
                }
                return token.Token;
            }
            catch (JsonException ex)
            {
                log.LogError(ex, $"WNS service returned unexpected response content.");
                return null;
            }
        }

        private readonly HttpClient httpClient;
        private readonly IPushDataAccessService dataService;
        private readonly string tokenRequestUri = "https://login.live.com/accesstoken.srf";
        private readonly string tokenRequestScope = "notify.windows.com";
        private readonly string validChannelHost = "notify.windows.com";
        private readonly string packageId;
        private readonly string secret;
    }
}
