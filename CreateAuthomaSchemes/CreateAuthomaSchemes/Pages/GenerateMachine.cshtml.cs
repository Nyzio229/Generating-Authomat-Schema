using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
            await GenerateContent(MachineDescription,apiKey);

            //try
            //{
            //    var result = await model.GenerateContentAsync(MachineDescription);
            //    // Wysyłamy żądanie POST do API
            //    //var response = await geminiAi.Client.PostAsync("https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=AIzaSyD9Yyv6lN8lzTJAecUZ6yBt4-_cxsB0IJo", content);
            //    //response.EnsureSuccessStatusCode();

            //    //// Odbieramy odpowiedź i przetwarzamy ją
            //    //ApiResponse = await response.Content.ReadAsStringAsync();
            //    //Console.WriteLine("Odpowiedź z API Gemini: " + ApiResponse);
            //    ApiResponse = result;
            //}
            //catch (HttpRequestException e)
            //{
            //    Console.WriteLine("Błąd podczas komunikacji z Gemini AI API: " + e.Message);
            //    ApiResponse = "Wystąpił błąd podczas komunikacji z API.";
            //}
            Console.Write(ApiResponse);
            ParseApiResponse(ApiResponse);

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

        private void GenerateGraph(List<string> states, string initialState, List<string> acceptingStates, List<(string Source, string Symbol, string Target)> transitions)
        {
            var graph = new AdjacencyGraph<string, TaggedEdge<string, string>>();

            // Dodaj wierzchołki
            foreach (var state in states)
            {
                graph.AddVertex(state);
            }

            // Dodaj krawędzie
            foreach (var transition in transitions)
            {
                // Tworzymy krawędź z etykietą (symbol)
                var edge = new TaggedEdge<string, string>(transition.Source, transition.Target, transition.Symbol);
                graph.AddEdge(edge);

                // Logowanie dla testowania
                Console.WriteLine($"Krawędź dodana do grafu: {transition.Source} --{transition.Symbol}--> {transition.Target}");
            }

            // Przekazanie grafu do metody renderującej
            GraphImage = RenderGraph(graph, initialState, acceptingStates);
        }


        private byte[] RenderGraph(AdjacencyGraph<string, TaggedEdge<string, string>> graph, string initialState, List<string> acceptingStates)
        {
            int width = 600, height = 400;
            using var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var vertexPositions = new Dictionary<string, PointF>();
            var random = new Random();

            // Pozycjonowanie wierzchołków
            foreach (var vertex in graph.Vertices)
            {
                vertexPositions[vertex] = new PointF(random.Next(50, width - 50), random.Next(50, height - 50));
            }

            // Rysowanie wierzchołków
            foreach (var vertex in graph.Vertices)
            {
                var position = vertexPositions[vertex];
                var isInitial = vertex == initialState;
                var isAccepting = acceptingStates.Contains(vertex);

                var brush = isInitial ? Brushes.Yellow : (isAccepting ? Brushes.LightGreen : Brushes.LightBlue);

                g.FillEllipse(brush, position.X - 20, position.Y - 20, 40, 40);
                g.DrawString(vertex, new Font("Arial", 10), Brushes.Black, position);
            }

            // Rysowanie krawędzi
            foreach (var edge in graph.Edges)
            {
                var start = vertexPositions[edge.Source];
                var end = vertexPositions[edge.Target];

                // Logowanie krawędzi do testowania
                Console.WriteLine($"Rysowanie krawędzi: {edge.Source} --{edge.Tag}--> {edge.Target}");

                // Rysuj strzałki reprezentujące przejścia
                g.DrawLine(Pens.Black, start, end);

                // Wyświetlanie etykiety symbolu przejścia
                var midpoint = new PointF((start.X + end.X) / 2, (start.Y + end.Y) / 2);
                g.DrawString(edge.Tag, new Font("Arial", 8), Brushes.Red, midpoint);
            }

            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            return memoryStream.ToArray();
        }



        private void ParseApiResponse(string apiResponse)
        {
            // Parsing stanów
            var statesMatch = Regex.Match(apiResponse, @"Stanów:\s*\{([^}]*)\}");
            var states = statesMatch.Groups[1].Value.Split(',').Select(s => s.Trim()).ToList();

            // Parsing stanu początkowego
            var initialStateMatch = Regex.Match(apiResponse, @"Stan początkowy:\s*\{([^}]*)\}");
            var initialState = initialStateMatch.Groups[1].Value.Trim();

            // Parsing stanów akceptujących
            var acceptingStatesMatch = Regex.Match(apiResponse, @"Stany akceptujące:\s*\{([^}]*)\}");
            var acceptingStates = acceptingStatesMatch.Groups[1].Value.Split(',').Select(s => s.Trim()).ToList();

            // Parsing funkcji przejścia
            var transitions = new List<(string Source, string Symbol, string Target)>();
            var transitionMatches = Regex.Matches(apiResponse, @"δ\((q\d+),\s*([a-z])\)\s*=\s*(q\d+)");
            foreach (Match match in transitionMatches)
            {
                var source = match.Groups[1].Value;
                var symbol = match.Groups[2].Value;
                var target = match.Groups[3].Value;
                transitions.Add((source, symbol, target));

                // Dodaj logowanie do sprawdzenia
                Console.WriteLine($"Dodano przejście: {source} --{symbol}--> {target}");
            }

            // Przekazanie wyników do metody rysującej graf
            GenerateGraph(states, initialState, acceptingStates, transitions);
        }

    }
}
