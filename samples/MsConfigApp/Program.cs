﻿namespace MsConfigApp
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using Serilog.Events;

    public class Program
    {
        public static void Main(string[] args)
        {
            var outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    outputTemplate: outputTemplate)
                /*.WriteTo.File(
                    path: "logs/ApiTpl.log",
                    outputTemplate: outputTemplate,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 5,
                    encoding: System.Text.Encoding.UTF8)*/
                .CreateLogger();

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            try
            {
                Log.ForContext<Program>().Information("Application starting...");
                CreateHostBuilder(args, Log.Logger).Build().Run();
            }
            catch (System.Exception ex)
            {
                Log.ForContext<Program>().Fatal(ex, "Application start-up failed!!");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, Serilog.ILogger logger) =>
            Host.CreateDefaultBuilder(args)
                 .ConfigureAppConfiguration((context, builder) =>
                 {
                     var c = builder.Build();

                     var dataId = c.GetValue<string>("NacosConfig:DataId");
                     var group = c.GetValue<string>("NacosConfig:Group");
                     var @namespace = c.GetValue<string>("NacosConfig:Namespace");
                     var optional = c.GetValue<bool>("NacosConfig:Optional");
                     var serverAddresses = c.GetSection("NacosConfig:ServerAddresses").Get<List<string>>();

                     // read configuration from config files
                     // default is json
                     // builder.AddNacosConfiguration(c.GetSection("NacosConfig"));
                     builder.AddNacosV2Configuration(c.GetSection("NacosConfig"), logAction: x => x.AddSerilog(logger));

                     // specify ini or yaml
                     // builder.AddNacosConfiguration(c.GetSection("NacosConfig"), Nacos.IniParser.IniConfigurationStringParser.Instance);
                     // builder.AddNacosConfiguration(c.GetSection("NacosConfig"), Nacos.YamlParser.YamlConfigurationStringParser.Instance);

                     // hard code here
                     /*builder.AddNacosConfiguration(x =>
                     {
                         x.DataId = dataId;
                         x.Group = group;
                         x.Namespace = @namespace;
                         x.Optional = optional;
                         x.ServerAddresses = serverAddresses;
                     });*/
                 })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseUrls("http://*:8787");
                })
            .UseSerilog();
    }
}
