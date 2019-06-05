// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Bot.ConfigOptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bot
{
    /// <summary>
    /// The Startup class configures services and the request pipeline.
    /// </summary>
    public class Startup
    {
        public Startup(IHostingEnvironment env, IConfiguration config)
        {
            Env = env;

            //var builder = new ConfigurationBuilder()
            //    .SetBasePath(env.ContentRootPath)
            //    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            //    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            //    .AddEnvironmentVariables();

            //Configuration = builder.Build();
            Configuration = config;
        }

        public IHostingEnvironment Env { get; }
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> specifies the contract for a collection of service descriptors.</param>
        /// <seealso cref="IStatePropertyAccessor{T}"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/dependency-injection"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0"/>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_2);

            // Options pattern for grouping related settings.
            services.Configure<CustomVisionOptions>(Configuration.GetSection("CustomVision"));

            // Prepare storage for state management.
            var storage = new MemoryStorage();
            var userState = new UserState(storage);
            var conversationState = new ConversationState(storage);

            services.AddSingleton(userState);
            services.AddSingleton(conversationState);

            services.AddSingleton<IBot, Bot>();

            // Add the http adapter to enable MVC style bot API
            services.AddSingleton<IBotFrameworkHttpAdapter>((sp) =>
            {
                var appId = Configuration.GetSection("MicrosoftAppId").Value;
                var appPassword = Configuration.GetSection("MicrosoftAppPassword").Value;
                var credentialProvider = new SimpleCredentialProvider(appId, appPassword);

                var botFrameworkHttpAdapter = new BotFrameworkHttpAdapter(credentialProvider)
                {
                    OnTurnError = async (context, exception) =>
                    {
                        await context.SendActivityAsync("Sorry, it looks like something went wrong.");
                    }
                };

                return botFrameworkHttpAdapter;
            });

            // TODO: When use entity framework, be careful of its lifetime. for more information, take a look at the url below.
            // Dependency injection in ASP.NET Core
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.2
            // Warning It's dangerous to resolve a scoped service from a singleton. It may cause the service to have incorrect state when processing subsequent requests.
            // Entity Framework contexts are usually added to the service container using the scoped lifetime because web app database operations are normally scoped to the client request. The default lifetime is scoped if a lifetime isn't specified by an AddDbContext<TContext> overload when registering the database context. Services of a given lifetime shouldn't use a database context with a shorter lifetime than the service.
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseMvc();
        }
    }
}
