using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GitOut.Features.Settings;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Storage
{
    public class FileStorage : IStorage
    {
        private readonly IOptions<SettingsOptions> options;

        public FileStorage(IOptions<SettingsOptions> options) => this.options = options;

        public T? Get<T>(string key) where T : class
        {
            if (options.Value.SettingsFolder == null)
            {
                throw new InvalidOperationException("Settings folder is not set");
            }
            string configFile = Path.Combine(options.Value.SettingsFolder, "config.json");

            try
            {
                string text = File.ReadAllText(configFile);
                IDictionary<string, JsonElement> sections = JsonSerializer.Deserialize<IDictionary<string, JsonElement>>(text);
                if (sections.TryGetValue(key, out JsonElement section))
                {
                    var json = new ArrayBufferWriter<byte>();
                    using (var writer = new Utf8JsonWriter(json))
                    {
                        section.WriteTo(writer);
                    }
                    return JsonSerializer.Deserialize<T>(json.WrittenSpan);
                }
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            return default;
        }

        public void Set(string key, object value)
        {
            if (options.Value.SettingsFolder == null)
            {
                throw new InvalidOperationException("Settings folder is not set");
            }
            string configFile = Path.Combine(options.Value.SettingsFolder, "config.json");

            IDictionary<string, object> sections;
            try
            {
                string text = File.ReadAllText(configFile);
                sections = JsonSerializer.Deserialize<IDictionary<string, object>>(text);
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

            string data = JsonSerializer.Serialize(sections, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(options.Value.SettingsFolder);
            File.WriteAllText(configFile, data);
        }
    }
}
