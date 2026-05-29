using Microsoft.AspNetCore.Mvc;
using SVM.Models;
using System.Text.Json;

namespace SVM.Controllers
{
    
    public class AdmissionInquiriesController : Controller
    {
        private readonly HttpClient _client;

        public AdmissionInquiriesController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        /* =========================
           USER SIDE FORM
        ========================= */

        // GET
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    AdmissionInquiry admissionInquiry)
        {
          

            if (string.IsNullOrWhiteSpace(
                admissionInquiry.StudentName))
            {
                ModelState.AddModelError(
                    "StudentName",
                    "Student name is required");
            }

            if (string.IsNullOrWhiteSpace(
                admissionInquiry.ParentName))
            {
                ModelState.AddModelError(
                    "ParentName",
                    "Parent name is required");
            }

            if (string.IsNullOrWhiteSpace(
                admissionInquiry.Phone))
            {
                ModelState.AddModelError(
                    "Phone",
                    "Mobile number is required");
            }

            if (string.IsNullOrWhiteSpace(
                admissionInquiry.ClassName))
            {
                ModelState.AddModelError(
                    "ClassName",
                    "Please select class");
            }

            if (string.IsNullOrWhiteSpace(
                admissionInquiry.Message))
            {
                ModelState.AddModelError(
                    "Message",
                    "Message is required");
            }

            if (!ModelState.IsValid)
            {
                return View(admissionInquiry);
            }

            admissionInquiry.InquiryDate =
                DateTime.Now;

            admissionInquiry.IsSeen = false;

            admissionInquiry.IsAttended = false;

            admissionInquiry.AttendedDate = null;

            admissionInquiry.ReplyMessage = "";

            admissionInquiry.SessionId = 1;

            // API CALL

            var response =
                await _client.PostAsJsonAsync(
                    "AdmissionInquiries",
                    admissionInquiry);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Thank you! We will contact you soon through call or message.";

                return RedirectToAction("Create");
            }

            ModelState.AddModelError(
                "",
                "Something went wrong.");
            var error =
    await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(
                "",
                error);

            return View(admissionInquiry);

            
        }

        /* =========================
           ADMIN PANEL
        ========================= */

        // LIST
        [LoginCheckFilter]
        public async Task<IActionResult> Index()
        {
            List<AdmissionInquiry> inquiryList = new();

            var response =
                await _client.GetAsync("AdmissionInquiries");

            if (response.IsSuccessStatusCode)
            {
                var data =
                    await response.Content.ReadAsStringAsync();

                var options =
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                inquiryList =
                    JsonSerializer.Deserialize
                    <List<AdmissionInquiry>>
                    (data, options)!;

                inquiryList =
                    inquiryList
                    .OrderBy(x => x.IsAttended)
                    .ThenByDescending(x => x.InquiryDate)
                    .ToList();
            }
			ViewBag.UnseenCount = inquiryList.Count(x => x.IsSeen == false);

			return View(inquiryList);
        }

        /* =========================
           DETAILS
        ========================= */
        [LoginCheckFilter]
        public async Task<IActionResult> Details(int id)
        {
            var response =
                await _client.GetAsync(
                    $"AdmissionInquiries/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var data =
                await response.Content.ReadAsStringAsync();

            var options =
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

            var inquiry =
                JsonSerializer.Deserialize
                <AdmissionInquiry>(data, options);

            if (inquiry == null)
            {
                return NotFound();
            }

            // Seen update
            if (inquiry.IsSeen == false)
            {
                inquiry.IsSeen = true;

                await _client.PutAsJsonAsync(
                    $"AdmissionInquiries/{id}",
                    inquiry);
            }

            return View(inquiry);
        }

        /* =========================
           ATTEND INQUIRY
        ========================= */
        [LoginCheckFilter]
        [HttpPost]
        public async Task<IActionResult> Attend(
            int id,
            string replyMessage)
        {
            var response =
                await _client.GetAsync(
                    $"AdmissionInquiries/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var data =
                await response.Content.ReadAsStringAsync();

            var options =
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

            var inquiry =
                JsonSerializer.Deserialize
                <AdmissionInquiry>(data, options);

            if (inquiry == null)
            {
                return RedirectToAction(nameof(Index));
            }

            inquiry.IsAttended = true;
            inquiry.IsSeen = true;
            inquiry.AttendedDate = DateTime.Now;
            inquiry.ReplyMessage = replyMessage;

            await _client.PutAsJsonAsync(
                $"AdmissionInquiries/{id}",
                inquiry);

            TempData["Success"] =
                "Inquiry attended successfully.";

            return RedirectToAction(nameof(Index));
        }

        /* =========================
           DELETE
        ========================= */
        [LoginCheckFilter]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _client.DeleteAsync(
                $"AdmissionInquiries/{id}");

            TempData["Success"] =
                "Inquiry deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        /* =========================
           NOTIFICATION COUNT
        ========================= */
        [LoginCheckFilter]
        [HttpGet]
		public async Task<JsonResult> GetUnseenInquiryCount()
		{
			var response = await _client.GetAsync("AdmissionInquiries");

			if (!response.IsSuccessStatusCode)
				return Json(0);

			var json = await response.Content.ReadAsStringAsync();

			var list = JsonSerializer.Deserialize<List<AdmissionInquiry>>(
				json,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			int count = list?.Count(x => x.IsSeen == false) ?? 0;

			return Json(count);
		}


	}
}