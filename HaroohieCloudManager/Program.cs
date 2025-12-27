using Mono.Options;

namespace HaroohieCloudManager;

public class Program
{
    static void Main(string[] args)
    {
        CommandSet commands = new("HaroohieCloudManager")
        {
            new CheckCorsProxyCommand(),
            new DownloadRomCommand(),
            new GenerateBuildMatrixCommand(),
            new UpdateWeblateCommand(),
            new UploadPatchesCommand(),
        };
        commands.Run(args);
    }
}