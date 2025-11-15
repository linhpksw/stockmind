using AspectCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using stockmind.Commons.Configurations;
using stockmind.Commons.Swagger;
using stockmind.Filters;
using stockmind.Middlewares;
using stockmind.Models;
using stockmind.Repositories;
using stockmind.Services;
using stockmind.Utils;
using System;
using System.Text;

namespace stockmind
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            const string FrontendCorsPolicy = "FrontendCorsPolicy";

            var connectionString = builder.Configuration.GetConnectionString("MyCnn");
            builder.Services.AddDbContext<StockMindDbContext>(opt =>
                opt.UseSqlServer(connectionString));

            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Query", LogLevel.Warning);

            var jwtSection = builder.Configuration.GetSection("Jwt");
            builder.Services.Configure<JwtSettings>(jwtSection);
            var jwtSettings = jwtSection.Get<JwtSettings>()
                               ?? throw new InvalidOperationException("Jwt: configuration section is missing.");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

            // Add services to the container.

            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(FrontendCorsPolicy, policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins);
                    }
                    else
                    {
                        policy.AllowAnyOrigin();
                    }

                    policy.AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            builder.Services.AddScoped<ApiExceptionFilter>();
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ApiExceptionFilter>();
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "StockMind API", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme.ToLowerInvariant(),
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter JWT token in the format: Bearer {token}"
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
                options.OperationFilter<SwaggerSecurityRequirementsOperationFilter>();
            });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<AuthRepository>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<JwtTokenService>();
            builder.Services.AddScoped<SupplierRepository>();
            builder.Services.AddScoped<SupplierService>();
            builder.Services.AddScoped<DataSeeder>();
            builder.Services.AddScoped<PoService>();
            builder.Services.AddScoped<PoRepository>();
            builder.Services.AddScoped<GrnRepository>();
            builder.Services.AddScoped<GrnService>();
            builder.Services.AddScoped<StockMovementRepository>();
            builder.Services.AddScoped<StockMovementService>();
            builder.Services.AddScoped<ReplenishmentService>();
            builder.Services.AddScoped<MarkdownRuleRepository>();
            builder.Services.AddScoped<SalesOrderItemRepository>();
            builder.Services.AddScoped<MarkdownService>();
            builder.Services.AddScoped<WasteService>();

            builder.Services.ConfigureDynamicProxy();
            builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());

            builder.Services.AddScoped<InventoryService>();
            builder.Services.AddScoped<ProductService>();
            builder.Services.AddScoped<ProductRepository>();
            builder.Services.AddScoped<CategoryRepository>();
            builder.Services.AddScoped<CategoryService>();
            builder.Services.AddScoped<MarginProfileRepository>();
            builder.Services.AddScoped<MarginProfileService>();

            builder.Services.AddScoped<LotService>();
            builder.Services.AddScoped<LotRepository>();

            builder.Services.Configure<AlertsOptions>(builder.Configuration.GetSection("Alerts"));
            builder.Services.AddScoped<AlertsService>();
            builder.Services.AddScoped<AlertsRepository>();


            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                seeder.SeedAsync().GetAwaiter().GetResult();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseMiddleware<RequestResponseLoggingMiddleware>();

            app.UseCors(FrontendCorsPolicy);

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
