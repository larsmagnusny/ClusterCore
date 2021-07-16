using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.WebSockets;

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
            Client.StopThread();
            Server.StopThread();
        }
    }
}
