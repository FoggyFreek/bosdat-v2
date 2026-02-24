using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Repositories;
using BosDAT.Infrastructure.Services;
using BosDAT.Infrastructure.Seeding;

namespace BosDAT.API.Extensions;

/// <summary>
/// Extension methods for configuring application services and dependencies in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the PostgreSQL database context with the application.
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }

    /// <summary>
    /// Configures ASP.NET Core Identity with ApplicationUser and role support.
    /// </summary>
    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// Registers JWT bearer authentication with token validation.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        services.Configure<JwtSettings>(jwtSettings);

        var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);
        services.AddAuthentication(options =>
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
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
            .AddPolicy("TeacherOrAdmin", policy => policy.RequireRole("Teacher", "Admin"));

        return services;
    }

    /// <summary>
    /// Registers application services including repositories, unit of work, and business logic services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
        services.AddScoped<ICourseTypePricingService, CourseTypePricingService>();
        services.AddScoped<IRegistrationFeeService, RegistrationFeeService>();
        services.AddScoped<IEnrollmentPricingService, EnrollmentPricingService>();
        services.AddScoped<IStudentTransactionRepository, StudentTransactionRepository>();
        services.AddScoped<IStudentTransactionService, StudentTransactionService>();
        services.AddScoped<IScheduleConflictService, ScheduleConflictService>();
        services.AddScoped<IInvoiceQueryService, InvoiceQueryService>();
        services.AddScoped<IInvoiceGenerationService, InvoiceGenerationService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInvoiceRunService, InvoiceRunService>();
        services.AddScoped<ICreditInvoiceService, CreditInvoiceService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ICourseTypeService, CourseTypeService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<ILessonGenerationService, LessonGenerationService>();
        services.AddScoped<IHolidayService, HolidayService>();
        services.AddScoped<IAbsenceService, AbsenceService>();
        services.AddScoped<IInstrumentService, InstrumentService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<ISchedulingService, SchedulingService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
        services.AddScoped<ICourseTaskService, CourseTaskService>();
        services.AddScoped<ILessonNoteService, LessonNoteService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }

    /// <summary>
    /// Configures Cross-Origin Resource Sharing (CORS) policy.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Registers OpenAPI documentation with JWT Bearer security scheme using Scalar UI.
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((doc, ctx, ct) =>
            {
                doc.Info = new() { Title = "BosDAT API", Version = "v1", Description = "Music School Management System API" };
                return Task.CompletedTask;
            });
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });
        return services;
    }
}

/// <summary>
/// Adds a Bearer security scheme to the OpenAPI document.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token"
        };

        var securityRequirement = new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() }
        };

        foreach (var path in document.Paths.Values)
        {
            foreach (var operation in path.Operations!.Values)
            {
                operation.Security ??= [];
                operation.Security.Add(securityRequirement);
            }
        }

        return Task.CompletedTask;
    }
}
