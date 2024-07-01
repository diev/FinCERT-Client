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

/// <summary>
/// Файл настроек App.config.json
/// </summary>
public class Config
{
    /// <summary>
    /// Отпечаток сертификата клиента, зарегистрированного на сервере в ЛК и
    /// имеющего допуск к серверу.
    /// </summary>
    public string MyThumbprint { get; set; } = "df252a127550135d35d43980e55ee94b98e268b0";
    
    /// <summary>
    /// Показывать дамп сертификата клиента при подключении.
    /// </summary>
    public bool VerboseClient { get; set; } = true;

    /// <summary>
    /// Адрес базового URL API.
    /// </summary>
    public string ServerAddress { get; set; } = "https://zoe-api.fincert.cbr.ru";

    /// <summary>
    /// Учетная запись на сервере.
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Пароль учетной записи.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Проверять отпечаток сервера
    /// (имеет смысл при указанном ServerThumbprint).
    /// </summary>
    public bool ValidateThumbprint { get; set; } = false;

    /// <summary>
    /// Отпечаток сертификата сервера ServerAddress
    /// (имеет смысл при включении ValidateThumbprint).
    /// </summary>
    public string ServerThumbprint { get; set; } = string.Empty;
    
    /// <summary>
    /// Проверять валидность сертификатов для подключения.
    /// </summary>
    public bool ValidateTls { get; set; } = true;
    
    /// <summary>
    /// Показывать дамп сертификата сервера при подключении.
    /// </summary>
    public bool VerboseServer { get; set; } = true;
    
    /// <summary>
    /// Использовать прокси для подключения.
    /// </summary>
    public bool UseProxy { get; set; } = false;
    
    /// <summary>
    /// Адрес и порт сервера прокси
    /// (имеет смысл при включении UseProxy).
    /// </summary>
    public string ProxyAddress { get; set; } = "http://192.168.2.1:3128";

    /// <summary>
    /// Папка для сохранения полученных файлов.
    /// </summary>
    public string Store { get; set; } = string.Empty;
}
