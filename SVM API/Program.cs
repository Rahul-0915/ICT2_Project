
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;
using SVM_API.Services;

namespace SVM_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<SvmContext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionStrings")));
            // Add services to the container.
            builder.Services.AddHttpClient();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            //builder.WebHost.UseUrls("http://0.0.0.0:7191");
            builder.Services.AddScoped<IEmailService, EmailService>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseStaticFiles();   // This enables serving files from wwwroot

            app.MapControllers();

            app.Run();
        }
    }
}