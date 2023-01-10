using YourName_DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

//מקביל לפונקציה הראשית המאחדת את כל הלוגיקה הקלאסים וכ'ו namespace
namespace Microservices.Api
{
    //route template for an MVC (Model-View-Controller)
    [Route("api/[controller]")]

    // שם הקונטרולרים
    [ApiController]

    //ControllerBase כאן אנו יוצרים קלאס שיודע להתנהג עם בקשות איפיאי מישום שהוא מסוג 
    public class ExchangeRatesController : ControllerBase
    {

        //ILogger<ExchangeRatesController> מסוג _logger יצירת שדה בשם 
        // This allows you to use the logger to log messages specifically for the ExchangeRatesController class.
        //זה אומר שלא ניתן לשנות את ערכו readonly
        // You can use the _logger field to log messages by calling one of the log methods, such as _logger.LogInformation() or _logger.LogError(). For 
        // example: _logger.LogInformation("Received request to get exchange rates");
        // The type parameter of ILogger<T> specifies the category for the logger. In this case, the category is the ExchangeRatesController class itself. This allows you to use the logger to log messages specifically for the ExchangeRatesController class.
        private readonly ILogger<ExchangeRatesController> _logger;


        // new way to handle HttpClient instances in .NET Core,
        // דרך לקבל בקשות איפיאי מצד הלקוח
        private readonly IHttpClientFactory _httpClientFactory;

        // הקונסטרקטור של הקלאס
        // This section of the code is the constructor of the ExchangeRatesController class. The constructor is used to initialize the class's fields and perform any other setup that the class requires.
        //It has two parameters:
        // ILogger<ExchangeRatesController> logger : This is a logger of type ILogger, that can be used to log messages from within this controller.
        // IHttpClientFactory httpClientFactory : is an instance of IHttpClientFactory, which allows you to create HttpClient instances with the settings you need and reuse them throughout your application.
        // The constructor takes these two parameters and assigns them to the private fields _logger and _httpClientFactory.
        // This way the logger and the HttpClient Factory can be used throughout the class methods. The _logger field can be used to log messages, and the _httpClientFactory can be used to create HttpClient instances, and make http calls, like calling the CoinGecko API. 
        public ExchangeRatesController(ILogger<ExchangeRatesController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }



        // שורה זאת קשורה לשורה שמתחתיה והיא אומרת שהפונ' שתחתיה מקבלת בקשת גט[HttpGet] 
        //זה אומר שפונ זו מחזירה ערך מהסוג הזה Task<ActionResult<ExchangeRatesResult>>
        // this means that the method returns a Task which when completes, will return an ActionResult<T> where T is of type ExchangeRatesResult.
        [HttpGet]
        public async Task<ActionResult<ExchangeRatesResult>> GetExchangeRates()
        {
            // Call the CoinGecko API to get the exchange rates
            // משתנה יודע לקבל קריאות מהפרונט
            var client = _httpClientFactory.CreateClient();

            // משתנה שמקבל את המידע מהבקשת האיפיאי
            var response = await client.GetAsync("https://api.coingecko.com/api/v3/exchange_rates");

            // EnsureSuccessStatusCode method is used to check if the status code of the response indicates that the request was successful.
            response.EnsureSuccessStatusCode();

            //  the Content property of the response object, which represents the body of the response
            // כך נבפוך את הערך לסטרינג
            var exchangeRatesJson = await response.Content.ReadAsStringAsync();

            // כך אנו הופכים את הסטרינג לאובייקט סי שארפ שהוא אובייקט הכולל מפתח וערך
            // במקרה הזה זה יהיה מילון שבו המפתחות הם קודי המטבע והערכים הם שערי החליפין.
            var exchangeRates = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(exchangeRatesJson);

            // Calculate the max rate and multiply all rates by 1000
            //maxRate כך אנו עובדים על כל האובייקטי הסי שארפ שיצרנו ומחלצים את האובייקט
            var maxRate = exchangeRates.Max(kvp => kvp.Value);

            //ונכפיל ב1000 את כל הערכים של האובייקטים ToDictionary פה אנו ניצור מערך חדש בעזרת הפקודה 
            var multipliedRates = exchangeRates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value * 1000);

            // Order the results by type and then by name
            // כך אנו נסדר את האובייקטים לפי סדר מסויים
            var orderedRates = multipliedRates.OrderBy(kvp => kvp.Key).ThenBy(kvp => kvp.Value);

            // Save the max rate in the database if it has changed
            //לדאטה בייס שלנו בעזרת סיפריות מסויימות YourName_DB ובמידה ואנ ו נרצה לעבוד עם דאטה בייס אמיתי אנו נקשר את 
            using (var db = new YourName_DB())
            {
                //previousMaxRate האחרון שנשמר בדאטה בייס ונכניס אותו למשתנה  maxRate במידה והיה לנו דאטה אמיתי אנו נחלץ את האובייקט
                var previousMaxRate = db.RatesHistory.OrderByDescending(r => r.Id).FirstOrDefault()?.Value;

                //החדש maxRate במידה והוא שונה מה
                if (previousMaxRate != maxRate)
                {
                    //החדש כך שתמיד בדאטה בייס יהיה את האובייקט המקסימלי העדכני maxRate אנו נחליף אותו ב
                    db.RatesHistory.Add(new RateHistory
                    {
                        DateTime = DateTime.Now,
                        Value = maxRate
                    });

                    // כך נשמור את השינויים בדאטה בייס
                    await db.SaveChangesAsync();
                }
            }

            // Write the results to the log file in JSON format
            //אשר יצרנו בתחתית הדף ExchangeRatesResult כאן אנו יוצרים אובייקט מסוג הקלאס
            // ושמים כערכים לפרופרטיז שלו את הערכים שחילצנו
            var result = new ExchangeRatesResult
            {
                MaxRate = maxRate,
                MaxRatePrevious = previousMaxRate,
                Rates = orderedRates
            };

            // כאן אנו נעבור את אובייקטי סי שארט לג'ייסון סטרינג
            var resultJson = JsonConvert.SerializeObject(result);

            // logger יוציא את התוצאה המסודרת בפורמט json עבור הלקוח,
            _logger.LogInformation(resultJson);

            //שהוא האובייקט עם הנתונים שחילצנו result ולבסוף אנו נחזיר את ה
            return result;
        }
    }

    // אינטרפייס קלאס
    public class ExchangeRatesResult
    {
        public decimal MaxRate { get; set; }
        public decimal? MaxRatePrevious { get; set; }
        public IEnumerable<KeyValuePair<string, decimal>> Rates { get; set; }
    }
}
