using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
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

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseRouting();
            var url = "";
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    var response = context.Response;
                    response.ContentType = "text/html; charset=utf-8";
                    Debug.WriteLine(context.Connection.RemoteIpAddress);

                    var str = new StringBuilder("Use <br/>");
                    str.Append("<div style='white-space:pre'>");
                    str.AppendLine("to detect own IP: <a href='/checkip?url=https://to.route'>/checkip?url=https://to.some.route.that.returns.IP</a>");
                    str.AppendLine("to return IP of request-sender: <a href='/myip'>/myip</a>");
                    str.AppendLine("to ping: <a href='/start?url=https://to.some.route'>/start?url=https://to.some.route</a>");
                    str.AppendLine("to stop :<a href='/stop'>/stop</a>");
                    str.Append("</div>");
                    await response.WriteAsync(str.ToString());
                });
                endpoints.MapGet("/myip", async context =>
                {
                    var ip = context.Connection.RemoteIpAddress.MapToIPv4()?.ToString();
                    await context.Response.WriteAsync(ip ?? "null");
                });
                endpoints.MapGet("/checkip", async context =>
                {
                    url = context.Request.Query["url"].ToString(); //context.Request.RouteValues["url"].ToString();

                    var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Add("User-Agent", "HttpClientFactory-Sample");
                    using var client = new HttpClient();
                    var res = await client.SendAsync(req);
                    var ip = await res.Content.ReadAsStringAsync();

                    await context.Response.WriteAsync("MyIp: " + ip);
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
