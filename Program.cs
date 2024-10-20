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

            // Обработка главной страницы
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

            // Обработка информации
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

            app.Use(async (context, next) =>
            {
                // Проверяем, если запрос на '/table'
                if (context.Request.Path == "/table")
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    string strResponse = "<HTML><HEAD><TITLE>Таблицы</TITLE></HEAD>" +
                     "<META http-equiv='Content-Type' content='text/html; charset=utf-8'/>" +
                     "<BODY>";
                    // Ссылки на все таблицы
                    strResponse += "<BR><A href='/table/Enterprises'>Enterprises</A>";
                    strResponse += "<BR><A href='/table/Inspections'>Inspections</A>";
                    strResponse += "<BR><A href='/table/Inspectors'>Inspectors</A>";
                    strResponse += "<BR><A href='/table/ViolationTypes'>ViolationTypes</A>";
                    strResponse += "<BR><A href='/table/VInspectorWorks'>VInspectorWorks</A>";
                    strResponse += "<BR><A href='/table/VOffendingEnterprises'>VOffendingEnterprises</A>";
                    strResponse += "<BR><A href='/table/VPenaltyDetails'>VPenaltyDetails</A>";
                    strResponse += "</BODY></HTML>";
                    await context.Response.WriteAsync(strResponse);
                    return;
                }
                await next.Invoke();
            });

            app.Use(async (context, next) =>
            {
                // Проверяем, начинается ли запрос с '/table', и определяем имя таблицы
                if (context.Request.Path.StartsWithSegments("/table", out var remainingPath) && remainingPath.HasValue && remainingPath.Value.StartsWith("/"))
                {
                    context.Response.ContentType = "text/html; charset=utf-8"; // Установка Content-Type
                    var tableName = remainingPath.Value.Substring(1); // Убираем начальный слэш

                    var cachedService = context.RequestServices.GetService<CachedDataService>();

                    // Логика выбора таблицы
                    if (tableName == "Enterprises")
                    {
                        var list = cachedService.GetEnterprises();
                        await RenderTable(context, list);
                    }
                    else if (tableName == "Inspections")
                    {
                        var list = cachedService.GetInspections();
                        await RenderTable(context, list);
                    }
                    else if (tableName == "Inspectors")
                    {
                        var list = cachedService.GetInspectors();
                        await RenderTable(context, list);
                    }
                    else if (tableName == "ViolationTypes")
                    {
                        var list = cachedService.GetViolationTypes();
                        await RenderTable(context, list);
                    }
                    else if (tableName == "VInspectorWorks")
                    {
                        var list = cachedService.GetVInspectorWorks();
                        await RenderTable(context, list);
                    }
                    else if (tableName == "VOffendingEnterprises")
                    {
                        var list = cachedService.GetVOffendingEnterprises();
                        await RenderTable(context, list);
                    }
                    else if (tableName == "VPenaltyDetails")
                    {
                        var list = cachedService.GetVPenaltyDetails();
                        await RenderTable(context, list);
                    }
                    else
                    {
                        // Если таблица не найдена, возвращаем 404
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Таблица не найдена");
                    }

                    return; // Завершаем обработку запроса
                }
                await next.Invoke();
            });

            async Task RenderTable<T>(HttpContext context, IEnumerable<T> data)
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                var html = "<table border='1' style='border-collapse:collapse'>";

                var type = typeof(T);

                // Генерация заголовков таблицы на основе свойств типа
                html += "<tr>";
                foreach (var prop in type.GetProperties())
                {
                    // Пропускаем свойства, которые являются объектами других классов или коллекциями
                    if (!IsSimpleType(prop.PropertyType))
                    {
                        continue;
                    }

                    html += $"<th>{prop.Name}</th>";
                }
                html += "</tr>";

                foreach (var item in data)
                {
                    html += "<tr>";
                    foreach (var prop in type.GetProperties())
                    {
                        if (!IsSimpleType(prop.PropertyType))
                        {
                            continue;
                        }

                        var value = prop.GetValue(item);

                        if (value is DateTime dateValue)
                        {
                            html += $"<td>{dateValue.ToString("dd.MM.yyyy")}</td>";
                        }
                        else
                        {
                            html += $"<td>{value}</td>";
                        }
                    }
                    html += "</tr>";
                }

                html += "</table>";
                await context.Response.WriteAsync(html);
            }

            bool IsSimpleType(Type type)
            {
                // Примитивные типы и типы, которые считаются простыми (string, DateTime и т.д.)
                return type.IsPrimitive ||
                       type.IsValueType ||
                       type == typeof(string) ||
                       type == typeof(DateTime) ||
                       type == typeof(decimal);
            }


            app.Run();
        }
    }
}
