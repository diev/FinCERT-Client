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

using System.Diagnostics;
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

namespace TLS;

// JSON-структуры
internal record Credential(string Login, string Password);

/// <summary>
/// Класс для подключения клиента к HTTP серверу по защищенному протоколу TLS.
/// </summary>
internal static class TlsClient
{
    private static HttpClient _httpClient = null!;
    private static TlsConfig _config = null!;
    private static string _token = string.Empty;

    private static readonly JsonSerializerOptions _jsonPasswordOptions = new()
    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static DateTime _ddosAllowedTime = DateTime.Now;

    public static readonly int _retrySecondsTimeout = 2; // retry * RetryTimeout
    public static readonly int _ddosSecondsTimeout = 1;
    public static readonly int _waitMinutesTimeout = 10;

    /// <summary>
    /// Подключение к АСОИ ФинЦЕРТ.
    /// </summary>
    public static async Task LoginAsync(TlsConfig config)
    {
        _config = config;
        var token = CreateCredential();
        CreateHttpClient();
        await LoginAsync(token);
    }

    private static StringContent CreateCredential()
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

        string api = _config.API!;
        string login = _config.Login!;
        string password = _config.Password!;

        if (!login.StartsWith("api_fincert@") &&
            !api.Split(':')[1].StartsWith("//zoe-"))
            throw new Exception("Неправильный логин для ЗПЭ.");

