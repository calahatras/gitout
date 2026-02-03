using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GitOut.Features.Settings;

namespace GitOut.Features.Storage;

public class FileStorage : IWritableStorage
{
    public void Write(string key, object value)
    {
        string configFile = SettingsOptions.GetSettingsPath();
        if (Directory.GetParent(configFile) is not DirectoryInfo existing)
        {
            throw new InvalidOperationException($"Could not get parent path of {configFile}");
        }
        Directory.CreateDirectory(existing.FullName);

        IDictionary<string, object> sections;
        try
        {
            string text = File.ReadAllText(configFile);
            sections = JsonSerializer.Deserialize<IDictionary<string, object>>(text)!;
        }
        catch (IOException)
        {
            sections = new Dictionary<string, object>();
        }
        if (sections.ContainsKey(key))
        {
            sections[key] = value;
        }
        else
        {
            sections.Add(key, value);
        }

        string data = JsonSerializer.Serialize(
            sections,
            new JsonSerializerOptions { WriteIndented = true }
        );
        File.WriteAllText(configFile, data);
    }
}
