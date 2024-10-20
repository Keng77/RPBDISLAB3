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

            app.Run();
        }
    }
}
