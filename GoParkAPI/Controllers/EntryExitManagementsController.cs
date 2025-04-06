using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoParkAPI.Models;
using GoParkAPI.DTO;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntryExitManagementsController : ControllerBase
    {
        private readonly EasyParkContext _context;

        public EntryExitManagementsController(EasyParkContext context)
        {
            _context = context;
        }

        //先抓出該用戶註冊的車牌
        private async Task<List<string>> GetUserCars(int userId)
        {
            return await _context.Car
                .Where(car => car.UserId == userId)
                .Select(car => car.LicensePlate)
                .ToListAsync();
        }

        //載入用戶的停車紀錄 & 透過車牌篩選(Option)
        // GET: api/EntryExitManagements
        [HttpGet]
        public async Task<IEnumerable<EntryExitManagementDTO>> GetEntryExit(int userId)
        {
            //篩選該用戶車牌的預訂資料
            var parkingRecords = _context.EntryExitManagement
                .Where(record => record.Car.UserId ==userId && record.Parktype == "Reservation" && record.IsFinish == true) // 比對車牌號碼                   
                .Select(record => new EntryExitManagementDTO
                 {
                     entryexitId = record.EntryexitId,
                     lotName = record.Lot.LotName,
                     district = record.Lot.District,
                     licensePlate = record.Car.LicensePlate,
                     entryTime = (DateTime)record.EntryTime,
                     exitTime = record.ExitTime,
                     totalMins = record.ExitTime.HasValue
                        ? (int)((TimeSpan)(record.ExitTime - record.EntryTime)).TotalMinutes
                        : null,                    
                     amount = record.Amount
                 })
                .OrderByDescending(record => record.entryTime);
                
            return parkingRecords;
        }

        //搜尋"停車場"載入停車紀錄
        [HttpGet("search/{lotName}")]
        public async Task<IEnumerable<EntryExitManagementDTO>> SearchEntryExitByLotname(int userId, string lotName)
        {
            //根據 userId抓出用戶的車牌號碼
            var userCars = await GetUserCars(userId);

            // 根據停車場名稱模糊查詢停車紀錄
            var parkingRecords = _context.EntryExitManagement
                .Where(record => userCars.Contains(record.Car.LicensePlate) && record.Lot.LotName.Contains(lotName))
                .Select(record => new EntryExitManagementDTO
                {
                    entryexitId = record.EntryexitId,
                    lotName = record.Lot.LotName,
                    licensePlate = record.Car.LicensePlate,
                    entryTime = (DateTime)record.EntryTime,
                    exitTime = record.ExitTime,
                    totalMins = (int)((TimeSpan)(record.ExitTime - record.EntryTime)).TotalMinutes,
                    amount = record.Amount
                });

            if (parkingRecords == null)
            {
                return null;
            }
            return parkingRecords;
        }



        // GET: api/EntryExitManagements/5
        [HttpGet("{id}")]
        public async Task<ParkingDetailDTO> GetEntryExitDetail(int id)
        {
            var parkingDetail = await _context.EntryExitManagement
                .Where(record => record.EntryexitId == id)
                .Select(record => new ParkingDetailDTO
                {
                    entryexitId = record.EntryexitId,
                    lotName = record.Lot.LotName,
                    district = record.Lot.District,
                    location = record.Lot.Location,
                    latitude = record.Lot.Latitude,
                    longitude = record.Lot.Longitude,
                    licensePlate = record.Car.LicensePlate,
                    entryTime = (DateTime)record.EntryTime,
                    exitTime = record.ExitTime,
                    //totalMins = (int)((TimeSpan)(record.ExitTime - record.EntryTime)).TotalMinutes,
                    amount = record.Amount
                })
                .FirstOrDefaultAsync(); ;

            if (parkingDetail == null)
            {
                return null;
            }
            return parkingDetail;

           

        }

        //------------line bot使用------------

        //查詢特定日期的停車紀錄
        // GET: api/EntryExitManagements/RecordByDate
        [HttpGet("RecordByDate")]
        public async Task<IEnumerable<EntryExitManagementDTO>> GetRecordByDate(int userId, string dateString)
        {
            DateTime date = DateTime.ParseExact(dateString, "yyyy-MM-dd", null); //將字串解析為datetime格式，時間部分為00:00:00
            DateTime startOfDate = date.Date;
            DateTime endOfDate = date.Date.AddDays(1).AddTicks(-1);

            //篩選該用戶車牌的預訂資料(還在進行中的預訂)
            var records = _context.EntryExitManagement
                .Where(record => record.Car.UserId == userId && record.IsFinish)
                .Where(record => record.EntryTime >= startOfDate && record.EntryTime <= endOfDate)
                .Select(record => new EntryExitManagementDTO
                {
                    entryexitId = record.EntryexitId,
                    lotName = record.Lot.LotName,
                    district = record.Lot.District,
                    location = record.Lot.Location,
                    licensePlate = record.Car.LicensePlate,
                    entryTime = (DateTime)record.EntryTime,
                    exitTime = record.ExitTime,
                    totalMins = (int)((TimeSpan)(record.ExitTime - record.EntryTime)).TotalMinutes,
                    amount = record.Amount
                });
            if (records == null)
            {
                return null;
            }
            return records;
        }


        private bool EntryExitManagementExists(int id)
        {
            return _context.EntryExitManagement.Any(e => e.EntryexitId == id);
        }
    }
}
