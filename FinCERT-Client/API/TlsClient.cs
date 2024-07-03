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
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace API;

// JSON-структуры
internal record Credentials(string Login, string Password);

/// <summary>
/// Класс для подключения клиента к HTTP серверу по защищенному протоколу TLS.
/// </summary>
internal static class TlsClient
{
    // UserAgent
    private static readonly string _app = Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? nameof(TlsClient);
    private static readonly string _ver = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "8.0";

    private static HttpClient _httpClient = null!;
    private static TlsConfig _config = null!;
    private static string _token = string.Empty;
    private static string _api = "https://lk.fincert.cbr.ru";

    private static readonly JsonSerializerOptions _jsonPasswordOptions = new()
    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static DateTime _ddosAllowedTime = DateTime.Now;

    public static readonly int _retrySecondsTimeout = 2; // retry * RetryTimeout
    public static readonly int _ddosSecondsTimeout = 1;
    public static readonly int _waitMinutesTimeout = 10;

    /// <summary>
    /// Функция подключения к АСОИ ФинЦЕРТ.
    /// </summary>
    /// <returns>Результат подключения.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task<bool> Login(TlsConfig config)
    {
        if (string.IsNullOrEmpty(config.MyThumbprint))
            throw new ArgumentNullException(nameof(config.MyThumbprint),
                "Не указан отпечаток вашего сертификата.");

        _config = config;
        _api = config.API;

        try
        {
            /*
            POST account/login
            Content-Type: application/json
            {
              "login": "Administrator",
              "password": "P@ssw0rd"
            }

            200:
            eyJhbGciOiJCJ9...eyJodHRwOi8
            */

            HttpClientHandler handler = new()
            {
                UseDefaultCredentials = false,
                ClientCertificateOptions = ClientCertificateOption.Manual,
                //CheckCertificateRevocationList = false,
                //ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => { return true; },
                ServerCertificateCustomValidationCallback = ServerCertificateValidation,
                SslProtocols = SslProtocols.Tls12
            };

            string thumbprint = X509.GetThumbprint(config.MyThumbprint);
            X509Certificate2 certificate = X509.GetMyCertificate(thumbprint);
            handler.ClientCertificates.Add(certificate);

            if (config.VerboseClient)
            {
                Console.WriteLine("--- Client ---");
                Console.WriteLine(X509.CertificateText(certificate));
            }

            if (config.UseProxy)
            {
                // DefaultProxyCredentials = null;
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(config.ProxyAddress);
            }

            _httpClient = new(handler, true);

            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(_app, _ver));

            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            return await LoginAsync(config.Login, config.Password);
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Функция авторизации на сервере.
    /// </summary>
    /// <param name="login">Учетная запись.</param>
    /// <param name="password">Пароль.</param>
    /// <returns>Результат авторизации.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task<bool> LoginAsync(string login, string password)
    {
        if (string.IsNullOrEmpty(login))
            throw new ArgumentNullException(nameof(login), "Не указан ваш логин.");
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password), "Не указан ваш пароль.");

