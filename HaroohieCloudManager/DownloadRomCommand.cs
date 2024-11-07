using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Mono.Options;

namespace HaroohieCloudManager;

public class DownloadRomCommand : Command
{
    private string _spacesKey, _spacesSecret, _spacesUrl, _spacesName, _romKey, _romPath;
    
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
        
        return 0;
    }
}