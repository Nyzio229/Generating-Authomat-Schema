using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Text.Json;
using System.Text;
using GenerativeAI;
using GenerativeAI.Models;
using GenerativeAI.Tools;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using QuickGraph.Graphviz;
using QuickGraph;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GenerativeAI.Types;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;

namespace CreateAuthomaSchemes.Pages
{
    public class GenerateMachineModel : PageModel
    {
        [BindProperty]
        public string MachineDescription { get; set; }
        
        [BindProperty]
        public string ApiResponse { get; set; } = string.Empty;

        public byte[] GraphImage { get; private set; }
        public string JFlapData{ get; private set; }
        public IActionResult JFlapResult{ get; set; }

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
            //ListAvailableModels();
            //if (string.IsNullOrWhiteSpace(MachineDescription))
            //{
            //    // Możesz dodać komunikat o błędzie lub zainicjalizować GraphImage jako pusty obraz
            //    GraphImage = Array.Empty<byte>();
            //    return Page();
            //}
            var apiKey = _options.ApiKey;
            var model = new GenerativeModel(apiKey);
            //await GenerateContent(MachineDescription,apiKey);
            ApiResponse = "Witaj to test";
        
            Console.Write(ApiResponse);
            string sample_api_response = "Stanów: {q0, q1, q2} \nStan początkowy: {q0}\nStany akceptujące: {q0, q1}\nFunkcje przejścia:\nδ(q0, a) = q1\nδ(q0, b) = q0\nδ(q1, a) = q1\nδ(q1, b) = q2\nδ(q2, a) = q2\nδ(q2, b) = q2";
            
            //Generowanie grafu przez serwer
            GraphImage = await GenerateGraphImageAsync(sample_api_response);
            //JFlapResult =  await OnGetDownloadJflap(sample_api_response);
            
            //Generowanie grafu przez skrypt
            //GenerateGraph();
            return Page();
        }
        public async Task ListAvailableModels()
        {
            var apiKey = _options.ApiKey;
            var endpointUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest?key={apiKey}";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(endpointUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Dostępne modele: " + jsonResponse);
            }
            else
            {
                Console.WriteLine($"Błąd {response.StatusCode}: {response.ReasonPhrase}");
            }
        }
        public async Task GenerateContent(string prompt, string apiKey)
        {
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/tunedModels/tuned-automat-model-v3-vtw6mau4ju2f:generateContent?key={apiKey}";
            var httpClient = _httpClientFactory.CreateClient();

            var requestBody = new
            {
                contents = new[]
                {
                new { parts = new[] { new { text = prompt } } }
            }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();

                // Parsuj JSON i wyciągnij 'parts'
                using var document = JsonDocument.Parse(responseBody);
                var root = document.RootElement;

                // Wyciągamy tekst z 'parts'
                var parts = root
                    .GetProperty("candidates")[0]  // wybiera pierwszy element listy 'candidates'
                    .GetProperty("content")
                    .GetProperty("parts")[0]       // wybiera pierwszy element listy 'parts'
                    .GetProperty("text")
                    .GetString();
                ApiResponse = parts;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Błąd podczas wywołania API Gemini: {e.Message}");
                ApiResponse =  $"Error: {e.Message}";
            }
        }

        public async Task<byte[]> GenerateGraphImageAsync(string apiResponse)
        {
            using (var client = new HttpClient())
            {
                //client.BaseAddress = new Uri("http://localhost:5001"); // URL serwera Flask
                client.BaseAddress = new Uri("https://nyzio.pythonanywhere.com/"); // URL serwera Flask
                var content = new MultipartFormDataContent();
                content.Add(new StringContent(apiResponse), "ApiResponse");

                var response = await client.PostAsync("/generate-graph", content);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync(); // Odbieranie grafu jako obraz PNG
                }
                else
                {
                    throw new Exception("Error generating graph: " + response.ReasonPhrase);
                }
            }
        }

        public async Task<IActionResult> OnGetDownloadJflap(string apiResponse)
        {
            string sample_api_response = "Stanów: {q0, q1, q2} \nStan początkowy: {q0}\nStany akceptujące: {q0, q1}\nFunkcje przejścia:\nδ(q0, a) = q1\nδ(q0, b) = q0\nδ(q1, a) = q1\nδ(q1, b) = q2\nδ(q2, a) = q2\nδ(q2, b) = q2";

            if (string.IsNullOrEmpty(apiResponse))
            {
                return BadRequest("ApiResponse cannot be null or empty.");
            }
            var jflapData = await GenerateJflapFileAsync(sample_api_response);
            return File(Encoding.UTF8.GetBytes(jflapData), "application/xml", "automat.jff");
        }

        public async Task<string> GenerateJflapFileAsync(string apiResponse)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://nyzio.pythonanywhere.com/");
                var content = new MultipartFormDataContent();
                content.Add(new StringContent(apiResponse), "ApiResponse");

                var response = await client.PostAsync("/generate-jflap", content);
                if (response.IsSuccessStatusCode)
                {
                    // Pobierz odpowiedź jako JSON
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    // Deserializuj JSON i wyodrębnij wartość "jflap"
                    var responseObject = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonResponse);
                    if (responseObject != null && responseObject.ContainsKey("jflap"))
                    {
                        return responseObject["jflap"]; // Zwróć czysty XML
                    }
                    else
                    {
                        throw new Exception("Invalid response format: 'jflap' key not found.");
                    }
                }
                else
                {
                    throw new Exception("Error generating JFLAP file: " + response.ReasonPhrase);
                }
            }
        }
    }
}
