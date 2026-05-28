using Microsoft.AspNetCore.Mvc;
using SVM.Models;
using System.Text.Json;

namespace SVM.Controllers.UserControllers
{
    public class UserHomeController : Controller
    {
        private readonly HttpClient _client;

        public UserHomeController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        public IActionResult UserHome()
        {
            ViewBag.ShowPreloader = true;
            return View();
        }

        public IActionResult About()
        {
            ViewBag.ShowPreloader = false;
            return View();
        }

        // =========================
        // NOTICE BOARD
        // =========================

        public async Task<IActionResult> Notice()
        {
            ViewBag.ShowPreloader = false;

            var response = await _client.GetAsync("Updates/active");

            if (!response.IsSuccessStatusCode)
            {
                return View(new List<Updates>());
            }

            var data = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var updates = JsonSerializer.Deserialize<List<Updates>>(data, options);

            var notices = updates
                .Where(x => x.Category != null &&
                            x.Category.ToLower() == "notice")
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View(notices);
        }

        // =========================
        // GALLERY
        // =========================

        public async Task<IActionResult> Gallery()
        {
            ViewBag.ShowPreloader = false;

            var response = await _client.GetAsync("Updates/active");

            if (!response.IsSuccessStatusCode)
            {
                return View(new List<Updates>());
            }

            var data = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var updates = JsonSerializer.Deserialize<List<Updates>>(data, options);

            var gallery = updates
                .Where(x => x.Category != null &&
                            x.Category.ToLower() == "event")
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View(gallery);
        }
    }
}