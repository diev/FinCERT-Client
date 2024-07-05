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
using System.Text;

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
        return file;
    }

    public static void TraceError(string message, Exception ex)
    {
        Console.WriteLine("Вывод информации об ошибке.");

        Trace.WriteLine($"{DateTime.Now:G} {message}: {ex.Message}.");
        StringBuilder sb = new();
        sb.AppendLine($"{DateTime.Now:G} Exception:")
            .AppendLine(ex.ToString())
            .AppendLine();

        if (ex.InnerException != null)
        {
            sb.AppendLine("Inner Exception:")
                .AppendLine(ex.InnerException.ToString())
                .AppendLine();
        }

        File.AppendAllText("error.log", sb.ToString());
    }
}
