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

using System.Text;

using API;

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

        if (_config.Feeds)
        {
            await LoadFeeds(_config.FeedsDownloads);
        }

        if (_config.Bulletins)
        {
            await LoadBulletinsList(_config.BulletinsDownloads);
            await LoadBulletinsDirs(_config.BulletinsDownloads);
        }

        // Завершить работу с АСОИ ФинЦЕРТ
        Environment.Exit(await TlsClient.LogoutAsync() ? 0 : 1);
    }

    private static async Task LoadFeeds(string path)
    {
        Console.WriteLine("Получение фидов...");

        if (!string.IsNullOrEmpty(path))
            Directory.CreateDirectory(path);

        foreach (var feed in Enum.GetValues<FeedType>())
        {
            // Проверить дату публикации фидов
            var status = await Feeds.GetFeedsStatusAsync(feed);

            if (status is null)
            {
                Console.WriteLine($"Статус {feed} не получен.");
                continue;
            }

            /*
            {
                "uploadDatetime": "2024-07-01T13:00:12.253506+03:00",
                "type": "inn",
                "version": 1
            }
            */

            var date = DateTime.Parse(status.UploadDatetime);
            Console.WriteLine($"{date:g} {feed}");

            // Выгрузить из АСОИ ФинЦЕРТ нужный тип фидов
            string file = Path.Combine(path, $"{feed}.csv");

            if (!await Feeds.DownloadFeedsAsync(feed, file))
            {
                Console.WriteLine($"Содержимое {feed} не получено.");
                continue;
            }
        }
    }

    private static async Task LoadBulletinsList(string path)
    {
        Console.WriteLine("Получение бюллетеней списком...");

        var ids = await Bulletins.GetBulletinIdsAsync();

        if (ids is null)
        {
            Console.WriteLine("Список бюллетеней не получен.");
            Environment.Exit(1);
        }

        int count = ids.Items.Length;
        string[] ids2 = new string[count];

        Console.WriteLine($"Всего: {ids.Total}, в листе: {count}.");

        for (int i = 0; i < count; i++)
        {
            ids2[i] = ids.Items[i].Id;
        }

        var BulletinInfos = await Bulletins.GetBulletinInfosAsync(ids2);
        StringBuilder sb = new();

        if (BulletinInfos is null)
        {
            Console.WriteLine("Список информации по бюллетеням не получен.");
            Environment.Exit(1);
        }

        foreach (var bulletin in BulletinInfos.Items)
        {
            var id = bulletin.Id;
            var date = DateTime.Parse(bulletin.PublishedDate);

            sb.AppendLine(new string('-', 36))
                .AppendLine(id)
                .AppendLine($"Дата публикации: {date:g}")
                .AppendLine($"Идентификатор:   {bulletin.Hrid}")
                .AppendLine($"Заголовок:       {bulletin.Header}")
                .AppendLine($"Тип рассылки:    {bulletin.Type}")
                .AppendLine($"Подтип рассылки: {bulletin.Subtype}");

            if (bulletin.AttachmentId != null)
            {
                sb.AppendLine($"Файл рассылки:   {bulletin.AttachmentId}");
                //string name = "attachment.dat";
                //await Bulletins.DownloadAttachmentAsync(bulletin.AttachmentId, PathCombine(dir, name));
            }

            sb.AppendLine($"Описание:        {bulletin.Description}");
        }

        string text = sb.ToString();
        string file = Path.Combine(path, "Bulletins.txt");
        await File.WriteAllTextAsync(file, text);

        Console.WriteLine(text);
    }

    private static async Task LoadBulletinsDirs(string path)
    {
        Console.WriteLine("Получение бюллетеней по папкам...");

        var ids = await Bulletins.GetBulletinIdsAsync();

        if (ids is null)
        {
            Console.WriteLine("Список бюллетеней не получен.");
            Environment.Exit(1);
        }

        Console.WriteLine($"Всего: {ids.Total}, в листе: {ids.Items.Length}.");

        foreach (var itemId in ids.Items)
        {
            var id = itemId.Id;
            var bulletin = await Bulletins.GetBulletinAttachInfoAsync(id);
            var date = DateTime.Parse(bulletin!.PublishedDate);
            var dir = PathCombine(path, $"{date:yyyy-MM-dd HHmm} {bulletin.Hrid.Trim()}");

            if (Directory.Exists(dir)) break;

            Directory.CreateDirectory(dir);

            StringBuilder sb = new();
            sb.AppendLine(id)
                .AppendLine($"Дата публикации: {date:g}")
                .AppendLine($"Идентификатор:   {bulletin.Hrid}")
                .AppendLine($"Заголовок:       {bulletin.Header}")
                .AppendLine($"Тип рассылки:    {bulletin.Type}")
                .AppendLine($"Подтип рассылки: {bulletin.Subtype}");

            if (bulletin.Attachment != null)
            {
                string name = bulletin.Attachment.Name;
                sb.AppendLine($"Файл рассылки:   {name}");
                await Bulletins.DownloadAttachmentAsync(bulletin.Attachment.Id, Path.Combine(dir, name));
            }

            int i = 0;
            foreach (var additional in bulletin.AdditionalAttachments)
            {
                string name = additional.Name;
                sb.AppendLine($"Доп. файл {++i}:     {name}");
                await Bulletins.DownloadAttachmentAsync(additional.Id, Path.Combine(dir, name));
            }

            sb.AppendLine($"Описание:        {bulletin.Description}");

            string text = sb.ToString();
            string file = Path.Combine(dir, bulletin.Type + ".txt");
            await File.WriteAllTextAsync(file, text);

            Console.WriteLine(new string('-', 36));
            Console.WriteLine(text);
        }
    }

    public static string PathCombine(string dir, string name)
    {
        string file = string.Concat(name.Split(Path.GetInvalidFileNameChars())).Trim();
        return Path.Combine(dir, file.Length > 0 ? file : "--");
    }
}
