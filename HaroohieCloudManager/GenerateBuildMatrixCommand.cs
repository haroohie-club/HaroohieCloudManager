using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using Mono.Options;

namespace HaroohieCloudManager;

public class GenerateBuildMatrixCommand : Command
{
    private string _pat = string.Empty, _commitsDir = string.Empty;
    private string[] _games = [];
    private string[][] _langCodes = [];
    private string[][] _extraProperties = [];
    
    public GenerateBuildMatrixCommand() : base("generate-build-matrix", "Generates a build matrix for an Azure pipelines build")
    {
        Options = new()
        {
            { "g|games=", "Comma-separated list of games to generate matrices for", g => _games = g.Split(',') },
            { "l|langcodes=", "Comma-separated list of available language codes for each game -- games are delimited by underscores",
                l => _langCodes = l.Split('_').Select(g => g.Split(',')).ToArray() },
            {
                "x|extra-props=", "Comma-separated list of extra properties for each language code (name value delimited by colons) -- games are delimited by underscores",
                x => _extraProperties = x.Split('_').Select(g => g.Split(',')).ToArray()
            },
            { "p|pat|github-pat=", "GitHub PAT for cloning private repos", p => _pat = p },
            { "c|commits-dir=", "Directory with most recent checked commit hashes", c => _commitsDir = c },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        CloneOptions options = new()
        {
            FetchOptions =
            {
                CredentialsProvider = (_, _, _) => new UsernamePasswordCredentials()
                    { Username = "gh", Password = _pat },
            },
        };
        
        for (int i = 0; i < _games.Length; i++)
        {
            string game = _games[i];
            string[] langs = _langCodes[i];
            string[] props = _extraProperties[i];
            
            string assetsDir = $"{game}TranslationAssets",
                buildDir = $"{game}TranslationBuild",
                stringsDir = $"{game}TranslationStrings",
                utilityDir = $"{game}TranslationUtility";

            Dictionary<string, bool> assetsChecks = CheckRepo(assetsDir, langs, options);
            Dictionary<string, bool> buildChecks = CheckRepo(buildDir, [], options);
            Dictionary<string, bool> stringsChecks = CheckRepo(stringsDir, langs, options);
            Dictionary<string, bool> utilityChecks = CheckRepo(utilityDir, [], options);

            StringBuilder sb = new();
            sb.Append("{ \"language\": [ ");
            bool wrote = false;
            for (int j = 0; j < langs.Length; j++)
            {
                string lang = langs[j];
                if (assetsChecks[lang] || buildChecks["all"] || stringsChecks[lang] || utilityChecks["all"])
                {
                    wrote = true;
                    sb.Append($" {{ \"code\": \"{lang}\", ");
                    foreach (string prop in props)
                    {
                        string[] propSplit = prop.Split('-');
                        sb.Append($"\"{propSplit[0]}\": {propSplit[j + 1]}, ");
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(" }, ");
                }
            }

            if (wrote)
            {
                sb.Remove(sb.Length - 2, 2);
            }
            sb.Append(" ] }");
            
            File.WriteAllText($"{game}-matrix.json", sb.ToString());
            Console.WriteLine($"{game}: {sb}\n\n");
        }
        
        return 0;
    }

    private Dictionary<string, bool> CheckRepo(string dir, string[] langs, CloneOptions options)
    {
        using Repository repo = new(Repository.Clone($"https://github.com/haroohie-club/{dir}.git", dir, options));
        string? prevBuildCommitSha = File.Exists(Path.Combine(_commitsDir, $"{dir}.txt"))
            ? File.ReadAllText(Path.Combine(_commitsDir, $"{dir}.txt"))
            : null; 
        string? latestAssetCommitSha = repo.Commits.FirstOrDefault()?.Sha;
        File.WriteAllText(Path.Combine(_commitsDir, $"{dir}.txt"), latestAssetCommitSha);

        if (langs.Length == 0 && prevBuildCommitSha != latestAssetCommitSha)
        {
            return new() { { "all", true } };
        }
        else if (langs.Length == 0)
        {
            return new() { { "all", false } };
        }

        Dictionary<string, bool> checks = [];

        foreach (string lang in langs)
        {
            if (string.IsNullOrEmpty(prevBuildCommitSha))
            {
                checks.Add(lang, true);
            }
            else
            {
                checks.Add(lang, repo.Diff
                    .Compare<Patch>(repo.Commits.First(c => c.Sha.Equals(prevBuildCommitSha)).Tree,
                        repo.Commits.First().Tree).Select(d => d.Path).Any(f =>
                        f.Contains($".{lang}.", StringComparison.OrdinalIgnoreCase) ||
                        f.Contains($"/{lang}/")));
            }
        }
        
        return checks;
    }
}