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
#region Records

public record BulletinId(
    string Id);

public record BulletinIds(
    int Total,
    BulletinId[] Items);

public record BulletinList(
    string[] Ids);

public record BulletinInfo(
    string Id,
    string Hrid,
    string Header,
    string Description,
    string PublishedDate,
    string AttachmentId,
    string Type,
    string Subtype);

public record BulletinInfos(
    int Total,
    BulletinInfo[] Items);

public record BulletinAttachInfo(
    string Id,
    string Hrid,
    string Header,
    string Description,
    BulletinAttachment Attachment,
    BulletinAttachment[] AdditionalAttachments,
    string PublishedDate,
    string Type,
    string Subtype);

public record BulletinAttachment(
    string Id,
    string Name,
    int Size,
    string Url);

#endregion Records

internal static class Bulletins
{
    /// <summary>
    /// Функция получения списка бюллетений.
    /// </summary>
    /// <param name="limit">Количество (1-100).</param>
    /// <param name="offset">Смещение по списку.</param>
    /// <returns>JSON-структура со списком бюллетеней.</returns>
    public static async Task<BulletinIds> GetBulletinIdsAsync(int limit = 100, long offset = 0)
    {
        string url = $"bulletins?limit={limit}&offset={offset}";
        var response = await TlsClient.GetAsync(url);
        return await response.Content.ReadFromJsonAsync<BulletinIds>()
            ?? throw new BulletinsException(
                "Список бюллетеней не получен.");
    }

    /// <summary>
    /// Функция получения списка бюллетений в файл.
    /// </summary>
    /// <param name="path">Путь для сохранения файла.</param>
    /// <param name="limit">Количество (1-100).</param>
    /// <param name="offset">Смещение по списку.</param>
    public static async Task GetBulletinIdsAsync(string path, int limit = 100, long offset = 0)
    {
        string url = $"bulletins?limit={limit}&offset={offset}";
        var response = await TlsClient.GetAsync(url);
        using var output = File.Create(path);
        await response.Content.CopyToAsync(output);
    }

    /// <summary>
    /// Функция получения массива информации по списку бюллетеней.
    /// </summary>
    /// <param name="ids">Массив идентификаторов.</param>
    /// <returns>Массив информации.</returns>
    public static async Task<BulletinInfos> GetBulletinInfosAsync(string[] ids)
    {
        string url = "bulletins/list";
        var content = new BulletinList(ids);
        var response = await TlsClient.PostAsJsonAsync(url, content);
        return await response.Content.ReadFromJsonAsync<BulletinInfos>()
            ?? throw new BulletinsException(
                "Информация по бюллетеням не получена.");
    }

    /// <summary>
    /// Функция получения массива информации по списку бюллетеней в файл.
    /// </summary>
    /// <param name="ids">Массив идентификаторов.</param>
    /// <param name="path">Путь для сохранения файла.</param>
    public static async Task GetBulletinInfosAsync(string[] ids, string path)
    {
        string url = "bulletins/list";
        var content = new BulletinList(ids);
        var response = await TlsClient.PostAsJsonAsync(url, content);
        using var output = File.Create(path);
        await response.Content.CopyToAsync(output);
    }

    /// <summary>
    /// Функция получения информации по идентификатору бюллетеня.
    /// </summary>
    /// <param name="id">Идентификатор бюллетеня.</param>
    /// <returns>Полная информация по бюллетеню.</returns>
    public static async Task<BulletinAttachInfo> GetBulletinAttachInfoAsync(string id)
    {
        string url = $"bulletins/{id}";
        var response = await TlsClient.GetAsync(url);
        return await response.Content.ReadFromJsonAsync<BulletinAttachInfo>()
            ?? throw new BulletinsException(
                $"Информация по бюллетеню '{id}' не получена.");
    }

    /// <summary>
    /// Функция получения информации по идентификатору бюллетеня в файл.
    /// </summary>
    /// <param name="id">Идентификатор бюллетеня.</param>
    /// <param name="path">Путь для сохранения файла.</param>
    public static async Task GetBulletinAttachInfoAsync(string id, string path)
    {
        string url = $"bulletins/{id}";
        var response = await TlsClient.GetAsync(url);
        using var output = File.Create(path);
        await response.Content.CopyToAsync(output);
    }

    /// <summary>
    /// Функция скачивания файла рассылки к бюллетеню.
    /// </summary>
    /// <param name="id">Идентификатор файла.</param>
    /// <param name="path">Путь для сохранения файла.</param>
    public static async Task DownloadAttachmentAsync(string id, string path)
    {
        string url = $"attachments/{id}/download";
        var response = await TlsClient.GetAsync(url);
        using var output = File.Create(path);
        await response.Content.CopyToAsync(output);
    }
}

public class BulletinsException : Exception
{
    const string message = "Ошибка бюллетеней: ";

    public BulletinsException()
        : base() { }

    public BulletinsException(string error)
        : base(message + error) { }

    public BulletinsException(string error, Exception inner)
        : base(message + error, inner) { }
}
