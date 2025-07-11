using MicroServices.Auth.API.Data;
using MicroServices.Auth.API.Models;
using MicroServices.Auth.API.Service;
using MicroServices.Auth.API.Service.IService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configuración de la base de datos - MySQL con esquemas separados
builder.Services.AddDbContext<AppDbContext>(option =>
{
    option.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 0)),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10, // ✅ Incrementar para Docker
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            mySqlOptions.CommandTimeout(120); // ✅ Incrementar timeout
            mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore); // ✅ Para una sola DB
        }
    );
});

// ✅ Configurar Identity con prefijos para tablas
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configurar opciones de Identity para mejorar la seguridad
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false; // ✅ Más flexible para Docker
    options.Password.RequiredLength = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // ✅ Configuraciones adicionales para Docker
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ✅ Configurar JWT Options
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("ApiSettings:JwtOptions"));

// ✅ Registrar servicios
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ✅ Configurar JWT Authentication
var secret = builder.Configuration["ApiSettings:JwtOptions:Secret"];
var issuer = builder.Configuration["ApiSettings:JwtOptions:Issuer"];
var audience = builder.Configuration["ApiSettings:JwtOptions:Audience"];

if (string.IsNullOrEmpty(secret))
{
    throw new InvalidOperationException("ApiSettings:JwtOptions:Secret no está configurado en appsettings.json");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // ✅ Importante para Docker
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero, // ✅ Para Docker
        RoleClaimType = "role" // ✅ CRÍTICO: Para que funcionen los roles
    };
    options.MapInboundClaims = false; // ✅ Para preservar claims originales

    // ✅ Eventos para debugging en Docker
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"❌ JWT Authentication failed: {context.Exception.Message}");
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("IS-TOKEN-EXPIRED", "true");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("✅ JWT Token validated successfully");
            return Task.CompletedTask;
        }
    };
});

// ✅ Configurar controladores con opciones para Docker
builder.Services.AddControllers().AddJsonOptions(opts =>
    opts.JsonSerializerOptions.ReferenceHandler =
    System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
);

builder.Services.AddEndpointsApiExplorer();

// ✅ Configurar CORS para Docker
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ✅ Configurar autorización con políticas mejoradas
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("role", "ADMINISTRATOR"));
    options.AddPolicy("CustomerOrAdmin", policy =>
        policy.RequireClaim("role", "CUSTOMER", "ADMINISTRATOR"));
    options.AddPolicy("RequireAuthenticated", policy =>
        policy.RequireAuthenticatedUser());
});

// ✅ Configurar Swagger para Docker
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auth API",
        Version = "v1",
        Description = "API de autenticación para microservicios"
    });

    option.AddSecurityDefinition(name: JwtBearerDefaults.AuthenticationScheme, securityScheme: new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter the Bearer Authorization string as following: `Bearer Generated-JWT-Token`",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// ✅ Configuración para todos los entornos (Docker)
app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API V1");
    c.RoutePrefix = "swagger"; // ✅ Para que funcione con nginx
});

// ✅ Middleware para Docker
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ✅ Aplicar migraciones automáticamente con reintentos para Docker
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var maxRetries = 10;
    var delay = TimeSpan.FromSeconds(5);

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            Console.WriteLine($"🔄 AuthDB: Intento {i + 1} - Aplicando migraciones...");

            // ✅ Verificar conexión primero
            await context.Database.CanConnectAsync();

            // ✅ Aplicar migraciones
            await context.Database.MigrateAsync();

            Console.WriteLine("✅ AuthDB: Migraciones aplicadas correctamente");
            break; // ✅ Salir del loop si es exitoso
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AuthDB: Error en intento {i + 1}: {ex.Message}");

            if (i == maxRetries - 1)
            {
                Console.WriteLine($"❌ AuthDB: Falló después de {maxRetries} intentos");
                throw; // ✅ Re-lanzar excepción en el último intento
            }

            Console.WriteLine($"⏳ AuthDB: Esperando {delay.TotalSeconds}s antes del siguiente intento...");
            await Task.Delay(delay);
        }
    }
}

Console.WriteLine("🚀 Microservicio de Auth iniciado correctamente");
app.Run();