# FinCERT-Client

[![Build status](https://ci.appveyor.com/api/projects/status/hpsbfj3qds34i4yb?svg=true)](https://ci.appveyor.com/project/diev/fincert-client)
[![GitHub Release](https://img.shields.io/github/release/diev/FinCERT-Client.svg)](https://github.com/diev/FinCERT-Client/releases/latest)

Получение по API фидов и бюллетеней из FinCERT (АСОИ ФинЦЕРТ) Банка России.

Здесь два проекта:

* FeedsAPI - обновленный старый референсный проект из бюллетеня
FinCERT-20220304-INFO: "О получении фидов посредством API" для скачивания
фидов с добавлением двусторонней аутентификации TLS, которой в нем не было.
Развивать далее этот проект не планируется.
* FinCERT-Client - новый структурированный проект с добавлением Bulletins
API - возможность скачивать бюллетени и файлы к ним - в дополнение к фидам.

Эти программы работают и в ЗОЭ (прошли чек-лист), и в ЗПЭ, в режиме
read-only. Для предотвращения блокировки от DDoS, в программу вставлены
рекомендуемые задержки между запросами.

### Feeds API / Фиды

Фиды скачиваются актуальные, затирая прежние, контроль необходимости
обновления пока не ведется. Ставьте нужное время по Планировщику.

### Bulletins API / Бюллетени 

Бюллетени скачиваются в отдельные папки с датой, временем, названием.
Внутри каждой папки есть файл Bulletin.txt, в котором то же, что видно и
в ЛК, а также все файлы, которые были приложениями к бюллетеню.

В первый раз будет скачано 100 (если не ограничить параметром `-limit`)
последних бюллетеней.

При последующих запусках будут скачиваться новые последние, пока не будет
достигнуто ограничение API в 100, ограничение параметром или встречена
папка, что была уже загружена ранее.

Чтобы скачать более ранние (за предел 100), нужно использовать параметр
`-offset`. Имейте в виду, что если этот параметр не 0, то отключается
прекращение скачивания при встрече ранее скачанной папки и будут скачаны
все недостающие, попавшие в этот лимит.

Если имя скачиваемого файла начинается с "feeds_20" (типа
feeds_20240703-03.zip), то его содержимое будет распаковано в две папки,
которые указаны в конфиге параметрами `MvdDownloads1` и `MvdDownloads2`.
Если имена этих папок совпадают, то будет распаковано только один раз.
Эта функциональность еще будет дорабатываться.

## Config / Конфигурация

При первом запуске и отсутствии файла конфигурации `.config.json`, он
создается рядом с программой с параметрами по умолчанию.
Никакие другие конфиги, переменные среды окружения и т.п. не используются.

В этом файле есть параметр `NewConfig` (true) - программа будет ждать
корректировки созданного нового файла конфигурации при каждом запуске,
пока этот параметр не будет удален или переключен в false.

Важно заполнить вашими данными значения параметров:

* `MyThumbprint` - отпечаток сертификата клиента, зарегистрированного на
сервере в ЛК и имеющего допуск к серверу;
* `Login` - учетная запись на сервере (логин);
* `Password` - пароль учетной записи.

Если пароль пуст, то программа попытается найти в *Диспетчере учетных
данных* (*Windows Credential Manager* в Панели управления) при запуске
на Windows учетку по строке из `Login` (создайте там учетку с именем
`FinCERT`, например).

Если указываете файловые пути, то по правилам JSON надо удваивать `\\`
в Windows и использовать `/` в Linux.

## Parameters / Опциональные параметры командной строки

* `-checklist` - сформировать в папке `BulletinsDownloads\CheckList`
комплект файлов для приложения к чек-листу на подключение.
* `-feeds` - получить время последнего обновления фидов на сервере.
Также это самый "экономичный" способ проверить функционирование программы.
* `-limit 100` - число от 1 до 100 - ограничение числа скачиваемых
бюллетеней. 100 - по умолчанию и максимум - это ограничение API - обойти
его невозможно - только указанием параметра `-offset`.
* `-offset 0` - число от 0 и выше - сдвиг начала скачиваемых бюллетеней.
0 - по умочанию. Указание этого параметра не 0 переключает режим поведения
при встрече ранее скачанной папки:
  * 0 - скачивание прекращается;
  * не 0 - ранее скачанная папка пропускается, продолжается перебор (в
пределах значения `-limit`).

### Первоначальное скачивание истории

При настройках по умолчанию (limit 100, offset 0) программа скачает в
пустую папку при первом запуске все 100 последних бюллетеней.
(Если это не требуется - можно указать limit меньше.)

Чтобы скачать далее, нужно (оставляя limit 100) наращивать при каждом
запуске offset с шагом несколько менее 100 (90, 180, ...), так как за
время скачивания могут появиться новые бюллетени, они встанут в начало
списка, и вся их история сдвинется.

Программа и так не станет скачивать заново те бюллетени, что уже есть.
А вот пропустить что-то, указывая offset ровно по границе с шагом 100 -
вы можете.

### Ежедневное пополнение

При настройках по умолчанию (limit 100, offset 0) программа скачает все
новые бюллетени от начала списка до уже имеющегося и не пойдет дальше.

## Exit codes / Коды возврата

* 0 - успешно;
* 1 - общая ошибка;
* далее специализированные (список пока может изменяться).

## Requirements / Требования

* .NET 6-7-8 (Windows или Linux)
* КриптоПро для подключения с сертификатом TLS
* Сертификат TLS клиента и цепочка доверия
* Логин и пароль

Вариант Linux пока не тестировался, Stunnel программе не требуется.

Пример сборки проекта под Linux (укажите нужную версию .NET) из папки
с файлом FinCERT-Client.csproj:

    dotnet publish -r linux-x64 -f net6.0 --self-contained

Запуск:

    dotnet FinCERT-Client.dll

## Versioning / Порядок версий

Номер версии программы указывается по нарастающему принципу и строится
от актуальной версии .NET на момент разработки и даты редакции:

* Актуальная версия .NET (8);
* Год текущей разработки (2024);
* Месяц без первого нуля и день редакции (624 - 24.06.2024);
* Номер билда, если указан - просто нарастающее число для внутренних отличий.

Продукт развивается для собственных нужд, а не по коробочной
стратегии, и поэтому *Breaking Changes* могут случаться чаще,
чем это принято в *SemVer*. Поэтому проще по датам актуализации кода.

При обновлении программы рекомендуется сохранить предыдущий конфиг,
удалить его из папки с программой, чтобы она создала новый, перенести
необходимые старые значения в новый конфиг перед новым запуском
программы.

## License / Лицензия

Licensed under the [Apache License, Version 2.0](LICENSE).
Вы можете использовать эти материалы под свою ответственность.