        var cred = new Credential(login, password);
        string json = JsonSerializer.Serialize(cred, _jsonPasswordOptions);
        var token = new StringContent(json, Encoding.UTF8, "application/json");
        return token;
    }

    private static HttpClientHandler CreateHttpClientHandler()
    {
        HttpClientHandler handler = new()
        {
            UseDefaultCredentials = false,
            ClientCertificateOptions = ClientCertificateOption.Manual,
            //CheckCertificateRevocationList = false,
            //ServerCertificateCustomValidationCallback =
            //    (request, cert, chain, errors) => { return true; },
            ServerCertificateCustomValidationCallback = ServerCertificateValidation,
            SslProtocols = SslProtocols.Tls12
        };

        string thumbprint = X509.GetThumbprint(_config.MyThumbprint!);
        X509Certificate2 certificate = X509.GetMyCertificate(thumbprint);
        handler.ClientCertificates.Add(certificate);

        if (_config.VerboseClient)
        {
            Console.WriteLine("--- Client ---");
            Console.WriteLine(X509.CertificateText(certificate));
        }

        if (_config.UseProxy)
        {
            // DefaultProxyCredentials = null;
            handler.UseProxy = true;
            handler.Proxy = new WebProxy(_config.ProxyAddress);
        }

        return handler;
    }

    /// <summary>
    /// Callback функция, вызываемая для самостоятельной проверки сертификата сервера
    /// при подключении к нему.
    /// </summary>
    /// <param name="requestMessage">Запрос к серверу.</param>
    /// <param name="certificate">Сертификат сервера.</param>
    /// <param name="chain">Путь сертификации.</param>
    /// <param name="sslErrors">Возможные ошибки проверки сертификата по протоколу.</param>
    /// <returns>Подключение разрешено.</returns>
    private static bool ServerCertificateValidation(HttpRequestMessage requestMessage,
        X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslErrors)
    {
        if (certificate is null)
            return false;

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

        if (_config.ValidateTls && ValidateTlsFail(sslErrors))
            return false;

        if (_config.ValidateServerThumbprint && ValidateServerThumbprintFail(certificate))
            return false;

        return true;

        static bool ValidateServerThumbprintFail(X509Certificate2 certificate)
        {
            bool fail = certificate.GetCertHashString() != 
                X509.GetThumbprint(_config.ServerThumbprint!);
            Trace.WriteLineIf(fail, "ServerThumbprint not match this server.");
            return fail;
        }

        static bool ValidateTlsFail(SslPolicyErrors sslErrors)
        {
            bool fail = sslErrors != SslPolicyErrors.None;
            Trace.WriteLineIf(fail, "TLS protocol error.");
            return fail;
        }
    }

    private static void CreateHttpClient()
    {
        var handler = CreateHttpClientHandler();
        _httpClient = new(handler, true);

        // UserAgent
        string app = Path.GetFileNameWithoutExtension(Environment.ProcessPath)
            ?? nameof(TlsClient);
        string ver = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? "8.0";
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(app, ver));

        //_httpClient.DefaultRequestHeaders.Accept.Add(
        //    new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Авторизации на сервере.
    /// </summary>
    private static async Task LoginAsync(HttpContent token)
    {
        var response = await ExecuteAsync(HttpMethod.Post, "account/login", token);
        // eyJhbGciOiJCJ9...eyJodHRwOi8
        _token = await response.Content.ReadAsStringAsync();
        // Authorization: Bearer eyJhbGciOiJCJ9...eyJodHRwOi8
        _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", _token);
    }

    /// <summary>
    /// Очистка соединения.
    /// </summary>
    public static async Task LogoutAsync()
    {
        await ExecuteAsync(HttpMethod.Post, "account/logout");
        _token = string.Empty;
        _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", _token);
        _httpClient.Dispose();
    }

    #region HttpClient public methods

    public static async Task<HttpResponseMessage> GetAsync(string url)
        => await ExecuteAsync(HttpMethod.Get, url);

    //public static async Task<HttpResponseMessage> GetFromJsonAsync(
    //    string url, object content)
    //    => await ExecuteAsync(HttpMethod.Get, url, JsonContent.Create(content));

    //public static async Task<HttpResponseMessage> PostAsync(
    //    string url, HttpContent? content = null)
    //    => await ExecuteAsync(HttpMethod.Post, url, content);

    public static async Task<HttpResponseMessage> PostAsJsonAsync(
        string url, object? content = null)
        => await ExecuteAsync(HttpMethod.Post, url, JsonContent.Create(content));

    //public static async Task<HttpResponseMessage> PutAsync(
    //    string url, HttpContent? content = null)
    //    => await ExecuteAsync(HttpMethod.Put, url, content);

    //public static async Task<HttpResponseMessage> PutAsJsonAsync(
    //    string url, object content)
    //    => await ExecuteAsync(HttpMethod.Put, url, JsonContent.Create(content));

    //public static async Task<HttpResponseMessage> PatchAsync(
    //    string url, HttpContent content)
    //    => await ExecuteAsync(HttpMethod.Patch, url, content);

    //public static async Task<HttpResponseMessage> DeleteAsync(string url)
    //    => await ExecuteAsync(HttpMethod.Delete, url);

    //public static async Task<HttpResponseMessage> HeadAsync(string url)
    //    => await ExecuteAsync(HttpMethod.Head, url);

    //public static async Task<HttpResponseMessage> OptionsAsync(string url)
    //    => await ExecuteAsync(HttpMethod.Options, url);

    #endregion HttpClient public methods

    private static async Task<HttpResponseMessage> ExecuteAsync(
        HttpMethod method, string url, HttpContent? content = null)
    {
        int retry = 0;
        string api = _config.API + url;
        HttpStatusCode code = HttpStatusCode.Continue;
        DateTime end = DateTime.Now.AddMinutes(_waitMinutesTimeout);

        Trace.WriteLineIf(_config.VerboseRequests,
            $"{method} {url}"); // url | api

        while (DateTime.Now < end)
        {
            if (DateTime.Now < _ddosAllowedTime)
                Thread.Sleep(_ddosAllowedTime - DateTime.Now);

            using (var request = new HttpRequestMessage(method, api))
            {
                if (content != null)
                    request.Content = content;

                retry++;
                var response = await _httpClient.SendAsync(request);
                _ddosAllowedTime = DateTime.Now.AddSeconds(_ddosSecondsTimeout);
                code = response.StatusCode;

                if (!RetryRequired(code))
                {
                    if (code == HttpStatusCode.OK) // for this API only!
                        return response;
                    else
                        throw new ResponseException(code);
                }
            }

            int pause = retry * _retrySecondsTimeout;
            Trace.WriteLineIf(_config.VerboseWaits,
                $"Код {code} - повтор {retry} через {pause} сек.");
            Thread.Sleep(pause * 1000);
        }

        throw new WaitException(_waitMinutesTimeout);
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
        return ((int)code >= (int)HttpStatusCode.InternalServerError) // 500+
            || (code == HttpStatusCode.RequestTimeout)      // 408
            || (code == HttpStatusCode.TooManyRequests)     // 429
            || (code == HttpStatusCode.NoContent);          // 204 if aplicable here
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
            if (string.IsNullOrEmpty(thumbprint))
                throw new Exception(
                    "My certificate thumbprint not found in config.");

            using X509Store store = new(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
            var found = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly);

            if (found.Count == 1)
                return found[0];

            throw new Exception(
                $"My certificate with thumbprint '{thumbprint}' not found in storage.");
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
}

public class TlsConfigException : Exception
{
    const string message = "Параметр '{0}' не указан в конфиге TLS.";

    public TlsConfigException()
        : base() { }

    public TlsConfigException(string paramName)
        : base(string.Format(message, paramName)) { }

    public TlsConfigException(string paramName, Exception inner)
        : base(string.Format(message, paramName), inner) { }
}

public class ResponseException : Exception
{
    const string message = "Ответ сервера отрицательный ({0}).";

    public ResponseException()
        : base() { }

    public ResponseException(HttpStatusCode code)
        : base(string.Format(message, code)) { }

    public ResponseException(HttpStatusCode code, Exception inner)
        : base(string.Format(message, code), inner) { }
}

public class WaitException : Exception
{
    const string message = "За {0} минут сервер не дал данных.";

    public WaitException()
        : base() { }

    public WaitException(int wait)
        : base(string.Format(message, wait)) { }

    public WaitException(int wait, Exception inner)
        : base(string.Format(message, wait), inner) { }
}
