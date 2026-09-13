# Публикуване на сайта

Сайтът е ASP.NET Core приложение (`src/SikUdostovereniya.Web`), което само раздава статични
файлове: страницата, стиловете и папките на изборите. Няма страници с код, няма форми, няма
база данни, няма настройки за пазене в тайна. Публикува се по същия начин като другите
ASP.NET Core сайтове на хостинга: **Visual Studio → Publish** с профил, свален от контролния
панел на SmarterASP.NET. При publish се генерира `web.config` с
`hostingModel="OutOfProcess"` (зададено в `.csproj` чрез `AspNetCoreHostingModel`).

## 1. Преди първото публикуване: сайтът в SmarterASP.NET

1. Control Panel → **Websites** → **Add Website** (или ползвайте вече създаден сайт).
2. В настройките на сайта изберете **.NET Core** и версия **.NET 10** (проектът е `net10.0`).
   При по-стар runtime приложението не стартира и връща 500.
3. Пак там натиснете **Download Publish Profile**. Сваля се файл `.publishsettings` с адреса на
   сървъра и потребителя за Web Deploy и FTP. **Не го слагайте в репото** – папката
   `Properties/PublishProfiles/` е в `.gitignore` точно затова.

## 2. Публикуване от Visual Studio

1. Отворете `SikUdostovereniya.slnx`.
2. Десен бутон върху проекта **SikUdostovereniya.Web** → **Publish** → **Import Profile** →
   изберете сваления `.publishsettings`. Visual Studio създава два профила: **Web Deploy** и
   **FTP**. Ползвайте **Web Deploy** (при него отметката *Remove additional files at
   destination* чисти стари файлове от сървъра).
3. **Publish**. Паролата се въвежда веднъж и се пази криптирана в `.pubxml.user`, който също
   е извън git.

Същото от командния ред, ако профилът вече е импортиран:

```
dotnet publish src\SikUdostovereniya.Web -c Release -p:PublishProfile="<име на профила>"
```

Или без профил – публикуване в папка и качване на цялата папка през FTP / File Manager:

```
dotnet publish src\SikUdostovereniya.Web -c Release -o publish
```

Качва се **цялото съдържание** на `publish\`, включително `web.config`,
`SikUdostovereniya.Web.dll` и `wwwroot\`, в папката на сайта на сървъра (`wwwroot` в
File Manager). При акаунт с няколко сайта всеки сайт е в своя папка под FTP главната папка.

## 3. Домейн

1. Домейнът се купува и управлява при Namecheap: <https://ap.www.namecheap.com/domains/list/>.
2. Control Panel на SmarterASP → **Domain** → **Add Domain** → `udostoverenia-sik.com`,
   свързан към този сайт. Добавете и `www.udostoverenia-sik.com`.
3. В Namecheap → Domain → **Advanced DNS**: **A запис** за `@` към IP адреса на хостинга
   (показан в панела на SmarterASP при домейна) и **CNAME** за `www` към
   `udostoverenia-sik.com`. Алтернатива: в **Nameservers** се задават сървърите за имена на
   SmarterASP; тогава DNS се управлява от техния панел.
4. Разпространението отнема от няколко минути до няколко часа.

## 4. SSL сертификат

Control Panel → **SSL Manager** → безплатен **Let's Encrypt** за домейна и за `www` варианта.
Прави се след като DNS е разпространен (проверката на Let's Encrypt минава през домейна).

Извън Development приложението пренасочва `http://` към `https://` и праща HSTS за една
година. Без валиден сертификат браузърите ще отказват сайта, затова: първо сертификат, после
разпространяване на адреса. Ако сертификатът се бави, временно закоментирайте `UseHsts()` и
`UseHttpsRedirection()` в `Program.cs` и публикувайте отново.

## 5. Проверка след качване

| Проверка | Очакван резултат |
|---|---|
| `https://udostoverenia-sik.com/` | страницата се зарежда |
| `http://udostoverenia-sik.com/` | пренасочва към `https://` |
| Бутонът „Всички файлове в един ZIP“ | сваля се ZIP, който се отваря |
| Връзката към `33-PVR.xlsx` | сваля се .xlsx, който Excel отваря без предупреждение |
| Връзката към `33-PVR.docx` | сваля се .docx, който Word отваря |
| Видеото | плеърът се зарежда (иска HTTPS и Referer, и двете са налице) |
| <kbd>F12</kbd> → Network → документът → Headers | вижда се `Content-Security-Policy` |

## 6. Ако нещо се счупи

**500 / приложението не стартира.** Най-често хостът е на по-стар runtime. Проверете в
панела, че сайтът е на **.NET 10**. Ако не е това: в `web.config` на сървъра сменете
`stdoutLogEnabled="false"` на `"true"`, презаредете, прочетете файла в `logs\` и върнете
`false`. `web.config` се генерира при всяко публикуване, така че промяната се губи при
следващия publish.

**500.19.** Означава, че `web.config` не се приема от IIS: обикновено е качен на грешно място
или е повреден. Публикувайте отново, така че файлът да се генерира наново.

**Страницата се отваря, но файловете за изтегляне дават 404.** Папката на избора не е
попаднала в `wwwroot` при публикуването. Проверете в `.csproj`, че има `Content Include` за
папката и за ZIP-а (по два реда на избор), и публикувайте отново.

**Кирилицата излиза като въпросителни.** Файлът е бил записан с грешна кодировка при
редакция. Всички файлове са UTF-8; редактирайте ги с Visual Studio или VS Code.

## 7. Нов избор или нов образец

1. Нова папка по образец на `2026-10-25-PVR/` с оригиналите на ЦИК в `originali/`.
2. От главната папка на репото:

   ```
   dotnet run --project src\SikUdostovereniya.Tools -- 2026-XX-XX-XXX
   ```

   Инструментът прави Word и Excel двойките, примерите и ZIP файла на папката.
3. В `src\SikUdostovereniya.Web\SikUdostovereniya.Web.csproj` – два нови реда `Content Include`
   по образец на съществуващите, за папката и за ZIP-а.
4. Нов раздел в `wwwroot\index.html` с връзките към новата папка.
5. Публикуване.

Страницата се раздава с `Cache-Control: no-cache`, файловете за изтегляне – с кеш един ден.
Ако след обновяване виждате стара версия на файл, презаредете с <kbd>Ctrl</kbd>+<kbd>F5</kbd>.

## 8. Локално

```
dotnet run --project src\SikUdostovereniya.Web
```

и отваряте <http://localhost:8765/>. Във VS Code същото прави **F5** (конфигурацията в
`.vscode/`, само за тази машина).
