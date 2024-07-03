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

using API;

using FincertClient.Managers;

namespace FincertClient;

internal class Program
{
    private static readonly Config _config = ConfigManager.Read();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!"); // :)

        // Подключиться к АСОИ ФинЦЕРТ
        if (!await TlsClient.Login(_config.Tls))
        {
            Console.WriteLine("Ошибка подключения к серверу.");
            Environment.Exit(1);
        }

        if (args.Length > 0 && args.Contains("-checklist"))
        {
            await BulletinsManager.GetCheckList(
                Path.Combine(_config.BulletinsDownloads, "CheckList"));
        }

        if (_config.Feeds)
        {
            await FeedsManager.LoadFeeds(_config.FeedsDownloads);
        }

        if (_config.Bulletins)
        {
            await BulletinsManager.LoadBulletinsList(_config.BulletinsDownloads);
            await BulletinsManager.LoadBulletinsDirs(_config.BulletinsDownloads);
        }

        // Завершить работу с АСОИ ФинЦЕРТ
        Environment.Exit(await TlsClient.LogoutAsync() ? 0 : 1);
    }
}
