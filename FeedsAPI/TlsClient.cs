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
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace FeedsAPI;

/// <summary>
/// Класс для подключения клиента к HTTP серверу по защищенному протоколу TLS.
/// </summary>
internal class TlsClient : HttpClient
{
    private static readonly HttpClientHandler _handler;
    private static readonly Config _config = Program.Config;

    static TlsClient()
    {
        _handler = new()
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            //CheckCertificateRevocationList = false,
            //ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => { return true; },
            ServerCertificateCustomValidationCallback = ServerCertificateValidation,
            SslProtocols = SslProtocols.Tls12
        };

        string thumbprint = X509.GetThumbprint(_config.MyThumbprint);
        X509Certificate2 certificate = X509.GetMyCertificate(thumbprint);
        _handler.ClientCertificates.Add(certificate);

        if (_config.VerboseClient)
        {
            Console.WriteLine("--- Client ---");
            Console.WriteLine(X509.CertificateText(certificate));
        }

        if (_config.UseProxy)
        {
            // DefaultProxyCredentials = null;
            _handler.UseProxy = true;
            _handler.Proxy = new WebProxy(_config.ProxyAddress);
        }
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    public TlsClient() : base(_handler)
    {
        //BaseAddress = new Uri(_config.ServerAddress);
    }

    /// <summary>
    /// Деструктор.
    /// </summary>
    ~TlsClient()
    {
        Dispose();
        _handler.Dispose();
    }

    /// <summary>
    /// Callback функция, вызываемая для самостоятельной проверки сертификата сервера
    /// при подключении к нему.
    /// </summary>
    /// <param name="requestMessage">Запрос к серверу.</param>
    /// <param name="certificate">Сертификат сервера.</param>
    /// <param name="chain">Путь сертификации.</param>
    /// <param name="sslErrors">Возможные ошибки проверки сертификата по протоколу.</param>
    /// <returns></returns>
    private static bool ServerCertificateValidation(HttpRequestMessage requestMessage,
        X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslErrors)
    {
        if (_config.VerboseServer)
        {
            Console.WriteLine("--- Server ---");
            // It is possible to inspect the certificate provided by the server.
            Console.WriteLine($"Request:    {requestMessage.RequestUri}");

            Console.Write(X509.CertificateText(certificate));

            // Based on the custom logic it is possible to decide
            // whether the client considers certificate valid or not
            Console.WriteLine($"Tls errors: {sslErrors}");
        }

        if (_config.ValidateTls && sslErrors != SslPolicyErrors.None)
            return false;

        if (_config.ValidateThumbprint && !string.IsNullOrEmpty(_config.ServerThumbprint) &&
            certificate?.GetCertHashString() != X509.GetThumbprint(_config.ServerThumbprint))
            return false;

        return true;
    }
}
