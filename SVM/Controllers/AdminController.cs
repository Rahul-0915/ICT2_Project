using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SVM.Models;

namespace SVM.Controllers
{
    public class AdminController : Controller
    {
        private readonly HttpClient _client;

        public AdminController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        public async Task<IActionResult> AdminDashboard()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login", "Account");

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            List<Updates> updatesList = new List<Updates>();

            try
            {
                var response = await _client.GetAsync("Updates");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    updatesList = JsonSerializer.Deserialize<List<Updates>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return View(updatesList ?? new List<Updates>());
        }

        // Keep AdminPanel as it was, or remove if not used
        public async Task<IActionResult> AdminPanel()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login", "Account");

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            List<Updates> updatesList = new List<Updates>();

            try
            {
                var response = await _client.GetAsync("Updates");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    updatesList = JsonSerializer.Deserialize<List<Updates>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return View(updatesList ?? new List<Updates>());
        }
    }
}