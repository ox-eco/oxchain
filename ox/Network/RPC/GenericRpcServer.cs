using Akka.Actor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Extensions;
using OX.IO;
using OX.IO.Json;
using OX.Ledger;
using OX.Network.P2P;
using OX.Network.P2P.Payloads;
using OX.Persistence;
using OX.Plugins;
using OX.SmartContract;
using OX.VM;
using OX.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OX.Network.RPC
{
    public abstract class GenericRpcServer : IDisposable
    {

        private IWebHost host;
        public GenericRpcServer()
        {

        }

        public void Dispose()
        {
            if (host != null)
            {
                host.Dispose();
                host = null;
            }
        }

        protected abstract bool PreProcessAsync(string path, Dictionary<string, string> query, out string resp);
        private async Task ProcessAsync(HttpContext context)
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            context.Response.Headers["Access-Control-Max-Age"] = "31536000";
            Dictionary<string, string> dic = new Dictionary<string, string>();
            foreach (KeyValuePair<string, StringValues> kvp in context.Request.Query)
            {
                dic[kvp.Key] = kvp.Value.ToString();
            }
            if (PreProcessAsync(context.Request.GetEncodedPathAndQuery(), dic, out string resp))
            {
                await context.Response.WriteAsync(resp, Encoding.UTF8);
            }
        }



        public void Start(int port, string sslCert = null, string password = null)
        {
            host = new WebHostBuilder().UseKestrel(options => options.Listen(IPAddress.Any, port, listenOptions =>
            {
                if (!string.IsNullOrEmpty(sslCert))
                    listenOptions.UseHttps(sslCert, password);
            }))
            .Configure(app =>
            {
                app.UseResponseCompression();
                app.Run(ProcessAsync);
            })
            .ConfigureServices(services =>
            {
                services.AddResponseCompression(options =>
                {
                    // options.EnableForHttps = false;
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json-rpc" });
                });

                services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });
            })
            .Build();

            host.Start();
        }
    }
}