using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text;
using GenerativeAI;
using GenerativeAI.Models;
using GenerativeAI.Tools;
using Microsoft.Extensions.Options;

namespace CreateAuthomaSchemes.Pages
{
    public class GenerateMachineModel : PageModel
    {
        [BindProperty]
        public string MachineDescription { get; set; }
        
        [BindProperty]
        public string ApiResponse { get; set; } = string.Empty;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GoogleAiOptions _options;

        public GenerateMachineModel(IHttpClientFactory httpClientFactory, IOptions<GoogleAiOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public void OnGet()
        {
            ViewData["Title"] = "Generowanie";
        }

        //public async Task<IActionResult> OnPost()
        //{
        //    ViewData["Title"] = "Generowanie";
        //    ApiResponse = "no co tam";
        //    Console.WriteLine(ApiResponse + MachineDescription);
        //    return Page();
        //}

        public async Task<IActionResult> OnPost()
        {
            var apiKey = _options.ApiKey;
            //var httpClient = _httpClientFactory.CreateClient("GoogleGeminiAI");
            //var geminiAi = new Gemini15Flash(apiKey);

            // Zbuduj treść żądania JSON zgodnie z wymaganiami API
            var promptRequest = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = MachineDescription }
                        }
                    }
                },
                generationConfig = new { response_mime_type = "application/json" }
            };

            var jsonRequest = JsonSerializer.Serialize(promptRequest);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var model = new GenerativeModel(apiKey);

            try
            {
                var result = await model.GenerateContentAsync(MachineDescription);
                // Wysyłamy żądanie POST do API
                //var response = await geminiAi.Client.PostAsync("https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=AIzaSyD9Yyv6lN8lzTJAecUZ6yBt4-_cxsB0IJo", content);
                //response.EnsureSuccessStatusCode();

                //// Odbieramy odpowiedź i przetwarzamy ją
                //ApiResponse = await response.Content.ReadAsStringAsync();
                //Console.WriteLine("Odpowiedź z API Gemini: " + ApiResponse);
                ApiResponse = result;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Błąd podczas komunikacji z Gemini AI API: " + e.Message);
                ApiResponse = "Wystąpił błąd podczas komunikacji z API.";
            }

            return Page();
        }
    }
}
