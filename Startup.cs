using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
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

        string _url = "";

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Use(async delegate (HttpContext context, Func<Task> next)
            {
                try { await next.Invoke(); }
                catch (Exception ex) { await context.Response.WriteAsync(ex.ToString()); }
            });

            app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
            app.UseRouting();
            _url = "";

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
                endpoints.MapGet("/health", async context => await context.Response.WriteAsync("healthy"));
                endpoints.MapGet("/myip", async context =>
                {
                    try
                    {
                        var ip = context.Connection.RemoteIpAddress.MapToIPv4()?.ToString();
                        var header = context.Request.Headers["X-Forwarded-For"].ToString();

                        var str = new StringBuilder();
                        str.Append(ip ?? "null");
                        if (!string.IsNullOrEmpty(header))
                        {
                            str.AppendLine(" - context.Connection.RemoteIpAddress.MapToIPv4()?.ToString()");
                            str.Append(header ?? "null");
                            str.AppendLine(" - header 'X-Forwarded-For'");
                        }
                        await context.Response.WriteAsync(str.ToString());
                    }
                    catch (Exception ex)
                    {
                        await context.Response.WriteAsync(ex.ToString());
                    }
                });
                endpoints.MapGet("/checkip", async context =>
                {
                    try
                    {
                        _url = context.Request.Query["url"].ToString(); //context.Request.RouteValues["url"].ToString();

                        var req = new HttpRequestMessage(HttpMethod.Get, _url);
                        req.Headers.Add("User-Agent", "HttpClientFactory-Sample");
                        using var client = new HttpClient();
                        var res = await client.SendAsync(req);
                        var ip = await res.Content.ReadAsStringAsync();

                        await context.Response.WriteAsync("MyIp (output traffic): " + ip);
                    }
                    catch (Exception ex)
                    {
                        await context.Response.WriteAsync(ex.ToString());
                    }
                });
                endpoints.MapGet("/start", async context =>
                {
                    _url = context.Request.Query["url"].ToString(); //context.Request.RouteValues["url"].ToString();
                    PingStart(_url);
                    await context.Response.WriteAsync("Started for " + _url);
                });
                endpoints.MapGet("/stop", async context =>
                {
                    tokenSource.Cancel();
                    _url = "";
                    await context.Response.WriteAsync("Stopped for " + _url);
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
