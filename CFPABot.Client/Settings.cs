using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GammaLibrary;

namespace CFPABot.Client
{
    [ConfigurationPath("cfpabot-client.json")]
    internal class Settings : Configuration<Settings>
    {
        public StorageLocation StorageLocationEnum { get; set; } = CFPABot.Client.StorageLocation.Appdata;

        [JsonIgnore]
        public string StorageLocation => StorageLocationEnum == Client.StorageLocation.Appdata
            ? Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "CFPABotClient")
            : "CFPABotClient";

        public string Email { get; set; }
        public string Token { get; set; }

        public bool UseProxy { get; set; } = false;
        public string Proxy { get; set; } = "127.0.0.1:1080";
        public Dictionary<string, string> PRCache { get; set; } = new();
    }

    enum StorageLocation
    {
        None,
        CurrentDirectory,
        Appdata
    }
}
