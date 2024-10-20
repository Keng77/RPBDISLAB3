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

            // �������� ������ ����������� �� secrets.json
            var connectionString = builder.Configuration.GetConnectionString("MsSqlConnection");

            // ��������� ������ ����������� �� secrets.development.json
            var fallbackConnectionString = builder.Configuration.GetConnectionString("RemoteConnection");

            // ����������� ��������
            builder.Services.AddDbContext<InspectionsDbContext>(options =>
            {
                // �������� ������������ � �������� ������� �����������
                var usedConnectionString = connectionString;
                try
                {
                    // ������� ��������� ����� ��� �������� �����������
                    var tempOptions = new DbContextOptionsBuilder<InspectionsDbContext>()
                        .UseSqlServer(connectionString)
                        .Options;

                    // ������� ������������ � ���� ������
                    using var context = new InspectionsDbContext(tempOptions);
                    Console.WriteLine("������� ����������� � ��");
                    if (!context.Database.CanConnect())
                    {
                        throw new Exception("�������� ����������� �� ��������.");
                    }
                }
                catch
                {
                    // � ������ ������� ������������� �� ��������� ������ �����������
                    Console.WriteLine("���������� ��������� �����������.");
                    usedConnectionString = fallbackConnectionString;
                }

                // ������������ ������������� ������ ����������� (�������� ��� ���������)
                options.UseSqlServer(usedConnectionString);
            });

            // ����������� ����������� � ������
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<CachedDataService>();

            // ����������� ������
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            app.UseSession();

            // ��������� ������� ��������
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    string strResponse = "<HTML><HEAD><TITLE>������� ��������</TITLE></HEAD>" +
                    "<META http-equiv='Content-Type' content='text/html; charset=utf-8'/>" +
                    "<BODY>";
                    strResponse += "<BR><A href='/table'>�������</A>";
                    strResponse += "<BR><A href='/info'>����������</A>";
                    strResponse += "<BR><A href='/searchform1'>SearchForm1</A>";
                    strResponse += "<BR><A href='/searchform2'>SearchForm2</A>";
                    strResponse += "</BODY></HTML>";
                    await context.Response.WriteAsync(strResponse);
                    return;
                }
                await next.Invoke();
            });

            // ��������� ����������
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/info")
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    string strResponse = "<HTML><HEAD><TITLE>����������</TITLE></HEAD>" +
                    "<META http-equiv='Content-Type' content='text/html; charset=utf-8'/>" +
                    "<BODY><H1>����������:</H1>";
                    strResponse += "<BR> ������: " + context.Request.Host;
                    strResponse += "<BR> ����: " + context.Request.Path;
                    strResponse += "<BR> ��������: " + context.Request.Protocol;
                    strResponse += "<BR><A href='/'>�������</A></BODY></HTML>";
                    await context.Response.WriteAsync(strResponse);
                    return;
                }
                await next.Invoke();
            });

            app.Use(async (context, next) =>
            {
                // ���������, ���� ������ �� '/table'
                if (context.Request.Path == "/table")
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    string strResponse = "<HTML><HEAD><TITLE>�������</TITLE></HEAD>" +
                     "<META http-equiv='Content-Type' content='text/html; charset=utf-8'/>" +
                     "<BODY>";
                    // ������ �� ��� �������
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
                // ���������, ���������� �� ������ � '/table', � ���������� ��� �������
                if (context.Request.Path.StartsWithSegments("/table", out var remainingPath) && remainingPath.HasValue && remainingPath.Value.StartsWith("/"))
                {
                    context.Response.ContentType = "text/html; charset=utf-8"; // ��������� Content-Type
                    var tableName = remainingPath.Value.Substring(1); // ������� ��������� ����

                    var cachedService = context.RequestServices.GetService<CachedDataService>();

                    // ������ ������ �������
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
                        // ���� ������� �� �������, ���������� 404
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("������� �� �������");
                    }

                    return; // ��������� ��������� �������
                }
                await next.Invoke();
            });

            async Task RenderTable<T>(HttpContext context, IEnumerable<T> data)
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                var html = "<table border='1' style='border-collapse:collapse'>";

                var type = typeof(T);

                // ��������� ���������� ������� �� ������ ������� ����
                html += "<tr>";
                foreach (var prop in type.GetProperties())
                {
                    // ���������� ��������, ������� �������� ��������� ������ ������� ��� �����������
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
                // ����������� ���� � ����, ������� ��������� �������� (string, DateTime � �.�.)
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
