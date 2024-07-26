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

using System.Net.Http.Json;

using TLS;

namespace API;

// JSON-структуры
public record FeedsStatus(
    string UploadDatetime,
    string Type,
    int Version);

internal static class Feeds
{
    /// <summary>
    /// Функция получения сведений по актуальности фидов.
    /// </summary>
    /// <param name="feed">Тип фидов.</param>
    /// <returns>JSON-структура, содержащая дату обновления фидов.</returns>
    public static async Task<FeedsStatus> GetFeedsStatusAsync(FeedType feed)
    {
        string url = $"antifraud/feeds/{feed}";
        using var response = await TlsClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FeedsStatus>()
            ?? throw new FeedsException("Статус фидов не получен.");
    }

    /// <summary>
    /// Функция получения фидов в формате CSV.
    /// </summary>
    /// <param name="feed">Тип фидов.</param>
    /// <param name="path">Путь для сохранения файлов фидов.</param>
    public static async Task DownloadFeedsAsync(FeedType feed, string path)
    {
        Directory.CreateDirectory(path);

        string url = $"antifraud/feeds/{feed}/download";
        using var response = await TlsClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string name = feed switch
        {
            FeedType.accountNumber => "account_number",
            FeedType.cardNumber    => "card_number",
            FeedType.ewalletNumber => "ewallet_number",
            FeedType.fastPayNumber => "fastpay_number",
            FeedType.hashPassport  => "passport_hash",
            FeedType.hashSnils     => "snils_hash",
            FeedType.inn           => "inn",
            FeedType.phoneNumber   => "phone_number",
            FeedType.swift         => "swift",
            _ => feed.ToString()
        };

        string file = Path.Combine(path, name + ".csv");
        using var output = File.Create(file);
        await response.Content.CopyToAsync(output);
    }
}

public class FeedsException : Exception
{
    const string message = "Ошибка фидов: ";

    public FeedsException()
        : base() { }

    public FeedsException(string error)
        : base(message + error) { }

    public FeedsException(string error, Exception inner)
        : base(message + error, inner) { }
}
