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

using TLS;

namespace FincertClient;

internal class Program
{
    public static Config Config { get; set; } = null!;

    static async Task<int> Main(string[] args)
    {
        try
        {
            // defaults
            bool checklist = false;
            bool feeds = false;
            int limit = 100;
            long offset = 0;

            Console.WriteLine("Hello, World!"); // :)

            // Console Trace
            Console.WriteLine("Подключение логирования на консоль.");
            using ConsoleTraceListener ConTracer = new() { Name = nameof(ConTracer) };
            Trace.Listeners.Add(ConTracer);

            // Опциональные параметры
            for (int i = 0; i < args.Length; i++)
            {
                string cmd = args[i];

                switch (cmd.ToLower())
                {
                    case "-checklist":
                        checklist = true;
                        break;

                    case "-feeds":
                        feeds = true;
                        break;

                    case "-limit":
                        if (args.Length > i)
                            limit = int.Parse(args[++i]);
                        break;

                    case "-offset":
                        if (args.Length > i)
                            offset = long.Parse(args[++i]);
                        break;

                    default:
                        throw new ArgumentException($"Непонятный параметр '{args[i]}'.");
                }
            }

            Trace.WriteLine("Чтение конфигурации...");
            Config = ConfigManager.Read();

            // Log Trace
            Trace.WriteLine("Подключение логирования в файл.");
            string log = Helper.GetLogFile(Config.Logs ?? string.Empty);
            using TextWriterTraceListener FileTracer = new(log, nameof(FileTracer));
            Trace.Listeners.Add(FileTracer);
            Trace.AutoFlush = true;

            // Start
            ConTracer.WriteLine(@$"Лог пишется в файл ""{log}"".");
            FileTracer.WriteLine($"--- {DateTime.Now:T} Start ---");

            #region Config
            // Конфиг
            Config.FeedsDownloads ??= string.Empty;
            Config.BulletinsDownloads ??= string.Empty;
            Config.MvdDownloads1 ??= string.Empty;
            Config.MvdDownloads2 ??= string.Empty;
            Config.Logs ??= string.Empty;

            var Tls = Config.Tls;
            if (Config.Tls is null)
                throw new ConfigException(nameof(Tls));

            // TLS Конфиг
            var MyThumbprint = Config.Tls.MyThumbprint;
            if (string.IsNullOrEmpty(MyThumbprint))
                throw new TlsConfigException(nameof(MyThumbprint));

            var API = Config.Tls.API;
            if (string.IsNullOrEmpty(API))
                throw new TlsConfigException(nameof(API));

            var Login = Config.Tls.Login;
            if (string.IsNullOrEmpty(Login))
                throw new TlsConfigException(nameof(Login));

            if (Config.Tls.ValidateServerThumbprint)
            {
                var ServerThumbprint = Config.Tls.ServerThumbprint;
                if (!string.IsNullOrEmpty(ServerThumbprint))
                    throw new TlsConfigException(nameof(ServerThumbprint));
            }

            if (Config.Tls.UseProxy)
            {
                var ProxyAddress = Config.Tls.ProxyAddress;
                if (!string.IsNullOrEmpty(ProxyAddress))
                    throw new TlsConfigException(nameof(ProxyAddress));
            }

            var Password = Config.Tls.Password;
            if (string.IsNullOrEmpty(Password))
            {
                Trace.WriteLine($"Получение '{Login}' из Диспетчера учетных данных Windows.");
                var cred = CredentialManager.ReadCredential(Login);
                Config.Tls.Login = cred.UserName
                    ?? throw new CredManagerException(nameof(Login));
                Config.Tls.Password = cred.Password
                    ?? throw new CredManagerException(nameof(Password));
            }
            #endregion Config

            // Подключиться к АСОИ ФинЦЕРТ
            Trace.WriteLine("Login...");
            await TlsClient.LoginAsync(Tls!);

            // Скачать комплект файлов для чек-листа
            if (checklist)
            {
                await BulletinsManager.GetCheckList(
                    Path.Combine(Config.BulletinsDownloads, "CheckList"));
                goto exit;
            }

            // Проверить время обновления фидов
            if (feeds)
            {
                var date = await FeedsManager.GetUpdatedAsync();
                Trace.WriteLine($"--- Время обновления фидов {date:T} ---");
                goto exit;
            }

            // Скачать фиды
            if (Config.Feeds)
            {
                await FeedsManager.LoadFeeds(Config.FeedsDownloads);
            }

            // Скачать бюллетени
            if (Config.Bulletins)
            {
                // Неполноценный вариант с загрузкой только основного файла (отлько для GUI?)
                // await BulletinsManager.LoadBulletinsList(Config.BulletinsDownloads, limit, offset);

                // Полноценный вариант с загрузкой основного и дополнительных файлов
                await BulletinsManager.LoadBulletinsDirs(Config.BulletinsDownloads, limit, offset);
            }

        exit:
            // Завершить работу с АСОИ ФинЦЕРТ
            Trace.WriteLine("Logout");
            await TlsClient.LogoutAsync();
            ConTracer.WriteLine("end.");
            FileTracer.WriteLine($"--- {DateTime.Now:T} end. ---");
            return 0;
        }

        #region Exceptions
        catch (ArgumentException ex)
        {
            Trace.WriteLine(ex.ToString());
            Usage();
            return 2;
        }
        catch (NewConfigException ex)
        {
            Console.WriteLine(ex.Message);
            return 3;
        }
        catch (ConfigException ex)
        {
            Console.WriteLine(ex.Message);
            return 4;
        }
        catch (TlsConfigException ex)
        {
            Console.WriteLine(ex.Message);
            return 5;
        }
        catch (ResponseException ex)
        {
            Console.WriteLine(ex.Message);
            return 6;
        }
        catch (WaitException ex)
        {
            Console.WriteLine(ex.Message);
            return 7;
        }
        catch (FeedsException ex)
        {
            Console.WriteLine(ex.Message);
            return 8;
        }
        catch (BulletinsException ex)
        {
            Console.WriteLine(ex.Message);
            return 9;
        }
        #endregion Exceptions

#if !DEBUG
        catch (Exception ex)
        {
            Helper.TraceError("Program Error", ex);
            return 1;
        }
#endif
        finally
        {
            Trace.Listeners.Clear();
        }
    }

    private static void Usage()
    {
        Console.WriteLine(@"Опциональные параметры:

-checklist  Сформировать в папке BulletinsDownloads\CheckList комплект
            файлов для приложения к чек-листу на подключение.

-feeds      Получить время последнего обновления фидов на сервере.
            Также это самый ""экономичный"" способ проверить
            функционирование программы.

-limit 100  Число от 1 до 100 - ограничение числа скачиваемых бюллетеней.
            100 - по умолчанию и максимум - это ограничение API - обойти
            его невозможно - только указанием параметра -offset.

-offset 0   Число от 0 и выше - сдвиг начала скачиваемых бюллетеней.
            0 - по умочанию. Указание этого параметра не 0 переключает
            режим поведения при встрече ранее скачанной папки:

    0       скачивание прекращается;
    не 0    ранее скачанная папка пропускается, продолжается
            перебор (в пределах значения -limit).
");
    }
}
