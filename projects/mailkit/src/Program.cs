﻿using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MailKit.Net.Smtp;
using MailKit.Net.Pop3;
using MailKit.Security;
using MimeKit.Text;
using MimeKit;

namespace MailkitBasic
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddTransient<EmailService>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseStaticFiles();
            app.UseMvc();
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

    [Route("")]
    public class HomeController : Controller
    {
        private readonly EmailService _emailService;

        public HomeController(EmailService EmailService)
        {
            _emailService = EmailService;
        }

        [HttpGet("")]
        public IActionResult Index(string message)
        {
            ViewBag.Message = message;
            return View();
        }

        [HttpPost("Send")]
        public IActionResult Send()
        {
            _emailService.Send(new EmailMessage()
            {
                SenderName = "Change This",
                SenderEmail = "Change This",
                RecipientName = "Change This",
                RecipientEmail = "Change This",
                Subject = "Subject",
                Content = "Good day"
            });

            return RedirectToAction("Index", new { Message = "Email is successfully sent" });
        }
    }

    public class EmailService
    {
        public void Send(EmailMessage email)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(email.SenderName, email.SenderEmail));
            message.To.Add(new MailboxAddress(email.RecipientName, email.RecipientEmail));
            message.Subject = email.Subject;

            message.Body = new TextPart(TextFormat.Html)
            {
                Text = email.Content
            };

            using (var emailClient = new SmtpClient())
            {
                // 587 is for smtp server port, this might change from one server to another.
                // SecureSocketOptions.Auto for SSL.
                emailClient.Connect("Change This", 587, SecureSocketOptions.Auto);

                //Remove any OAuth functionality as we won't be using it. 
                emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                emailClient.Authenticate("Change This", "Change This");
                emailClient.Send(message);
                emailClient.Disconnect(true);
            }
        }
    }

    public class EmailMessage
    {
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string RecipientName { get; set; }
        public string RecipientEmail { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
    }
}
