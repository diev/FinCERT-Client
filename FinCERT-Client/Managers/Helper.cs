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

using System.Diagnostics;

namespace FincertClient.Managers;

internal static class Helper
{
    public static string CorrectName(string name)
    {
        string file = string.Concat(name.Split(Path.GetInvalidFileNameChars())).Trim();
        return file.Length > 0 ? file : "--";
    }

    public static string GetLogFile(string? logDirectory)
    {
        string logs = logDirectory ?? nameof(logs);
        var now = DateTime.Now;
        var logsPath = Directory.CreateDirectory(Path.Combine(logs, now.ToString("yyyy")));
        var file = Path.Combine(logsPath.FullName, now.ToString("yyyyMMdd") + ".log");

        Console.WriteLine(@$"Лог пишется в файл ""{file}"".");

        return file;
    }

    public static void TraceError(string message, Exception ex)
    {
        Console.WriteLine("Вывод информации об ошибке.");

        Trace.WriteLine($"{DateTime.Now:G} {message}: {ex.Message}.");
        string text = $"{DateTime.Now:G} Exception:{Environment.NewLine}{ex}{Environment.NewLine}";

        if (ex.InnerException != null)
        {
            text += $"Inner Exception:{Environment.NewLine}{ex.InnerException}{Environment.NewLine}";
        }

        File.AppendAllText("error.log", text);
    }
}
