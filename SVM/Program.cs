using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using SVM.Models;

namespace SVM
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddHttpClient();

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<SvmContext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionStrings")));
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 50_000_000; // 50 MB
            });
            var app = builder.Build();
            app.UseStaticFiles(); // This should be there

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();


            app.MapControllerRoute(
      name: "default",
      pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}
