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
using System.IO.Compression;
using System.Text;

using API;

using static API.Bulletins;
using static FincertClient.Managers.Helper;

namespace FincertClient.Managers;

internal static class BulletinsManager
{
    /// <summary>
    /// Получение комплекта файлов и папок для прохождения чек-листа.
    /// </summary>
    /// <param name="path">Путь к папке для формируемого комплекта.</param>
    /// <param name="limit">Ограничение на несколько записей для примера (4).</param>
    public static async Task GetCheckList(string path, int limit = 4)
    {
        Directory.CreateDirectory(path);

        // Получить id бюллетеней.
        var ids = await GetBulletinIdsAsync(limit);
        await GetBulletinIdsAsync(Path.Combine(path, "1.json"), limit);

        // Получить информации о нескольких бюллетенях.
        int count = ids.Items.Length;
        string[] ids2 = new string[count];

        for (int i = 0; i < count; i++)
            ids2[i] = ids.Items[i].Id;

        await GetBulletinInfosAsync(ids2, Path.Combine(path, "2.json"));

        // Получить информации об одном бюллетене.
        var id = ids2[0];
        await GetBulletinAttachInfoAsync(id, Path.Combine(path, "3.json"));

        // Скачать несколько бюллетеней, приложить файлы к форме.
        await LoadBulletinsDirs(path, limit);
    }

    /// <summary>
    /// Неполноценный вариант загрузки бюллетеней, т.к. нет возможности получить
    /// из него дополнительные файлы.
    /// Получается список, из него составляется список для получения бюллетеней,
    /// а в них нет информации о дополнительных файлах - только id основного.
    /// Видимо, это только для отображения таблицы бюллетеней в интерфейсе.
    /// </summary>
    /// <param name="path">Путь к папке, где будет создан файл 'Bulletins.txt'.
    /// Если он существует, то он будет перезаписан.</param>
    /// <param name="limit">Ограничение на число бюллетеней (1-100), максимум 100.</param>
    /// <param name="offset">Смещение по списку бюллетеней вглубь истории (0 - без смещения).</param>
    public static async Task LoadBulletinsList(string path, int limit = 100, long offset = 0)
    {
        Directory.CreateDirectory(path);

        Trace.WriteLine("Получение бюллетеней списком...");

        var ids = await GetBulletinIdsAsync(limit, offset);
        int count = ids.Items.Length;
        string[] ids2 = new string[count];

        Trace.WriteLine($"Всего: {ids.Total}, в листе: {count}.");

        for (int i = 0; i < count; i++)
            ids2[i] = ids.Items[i].Id;

        var BulletinInfos = await GetBulletinInfosAsync(ids2);
        StringBuilder sb = new();

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
                sb.AppendLine($"Файл рассылки:   {bulletin.AttachmentId}");

            sb.AppendLine($"Описание:        {bulletin.Description}");
        }

        string text = sb.ToString();
        string file = Path.Combine(path, "Bulletins.txt");
        await File.WriteAllTextAsync(file, text);

        Console.WriteLine(text);
    }

    /// <summary>
    /// Полноценный вариант загрузки бюллетеней со всеми файлами.
    /// Получается список, по нему запрашиваются поштучно бюллетени, для них - файлы.
    /// </summary>
    /// <param name="path">Путь к папке для скачиваний.</param>
    /// <param name="limit">Ограничение на число бюллетеней (1-100), максимум 100.</param>
    /// <param name="offset">Смещение по списку бюллетеней вглубь истории (0 - без смещения).</param>
    public static async Task LoadBulletinsDirs(string path, int limit = 100, long offset = 0)
    {
        string lastMvdZip = "feeds_20240101-01.zip";

        Directory.CreateDirectory(path);

        Trace.WriteLine("Получение бюллетеней по папкам...");

        var ids = await GetBulletinIdsAsync(limit, offset);

        Trace.WriteLine($"Всего: {ids.Total}, в листе: {ids.Items.Length}.");

        foreach (var itemId in ids.Items)
        {
            var id = itemId.Id;
            var bulletin = await GetBulletinAttachInfoAsync(id);
            var date = DateTime.Parse(bulletin!.PublishedDate);

            // Генератор имени папки - не изменяйте его, чтобы не скачать заново лишнее!

            var name = CorrectName($"{date:yyyy-MM-dd HHmm} {bulletin.Hrid.Trim()}");
            var dir = Path.Combine(path, name);

            // Встречена ранее скачанная папка
            if (Directory.Exists(dir))
            {
                if (offset == 0)
                {
                    // Скачивание прекращается
                    Trace.WriteLine($"{name} // конец скачивания.");
                    break;
                }
                else
                {
                    // Ранее скачанная папка пропускается
                    Trace.WriteLine($"{name} // есть");
                    continue;
                }
            }

            // Создается новая папка
            Trace.WriteLine(name);
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
                string filename = bulletin.Attachment.Name;
                sb.AppendLine($"Файл рассылки:   {filename}");
                string attach = Path.Combine(dir, filename);
                await DownloadAttachmentAsync(bulletin.Attachment.Id, attach);

                // Идентификатор:   FinCERT-20240703-03-FEEDS
                // Заголовок:       Фиды на блокировку
                // Тип рассылки:    Bulletin
                // Подтип рассылки: Фиды на блокировку
                // Файл рассылки:   feeds_20240703-03.zip

                if (filename.StartsWith("feeds_20", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Compare(filename, lastMvdZip, true) > 0) //TODO
                    {
                        Trace.WriteLine($"Получен фид МВД '{filename}'");

                        lastMvdZip = filename;
                        string dir1 = Program.Config.MvdDownloads1!; //TODO

                        if (!dir1.Equals(string.Empty))
                            Directory.CreateDirectory(dir1);

                        ZipFile.ExtractToDirectory(attach, dir1, true);

                        string dir2 = Program.Config.MvdDownloads2!; //TODO

                        if (!dir2.Equals(dir1))
                        {
                            if (!dir2.Equals(string.Empty))
                                Directory.CreateDirectory(dir2);

                            ZipFile.ExtractToDirectory(attach, dir2, true);
                        }
                    }
                }
            }

            int i = 0;
            foreach (var additional in bulletin.AdditionalAttachments)
            {
                string filename = additional.Name;
                sb.AppendLine($"Доп. файл {++i}:     {filename}");
                await DownloadAttachmentAsync(additional.Id, Path.Combine(dir, filename));
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
