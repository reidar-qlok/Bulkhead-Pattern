using Microsoft.AspNetCore.Mvc;
using Polly.Bulkhead;

namespace Bulkhead_Pattern.Controllers
{
    public class BulkheadController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static int _requestCount = 0; // To track the number of requests
        public BulkheadController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("BulkheadClient");

            try
            {
                _requestCount++;
                // Introduce a delay to simulate load
                await Task.Delay(1000); // 1 second delay
                var response = await client.GetAsync("https://jsonplaceholder.typicode.com/todos/1");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                ViewData["Response"] = $"Response {_requestCount}: {content}";
            }
            catch (BulkheadRejectedException)
            {
                ViewData["Response"] = $"Request {_requestCount} rejected by bulkhead policy";
            }
            finally
            {
                _requestCount--;
            }

            return View();
        }

        public async Task<IActionResult> Delayed()
        {
            var client = _httpClientFactory.CreateClient("BulkheadClient");

            try
            {
                _requestCount++;
                // Introduce a longer delay to simulate heavy load
                await Task.Delay(5000); // 5 seconds delay
                var response = await client.GetAsync("https://jsonplaceholder.typicode.com/todos/2");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                ViewData["Response"] = $"Delayed Response {_requestCount}: {content}";
            }
            catch (BulkheadRejectedException)
            {
                ViewData["Response"] = $"Delayed request {_requestCount} rejected by bulkhead policy";
            }
            finally
            {
                _requestCount--;
            }

            return View("Index");
        }

        public IActionResult LoadTest()
        {
            var tasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(SendRequest());
            }

            Task.WaitAll(tasks.ToArray());

            var results = new List<string>();
            foreach (var task in tasks)
            {
                results.Add(task.Result);
            }

            ViewData["Response"] = string.Join("<br/>", results);

            return View("Index");
        }

        private async Task<string> SendRequest()
        {
            var client = _httpClientFactory.CreateClient("BulkheadClient");

            try
            {
                await Task.Delay(1000); // 1 second delay to simulate load
                var response = await client.GetAsync("https://jsonplaceholder.typicode.com/todos/3");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return $"Response: {content}";
            }
            catch (BulkheadRejectedException)
            {
                return "Request rejected by bulkhead policy";
            }
        }
    }
}
