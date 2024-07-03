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

#define TRACE

using System.Diagnostics;

using API;

using FincertClient.Managers;

namespace FincertClient;

internal class Program
{
    private static readonly Config _config = ConfigManager.Read();

    static async Task Main(string[] args)
    {
        // defaults
        bool checklist = false;
        int limit = 100;
        long offset = 0;

        Console.WriteLine("Hello, World!"); // :)

        // Console Trace
        using ConsoleTraceListener ConTracer = new() { Name = nameof(ConTracer) };
        Trace.Listeners.Add(ConTracer);

        // Log Trace
        string log = Helper.GetLogFile(_config.Logs);
        using TextWriterTraceListener FileTracer = new(log, nameof(FileTracer));
        Trace.Listeners.Add(FileTracer);
        Trace.AutoFlush = true;

        try
        {
            // Опциональные параметры
            for (int i = 0; i < args.Length; i++)
            {
                string cmd = args[i];

                switch (cmd.ToLower())
                {
                    case "-checklist":
                        checklist = true;
                        Trace.WriteLine(cmd);
                        break;

                    case "-limit":
                        if (args.Length > i)
                            limit = int.Parse(args[++i]);
                        Trace.WriteLine($"{cmd} {limit}");
                        break;

                    case "-offset":
                        if (args.Length > i)
                            offset = long.Parse(args[++i]);
                        Trace.WriteLine($"{cmd} {offset}");
                        break;

                    default:
                        Trace.WriteLine($"Непонятная команда '{cmd}'.");
                        Environment.Exit(1);
                        break;
                }
            }

            // Конфиг
            Trace.WriteLine("Получение учетных данных...");
            var tls = _config.Tls;

            if (string.IsNullOrEmpty(tls.Password))
            {
                string entry = "FinCERT";
                Trace.WriteLine($"Пароль пуст - получение от Windows ('{entry}').");
                var cred = CredentialManager.ReadCredential(entry);
                tls.Login = cred.UserName ?? string.Empty;
                tls.Password = cred.Password ?? string.Empty;
            }

            // Подключиться к АСОИ ФинЦЕРТ
            Trace.WriteLine($"{DateTime.Now:T} Login...");

            if (!await TlsClient.Login(tls))
            {
                Trace.WriteLine("Ошибка подключения к серверу.");
                Environment.Exit(1);
            }

            // Скачать комплект файлов для чек-листа
            if (checklist)
            {
                await BulletinsManager.GetCheckList(
                    Path.Combine(_config.BulletinsDownloads, "CheckList"));
                Environment.Exit(0);
            }

            // Скачать фиды
            if (_config.Feeds)
            {
                await FeedsManager.LoadFeeds(_config.FeedsDownloads);
            }

            // Скачать бюллетени
            if (_config.Bulletins)
            {
                // Неполноценный вариант с загрузкой только основного файла
                // await BulletinsManager.LoadBulletinsList(_config.BulletinsDownloads, limit, offset);

                // Полноценный вариант с загрузкой основного и дополнительных файлов
                await BulletinsManager.LoadBulletinsDirs(_config.BulletinsDownloads, limit, offset);
            }

            // Завершить работу с АСОИ ФинЦЕРТ
            Trace.WriteLine($"{DateTime.Now:T} Logout...");

            if (await TlsClient.LogoutAsync())
            {
                Trace.WriteLine("end.");
                Environment.Exit(0);
            }
            else
            {
                Trace.WriteLine("Ошибка завершения сеанса.");
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            Helper.TraceError("Program Error", ex);
            Environment.Exit(4);
        }
        finally
        {
            Trace.Listeners.Clear();
        }
    }
}
