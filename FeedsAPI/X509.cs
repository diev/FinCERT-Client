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

using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FeedsAPI;

/// <summary>
/// Класс для работы с форматом X509.
/// </summary>
internal static class X509
{
    /// <summary>
    /// Приведение любого скопированного отпечатка к системному (без пробелов, uppercase).
    /// </summary>
    /// <param name="value">Скопированное значение.</param>
    /// <returns>Приведенная строка.</returns>
    public static string GetThumbprint(string value)
        => value.Replace(" ", string.Empty).ToUpper();

    /// <summary>
    /// Получение сертификата из хранилища по его отпечатку.
    /// </summary>
    /// <param name="thumbprint">Отпечаток сертификата.</param>
    /// <param name="validOnly">Отбирать только действительные.</param>
    /// <returns>Искомый сертификат.</returns>
    public static X509Certificate2 GetMyCertificate(string thumbprint, bool validOnly = true)
    {
        using X509Store store = new(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
        var found = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly);

        if (found.Count == 1)
        {
            return found[0];
        }

        throw new ArgumentNullException(nameof(thumbprint),
            $"My certificate thumbprint {thumbprint} not found.");
    }

    /// <summary>
    /// Дамп сведений о сертификате.
    /// </summary>
    /// <param name="certificate">Сертификат.</param>
    /// <returns>Многострочный текст со сведениями из сертификата.</returns>
    public static string CertificateText(X509Certificate2? certificate)
    {
        if (certificate is null)
            return "Certificate not found.";

        StringBuilder sb = new();
        return sb
            .AppendLine($"From date:  {certificate.GetEffectiveDateString()}")
            .AppendLine($"Exp date:   {certificate.GetExpirationDateString()}")
            .AppendLine($"Issuer:     {certificate.Issuer}")
            .AppendLine($"Subject:    {certificate.Subject}")
            .AppendLine($"Serial num: {certificate.GetSerialNumberString()}") //SerialNumber
            .AppendLine($"Thumbprint: {certificate.GetCertHashString()}") //Thumbprint
            .ToString();
    }
}
