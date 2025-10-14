using System;
using System.IO;

namespace GitOut.Features.Settings
{
    public static class SettingsOptions
    {
        public static string GetSettingsPath() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gitout",
                "config.json"
            );
    }
}
