using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TestIP
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            var url = "";
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    var response = context.Response;
                    response.ContentType = "text/html; charset=utf-8";
                    await response.WriteAsync("Use<br/><a href='/start?url=https://to.some.route'>/start?https://to.some.route</a> to ping<br/><a href='/stop'>/stop</a> to stop");

                    //context.Response.Headers.Add("ContentType", "text/html; charset=utf-8");
                    //await context.Response.WriteAsync("Use\n  <a href='/start?https://to.some.route'>/start?https://to.some.route</a> to ping\n  <a href='/stop'>/stop</a> to stop");
                });
                endpoints.MapGet("/start", async context =>
                {
                    url = context.Request.Query["url"].ToString(); //context.Request.RouteValues["url"].ToString();
                    PingStart(url);
                    await context.Response.WriteAsync("Started for " + url);
                });
                endpoints.MapGet("/stop", async context =>
                {
                    tokenSource.Cancel();
                    url = "";
                    await context.Response.WriteAsync("Stopped for " + url);
                });
            });
        }

        CancellationTokenSource tokenSource = new();
        public void PingStart(string url)
        {
            tokenSource.Cancel();
            tokenSource = new();
            var ct = tokenSource.Token;
            var task = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    Debug.WriteLine("Ping for " + url);
                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, url);
                        request.Headers.Add("User-Agent", "HttpClientFactory-Sample");
                        using var client = new HttpClient();
                        await client.SendAsync(request);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    if (!ct.IsCancellationRequested)
                        await Task.Delay(4000);
                }
                Debug.WriteLine("Stopped: " + url);
            }, ct);
        }
    }
}
