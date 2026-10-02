using API.Infrastructure.Auditoria;
using API.Infrastructure.Filters;
using API.Infrastructure.Jwt;
using BL.Servicios;
using DA.AccesoDatos;
using DA.API;
using DA.API.v2;
using DA.Configuracion;
using DA.Repositorio;
using DA.Repositorio.Repositorio_Auditoria;
using DA.Repositorio.Repositorio_Errores;
using DA.Repositorio.Repositorio_Login;
using DA.Repositorio.Repositorio_Menu;
using DA.Repositorio.Repositorio_PuntoVenta;
using DA.Repositorio.Repositorio_Usuario;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Globalization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Conexiones
builder.Services.Configure<ConfiguracionConexion>(builder.Configuration.GetSection("ConfiguracionConexion"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Configurar rutas de logs dinámicas si aplica
var configConexion = builder.Configuration.GetSection("ConfiguracionConexion").Get<ConfiguracionConexion>();
if (configConexion != null)
{
    FileLogger.ConfigurarRutaDefault(@"C:\LogPuntoVentaGeneral");
    if (!string.IsNullOrWhiteSpace(configConexion.LogPathPuntoVentaCondicionPago))
    {
        FileLogger.ConfigurarRutaModulo("PUNTO_VENTA_COND_PAGO", configConexion.LogPathPuntoVentaCondicionPago);
    }
}

// Servicio JWT
builder.Services.AddSingleton<IJwtService, JwtService>();

// Configuración de Autenticación JWT Bearer
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = !string.IsNullOrEmpty(jwtSettings.Issuer),
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = !string.IsNullOrEmpty(jwtSettings.Audience),
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// SAP Service Layer Singleton & SapServiceClient
builder.Services.AddSingleton<API_ServiceLayer>(sp => new API_ServiceLayer(sp.GetRequiredService<IOptions<ConfiguracionConexion>>().Value));

builder.Services.AddHttpClient(SapServiceClient.HttpClientName)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        CookieContainer = SapServiceClient.SharedCookieContainer,
        AllowAutoRedirect = true,
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    });
builder.Services.AddSingleton<ISapServiceClient, SapServiceClient>();

// Ejecutores de BD
builder.Services.AddScoped<ISqlExecutor, SqlExecutor>();
builder.Services.AddScoped<IHanaExecutor, HanaExecutor>();

// Repositorios requeridos
builder.Services.AddScoped<IErrores, Errores>();
builder.Services.AddScoped<ILogin, Login>();
builder.Services.AddScoped<IUsuario, Usuario>();
builder.Services.AddScoped<IMenu, Menu>();
builder.Services.AddScoped<IPuntoVenta, PuntoVenta>();
builder.Services.AddScoped<DA.Repositorio.Repositorio_Permisos.IPermisosPvRepositorio, DA.Repositorio.Repositorio_Permisos.PermisosPvRepositorio>();

// Auditoría de Endpoints
builder.Services.AddScoped<IAuditoria, Auditoria>();
builder.Services.AddScoped<IEndpointAuditoria, EndpointAuditoria>();
builder.Services.AddScoped<FiltroAuditoriaAttribute>();

// Memoria Caché para Permisos y Rendimiento
builder.Services.AddMemoryCache();

// Capa de Servicios de Negocio (BL)
builder.Services.AddScoped<IPuntoVentaService, PuntoVentaService>();
builder.Services.AddScoped<PuntoVentaService>();
builder.Services.AddScoped<IPermisosPvService, PermisosPvService>();

// CORS para permitir peticiones desde MVC o SPA
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Controllers & JSON con PascalCase/propiedades exactas
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<FiltroAuditoriaAttribute>();
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

// Configuración de Swagger / OpenAPI con soporte JWT Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CBF - API Punto de Venta",
        Version = "v1",
        Description = "API RESTful protegida con JWT para el sistema de Punto de Venta CBF"
    });

    options.CustomSchemaIds(type => type.FullName ?? type.Name);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT Bearer en el formato: Bearer {su_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Serilog Logger
Log.Logger = new LoggerConfiguration()
   .WriteTo.File("C:\\CBF\\Log-PuntoVenta-API-.log", rollingInterval: RollingInterval.Day)
   .CreateLogger();

// Límites de Request
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
});

try
{
    var defaultCulture = new CultureInfo("es-PE");
    var localizationOptions = new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture(defaultCulture),
        SupportedCultures = new List<CultureInfo> { defaultCulture },
        SupportedUICultures = new List<CultureInfo> { defaultCulture }
    };

    CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
    CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

    var app = builder.Build();

    app.UseRequestLocalization(localizationOptions);

    if (app.Environment.IsDevelopment() || true) // Habilitar Swagger siempre para facilitar validaciones
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "CBF Punto de Venta API v1");
            c.RoutePrefix = string.Empty; // Swagger en raíz de la API
        });
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors("CorsPolicy");

    // Normalización de doble slash
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value;
        if (path != null && path.Contains("//"))
        {
            var newPath = System.Text.RegularExpressions.Regex.Replace(path, "/+", "/");
            context.Request.Path = newPath;
        }
        await next();
    });

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    var errorPath = Path.Combine(AppContext.BaseDirectory, "startup_error_api.txt");
    File.WriteAllText(errorPath, ex.ToString());
}