        try
        {
            var cred = new Credentials(login, password);
            string json = JsonSerializer.Serialize(cred, _jsonPasswordOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await ExecuteAsync(HttpMethod.Post, "account/login", content);
            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                // eyJhbGciOiJCJ9...eyJodHRwOi8
                _token = await response.Content.ReadAsStringAsync();
                // Authorization: Bearer eyJhbGciOiJCJ9...eyJodHRwOi8
                _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", _token);
                return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Функция выхода из системы.
    /// </summary>
    /// <returns>Результат выхода.</returns>
    public static async Task<bool> LogoutAsync()
    {
        try
        {
            var response = await ExecuteAsync(HttpMethod.Post, "account/logout");
            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _token = string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", _token);
                _httpClient.Dispose();
                return true;
            }
        }
        catch { }
        return false;
    }

    public static async Task<HttpResponseMessage> GetAsync(string url)
        => await ExecuteAsync(HttpMethod.Get, url);

    //public static async Task<HttpResponseMessage> GetFromJsonAsync(string url, object content)
    //    => await ExecuteAsync(HttpMethod.Get, url, JsonContent.Create(content));

    //public static async Task<HttpResponseMessage> PostAsync(string url, HttpContent? content = null)
    //    => await ExecuteAsync(HttpMethod.Post, url, content);

    public static async Task<HttpResponseMessage> PostAsJsonAsync(string url, object? content = null)
    => await ExecuteAsync(HttpMethod.Post, url, JsonContent.Create(content));

    //public static async Task<HttpResponseMessage> PutAsync(string url, HttpContent? content = null)
    //    => await ExecuteAsync(HttpMethod.Put, url, content);

    //public static async Task<HttpResponseMessage> PutAsJsonAsync(string url, object content)
    //    => await ExecuteAsync(HttpMethod.Put, url, JsonContent.Create(content));

    //public static async Task<HttpResponseMessage> PatchAsync(string url, HttpContent content)
    //    => await ExecuteAsync(HttpMethod.Patch, url, content);

    //public static async Task<HttpResponseMessage> DeleteAsync(string url)
    //    => await ExecuteAsync(HttpMethod.Delete, url);

    //public static async Task<HttpResponseMessage> HeadAsync(string url)
    //    => await ExecuteAsync(HttpMethod.Head, url);

    //public static async Task<HttpResponseMessage> OptionsAsync(string url)
    //    => await ExecuteAsync(HttpMethod.Options, url);

    public static async Task<HttpResponseMessage> ExecuteAsync(HttpMethod method,
        string url, HttpContent? content = null)
    {
        int retry = 0;
        DateTime end = DateTime.Now.AddMinutes(_waitMinutesTimeout);

        while (true)
        {
            if (DateTime.Now < _ddosAllowedTime)
            {
                Thread.Sleep(_ddosAllowedTime - DateTime.Now);
            }

            using (var request = new HttpRequestMessage(method, _api + url))
            {
                if (content != null)
                {
                    request.Content = content;
                }

                var response = await _httpClient.SendAsync(request);
                _ddosAllowedTime = DateTime.Now.AddSeconds(_ddosSecondsTimeout);

                if (!RetryRequired(response.StatusCode) || (DateTime.Now > end))
                {
                    return response;
                }
            }

            int pause = ++retry * _retrySecondsTimeout;
            Thread.Sleep(pause * 1000);
        }
    }

    private static bool RetryRequired(HttpStatusCode code)
    {
        /*
        100 	HttpStatusCode.Continue
        101 	HttpStatusCode.SwitchingProtocols
        102 	HttpStatusCode.Processing
        103 	HttpStatusCode.EarlyHints

        200 	HttpStatusCode.OK
        201 	HttpStatusCode.Created
        202 	HttpStatusCode.Accepted
        203 	HttpStatusCode.NonAuthoritativeInformation
        204 	HttpStatusCode.NoContent
        205 	HttpStatusCode.ResetContent
        206 	HttpStatusCode.PartialContent
        207 	HttpStatusCode.MultiStatus
        208 	HttpStatusCode.AlreadyReported
        226 	HttpStatusCode.IMUsed

        300 	HttpStatusCode.MultipleChoices or HttpStatusCode.Ambiguous
        301 	HttpStatusCode.MovedPermanently or HttpStatusCode.Moved
        302 	HttpStatusCode.Found or HttpStatusCode.Redirect
        303 	HttpStatusCode.SeeOther or HttpStatusCode.RedirectMethod
        304 	HttpStatusCode.NotModified
        305 	HttpStatusCode.UseProxy
        306 	HttpStatusCode.Unused
        307 	HttpStatusCode.TemporaryRedirect or HttpStatusCode.RedirectKeepVerb
        308 	HttpStatusCode.PermanentRedirect

        400 	HttpStatusCode.BadRequest
        401 	HttpStatusCode.Unauthorized
        402 	HttpStatusCode.PaymentRequired
        403 	HttpStatusCode.Forbidden
        404 	HttpStatusCode.NotFound
        405 	HttpStatusCode.MethodNotAllowed
        406 	HttpStatusCode.NotAcceptable
        407 	HttpStatusCode.ProxyAuthenticationRequired
        408 	HttpStatusCode.RequestTimeout
        409 	HttpStatusCode.Conflict
        410 	HttpStatusCode.Gone
        411 	HttpStatusCode.LengthRequired
        412 	HttpStatusCode.PreconditionFailed
        413 	HttpStatusCode.RequestEntityTooLarge
        414 	HttpStatusCode.RequestUriTooLong
        415 	HttpStatusCode.UnsupportedMediaType
        416 	HttpStatusCode.RequestedRangeNotSatisfiable
        417 	HttpStatusCode.ExpectationFailed
        418 	I'm a teapot
        421 	HttpStatusCode.MisdirectedRequest
        422 	HttpStatusCode.UnprocessableEntity
        423 	HttpStatusCode.Locked
        424 	HttpStatusCode.FailedDependency
        426 	HttpStatusCode.UpgradeRequired
        428 	HttpStatusCode.PreconditionRequired
        429 	HttpStatusCode.TooManyRequests
        431 	HttpStatusCode.RequestHeaderFieldsTooLarge
        451 	HttpStatusCode.UnavailableForLegalReasons

        500     HttpStatusCode.InternalServerError
        501     HttpStatusCode.NotImplemented
        502     HttpStatusCode.BadGateway
        503     HttpStatusCode.ServiceUnavailable
        504     HttpStatusCode.GatewayTimeout
        505     HttpStatusCode.HttpVersionNotSupported
        506     HttpStatusCode.VariantAlsoNegotiates
        507     HttpStatusCode.InsufficientStorage
        508     HttpStatusCode.LoopDetected
        510     HttpStatusCode.NotExtended
        511     HttpStatusCode.NetworkAuthenticationRequired
        */

        // RetryRequired if
        return (code >= HttpStatusCode.InternalServerError) // 500+
            || (code == HttpStatusCode.RequestTimeout) // 408
            || (code == HttpStatusCode.TooManyRequests) // 429
            || (code == HttpStatusCode.NoContent); // 204
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

        if (_config.ValidateServerThumbprint && !string.IsNullOrEmpty(_config.ServerThumbprint) &&
            certificate?.GetCertHashString() != X509.GetThumbprint(_config.ServerThumbprint))
            return false;

        return true;
    }
}

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
