using SteamDatabase.ValvePak;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;
using System.CommandLine;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

using MapPlaces = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Collections.Generic.List<float>>>;


public class Program
{
    static void Main(string[] args)
    {
        var inputArgument = new Argument<DirectoryInfo?>(name: "path", description: "directory containing map vpk files", getDefaultValue: GetDefaultInputDir);
        var outputOption = new Option<DirectoryInfo>(aliases: ["-o", "--output"], description: "output directory", getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()));
        var mergeOption = new Option<bool>(aliases: ["-m", "--merge"], description: "merge into one json file");
        var filterOption = new Option<string>(aliases: ["-f", "--filter"], description: "regex filter", getDefaultValue: () => @"^(ar|cs|de)((?!vanity).)*\.vpk$");
        var prettyOption = new Option<bool>(aliases: ["-p", "--pretty"], description: "indent output json");

        var rootCommand = new RootCommand("Extract placenames from CS2 map vpk files");

        rootCommand.AddArgument(inputArgument);
        rootCommand.AddOption(outputOption);
        rootCommand.AddOption(mergeOption);
        rootCommand.AddOption(filterOption);
        rootCommand.AddOption(prettyOption);
        rootCommand.SetHandler(RootHandler, inputArgument, outputOption, mergeOption, filterOption, prettyOption);

        rootCommand.Invoke(args);
    }

    static DirectoryInfo? GetDefaultInputDir()
    {
        DirectoryInfo guess = new DirectoryInfo(@"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\csgo\maps");
        return guess.Exists ? guess : null;
    }

    static void RootHandler(DirectoryInfo? input, DirectoryInfo output, bool merge, string regexPattern, bool pretty)
    {
        if (input == null)
        {
            Console.WriteLine("No input folder provided. --help for help");
            return;
        }

        if (!output.Exists)
        {
            Console.WriteLine("Output folder does not exist.");
            return;
        }

        FileInfo[] files;
        Regex filter = new Regex(regexPattern);

        MapPlaces? mp;
        Dictionary<string, MapPlaces> result = new Dictionary<string, MapPlaces>();

        try
        {
            files = input.GetFiles();
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("That input directory does not exist.");
            return;
        }

        foreach (FileInfo file in files)
        {
            string name = file.Name;

            if (!filter.IsMatch(name))
            {
                Console.WriteLine("Filtered: " + name);
                continue;
            }

            Console.WriteLine("Processing: " + name);
            mp = ProcessVpk(file);

            if (mp == null || mp.Count == 0)
            {
                Console.WriteLine("Skipping " + name + ", no vpk or no ents");
                continue;
            }

            Console.WriteLine("Found " + mp.Count + " placenames");
            result[name.Substring(0, name.Length - 4)] = mp;
        }

        Formatting formatting = pretty ? Formatting.Indented : Formatting.None;

        if (merge)
        {
            FileInfo outputFile = new FileInfo(output + "/merged.json");
            string serialized = JsonConvert.SerializeObject(result, formatting);
            File.WriteAllText(outputFile.FullName, serialized);
            Console.WriteLine("Written merged file to " + outputFile.FullName);
        }
        else
        {
            foreach (var item in result)
            {
                FileInfo outputFile = new FileInfo(output + "/" + item.Key + ".json");
                string serialized = JsonConvert.SerializeObject(item.Value, formatting);
                File.WriteAllText(outputFile.FullName, serialized);
                Console.WriteLine("Written map file to " + outputFile.FullName);
            }
        }

        Console.WriteLine("Done!");
    }

    static MapPlaces? ProcessVpk(FileInfo vpkFile)
    {
        var package = new Package();
        package.Read(vpkFile.FullName);

        foreach (var item in package.Entries)
        {
            if (item.Key == "vents_c")
                return HandleVents(package, item.Value[0]);
        }

        Console.WriteLine("Could not find 'vents_c' package entry");
        return null;
    }

    static MapPlaces? HandleVents(Package package, PackageEntry entry)
    {

        MapPlaces mp = new MapPlaces();
        List<float> vec;

        package.ReadEntry(entry, out var rawFile);
        var ms = new MemoryStream(rawFile);

        var resource = new Resource();
        resource.Read(ms);

        EntityLump el = (EntityLump)resource.DataBlock;

        foreach (EntityLump.Entity item in el.GetEntities())
        {
            string classname = (string)item.GetProperty("classname").Value;

            if (classname != "env_cs_place")
                continue;

            string place_name = (string)item.GetProperty("place_name").Value;
            string origin = (string)item.GetProperty("origin").Value;

            string[] components = origin.Split(" ");

            if (float.TryParse(components[0], out float x) &&
                float.TryParse(components[1], out float y) &&
                float.TryParse(components[2], out float z))
            {
                vec = new List<float> { x, y, z };
            }
            else
            {
                throw new Exception("Failed to parse vector components: " + origin);
            }

            if (mp.ContainsKey(place_name))
                mp[place_name].Add(vec);
            else
                mp[place_name] = new List<List<float>>() { vec };
        }

        return mp;
    }
}
