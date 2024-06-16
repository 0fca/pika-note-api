using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenIddict.Client;
using OpenIddict.Validation.AspNetCore;
using Pika.Adapters.Persistence.Note.Repositories;
using PikaNoteAPI.Extensions;
using PikaNoteAPI.Middlewares;
using PikaNoteAPI.Services;
using PikaNoteAPI.Services.Security;

namespace PikaNoteAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(o =>
                {
                    o.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                })
                .AddJwtBearer();
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.Secure = CookieSecurePolicy.Always;
                options.ConsentCookie.Domain = Configuration.GetSection("Auth")["CookieDomain"];
            });

            services.AddOpenIddict()
                .AddClient(o =>
                {
                    o.AllowPasswordFlow();
                    o.UseSystemNetHttp()
                        .SetProductInformation(typeof(Program).Assembly);
                    o.AddRegistration(new OpenIddictClientRegistration
                    {
                        ClientId = Configuration.GetSection("Auth")["ClientId"],
                        ClientSecret = Configuration.GetSection("Auth")["ClientSecret"],
                        Issuer = new Uri(Configuration.GetSection("Auth")["Authority"], UriKind.Absolute)
                    });
                })
                .AddValidation(o =>
                {
                    o.SetIssuer(Configuration.GetSection("Auth")["Authority"]);
                    o.UseIntrospection()
                        .SetClientId(Configuration.GetSection("Auth")["ClientId"])
                        .SetClientSecret(Configuration.GetSection("Auth")["ClientSecret"]);
                    o.UseSystemNetHttp();
                    o.UseAspNetCore();
                });
            services.AddAuthorization();
            services.AddHealthChecks();
            services.AddCors(options => options.AddPolicy("Base", builder =>
            {
                builder
                    .AllowCredentials()
                    .WithOrigins(new []{"http://note.cloud.localhost:8080", "https://note.lukas-bownik.net"})
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));
            services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "PikaNote API",
                        Version = "v1.0",
                        Description = "Simple REST API for PikaCloud subsystem PikaNote",
                        Contact = new OpenApiContact
                        {
                            Name = "0fca",
                            Email = "lukasbownik99@gmail.com",
                            Url = new Uri("https://lukas-bownik.net/")
                        }
                    });
                }
            );
            services.AddTransient<ISecurityService, SecurityService>();
            services.AddSingleton<INoteService>(
                InitializeCosmosClientInstanceAsync().GetAwaiter().GetResult());
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseOiddictAuthenticationCookieSupport();
            app.UseAuthentication();
            app.UseEnsureJwtBearerValid();
            app.UseMapJwtClaimsToIdentity();

            app.UseCors("Base");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PikaNote API"));
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.UseMiddleware<NoteFileStorageSecurity>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }

        private async Task<NoteService> InitializeCosmosClientInstanceAsync()
        {
            var client = new CosmosClient(Configuration.GetConnectionString("Main"));
            var databaseName = this.Configuration["DatabaseName"];
            var containerName = this.Configuration["ContainerName"];
            var noteRepository = new NoteRepository(client, databaseName, containerName);
            var noteService = new NoteService(noteRepository);
            var database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");
            return noteService;
        }
    }
}