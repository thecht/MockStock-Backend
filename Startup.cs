using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockStockBackend.DataModels;
using MockStockBackend.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MockStockBackend
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Set database provider
            services.AddEntityFrameworkNpgsql().AddDbContext<ApplicationDbContext>(opt =>
                opt.UseNpgsql(Configuration.GetConnectionString("PostgreSQLConnectionString")));
            
            // Do environment-specific setup.
            if(Environment.GetEnvironmentVariable("ASPNET_ENVIRONMENT") == "PRODUCTION")
            {
                // Apply any new database migrations
                services.BuildServiceProvider().GetService<ApplicationDbContext>().Database.Migrate();
            }
            else // Development Environment 
            {
                // Set a development secret for JWT token authentication. (WARNING: DO NOT PUT PRODUCTION SECRET HERE!)
                Environment.SetEnvironmentVariable("APP_TOKENSECRET", "lu2ijh342inlksIUSA84likj3h24uibh23TE5eu2jh34iluli2");
            }
            
            // services.AddEntityFrameworkNpgsql().AddDbContext<ApplicationDbContext>(opt =>
            //     opt.UseNpgsql(Configuration.GetConnectionString("PostgreSQLConnectionString")));
            // services.AddEntityFrameworkMySql().AddDbContext<ApplicationDbContext>(opt =>
            //     opt.UseMySql(Configuration.GetConnectionString("MySQLConnectionString")));
            
            // Business Logic Services
            services.AddScoped<LeagueService>();
            services.AddScoped<UserService>();
            services.AddScoped<StockService>();
            services.AddScoped<TransactionService>();
            services.AddScoped<PortfolioService>();

            // configure jwt authentication
            var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("APP_TOKENSECRET"));
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var userId = Int32.Parse(context.Principal.Identity.Name);
                        context.HttpContext.Items["userId"] = userId;
                        return Task.CompletedTask;
                    }
                };
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
