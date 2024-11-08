using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Discord;
using Discord.Webhook;
using Mono.Options;

namespace HaroohieCloudManager;

public class UploadPatchesCommand : Command
{
    private string _spacesKey = string.Empty, _spacesSecret = string.Empty, _spacesUrl = string.Empty, _spacesName = string.Empty, _language = string.Empty, _game = string.Empty, _version = string.Empty, _webhookUri = string.Empty;
    private string[] _patchesList = [];
    
    public UploadPatchesCommand() : base("upload-patches", "Uplaods patches to Digital Ocean storage and optionally posts them to Discord")
    {
        Options = new()
        {
            { "k|key=", "The Digital Ocean Spaces key", k => _spacesKey = k },
            { "s|secret=", "The Digital Ocean Spaces secret", s => _spacesSecret = s },
            { "u|url=", "The Digital Ocean Spaces URL", u => _spacesUrl = u },
            { "n|name=", "The Digital Ocean Spaces name", n => _spacesName = n },
            { "g|game=", "The ID of the game the patch should be uploaded for", g => _game = g },
            { "v|version=", "The patch version being uploaded", v => _version = v },
            { "l|language=", "The language of the current patch set", l => _language = l },
            { "p|patches=", "A semicolon-separated list of patch files to upload (in the form of \"title:path\")", p => _patchesList = p.Split(';') },
            { "w|webhook=", "Discord webhook URI to post patches to", w => _webhookUri = w },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        return InvokeAsync(arguments).GetAwaiter().GetResult();
    }

    public async Task<int> InvokeAsync(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        AmazonS3Config config = new() { ServiceURL = _spacesUrl };
        AmazonS3Client client = new(_spacesKey, _spacesSecret, config);

        List<Patch> patches = [];
        foreach (string patch in _patchesList)
        {
            string[] patchParts = patch.Split(':');
            string title = patchParts[0];
            string path = patchParts[1];
            string key = $"patches/{_game}/{Path.GetFileName(path)}";
            
            PutObjectRequest patchRequest = new() { BucketName = _spacesName, Key = key, FilePath = path };
            await client.PutObjectAsync(patchRequest);
            
            GetPreSignedUrlRequest patchUrlRequest = new() { BucketName = _spacesName, Key = key, Expires = DateTimeOffset.UtcNow.AddMonths(6).DateTime };
            string url = await client.GetPreSignedURLAsync(patchUrlRequest);
            patches.Add(new(title, url));
        }

        DiscordWebhookClient discordClient = new(_webhookUri);
        EmbedBuilder embedBuilder = new();
        embedBuilder.WithTitle($"Nightly {_language} Patches")
            .WithAuthor($"{_language} - {_version}")
            .WithDescription($"A new set of {_language} patches are available for testing!")
            .WithFields(patches.Select(p =>
                new EmbedFieldBuilder()
                    .WithName(p.Title)
                    .WithValue($"[Download]({p.Url})")
            ));
        await discordClient.SendMessageAsync(embeds: [embedBuilder.Build()]);
        
        return 0;
    }

    private struct Patch(string title, string url)
    {
        public string Title { get; set; } = title;
        public string Url { get; set; } = url;
    }
}