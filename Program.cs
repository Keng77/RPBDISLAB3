using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RPBDISLAB3.Controllers;
using RPBDISLAB3.Services;

namespace RPBDISLAB3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Основная строка подключения из secrets.json
            var connectionString = builder.Configuration.GetConnectionString("MsSqlConnection");

            // Резервная строка подключения из secrets.development.json
            var fallbackConnectionString = builder.Configuration.GetConnectionString("RemoteConnection");

            // Регистрация сервисов
            builder.Services.AddDbContext<InspectionsDbContext>(options =>
            {
                // Пытаемся подключиться с основной строкой подключения
                var usedConnectionString = connectionString;
                try
                {
                    // Создаем временные опции для проверки подключения
                    var tempOptions = new DbContextOptionsBuilder<InspectionsDbContext>()
                        .UseSqlServer(connectionString)
                        .Options;

                    // Пробуем подключиться к базе данных
                    using var context = new InspectionsDbContext(tempOptions);
                    Console.WriteLine("Попытка подключения к бд");
                    if (!context.Database.CanConnect())
                    {
                        throw new Exception("Основное подключение не работает.");
                    }
                }
                catch
                {
                    // В случае неудачи переключаемся на резервную строку подключения
                    Console.WriteLine("Используем резервное подключения.");
                    usedConnectionString = fallbackConnectionString;
                }

                // Регистрируем окончательную строку подключения (основную или резервную)
                options.UseSqlServer(usedConnectionString);
            });

            // Регистрация кэширования и сессий
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<CachedDataService>();

            // Регистрация сессий
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });


            var app = builder.Build();

            app.UseSession();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    string strResponse = "<HTML><HEAD><TITLE>Главная страница</TITLE></HEAD>" +
                    "<META http-equiv='Content-Type' content='text/html; charset=utf-8'/>" +
                    "<BODY>";
                    strResponse += "<BR><A href='/table'>Таблицы</A>";
                    strResponse += "<BR><A href='/info'>Информация</A>";
                    strResponse += "<BR><A href='/searchform1'>SearchForm1</A>";
                    strResponse += "<BR><A href='/searchform2'>SearchForm2</A>";
                    strResponse += "</BODY></HTML>";
                    await context.Response.WriteAsync(strResponse);
                    return;
                }
                await next.Invoke();
            });

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/info")
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    string strResponse = "<HTML><HEAD><TITLE>Информация</TITLE></HEAD>" +
                    "<META http-equiv='Content-Type' content='text/html; charset=utf-8'/>" +
                    "<BODY><H1>Информация:</H1>";
                    strResponse += "<BR> Сервер: " + context.Request.Host;
                    strResponse += "<BR> Путь: " + context.Request.Path;
                    strResponse += "<BR> Протокол: " + context.Request.Protocol;
                    strResponse += "<BR><A href='/'>Главная</A></BODY></HTML>";
                    await context.Response.WriteAsync(strResponse);
                    return;
                }
                await next.Invoke();
            });

            app.Run();
        }
    }
}
