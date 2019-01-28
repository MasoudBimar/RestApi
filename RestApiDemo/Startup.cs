using System;
using System.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;
using Npgsql;
using Dapper;

namespace RestApiDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddCors(o => o.AddPolicy("AllowCors", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            }));

            if (Configuration["DatabaseType"] == "oracle")
            {
                services.AddSingleton<IDbConnection>(provider =>
                {
                    return new OracleConnection(Configuration["ConnectionStrings:OraConnection"]);
                });
            }
            else
            {
                services.AddSingleton<IDbConnection>(provider =>
                {
                    return new NpgsqlConnection(Configuration["ConnectionStrings:PgConnection"]);
                });
            }


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            // app.UseHttpsRedirection();
            app.UseCors("AllowCors");
            app.UseMvc();

            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }
    }
}
