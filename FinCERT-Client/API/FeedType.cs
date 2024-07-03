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

namespace API;

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
