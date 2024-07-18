using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class Config
{
    private static string CONFIG_PATH = "config.settings";
    private static string CONFIG_OLD_PATH = "config_old.settings";
    private static ConfigurationFile _configFile = null;
    public static ConfigurationFile ConfigFile
    {
        get
        {
            if( _configFile == null)
                _configFile = GetOrGenerateConfig();
            return _configFile; 
        }
    }

    public static void SaveConfigFile()
    {
        // serialize and write all changes
        File.WriteAllText(CONFIG_PATH, JsonSerializer.Serialize(ConfigFile));
    }

    private static ConfigurationFile GetOrGenerateConfig()
    {
        // does it exist?
        if( File.Exists(CONFIG_PATH) )
        {
            // read in existing file
            try
            {
                var existing = JsonSerializer.Deserialize<ConfigurationFile>(File.ReadAllText(CONFIG_PATH));
                if (existing != null) return existing;

                // existing file is null, corrupt config file?
                File.Copy(CONFIG_OLD_PATH, CONFIG_PATH);
            }
            catch(Exception)
            {
                // failed to parse config, back it up
                File.Copy(CONFIG_OLD_PATH, CONFIG_PATH);
            }
        }

        // file either doesnt exist, or was corrupted, generate new
        var config = new ConfigurationFile();
        File.WriteAllText(CONFIG_PATH, JsonSerializer.Serialize(config));
        return config;
    }

    [Serializable]
    public class ConfigurationFile
    {
        [JsonInclude]
        public float MasterVol { get; set; }
        [JsonInclude]
        public float MusicVol { get; set; }
        [JsonInclude]
        public float UIVol { get; set; }
     
        public ConfigurationFile()
        {
            // some defaults
            MasterVol = 0.5f;
            MusicVol = 0.5f;
            UIVol = 0.5f;
        }
    }
}