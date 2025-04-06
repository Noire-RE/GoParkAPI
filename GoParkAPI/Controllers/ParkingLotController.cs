using GoParkAPI.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Text.Json;
using System.Web;

namespace GoParkAPI.Controllers
{
    //[EnableCors("EasyParkCors")]
    [Route("api/[controller]")]
    [ApiController]
    public class ParkingLotController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly EasyParkContext _context;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly IHttpClientFactory _httpClientFactory;


        public ParkingLotController(HttpClient httpClient, EasyParkContext context, IConnectionMultiplexer redis, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = httpClient;
            _context = context;
            _redis = redis;
            _db = redis.GetDatabase();
        }

        //接收前端傳來的字串
        [HttpGet]
        public async Task<IActionResult> GetGeocode([FromQuery] string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return BadRequest("地址不能為空");
            }

            // URL encode the address to ensure it's safely included in the URL
            //var encodedAddress = HttpUtility.UrlEncode(address);
            var decodedAddress = HttpUtility.UrlEncode(address);
            var url = $"https://nominatim.openstreetmap.org/search?format=json&q={decodedAddress}&limit=1";

            try
            {
                using var requestMessage = _httpClientFactory.CreateClient();
                requestMessage.DefaultRequestHeaders.Add("User-Agent", "GeocodeAPI/1.0");

                var response = await requestMessage.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "無法從 Nominatim API 獲取數據");
                }

                var jsonData = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(jsonData))
                {
                    return NotFound("未找到該地址的經緯度數據");
                }

                var lat = (string)JArray.Parse(jsonData)[0]["lat"];
                var lon = (string)JArray.Parse(jsonData)[0]["lon"];
                if (string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lon))
                {
                    return NotFound("經緯度數據缺失");
                }
                return Ok(new { Latitude = lat, Longitude = lon });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new {Message = "請求超時"});
            }
            catch (HttpRequestException e)
            {
                return StatusCode(502, $"HTTP 請求錯誤: {e.Message}");
            }
            catch (Exception e)
            {
                return StatusCode(500, $"伺服器錯誤：{e.Message}");
            }

            //try
            //{
            //    var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            //    //requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
            //    //requestMessage.Headers.Add("Referer", "https://goparkapi.azurewebsites.net");

            //    // Send HTTP request to Nominatim API
            //    var response = await _httpClient.SendAsync(requestMessage);
            //    if (response.IsSuccessStatusCode)
            //    {
            //        var jsonData = await response.Content.ReadAsStringAsync();
            //        // 解析 JSON 並提取經緯度
            //        //JArray 是屬於 Newtonsoft.Json（又稱為 Json.NET）中的一個類，用來處理 JSON 陣列。由於 Nominatim API 返回的結果是 JSON 陣列，所以使用 JArray.Parse 來解析這個陣列，並提取其中的經緯度。需安裝Json.linq
            //        var jsonArray = JArray.Parse(jsonData);
            //        if (jsonArray.Count > 0)
            //        {
            //            var lat = (string)jsonArray[0]["lat"];
            //            var lon = (string)jsonArray[0]["lon"];

            //            // 返回經緯度
            //            return Ok(new { Latitude = lat, Longitude = lon });
            //        }
            //        else
            //        {
            //            return NotFound("未找到該地址的經緯度數據");
            //        }
            //    }
            //    else
            //    {
            //        return StatusCode((int)response.StatusCode, "無法從 Nominatim API 獲取數據");
            //    }
            //}
            //catch (HttpRequestException e)
            //{
            //    return StatusCode(500, $"HTTP 請求錯誤: {e.Message}");
            //}
        }

        [HttpGet("GetParkingLots")]
        public async Task<IActionResult> GetParkingLots([FromServices] IMemoryCache memoryCache)
        {
            string cacheKey = "AllParkingLot";

            var jsonSerialize = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            try
            {
                //var cacheParkingLots = await _db.StringGetAsync(redisKey);

                if (memoryCache.TryGetValue(cacheKey, out List<object>? cacheParkingLots))
                {
                    //var LotsFromCache = JsonSerializer.Deserialize<List<object>>(cacheParkingLots, jsonSerialize);
                    return Ok(cacheParkingLots);
                }

                var parkingLots = await _context.ParkingLots.Select(p => new
                {
                    lotId = p.LotId,
                    lotName = p.LotName ?? "無資料",
                    location = p.Location ?? "無資料",
                    latitude = p.Latitude,
                    longitude = p.Longitude,
                    smallCarSpace = p.SmallCarSpace,
                    monRentalSpace = p.MonRentalSpace,
                    etcSpace = p.EtcSpace,
                    RateRules = p.RateRules ?? "無資料",
                    weekdayRate = p.WeekdayRate,
                    holidayRate = p.HolidayRate,
                    monRate = p.MonRentalRate,
                    resDeposit = p.ResDeposit,
                    opendoorTime = p.OpendoorTime ?? "無資料",
                    tel = p.Tel ?? "無資料",
                    validSpace = p.ValidSpace,
                }).ToListAsync();
                //await _db.StringSetAsync(redisKey, JsonSerializer.Serialize(parkingLots, jsonSerialize), TimeSpan.FromMinutes(10));
                memoryCache.Set(cacheKey, parkingLots, TimeSpan.FromMinutes(10));
                return Ok(parkingLots);
            }
            catch (Exception e)
            {
                return StatusCode(500, $"伺服器錯誤：{e.Message}");
            }
        }
    }
}
