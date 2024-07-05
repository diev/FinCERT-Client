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

using TLS;

namespace FincertClient;

/// <summary>
/// Файл настроек App.config.json
/// </summary>
public class Config
{
    /// <summary>
    /// Признак нового файла - надо удалить или отключить его
    /// для начала использования программой.
    /// </summary>
    public bool NewConfig { get; set; }

    /// <summary>
    /// Секция настройки TLS.
    /// </summary>
    public TlsConfig? Tls { get; set; }

    /// <summary>
    /// Загружать фиды.
    /// </summary>
    public bool Feeds { get; set; }

    /// <summary>
    /// Папка для сохранения полученных файлов фидов.
    /// </summary>
    public string? FeedsDownloads { get; set; }

    /// <summary>
    /// Загружать бюллетени.
    /// </summary>
    public bool Bulletins { get; set; }

    /// <summary>
    /// Папка для сохранения полученных файлов бюллетеней.
    /// </summary>
    public string? BulletinsDownloads { get; set; }

    /// <summary>
    /// Папка 1 для сохранения полученных файлов фидов МВД.
    /// </summary>
    public string? MvdDownloads1 { get; set; }

    /// <summary>
    /// Папка 2 для сохранения полученных файлов фидов МВД.
    /// </summary>
    public string? MvdDownloads2 { get; set; }

    /// <summary>
    /// Папка для ведения логов.
    /// </summary>
    public string? Logs { get; set; }

    public Config() { }

    public Config(bool newConfig)
    {
        NewConfig = newConfig;

        Tls = new(newConfig);

        Feeds = false;
        FeedsDownloads = nameof(FeedsDownloads);

        Bulletins = false;
        BulletinsDownloads = nameof(BulletinsDownloads);
        MvdDownloads1 = nameof(MvdDownloads1);
        MvdDownloads2 = nameof(MvdDownloads2);

        Logs = nameof(Logs);
    }
}
