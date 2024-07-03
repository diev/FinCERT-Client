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

using System.Net;
using System.Net.Http.Json;

namespace API;

// JSON-структуры
public record FeedsStatus(string UploadDatetime, string Type, int Version);

internal static class Feeds
{
    /// <summary>
    /// Функция получения сведений по актуальности фидов.
    /// </summary>
    /// <param name="feed">Тип фидов.</param>
    /// <returns>JSON-структура, содержащая дату обновления фидов.</returns>
    public static async Task<FeedsStatus?> GetFeedsStatusAsync(FeedType feed)
    {
        try
        {
            var response = await TlsClient.GetAsync($"antifraud/feeds/{feed}");
            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadFromJsonAsync<FeedsStatus>();
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Функция получения фидов в формате CSV.
    /// </summary>
    /// <param name="feed">Тип фидов.</param>
    /// <param name="path">Путь для сохранения фидов.</param>
    /// <returns>Файл сохранен.</returns>
    public static async Task<bool> DownloadFeedsAsync(FeedType feed, string path)
    {
        try
        {
            var response = await TlsClient.GetAsync($"antifraud/feeds/{feed}/download");
            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.OK)
            {
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
                output.Close();
                return File.Exists(file);
            }
        }
        catch { }
        return false;
    }
}
