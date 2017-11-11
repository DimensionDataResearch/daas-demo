using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace DaaSDemo.UI.Controllers
{
    /// <summary>
    ///     Controller for the API that provides sample data to the UI (TODO: remove).
    /// </summary>
    [Route("api/data/sample")]
    public class SampleDataController
        : Controller
    {
        /// <summary>
        ///     Summaries used for sample data.
        /// </summary>
        static string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        /// <summary>
        ///     Get some sample weather forecasts.
        /// </summary>
        [HttpGet("weather-forecasts")]
        public IActionResult WeatherForecasts()
        {
            var random = new Random();

            return Ok(Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                DateFormatted = DateTime.Now.AddDays(index).ToString("d"),
                TemperatureC = random.Next(-20, 55),
                Summary = Summaries[random.Next(Summaries.Length)]
            }));
        }

        /// <summary>
        ///     Model for weather-forecast sample data.
        /// </summary>
        public class WeatherForecast
        {
            /// <summary>
            ///     The date the measurements were taken.
            /// </summary>
            public string DateFormatted { get; set; }

            /// <summary>
            ///     The temperature (in degrees Celsius).
            /// </summary>
            public int TemperatureC { get; set; }

            /// <summary>
            ///     A short summary of weather conditions.
            /// </summary>
            public string Summary { get; set; }

            /// <summary>
            ///     The temperature (in degrees Farenheit).
            /// </summary>
            public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        }
    }
}
