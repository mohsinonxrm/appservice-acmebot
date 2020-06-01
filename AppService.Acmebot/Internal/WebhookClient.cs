﻿using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace AppService.Acmebot.Internal
{
    public class WebhookClient
    {
        public WebhookClient(IHttpClientFactory httpClientFactory, IOptions<AcmebotOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AcmebotOptions _options;

        public Task SendCompletedEventAsync(string appName, string slotName, DateTime? expirationDate, string[] dnsNames)
        {
            object model;

            if (_options.Webhook.Contains("hooks.slack.com"))
            {
                model = new
                {
                    username = "Acmebot",
                    attachments = new[]
                    {
                        new
                        {
                            text = "A new certificate has been issued.",
                            color = "good",
                            fields = new object[]
                            {
                                new
                                {
                                    title = "App Name",
                                    value= appName,
                                    @short = true
                                },
                                new
                                {
                                    title = "Slot Name",
                                    value = slotName,
                                    @short = true
                                },
                                new
                                {
                                    title = "Expiration Date",
                                    value = expirationDate
                                },
                                new
                                {
                                    title = "DNS Names",
                                    value = string.Join("\n", dnsNames)
                                }
                            }
                        }
                    }
                };
            }
            else if (_options.Webhook.Contains("outlook.office.com"))
            {
                model = new
                {
                    title = $"{appName} - {slotName}",
                    text = string.Join("\n", dnsNames),
                    themeColor = "2EB886"
                };
            }
            else
            {
                model = new
                {
                    appName,
                    slotName,
                    dnsNames
                };
            }

            return SendEventAsync(model);
        }

        public Task SendFailedEventAsync(string functionName, string reason)
        {
            object model;

            if (_options.Webhook.Contains("hooks.slack.com"))
            {
                model = new
                {
                    attachments = new[]
                    {
                        new
                        {
                            title = functionName,
                            text = reason,
                            color = "danger"
                        }
                    }
                };
            }
            else if (_options.Webhook.Contains("outlook.office.com"))
            {
                model = new
                {
                    title = functionName,
                    text = reason,
                    themeColor = "A30200"
                };
            }
            else
            {
                model = new
                {
                    functionName,
                    reason
                };
            }

            return SendEventAsync(model);
        }

        private async Task SendEventAsync(object model)
        {
            if (string.IsNullOrEmpty(_options.Webhook))
            {
                return;
            }

            var httpClient = _httpClientFactory.CreateClient();

            await httpClient.PostAsJsonAsync(_options.Webhook, model);
        }
    }
}
