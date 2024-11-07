using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using Mono.Options;

namespace HaroohieCloudManager;

public class CheckCorsProxyCommand : Command
{
    private string _corsOrigin, _corsUri, _discordWebhookUri;
    
    public CheckCorsProxyCommand() : base("check-cors-proxy", "Checks to see if the CORS proxy is up and posts notif to Discord if not")
    {
        Options = new()
        {
            { "o|origin|cors-origin=", "Origin of the CORS proxy", o => _corsOrigin = o },
            { "c|uri|cors-uri=", "URI to download using the CORS proxy", c => _corsUri = c },
            { "d|w|discord|webhook=", "Discord webhook URI to send failed reports to (optional)", d => _discordWebhookUri = d },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        return InvokeAsync(arguments).GetAwaiter().GetResult();
    }

    private async Task<int> InvokeAsync(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);
        
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("Origin", _corsOrigin);
        try
        {
            HttpResponseMessage httpResponse = await client.GetAsync(new Uri(_corsUri));
            httpResponse.EnsureSuccessStatusCode();
            CommandSet.Out.WriteLine("CORS proxy check succeeded!");
        }
        catch (HttpRequestException e)
        {
            CommandSet.Out.WriteLine($"CORS Proxy Error ({e.StatusCode}: {e.Message}");
            if (!string.IsNullOrEmpty(_discordWebhookUri))
            {
                DiscordWebhookClient discordWebhook = new(_discordWebhookUri);
                await discordWebhook.SendMessageAsync(embeds:
                [
                    new EmbedBuilder().WithTitle("CORS Proxy Down!")
                        .WithDescription("Attempting to download the patch from the CORS proxy failed!")
                        .WithAuthor("CORS Proxy Checker")
                        .Build()
                ]);
            }
        }
        
        return 0;
    }
}