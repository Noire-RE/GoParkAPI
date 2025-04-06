using GoParkAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GoParkAPI.Services
{
    public class MonRentalService
    {
        private readonly EasyParkContext _context;
        public MonRentalService(EasyParkContext context)
        {
            _context = context;
        }
        public async Task<ParkingLots> GetParkingLotAsync(int lotId)
        {
            return await _context.ParkingLots.FirstOrDefaultAsync(p => p.LotId == lotId);
        }

        // 檢查月租車位是否已滿 bool返回布林值的結果
        public async Task<bool> isMonResntalSpaceAvailableAsync(int lotId)
        {
            var parkingLots = await _context.ParkingLots.FirstOrDefaultAsync(p => p.LotId == lotId);
            if (parkingLots == null || parkingLots.MonRentalSpace <= 0 || parkingLots.MonRentalRate <= 0)
            {
                return false;
            }
            return true;
        }
    }
}
