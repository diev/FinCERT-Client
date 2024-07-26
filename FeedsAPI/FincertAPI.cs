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

namespace FeedsAPI;

/// <summary>
/// Фиды.
/// </summary>
public enum FeedType
{
    /// <summary>
    /// Хеш паспорта получателя.
    /// </summary>
    hashPassport,

    /// <summary>
    /// Хеш номера СНИЛС получателя.
    /// </summary>
    hashSnils,

    /// <summary>
    /// Счет SWIFT.
    /// </summary>
    swift,

    /// <summary>
    /// Система быстрых платежей.
    /// </summary>
    fastPayNumber,

    /// <summary>
    /// Номер телефонов получателя.
    /// </summary>
    phoneNumber,

    /// <summary>
    /// Номер кошелька получателя.
    /// </summary>
    ewalletNumber,

    /// <summary>
    /// Номер карты получателя.
    /// </summary>
    cardNumber,

    /// <summary>
    /// Лицевой счет получателя.
    /// </summary>
    accountNumber,

    /// <summary>
    /// ИНН получателя.
    /// </summary>
    inn
}

// JSON-структуры
internal record Credentials(string Login, string Password);
public record FeedsStatus (string UploadDatetime, string Type, int Version);

/// <summary>
/// АСОИ FinCERT API
/// </summary>
public class FincertAPI
{
    private static FincertAPI? _instance;
    private static string _token = string.Empty;
    private static string _api = "https://lk.fincert.cbr.ru";
    private readonly TlsClient _tlsClient = new();

    /// <summary>
    /// Функция получения экземпляра класса с API-вызовами.
    /// </summary>
    /// <returns>Экземпляр класса.</returns>
    public static FincertAPI GetInstance() => _instance ??= new();

    /// <summary>
    /// Функция подключения к АСОИ ФинЦЕРТ.
    /// </summary>
    /// <param name="server">Адрес сервера.</param>
    /// <param name="login">Логин.</param>
    /// <param name="password">Пароль.</param>
    /// <returns>Результат подключения.</returns>
    public async Task<bool> ConnectToASOIAsync(string server, string login, string password)
    {
        if (string.IsNullOrEmpty(server))
            throw new ArgumentNullException(nameof(server), "Не задан сервер.");

        if (string.IsNullOrEmpty(login))
            throw new ArgumentNullException(nameof(login), "Не задан логин.");

        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password), "Не задан пароль.");

        try
        {
            /*
            POST <Корневой URL API>/api/v1/account/login
            Content-Type: application/json
            {
              "login": "Administrator",
              "password": "P@ssw0rd"
            }

            200:
            eyJhbGciOiJCJ9...eyJodHRwOi8
            */

            _api = server + "/api/v1/";

            var response = await _tlsClient.PostAsJsonAsync(
                _api + "account/login",
                new Credentials(login, password));

            response.EnsureSuccessStatusCode();
            _token = await response.Content.ReadAsStringAsync();

            // Authorization: Bearer eyJhbGciOiJCJ9...eyJodHRwOi8
            _tlsClient.DefaultRequestHeaders.Authorization = new("Bearer", _token);
            return true;
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Функция выхода из системы.
    /// </summary>
    /// <returns>Результат выхода.</returns>
    public async Task<bool> LogoutFromASOIAsync()
    {
        try
        {
            using var response = await _tlsClient.PostAsJsonAsync(_api + "account/logout", string.Empty);
            response.EnsureSuccessStatusCode();
        
            if (response.StatusCode == HttpStatusCode.OK)
            {
                _token = string.Empty;
                _tlsClient.DefaultRequestHeaders.Authorization = new("Bearer", _token);
                return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Функция получения сведений по актуальности фидов.
    /// </summary>
    /// <param name="feed">Тип фидов.</param>
    /// <returns>JSON-структура, содержащая дату обновления фидов.</returns>
    /// <exception cref="NotImplementedException"/>
    public async Task<FeedsStatus?> GetFeedsStatusAsync(FeedType feed)
    {
        try
        {
            /*
            GET <Корневой URL API>/antifraud/feeds/<Тип фидов>

            200:
            {
              "uploadDatetime": "<Строка в формате RFC 3339>",
              "type": "<Строка>",
              "version": 1
            }

            где
            uploadDatetime - Время загрузки фида в систему ("2024-01-15T09:01:27.523112+03:00");
            type - Тип фидов - соответствует типу фидов, указанному в URL запроса ("hashPassport");
            version - Номер версии (1).

            Возможные типы фидов в URL запроса:
            — hashPassport — хеш-суммы номеров и серий паспортов;
            — hashSnils — хеш-суммы СНИЛС;
            — inn — ИНН;
            — phoneNumber — номера телефонов;
            — cardNumber — номера платежных карт;
            — accountNumber — номера счетов;
            — ewalletNumber — номера электронных кошельков;
            — swift — счета SWIFT;
            — retailAtm — Retail/ATM;
            — fastPayNumber — Система быстрых платежей.
            */

            for (int i = 0; i < 3; i++) // Сколько делаем попыток получить данные
            {
                using var response = await _tlsClient.GetAsync(_api + $"antifraud/feeds/{feed}");
                response.EnsureSuccessStatusCode();

                if (response.StatusCode == HttpStatusCode.OK) // Ответ получен
                {
                    return await response.Content.ReadFromJsonAsync<FeedsStatus>();
                }

                if (response.StatusCode == HttpStatusCode.NoContent) // Система не готова предоставить ответ
                {
                    Thread.Sleep(150000); // Ждем 150 секунд
                }
                else break; // Ошибка
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Функция получения фидов в формате CSV.
    /// </summary>
    /// <param name="feed">Тип фидов.</param>
    /// <returns>Сплошной неформатированный текст,
    /// состоящий из строк разделенных запятыми значений.</returns>
    /// <exception cref="NotImplementedException"/>
    public async Task<Stream?> GetFeedsAsync(FeedType feed)
    {
        try
        {
            /*
            GET <Корневой URL API>/antifraud/feeds/<Тип фидов>/download

            200:
            Номер телефона,date,count,country
            79001245677,06.08.2019 21:00:00,1,RUS
            79001245678,13.08.2019 21:00:00,2,RUS
            79001245679,14.08.2019 00:00:00,1,RUS
            ...

            где
            Возможные типы фидов в URL запроса:
            — hashPassport — хеш-суммы номеров и серий паспортов;
            — hashSnils — хеш-суммы СНИЛС;
            — inn — ИНН;
            — phoneNumber — номера телефонов;
            — cardNumber — номера платежных карт;
            — accountNumber — номера счетов;
            — ewalletNumber — номера электронных кошельков;
            — swift — счета SWIFT;
            — retailAtm — Retail/ATM;
            — fastPay — Система быстрых платежей.
            */

            for (int i = 0; i < 3; i++) // Сколько делаем попыток получить данные
            {
                using var response = await _tlsClient.GetAsync(_api + $"antifraud/feeds/{feed}/download");
                response.EnsureSuccessStatusCode();

                if (response.StatusCode == HttpStatusCode.OK) // Ответ получен
                {
                    return await response.Content.ReadAsStreamAsync();
                }

                if (response.StatusCode == HttpStatusCode.NoContent) // Система не готова предоставить ответ
                {
                    Thread.Sleep(150000); // Ждем 150 секунд
                }
                else break; // Ошибка
            }
        }
        catch { }
        return null;
    }
}
