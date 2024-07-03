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
        try
        {
            string appsettings = Path.ChangeExtension(Environment.ProcessPath!, ".config.json");

            if (File.Exists(appsettings))
            {
                using var read = File.OpenRead(appsettings);
                return JsonSerializer.Deserialize<Config>(read)!;
            }

            using var write = File.OpenWrite(appsettings);
            JsonSerializer.Serialize(write, new Config(), GetJsonOptions());

            Console.WriteLine(@$"Создан новый файл настроек ""{appsettings}"" - откорректируйте его.");
            throw new Exception();
        }
        catch
        {
            Environment.Exit(2);
        }

        return new();
    }
}
