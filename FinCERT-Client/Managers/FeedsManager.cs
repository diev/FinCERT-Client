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

using static API.Feeds;

namespace FincertClient.Managers;

internal static class FeedsManager
{
    public static async Task LoadFeeds(string path)
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
            if (!await DownloadFeedsAsync(feed, path))
            {
                Console.WriteLine($"Содержимое {feed} не получено.");
                continue;
            }
        }
    }
}
