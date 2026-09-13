// Удостоверения за членове на СИК - сайтът.
//
// Приложението само раздава статични файлове от wwwroot: страницата, стиловете и
// папките на изборите (Word и Excel двойките). Няма страници с код, няма форми,
// няма база данни. Съществува като ASP.NET Core приложение единствено защото така
// се публикува и хоства по изпитания начин (IIS + Kestrel, OutOfProcess).

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Заглавки за сигурност на всеки отговор. Разрешени са само собствените стилове и скриптове
// (единственият скрипт е подписът на автора във футъра), иконата и вграденото видео от YouTube.
// Подписът рисува стиловете си като вграден <style> в shadow root; хешът по-долу е на точно
// този блок (wwwroot/js/made-by-david.js, масивът STYLE). Ако файлът се смени, хешът се
// преизчислява: в конзолата на браузъра
//   const t = document.querySelector('made-by-david').shadowRoot.querySelector('style').textContent;
//   btoa(String.fromCharCode(...new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(t)))))
const string signatureStyleHash = "'sha256-SrgF+LPnNcsHz3Qy+hlRwb1M6TgR3iPaXYw9sdXCEnU='";
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["Content-Security-Policy"] =
        "default-src 'none'; script-src 'self'; style-src 'self' " + signatureStyleHash + "; img-src 'self' data:; " +
        "frame-src https://www.youtube-nocookie.com; " +
        "form-action 'none'; base-uri 'none'; frame-ancestors 'none'";
    headers["X-Content-Type-Options"] = "nosniff";
    // YouTube иска Referer (само адресът на сайта, без път), иначе плеърът дава грешка 153.
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), interest-cohort=()";
    await next();
});

// Файловете на изборите са свързани в проекта (Link в .csproj) и при build попадат в
// bin/.../wwwroot. При dotnet run сайтът раздава и оттам; след publish двете папки съвпадат.
var buildWwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
var files = Directory.Exists(buildWwwroot)
    ? new CompositeFileProvider(app.Environment.WebRootFileProvider, new PhysicalFileProvider(buildWwwroot))
    : app.Environment.WebRootFileProvider;

var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".md"] = "text/markdown; charset=utf-8";
contentTypes.Mappings[".zip"] = "application/zip";

app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = files,
    ContentTypeProvider = contentTypes,
    OnPrepareResponse = ctx =>
    {
        // Страницата се проверява при всяко отваряне; файловете за изтегляне - кеш един ден.
        var isPage = ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
        ctx.Context.Response.Headers.CacheControl = isPage ? "no-cache" : "public, max-age=86400";
    }
});

app.Run();
