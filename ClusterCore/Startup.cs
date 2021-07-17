using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using Universe.CpuUsage;

namespace ClusterCore
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {
            var serviceScopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;

            applicationLifetime.ApplicationStopping.Register(OnShutdown);

            var wsOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120)
            };

            var executionHandler = serviceProvider.GetService<ClusterExecutionHandler>();
            Server.ExecutionHandler = executionHandler;
            app.UseCors("CorsPolicy");
            app.UseWebSockets(wsOptions);

            app.MapWebSocketManager("/program", executionHandler);
            app.MapWebSocketManager("/statistics", serviceProvider.GetService<ClusterStatisticsHandler>());

            app.Use(async (context, next) =>
            {
                await next();

                List<Metrics> metrics = new List<Metrics>();

                metrics.Add((new MetricsClient()).GetMetrics());

                metrics.AddRange(ClusterStatisticsHandler.GetInstance().ConnectionManager.GetAllStatistics());

                var response = context.Response;

                response.StatusCode = 200;
                response.ContentType = "application/json";

                await response.WriteAsync(JsonConvert.SerializeObject(metrics, Formatting.Indented));
            });

            app.UseFileServer();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebSocketManager();
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });
        }

        private void OnShutdown()
        {
            Client.GetInstance().StopThread();
            Server.GetInstance().StopThread();
        }
    }
}
