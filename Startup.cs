using System;
using System.Text.Json;
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
using PikaNoteAPI.Adapters.Database.Note.Repositories;
using PikaNoteAPI.Application.Extensions;
using PikaNoteAPI.Application.Middlewares;
using PikaNoteAPI.Domain;
using PikaNoteAPI.Domain.Contract;
using PikaNoteAPI.Infrastructure.Adapters.Http;
using PikaNoteAPI.Infrastructure.Adapters.Http.Repositories;
using PikaNoteAPI.Infrastructure.Services.Security;

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
            services.AddDistributedMemoryCache();
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
                options.ConsentCookie.Domain = Configuration["CookieDomain"];
            });

            services.AddOpenIddict()
                .AddClient(o =>
                {
                    o.AllowClientCredentialsFlow();
                    o.UseSystemNetHttp()
                        .SetProductInformation(typeof(Program).Assembly);
                    o.AddRegistration(new OpenIddictClientRegistration
                    {
                        RegistrationId = "base",
                        ClientId = Configuration["ClientId"],
                        ClientSecret = Configuration["ClientSecret"],
                        Issuer = new Uri(Configuration["Authority"], UriKind.Absolute)
                    });
                })
                .AddValidation(o =>
                {
                    o.SetIssuer(Configuration["Authority"]);
                    o.UseIntrospection()
                        .SetClientId(Configuration["ClientId"])
                        .SetClientSecret(Configuration["ClientSecret"]);
                    o.UseSystemNetHttp();
                    o.UseAspNetCore();
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdministratorOrModerator", policy =>
                    policy.RequireAssertion(context =>
                    {
                        var realmAccess = context.User.FindFirst("realm_access")?.Value;
                        if (string.IsNullOrEmpty(realmAccess)) return false;

                        try
                        {
                            using var doc = JsonDocument.Parse(realmAccess);
                            if (doc.RootElement.TryGetProperty("roles", out var rolesElement) &&
                                rolesElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var role in rolesElement.EnumerateArray())
                                {
                                    var roleName = role.GetString();
                                    if (roleName == "Administrator" || roleName == "Moderator")
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            return false;
                        }

                        return false;
                    }));
            });
            services.AddHealthChecks();
            services.AddCors(options => options.AddPolicy("Base", builder =>
            {
                builder
                    .AllowCredentials()
                    .WithOrigins(["http://note.cloud.localhost:8080", "https://note.lukas-bownik.net", "https://note.cloud.localhost:8443"])
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
                            Url = new Uri("https://note.lukas-bownik.net/")
                        }
                    });
                }
            );
            
            services.AddTransient<ISecurityService, SecurityService>();
            services.AddTransient<BucketRepository>();
            services.AddSingleton<NoteStorageHttpClient>();
            services.AddSingleton<INotes>(
                InitializeCosmosClientInstanceAsync().GetAwaiter().GetResult()
                );
            services.AddSingleton<IBuckets, Buckets>();
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseConfigureNotesStorageHttpClient();
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
            //app.UseHttpsRedirection();
            app.UseAuthorization();
            app.UseMiddleware<NoteFileStorageSecurity>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }

        private async Task<Notes> InitializeCosmosClientInstanceAsync()
        {
            var client = new CosmosClient(Configuration.GetConnectionString("Main"));
            var databaseName = this.Configuration["DatabaseName"];
            var containerName = this.Configuration["ContainerName"];
            var noteRepository = new NoteRepository(client, databaseName, containerName);
            var noteFileRepository = new NoteFileRepository(this.Configuration);
            var noteService = new Notes(noteRepository, noteFileRepository);
            var database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");
            return noteService;
        }
    }
}