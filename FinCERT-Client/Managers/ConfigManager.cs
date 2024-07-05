#region License
/*
Copyright 2022-2024 Dmitrii Evdokimov
Open source software

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
#endregion

using System.Text.Json;

namespace FincertClient.Managers;

internal static class ConfigManager
{
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new()
        {
            WriteIndented = true
        };
    }

    public static Config Read()
    {
        string appsettings = Path.ChangeExtension(Environment.ProcessPath!, ".config.json");

        if (File.Exists(appsettings))
        {
            using var read = File.OpenRead(appsettings);
            var config = JsonSerializer.Deserialize<Config>(read);

            if (config is null || config.NewConfig)
                throw new NewConfigException(
                    );

            return config;
        }

        var newConfig = new Config(true);
        using var write = File.OpenWrite(appsettings);
        JsonSerializer.Serialize(write, newConfig, GetJsonOptions());

        throw new NewConfigException(
            @$"Создан новый файл настроек ""{appsettings}"".");
    }
}

internal class NewConfigException : Exception
{
    const string message = @"Необходимо откорректировать новый конфиг ""{0}"".";

    public NewConfigException()
        : base() { }

    public NewConfigException(string config)
        : base(string.Format(message, config)) { }

    public NewConfigException(string config, Exception inner)
        : base(string.Format(message, config), inner) { }
}

public class ConfigException : Exception
{
    const string message = "Параметр '{0}' не указан в конфиге.";

    public ConfigException()
        : base() { }

    public ConfigException(string paramName)
        : base(string.Format(message, paramName)) { }

    public ConfigException(string paramName, Exception inner)
        : base(string.Format(message, paramName), inner) { }
}
