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

using static API.Bulletins;
using static FincertClient.Managers.Helper;

namespace FincertClient.Managers;

internal static class BulletinsManager
{
    public static async Task<bool> GetCheckList(string path, int limit = 4)
    {
        try
        {
            Directory.CreateDirectory(path);

            // Получить id бюллетеней.
            var ids = await GetBulletinIdsAsync(limit);

            if (ids is null) return false;

            await GetBulletinIdsAsync(Path.Combine(path, "1.json"), limit);

            // Получить информации о нескольких бюллетенях.
            int count = ids.Items.Length;
            string[] ids2 = new string[count];

            for (int i = 0; i < count; i++)
            {
                ids2[i] = ids.Items[i].Id;
            }

            var infos = await GetBulletinInfosAsync(ids2);
            await GetBulletinInfosAsync(ids2, Path.Combine(path, "2.json"));

            // Получить информации об одном бюллетене.
            var id = ids2[0];
            var info = await GetBulletinAttachInfoAsync(id);
            await GetBulletinAttachInfoAsync(id, Path.Combine(path, "3.json"));

            // Скачать несколько бюллетеней, приложить файлы к форме.
            await LoadBulletinsDirs(path, limit);

            return true;
        }
        catch { }
        return false;
    }

    public static async Task LoadBulletinsList(string path, int limit = 100, long offset = 0)
    {
        Console.WriteLine("Получение бюллетеней списком...");

        var ids = await GetBulletinIdsAsync(limit, offset);

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

        var BulletinInfos = await GetBulletinInfosAsync(ids2);
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

    public static async Task LoadBulletinsDirs(string path, int limit = 100, long offset = 0)
    {
        Console.WriteLine("Получение бюллетеней по папкам...");

        var ids = await GetBulletinIdsAsync(limit, offset);

        if (ids is null)
        {
            Console.WriteLine("Список бюллетеней не получен.");
            Environment.Exit(1);
        }

        Console.WriteLine($"Всего: {ids.Total}, в листе: {ids.Items.Length}.");

        foreach (var itemId in ids.Items)
        {
            var id = itemId.Id;
            var bulletin = await GetBulletinAttachInfoAsync(id);
            var date = DateTime.Parse(bulletin!.PublishedDate);

            // Dirname generator! Do not change it to prevent an unwanted full reload!

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
                await DownloadAttachmentAsync(bulletin.Attachment.Id, Path.Combine(dir, name));
            }

            int i = 0;
            foreach (var additional in bulletin.AdditionalAttachments)
            {
                string name = additional.Name;
                sb.AppendLine($"Доп. файл {++i}:     {name}");
                await DownloadAttachmentAsync(additional.Id, Path.Combine(dir, name));
            }

            sb.AppendLine($"Описание:        {bulletin.Description}");

            string text = sb.ToString();
            string file = Path.Combine(dir, bulletin.Type + ".txt");
            await File.WriteAllTextAsync(file, text);

            Console.WriteLine(new string('-', 36));
            Console.WriteLine(text);
        }
    }
}
