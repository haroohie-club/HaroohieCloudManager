using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon.S3;
using Mono.Options;

namespace HaroohieCloudManager;

public class DownloadRomCommand : Command
{
    private string _spacesKey = string.Empty, _spacesSecret = string.Empty, _spacesUrl = string.Empty, _spacesName = string.Empty, _romKey = string.Empty, _romPath = string.Empty;
    
    public DownloadRomCommand() : base("download-rom", "Downloads a ROM from Digital Ocean storage")
    {
        Options = new()
        {
            { "k|key=", "The Digital Ocean Spaces key", k => _spacesKey = k },
            { "s|secret=", "The Digital Ocean Spaces secret", s => _spacesSecret = s },
            { "u|url=", "The Digital Ocean Spaces URL", u => _spacesUrl = u },
            { "n|name=", "The Digital Ocean Spaces name", n => _spacesName = n },
            { "r|rom=", "The key of the ROM in Digital Oceans Spaces", r => _romKey = r },
            { "p|path=", "The path to write the ROM to on disk", p => _romPath = p },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        return InvokeAsync(arguments).GetAwaiter().GetResult();
    }

    private async Task<int> InvokeAsync(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);
        
        AmazonS3Config config = new() { ServiceURL = _spacesUrl };
        AmazonS3Client client = new(_spacesKey, _spacesSecret, config);

        await (await client.GetObjectAsync(_spacesName, _romKey))
            .WriteResponseStreamToFileAsync(_romPath, append: false, new());

        if (Path.GetExtension(_romPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            string romName = string.Empty;
            using FileStream zipStream = File.OpenRead(_romPath);
            {
                using ZipArchive zip = new(zipStream);
                romName = zip.Entries[0].Name;
                zip.ExtractToDirectory(Path.GetDirectoryName(_romPath)!);
            }
            File.Delete(_romPath);
            _romPath = Path.Combine(Path.GetDirectoryName(_romPath)!, romName);
        }
        
        using FileStream romStream = File.OpenRead(_romPath);
        CommandSet.Out.WriteLine($"Original ROM MD5 Hash: {string.Join("", MD5.HashData(romStream).Select(b => $"{b:X2}"))}");
        
        return 0;
    }
}