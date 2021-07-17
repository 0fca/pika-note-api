using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PikaNoteAPI.Data;
using PikaNoteAPI.Repositories;
using PikaNoteAPI.Services;

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
            services.AddCors(options => options.AddPolicy("Base", builder =>
            {
                builder
                    .WithOrigins("http://localhost:8080", "https://note.lukas-bownik.net")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
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
                            Name = "Arkasian",
                            Email = "lukasbownik99@gmail.com",
                            Url = new Uri("https://me.lukas-bownik.net/")
                        }
                    });
                }
            );
            services.AddSingleton<INoteService>(
                InitializeCosmosClientInstanceAsync(Configuration).GetAwaiter().GetResult());
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseCors("Base");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PikaNote API")); 
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        private static async Task<NoteService> InitializeCosmosClientInstanceAsync(
            IConfiguration configurationSection)
        {
            var client = new CosmosClient(configurationSection.GetConnectionString("Main"));
            var databaseName = configurationSection["DatabaseName"];
            var containerName = configurationSection["ContainerName"];
            var noteRepository = new NoteRepository(client, databaseName, containerName);
            var noteService = new NoteService(noteRepository);
            var database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");
            return noteService;
        }
    }
}
