using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;

namespace HaroohieCloudManager;

public class UpdateWeblateCommand : Command
{
    private string _apiKey = string.Empty, _projectUri = string.Empty;
    
    public UpdateWeblateCommand() : base("update-weblate", "Commits and pushes a Weblate project")
    {
        Options = new()
        {
            { "k|api-key=", "The Weblate API key", k => _apiKey = k },
            { "p|project-uri=", "The Weblate project URI (e.g. https://weblate.uri.com/api/projects/project-name", p => _projectUri = p },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        return InvokeAsync(arguments).GetAwaiter().GetResult();
    }

    public async Task<int> InvokeAsync(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);
        
        HttpClient client = new() { BaseAddress = new(_projectUri) };
        client.DefaultRequestHeaders.Authorization = new("Token", _apiKey);
        try
        {
            HttpContent commitContent = new StringContent("{ \"operation\": \"commit\" }", Encoding.UTF8,
                new MediaTypeHeaderValue("application/json"));
            HttpResponseMessage commitResponse = await client.PostAsync(new Uri("repository"), commitContent);
            commitResponse.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            CommandSet.Out.WriteLine($"Failed to commit Weblate ({ex.StatusCode}): {ex.Message})");
        }

        try
        {
            HttpContent pushContent = new StringContent("{ \"operation\": \"push\" }", Encoding.UTF8,
                new MediaTypeHeaderValue("application/json"));
            HttpResponseMessage pushResponse = await client.PutAsync(new Uri("repository"), pushContent);
            pushResponse.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            CommandSet.Out.WriteLine($"Failed to push Weblate ({ex.StatusCode}): {ex.Message})");
        }
        
        CommandSet.Out.WriteLine("Successfully updated Weblate repository.");
        
        return 0;
    }
}