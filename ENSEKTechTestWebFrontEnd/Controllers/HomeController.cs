using ENSEKTechTestWebFrontEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using RestAPI_NS;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using System.Globalization;

namespace ENSEKTechTestWebFrontEnd.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var httpclient = new HttpClient();
            var restAPI = new RestAPI("https://localhost:44381/", httpclient);

            var accountReadings = restAPI.GetAccountsListAsync().Result;

            return View(accountReadings);
        }

        public IActionResult UploadCSV()
        {
            return View();
        }

        [HttpPost("FileUpload")]
        public async Task<IActionResult> FileUpload(List<IFormFile> files)
        {
            var toUpload = new List<MeterReading>();
            var results = new List<Models.UploadResults>();
            int i;
            DateTime d;

            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    using (StreamReader s = new StreamReader(formFile.OpenReadStream()))
                    {
                        var csvLine = s.ReadLine(); // Skip header line
                        while (!s.EndOfStream)
                        {
                            csvLine = s.ReadLine();
                            var segments = csvLine.Split(',');
                            if (segments.Count() != 3)
                            {
                                results.Add(new Models.UploadResults
                                {
                                    CSVLine = csvLine,
                                    Information = "Incorrect number of items"
                                });
                            }
                            else if (!int.TryParse(segments[0], out i)) {
                                results.Add(new Models.UploadResults
                                {
                                    CSVLine = csvLine,
                                    Information = "AccountId is not an integer"
                                });
                            }
                            else if (!DateTime.TryParseExact(segments[1], "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                            {
                                results.Add(new Models.UploadResults
                                {
                                    CSVLine = csvLine,
                                    Information = "MeterReadingDateTime is not a valid date"
                                });
                            }
                            else if (!int.TryParse(segments[2], out i))
                            {
                                results.Add(new Models.UploadResults
                                {
                                    CSVLine = csvLine,
                                    Information = "MeterReadValue is not an integer"
                                });
                            }
                            else
                            {
                                toUpload.Add(new MeterReading
                                {
                                    AccountId = int.Parse(segments[0]),
                                    MeterReadingDateTime = DateTime.ParseExact(segments[1], "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None),
                                    MeterReadValue = int.Parse(segments[2])
                                });
                            }
                        }
                    }
                }
            }

            if (toUpload.Count > 0)
            {
                var httpclient = new HttpClient();
                var restAPI = new RestAPI("https://localhost:44381/", httpclient);

                var uploadResults = restAPI.MeterReadingUploadsAsync(toUpload).Result;

                foreach (var result in uploadResults)
                {
                    results.Add(new Models.UploadResults
                    {
                        CSVLine = string.Format("{0},{1:dd/MM/yyyy HH:mm},{2}", result.AccountId, result.MeterReadingDateTime, result.MeterReadValue),
                        Information = result.Result
                    });
                }
            }

            return View("UploadResults", results);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }




}
