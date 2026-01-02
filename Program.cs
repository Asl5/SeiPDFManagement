using Microsoft.EntityFrameworkCore;
using SeiPDFManagement.Authorization;
using SeiPDFManagement.Data;
using SeiPDFManagement.Models;
using SeiPDFManagement.Repositories;
using SeiPDFManagement.Services;
using SeiPDFManagement.Services.Context;
using SeiPDFManagement.Services.MailProcessing;
using SeiPDFManagement.Services.SeiPdfExport;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// --- Configurazione logging su file giornaliero ---
var logDir = builder.Configuration["Logging:LogDirectory"] ?? "C:\\temp\\seipdfmanagement_logs";
Directory.CreateDirectory(logDir);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(logDir, "seipdf_log_.txt"), // crea un file per giorno
        rollingInterval: RollingInterval.Day,         // 1 file per giorno
        retainedFileCountLimit: 365,                   // tiene gli ultimi 365 giorni
        shared: true,
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}\r\n"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "SeiPDF Management API",
        Version = "v1"
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});


builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddHttpClient();
//builder.Services.AddScoped<IMailService, GraphMailService>();
builder.Services.AddScoped<IRequestContext, RequestContext>();

builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<SeiPdfAttachmentProcessor>();
builder.Services.Configure<SeiPdfExportSettings>(builder.Configuration.GetSection("SeiPdfExport"));

builder.Services.AddScoped<ISeiPdfExportRepository, SeiPdfExportRepository>();
builder.Services.AddScoped<ISeiPdfExportService, SeiPdfExportService>();
builder.Services.AddDbContext<AuthorizationDbContext>(options =>
{
    options.UseOracle(
        builder.Configuration.GetConnectionString("OracleConnectionString"));
});
builder.Services.AddScoped<IUserAuthorizationService, UserAuthorizationServiceEF>();


// Swagger


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // ← anche se ora non l’hai ancora messa, va qui
app.UseAuthorization();

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SeiPDF Management API v1");
    c.RoutePrefix = "swagger"; // https://host/swagger
});

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/swagger"),
    swaggerApp =>
    {
        swaggerApp.Use(async (context, next) =>
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Response.StatusCode = 401;
                return;
            }

            var authService = context.RequestServices
                .GetRequiredService<IUserAuthorizationService>();

            if (!await authService.IsEnabledAsync(context.User.Identity!.Name!))
            {
                context.Response.StatusCode = 403;
                return;
            }

            await next();
        });
    });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");





app.Run();
