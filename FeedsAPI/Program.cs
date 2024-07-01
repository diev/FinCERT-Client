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

namespace FeedsAPI;

internal class Program
{
    public static Config Config { get; } = ConfigManager.Read();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!"); // :)

        // Получить экземпляр класса с функциями API
        var api = FincertAPI.GetInstance();

        // Подключиться к АСОИ ФинЦЕРТ
        if (!await api.ConnectToASOIAsync(Config.ServerAddress, Config.Login, Config.Password))
        {
            Console.WriteLine("Ошибка подключения к серверу.");
            Environment.Exit(1);
        }
        
        if (!string.IsNullOrEmpty(Config.Store))
            Directory.CreateDirectory(Config.Store);

        foreach (var feed in Enum.GetValues<FeedType>())
        {
            // Проверить дату публикации фидов
            var status = await api.GetFeedsStatusAsync(feed);

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
            var data = await api.GetFeedsAsync(feed);

            if (data is null)
            {
                Console.WriteLine($"Содержимое {feed} не получено.");
                continue;
            }

            string path = Path.Combine(Config.Store, $"{feed}.csv");
            using var output = File.Create(path);
            await data.CopyToAsync(output);
        }

        // Завершить работу с АСОИ ФинЦЕРТ
        Environment.Exit(await api.LogoutFromASOIAsync() ? 0 : 1);
    }
}
