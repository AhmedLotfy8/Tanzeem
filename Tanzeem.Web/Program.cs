using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.AIDemand;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Persistence;
using Tanzeem.Persistence.Data.DbContexts;
using Tanzeem.Persistence.Data.Migrations;
using Tanzeem.Services.Abstractions.AI;
using Tanzeem.Services.Abstractions.Alerts;
using Tanzeem.Services.Abstractions.Authentication;
using Tanzeem.Services.Abstractions.Branches;
using Tanzeem.Services.Abstractions.BusinessCore;
using Tanzeem.Services.Abstractions.Companies;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Dashboard;
using Tanzeem.Services.Abstractions.DeliveryIssues;
using Tanzeem.Services.Abstractions.Notifications;
using Tanzeem.Services.Abstractions.Onboarding;
using Tanzeem.Services.Abstractions.Orders;
using Tanzeem.Services.Abstractions.Products;
using Tanzeem.Services.Abstractions.Settings;
using Tanzeem.Services.Abstractions.Suppliers;
using Tanzeem.Services.Abstractions.Transactions;
using Tanzeem.Services.Alerts;
using Tanzeem.Services.Authentication;
using Tanzeem.Services.Branches;
using Tanzeem.Services.BusinessCore;
using Tanzeem.Services.Companies;
using Tanzeem.Services.Current;
using Tanzeem.Services.Dashboard;
using Tanzeem.Services.DeliveryIssues;
using Tanzeem.Services.Notifications;
using Tanzeem.Services.Onboarding;
using Tanzeem.Services.Orders;
using Tanzeem.Services.Products;
using Tanzeem.Services.Settings;
using Tanzeem.Services.Suppliers;
using Tanzeem.Services.Transactions;
using Tanzeem.Shared;

namespace Tanzeem.Web {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            #region Added Services
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ProductHelperService>();
            builder.Services.AddScoped<ICompanyService, CompanyService>();
            builder.Services.AddScoped<IBranchService, BranchService>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IBusinessCoreService, BusinessCoreService>();
            builder.Services.AddScoped<IOnboardingService, OnboardingService>();
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
            builder.Services.AddScoped<ICurrentService, CurrentService>();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<ISupplierService, SupplierService>();
            builder.Services.AddScoped<IOrderService,OrderService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IAlertService, AlertService>();
            builder.Services.AddScoped<IAlertConfigurationsService, AlertConfigurationsService>();
            builder.Services.AddScoped<IDeliveryIssuesService, DeliveryIssuesService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IDemandForecastingService, DemandForecastingService>();
            #endregion

            #region Added Authentication

            var jwtOptions = builder.Configuration.GetSection("JwtOptions").Get<JwtOptions>()!;

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey))
                    };
                });

            #endregion

            #region Added Hangfire
            builder.Services.AddHangfire(config => config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer() //when create job use simple service name not full name with version and Public Key Token
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHangfireServer();
            #endregion

            #region Add CORS

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("https://tanzeem.runasp.net/", "https://tanzeem-self.vercel.app/")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            #endregion

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            builder.Services.AddDbContext<TanzeemDbContext>(options => {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });



            var app = builder.Build();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            #region background services
            using (var scope = app.Services.CreateScope())
            {
                var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                recurringJobManager.AddOrUpdate(
                    "check-inventory-weekly",
                    () => notificationService.CreateNotification(),
                    Cron.Weekly(DayOfWeek.Saturday, 1)
                );
                recurringJobManager.AddOrUpdate<DemandForecastingService>(
                        "update-ai-demand-forecast-daily",
                service => service.UpdateAllForecastsAsync(),
                Cron.Daily(23, 0));
            }
            #endregion
            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment()){}
            app.UseSwagger();
            app.UseSwaggerUI(options => {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");
                options.RoutePrefix = string.Empty;
            });

            app.UseHangfireDashboard("/hangfire"); // move it after auth middlewares -- at production phase
            app.UseHttpsRedirection();

            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
