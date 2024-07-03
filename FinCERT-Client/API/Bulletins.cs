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
public record BulletinId(string Id);
public record BulletinIds(int Total, BulletinId[] Items);
public record BulletinList(string[] Ids);
public record BulletinInfo(string Id, string Hrid, string Header, string Description,
    string PublishedDate, string AttachmentId, string Type, string Subtype);
public record BulletinInfos(int Total, BulletinInfo[] Items);
public record BulletinAttachInfo(string Id, string Hrid, string Header, string Description,
    BulletinAttachment Attachment, BulletinAttachment[] AdditionalAttachments,
    string PublishedDate, string Type, string Subtype);
public record BulletinAttachment(string Id, string Name, int Size, string Url);

internal static class Bulletins
{
    /// <summary>
    /// Функция получения списка бюллетений.
    /// </summary>
    /// <param name="limit">Количество (1-100).</param>
    /// <param name="offset">Смещение по списку.</param>
    /// <returns>JSON-структура со списком бюллетеней.</returns>
    public static async Task<BulletinIds?> GetBulletinIdsAsync(int limit = 100, long offset = 0)
    {
        try
        {
            var response = await TlsClient.GetAsync($"bulletins?limit={limit}&offset={offset}");
            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadFromJsonAsync<BulletinIds>();
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Функция получения массива информации по списку бюллетеней.
    /// </summary>
    /// <param name="ids">Массив идентификаторов.</param>
    /// <returns>Массив информации.</returns>
    public static async Task<BulletinInfos?> GetBulletinInfosAsync(string[] ids)
    {
        try
        {
            var response = await TlsClient.PostAsJsonAsync("bulletins/list", new BulletinList(ids));
            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadFromJsonAsync<BulletinInfos>();
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Функция получения информации по идентификатору бюллетеня.
    /// </summary>
    /// <param name="id">Идентификатор бюллетеня.</param>
    /// <returns>Полная информация по бюллетеню.</returns>
    public static async Task<BulletinAttachInfo?> GetBulletinAttachInfoAsync(string id)
    {
        try
        {
            var response = await TlsClient.GetAsync($"bulletins/{id}");
            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadFromJsonAsync<BulletinAttachInfo>();
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Функция скачивания файла рассылки к бюллетеню.
    /// </summary>
    /// <param name="id">Идентификатор файла.</param>
    /// <param name="path">Путь для сохранения файла.</param>
    /// <returns>Файл сохранен.</returns>
    public static async Task<bool> DownloadAttachmentAsync(string id, string path)
    {
        try
        {
            var response = await TlsClient.GetAsync($"attachments/{id}/download");
            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                using var output = File.Create(path);
                await response.Content.CopyToAsync(output);
                output.Close();
                return File.Exists(path);
            }
        }
        catch { }
        return false;
    }
}
