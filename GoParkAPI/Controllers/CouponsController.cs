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
    public class CouponsController : ControllerBase
    {
        private readonly EasyParkContext _context;

        public CouponsController(EasyParkContext context)
        {
            _context = context;
        }


        // GET: api/Coupons
        [HttpGet]
        public async Task<IEnumerable<CouponsDTO>> GetCoupon(int userId)
        {

            //篩選該用戶擁有的優惠券
            var coupons = _context.Coupon
                .Where(coupon => coupon.UserId == userId)
                .Select(res => new CouponsDTO
                {
                    couponId = res.CouponId,
                    couponCode = res.CouponCode,
                    discountAmount = res.DiscountAmount,
                    validFrom = res.ValidFrom,
                    validUntil = res.ValidUntil,
                    isUsed = res.IsUsed
                });
            if (coupons == null)
            {
                return null;
            }
            return coupons;
        }

        //依據優惠券使用狀態篩選
        [HttpGet("filter")]
        public async Task<IEnumerable<CouponsDTO>> couponFilter(int userId, string filter)
        {

            IQueryable<CouponsDTO> vouchers = null;
            var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var taiwanTime = TimeZoneInfo.ConvertTime(DateTime.Now, taiwanTimeZone);

            //篩選該用戶擁有的優惠券
            switch (filter)
            {
                case "isUsed":
                    // 返回已使用的優惠券
                    vouchers = _context.Coupon
                        .Where(coupon => coupon.UserId == userId && coupon.IsUsed)
                        .Select(coupon => new CouponsDTO
                        {
                            couponId = coupon.CouponId,
                            couponCode = coupon.CouponCode,
                            discountAmount = coupon.DiscountAmount,
                            validFrom = coupon.ValidFrom,
                            validUntil = coupon.ValidUntil,
                            isUsed = coupon.IsUsed
                        });
                    break;
                case "available":
                    // 返回未使用的優惠券
                    vouchers = _context.Coupon
                        .Where(coupon => coupon.UserId == userId && !coupon.IsUsed && coupon.ValidUntil.Date > taiwanTime.Date)
                        .Select(coupon => new CouponsDTO
                        {
                            couponId = coupon.CouponId,
                            couponCode = coupon.CouponCode,
                            discountAmount = coupon.DiscountAmount,
                            validFrom = coupon.ValidFrom,
                            validUntil = coupon.ValidUntil,
                            isUsed = coupon.IsUsed
                        });
                    break;
                case "expired":
                    // 返回已失效的優惠券
                    vouchers = _context.Coupon
                        .Where(coupon => coupon.UserId == userId && !coupon.IsUsed && coupon.ValidUntil.Date < taiwanTime.Date)
                        .Select(coupon => new CouponsDTO
                        {
                            couponId = coupon.CouponId,
                            couponCode = coupon.CouponCode,
                            discountAmount = coupon.DiscountAmount,
                            validFrom = coupon.ValidFrom,
                            validUntil = coupon.ValidUntil,
                            isUsed = coupon.IsUsed
                        });
                    break;
            }
            if (vouchers == null)
            {
                return null;
            }
            return vouchers;
        }





        // GET: api/Coupons/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<Coupon>> GetCoupon(int id)
        //{
        //    var coupon = await _context.Coupon.FindAsync(id);

        //    if (coupon == null)
        //    {
        //        return NotFound();
        //    }

        //    return coupon;
        //}

        // PUT: api/Coupons/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutCoupon(int id, Coupon coupon)
        //{
        //    if (id != coupon.CouponId)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(coupon).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!CouponExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}

        // POST: api/Coupons
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPost]
        //public async Task<ActionResult<Coupon>> PostCoupon(Coupon coupon)
        //{
        //    _context.Coupon.Add(coupon);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetCoupon", new { id = coupon.CouponId }, coupon);
        //}

        // DELETE: api/Coupons/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteCoupon(int id)
        //{
        //    var coupon = await _context.Coupon.FindAsync(id);
        //    if (coupon == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Coupon.Remove(coupon);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        private bool CouponExists(int id)
        {
            return _context.Coupon.Any(e => e.CouponId == id);
        }
    }
}
