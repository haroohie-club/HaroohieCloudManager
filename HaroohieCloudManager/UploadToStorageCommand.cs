using Mono.Options;

namespace HaroohieCloudManager;

public class UploadToStorageCommand : Command
{
    public UploadToStorageCommand() : base("upload-to-storage", "Uplaods files to Digital Ocean storage and optionally posts them to Discord")
    {
        
    }
}