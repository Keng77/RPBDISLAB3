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

            app.Map("/searchform1", async (HttpContext context) =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                var dbContext = context.RequestServices.GetService<InspectionsDbContext>();

                if (context.Request.Method == "GET")
                {
                    // Получаем список типов собственности и адресов из БД
                    var ownershipTypes = await dbContext.Enterprises
                                                         .Select(e => e.OwnershipType)
                                                         .Distinct()
                                                         .ToListAsync();

                    var addresses = await dbContext.Enterprises
                                                    .Select(e => e.Address)
                                                    .Distinct()
                                                    .ToListAsync();

                    var html = "<form method='post'>";
                    html += "<label for='EnterpriseName'>Enterprise Name:</label><br/>";
                    html += "<input type='text' id='EnterpriseName' name='EnterpriseName'><br/><br/>";

                    html += "<label for='OwnershipType'>Ownership Type:</label><br/>";
                    html += "<select id='OwnershipType' name='OwnershipType'>";
                    html += "<option value=''>All</option>";  // Значение по умолчанию для 'All'
                    foreach (var ownershipType in ownershipTypes)
                    {
                        html += $"<option value='{ownershipType}'>{ownershipType}</option>";
                    }
                    html += "</select><br/><br/>";

                    html += "<label for='Address'>Address:</label><br/>";
                    // Используем 'size' для создания прокручиваемого списка
                    html += "<select id='Address' name='Address' size='5'>";  // размер выпадающего списка 5
                    html += "<option value=''>All</option>";  // Значение по умолчанию для 'All'
                    foreach (var address in addresses)
                    {
                        html += $"<option value='{address}'>{address}</option>";
                    }
                    html += "</select><br/><br/>";

                    html += "<button type='submit'>Search</button>";
                    html += "</form>";

                    await context.Response.WriteAsync(html);
                }
                else if (context.Request.Method == "POST")
                {
                    var formData = await context.Request.ReadFormAsync();
                    var enterpriseName = formData["EnterpriseName"];
                    var ownershipType = formData["OwnershipType"];
                    var address = formData["Address"];

                    var query = dbContext.Enterprises.AsQueryable();

                    // Применяем фильтры
                    if (!string.IsNullOrEmpty(enterpriseName))
                    {
                        query = query.Where(e => e.Name.Contains(enterpriseName));
                    }
                    if (!string.IsNullOrEmpty(ownershipType))
                    {
                        query = query.Where(e => e.OwnershipType == ownershipType);
                    }
                    if (!string.IsNullOrEmpty(address))
                    {
                        query = query.Where(e => e.Address == address);
                    }

                    // Загружаем данные о предприятии и его проверках
                    var enterprise = await query
                                          .Include(e => e.Inspections)
                                          .ThenInclude(i => i.ViolationType)
                                          .FirstOrDefaultAsync();

                    var html = "<h1>Enterprise Search Results (Cookie)</h1>";

                    // Отображаем данные поиска с заменой пустых значений на "All"
                    html += "<h3>Search Criteria:</h3>";
                    html += "<table border='1' style='border-collapse:collapse'>";
                    html += "<tr><th>Enterprise Name</th><th>Ownership Type</th><th>Address</th></tr>";

                    // Если в поле было выбрано "All", заменяем его на "All" в выводе
                    html += $"<tr><td>{(string.IsNullOrEmpty(enterpriseName) ? "All" : enterpriseName)}</td>";
                    html += $"<td>{(string.IsNullOrEmpty(ownershipType) ? "All" : ownershipType)}</td>";
                    html += $"<td>{(string.IsNullOrEmpty(address) ? "All" : address)}</td></tr>";

                    html += "</table>";

                    if (enterprise != null)
                    {
                        html += $"<h2>{enterprise.Name}</h2>";
                        html += "<h3>Violations:</h3>";

                        var violations = enterprise.Inspections
                                                   .Where(i => i.ViolationType != null)
                                                   .Select(i => new
                                                   {
                                                       ViolationId = i.ViolationTypeId,
                                                       ViolationName = i.ViolationType.Name,
                                                       InspectionDate = i.InspectionDate,
                                                   })
                                                   .Distinct()
                                                   .ToList();

                        if (violations.Count == 0)
                        {
                            html += "<p>No violations found.</p>";
                        }
                        else
                        {
                            html += "<table border='1' style='border-collapse:collapse'>";
                            html += "<tr><th>Violation ID</th><th>Violation Name</th><th>Inspection Date</th></tr>";
                            foreach (var violation in violations)
                            {
                                html += $"<tr><td>{violation.ViolationId}</td><td>{violation.ViolationName}</td><td>{violation.InspectionDate.ToString("yyyy-MM-dd")}</td></tr>";
                            }
                            html += "</table>";
                        }
                    }
                    else
                    {
                        html += "<p>No enterprise found matching the search criteria.</p>";
                    }

                    await context.Response.WriteAsync(html);
                }
            });





            app.Map("/searchform2", async (HttpContext context) =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                var dbContext = context.RequestServices.GetService<InspectionsDbContext>();

                if (context.Request.Method == "GET")
                {
                    // Получаем список типов собственности и адресов из БД
                    var ownershipTypes = await dbContext.Enterprises
                                                         .Select(e => e.OwnershipType)
                                                         .Distinct()
                                                         .ToListAsync();

                    var addresses = await dbContext.Enterprises
                                                    .Select(e => e.Address)
                                                    .Distinct()
                                                    .ToListAsync();

                    // Получаем данные из сессии
                    var enterpriseName = context.Session.GetString("EnterpriseName") ?? "";
                    var ownershipType = context.Session.GetString("OwnershipType") ?? "";
                    var address = context.Session.GetString("Address") ?? "";

                    var html = "<form method='post'>";
                    html += "<label for='EnterpriseName'>Enterprise Name:</label><br/>";
                    html += $"<input type='text' id='EnterpriseName' name='EnterpriseName' value='{enterpriseName}' /><br/><br/>";

                    html += "<label for='OwnershipType'>Ownership Type:</label><br/>";
                    html += "<select id='OwnershipType' name='OwnershipType'>";
                    html += "<option value=''>All</option>";  // Значение по умолчанию для 'All'
                    foreach (var ownership in ownershipTypes)
                    {
                        var selected = ownership == ownershipType ? "selected" : "";
                        html += $"<option value='{ownership}' {selected}>{ownership}</option>";
                    }
                    html += "</select><br/><br/>";

                    html += "<label for='Address'>Address:</label><br/>";
                    // Используем 'size' для создания прокручиваемого списка
                    html += "<select id='Address' name='Address' size='5'>";  // размер выпадающего списка 5
                    html += "<option value=''>All</option>";  // Значение по умолчанию для 'All'
                    foreach (var addr in addresses)
                    {
                        var selected = addr == address ? "selected" : "";
                        html += $"<option value='{addr}' {selected}>{addr}</option>";
                    }
                    html += "</select><br/><br/>";

                    html += "<button type='submit'>Search</button>";
                    html += "</form>";

                    await context.Response.WriteAsync(html);
                }
                else if (context.Request.Method == "POST")
                {
                    var formData = await context.Request.ReadFormAsync();
                    var enterpriseName = formData["EnterpriseName"];
                    var ownershipType = formData["OwnershipType"];
                    var address = formData["Address"];

                    // Сохраняем данные в сессию
                    context.Session.SetString("EnterpriseName", enterpriseName);
                    context.Session.SetString("OwnershipType", ownershipType);
                    context.Session.SetString("Address", address);

                    var query = dbContext.Enterprises.AsQueryable();

                    // Применяем фильтры
                    if (!string.IsNullOrEmpty(enterpriseName))
                    {
                        query = query.Where(e => e.Name.Contains(enterpriseName));
                    }
                    if (!string.IsNullOrEmpty(ownershipType))
                    {
                        query = query.Where(e => e.OwnershipType == ownershipType);
                    }
                    if (!string.IsNullOrEmpty(address))
                    {
                        query = query.Where(e => e.Address == address);
                    }

                    // Загружаем данные о предприятии и его проверках
                    var enterprise = await query
                                          .Include(e => e.Inspections)
                                          .ThenInclude(i => i.ViolationType)
                                          .FirstOrDefaultAsync();

                    var html = "<h1>Enterprise Search Results (Session)</h1>";

                    // Отображаем данные поиска с заменой пустых значений на "All"
                    html += "<h3>Search Criteria:</h3>";
                    html += "<table border='1' style='border-collapse:collapse'>";
                    html += "<tr><th>Enterprise Name</th><th>Ownership Type</th><th>Address</th></tr>";

                    // Если в поле было выбрано "All", заменяем его на "All" в выводе
                    html += $"<tr><td>{(string.IsNullOrEmpty(enterpriseName) ? "All" : enterpriseName)}</td>";
                    html += $"<td>{(string.IsNullOrEmpty(ownershipType) ? "All" : ownershipType)}</td>";
                    html += $"<td>{(string.IsNullOrEmpty(address) ? "All" : address)}</td></tr>";

                    html += "</table>";

                    if (enterprise != null)
                    {
                        html += $"<h2>{enterprise.Name}</h2>";
                        html += "<h3>Violations:</h3>";

                        var violations = enterprise.Inspections
                                                   .Where(i => i.ViolationType != null)
                                                   .Select(i => new
                                                   {
                                                       ViolationId = i.ViolationTypeId,
                                                       ViolationName = i.ViolationType.Name,
                                                       InspectionDate = i.InspectionDate,
                                                   })
                                                   .Distinct()
                                                   .ToList();

                        if (violations.Count == 0)
                        {
                            html += "<p>No violations found.</p>";
                        }
                        else
                        {
                            html += "<table border='1' style='border-collapse:collapse'>";
                            html += "<tr><th>Violation ID</th><th>Violation Name</th><th>Inspection Date</th></tr>";
                            foreach (var violation in violations)
                            {
                                html += $"<tr><td>{violation.ViolationId}</td><td>{violation.ViolationName}</td><td>{violation.InspectionDate.ToString("yyyy-MM-dd")}</td></tr>";
                            }
                            html += "</table>";
                        }
                    }
                    else
                    {
                        html += "<p>No enterprise found matching the search criteria.</p>";
                    }

                    await context.Response.WriteAsync(html);
                }
            });






            app.Run();
        }
    }
}
