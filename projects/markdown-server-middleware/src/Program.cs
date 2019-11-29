using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore;

namespace MarkdownServerWithMiddleware
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Use((next) =>
            {
                var markdown = new MarkdownMiddleware(next, env);
                return markdown.Invoke;
            });

            app.Run(context =>
            {
                return context.Response.WriteAsync("\nIf you read this, the file does not exist.");
            });
        }
    }

    public class MarkdownMiddleware
    {
        readonly RequestDelegate _next;

        readonly IHostingEnvironment _env;
        public MarkdownMiddleware(RequestDelegate next, IHostingEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            _env.ContentRootPath = Directory.GetCurrentDirectory();
            _env.WebRootPath = Path.Combine(_env.ContentRootPath, "markdown");

            var requestPath = context.Request.Path;

            //Get default page
            if (requestPath == "/")
            {
                var defaultMd = Path.Combine(_env.WebRootPath, "index.md");
                if (!File.Exists(defaultMd))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("File Not Found");
                    await _next.Invoke(context);
                    //write here for post logic
                    return;
                }

                //no more processing. This code is shortcircuited.
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(ProduceMarkdown(defaultMd));
                return;
            }

            //Replace the path and remove the beginning \ of the path
            //every request path segment represent a folder within markdown folder, e.g. 
            // /about/us is mapped to markdown\about\us.md File
            // /hello is mapped to markdown\hello.md

            var localPath = requestPath.ToString().Replace('/', '\\').TrimStart(new char[] { '\\' }) + ".md";
            var md = Path.Combine(_env.WebRootPath, localPath);
            if (!File.Exists(md))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("File Not Found");
                await _next.Invoke(context);
                //write here for post logic
                return;
            }

            //no more processing. This code is shortcircuited.
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(ProduceMarkdown(md));
        }

        string ProduceMarkdown(string path)
        {
            var md = File.ReadAllText(path);

            var res = Markdig.Markdown.ToHtml(md);
            return res;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder.UseStartup<Startup>()
                );
    }
}