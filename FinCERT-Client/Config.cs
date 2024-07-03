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

namespace FincertClient;

/// <summary>
/// Файл настроек App.config.json
/// </summary>
public class Config
{
    /// <summary>
    /// Секция настройки TLS.
    /// </summary>
    public TlsConfig Tls { get; set; } = new();

    /// <summary>
    /// Загружать фиды.
    /// </summary>
    public bool Feeds { get; set; } = true;

    /// <summary>
    /// Папка для сохранения полученных файлов фидов.
    /// </summary>
    public string FeedsDownloads { get; set; } = nameof(FeedsDownloads);

    /// <summary>
    /// Загружать бюллетени.
    /// </summary>
    public bool Bulletins { get; set; } = true;

    /// <summary>
    /// Папка для сохранения полученных файлов бюллетеней.
    /// </summary>
    public string BulletinsDownloads { get; set; } = nameof(BulletinsDownloads);
}
