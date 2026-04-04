using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SVM.Models;

namespace SVM.Controllers
{
    public class SessionsController : Controller
    {
        private readonly HttpClient _client;

        public SessionsController(IHttpClientFactory client)
        {
            _client = client.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        // GET: Sessions
        public async Task<IActionResult> Index()
        {
            List<Session> sessions = new List<Session>();

            var response = await _client.GetAsync("Sessions");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                sessions = JsonSerializer.Deserialize<List<Session>>(data, option);
            }
            else
            {
                ModelState.AddModelError("", "Failed to load sessions");
            }

            return View(sessions);
        }

        // GET: Sessions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var response = await _client.GetAsync($"Sessions/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var data = await response.Content.ReadAsStringAsync();

            var session = JsonSerializer.Deserialize<Session>(data, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(session);
        }

        // GET: Sessions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Sessions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Session session)
        {
            if (ModelState.IsValid)
            {
                var response = await _client.PostAsJsonAsync("Sessions", session);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Create failed!");
            }

            return View(session);
        }

        // GET: Sessions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var response = await _client.GetAsync($"Sessions/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var data = await response.Content.ReadAsStringAsync();

            var session = JsonSerializer.Deserialize<Session>(data, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (session == null)
                return NotFound();

            return View(session);
        }

        // POST: Sessions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Session session)
        {
            if (id != session.SessionId)
                return NotFound();

            if (ModelState.IsValid)
            {
                var response = await _client.PutAsJsonAsync($"Sessions/{id}", session);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Update failed!");
            }

            return View(session);
        }

        // GET: Sessions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var response = await _client.GetAsync($"Sessions/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var data = await response.Content.ReadAsStringAsync();

            var session = JsonSerializer.Deserialize<Session>(data, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (session == null)
                return NotFound();

            return View(session);
        }

        // POST: Sessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var response = await _client.DeleteAsync($"Sessions/{id}");

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Delete failed!");
            return RedirectToAction(nameof(Index));
        }
    }
}