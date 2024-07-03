# FinCERT-Client
[![Build status](https://ci.appveyor.com/api/projects/status/hpsbfj3qds34i4yb?svg=true)](https://ci.appveyor.com/project/diev/fincert-client)
[![GitHub Release](https://img.shields.io/github/release/diev/FinCERT-Client.svg)](https://github.com/diev/FinCERT-Client/releases/latest)

Получение по API фидов и бюллетеней из FinCERT Банка России.

Здесь два проекта:

* FeedsAPI - Обновленный до NET8 референсный проект для скачивания
фидов с добавлением двусторонней аутентификации TLS, которой в нем не было.
* FinCERT-Client - Новый проект с добавлением Bulletins API -
возможность скачивать бюллетени и файлы к ним - в дополнение к фидам.

## Опциональные параметры командной строки

* `-checklist` - сформировать в папке `BulletinsDownloads\CheckList`
комплект файлов для приложения в чек-листу на подключение.

## Requirements

* .NET 8
* CryptoPro CSP
* Сертификат TLS клиента
* Логин и пароль

## Versioning

Номер версии программы указывается по нарастающему принципу:

* Требуемая версия .NET (8);
* Год текущей разработки (2024);
* Месяц без первого нуля и день редакции (624 - 24.06.2024);
* Номер билда - просто нарастающее число для внутренних отличий.
Если настроен сервис AppVeyor, то это его автоинкремент.

Продукт развивается для собственных нужд, и поэтому
Breaking Changes могут случаться чаще, чем это в SemVer.

## License

Licensed under the [Apache License, Version 2.0](LICENSE).
