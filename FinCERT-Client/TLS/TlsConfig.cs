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

namespace TLS;

/// <summary>
/// Секция Tls в файле настроек App.config.json
/// </summary>
public class TlsConfig
{
    /// <summary>
    /// Показывать дамп сертификата клиента при подключении.
    /// </summary>
    public bool VerboseClient { get; set; }

    /// <summary>
    /// Отпечаток сертификата клиента, зарегистрированного на сервере в ЛК и
    /// имеющего допуск к серверу (замените на Ваш сертификат!).
    /// </summary>
    public string? MyThumbprint { get; set; }

    /// <summary>
    /// Учетная запись на сервере (если Password пустой,
    /// то будет попытка найти в Диспетчере учетных данных по этому Login).
    /// </summary>
    public string? Login { get; set; }

    /// <summary>
    /// Пароль учетной записи
    /// (если пустой, то будет попытка найти в Диспетчере учетных данных по Login).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Показывать дамп сертификата сервера при подключении.
    /// </summary>
    public bool VerboseServer { get; set; }

    /// <summary>
    /// Адрес базового URL API (сервер + базовый путь с версией).
    /// </summary>
    public string? API { get; set; }

    /// <summary>
    /// Проверять отпечаток сервера
    /// (имеет смысл лишь при указанном ServerThumbprint).
    /// </summary>
    public bool ValidateServerThumbprint { get; set; }

    /// <summary>
    /// Отпечаток сертификата сервера ServerAddress
    /// (имеет смысл при включенном ValidateThumbprint,
    /// но если только кто-то отслеживает его изменения на сервере).
    /// </summary>
    public string? ServerThumbprint { get; set; }

    /// <summary>
    /// Проверять валидность сертификатов для подключения.
    /// </summary>
    public bool ValidateTls { get; set; }

    /// <summary>
    /// Использовать прокси для подключения.
    /// </summary>
    public bool UseProxy { get; set; }

    /// <summary>
    /// Адрес и порт сервера прокси
    /// (имеет смысл при включенном UseProxy).
    /// </summary>
    public string? ProxyAddress { get; set; }

    /// <summary>
    /// Писать в лог запросы.
    /// </summary>
    public bool VerboseRequests { get; set; }
    
    /// <summary>
    /// Писать в лог паузы между запросами.
    /// </summary>
    public bool VerboseWaits { get; set; }

    public TlsConfig() { }

    public TlsConfig(bool newConfig)
    {
        VerboseClient = newConfig;
        MyThumbprint = string.Empty;
        Login = "FinCERT";
        Password = string.Empty;

        VerboseServer = false;
        API = "https://zoe-api.fincert.cbr.ru/api/v1/";
        ValidateServerThumbprint = false;
        ServerThumbprint = string.Empty;
        ValidateTls = false;

        UseProxy = false;
        ProxyAddress = "http://192.168.2.1:3128";

        VerboseRequests = false;
        VerboseWaits = false;
    }
}
