using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoParkAPI.Models;
using Azure.Identity;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.ComponentModel.DataAnnotations;
using GoParkAPI.DTO;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using NuGet.Protocol.Plugins;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Configuration;

namespace GoParkAPI.Controllers
{
    //[EnableCors("EasyParkCors")]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly EasyParkContext _context;
        private readonly pwdHash _hash;
        private readonly MailService _sentmail;
        private readonly IConfiguration _configuration;
        //private readonly IGoogleTokenService _googleTokenService;
        //private readonly IUserService _userService;
        //private readonly IJwtService _jwtService;
        public CustomersController(EasyParkContext context, pwdHash hash, MailService sentmail, IConfiguration configuration)
        {
            _context = context;
            _hash = hash;
            _sentmail = sentmail;
            _configuration = configuration;
        }

       
        // GET: api/Customers
        [HttpGet]
        public async Task<IEnumerable<CustomerDTO>> GetCustomer()
        {
            return _context.Customer.Select(cust => new CustomerDTO
            {
                UserId = cust.UserId,
                Username = cust.Username,
                Password = cust.Password,
                Salt = cust.Salt,
                Email = cust.Email,
                Phone = cust.Phone,
                LicensePlate = _context.Car.Where(car => car.UserId == cust.UserId).Select(car => car.LicensePlate).FirstOrDefault()
            });
        }

        //GET: api/Customers/5
        [HttpGet("info{id}")]
        public async Task<CustomerDTO> GetCustomer(int id)
        {
            var l = await _context.Car.Where(car => car.UserId == id).FirstAsync();
            string cnum = l.LicensePlate;
            var customer = await _context.Customer.FindAsync(id);
            CustomerDTO custDTO = new CustomerDTO
            {
                UserId = customer.UserId,
                Username = customer.Username,
                Password = customer.Password,
                Salt = customer.Salt,
                Email = customer.Email,
                Phone = customer.Phone,
                LicensePlate = cnum,
                IsBlack = customer.IsBlack
            };

            if (customer == null)
            {
                return null;
            }

            return custDTO;
        }

        // PUT: api/Customers/id5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //網址列id 第一個參數
        [HttpPut("id{id}")]
        public async Task<string> PutCustomer(int id, EditDTO custDTO)
        {
            var customer = await _context.Customer.FindAsync(id);
            if (id != customer.UserId)
            {
                return "無法修改";
            }

            // 查找 Customer
            Customer cust = await _context.Customer.FindAsync(id);
            if (cust == null)
            {
                return "無法找到會員資料";
            }

            // 查找 Car，假設每個 Customer 對應一輛 Car
            Car car = await _context.Car.FirstOrDefaultAsync(c => c.UserId == id);
            if (car == null)
            {
                return "無法找到車輛資料";
            }



            //// 密碼加密與加鹽
            //var (hashedPassword, salt) = _hash.HashPassword(custDTO.Password);
            //custDTO.Password = hashedPassword;
            //custDTO.Salt = salt;

            // 更新 Customer 資料
            cust.Username = custDTO.Username;
            //cust.Password = custDTO.Password; // 確保已經 hash 過密碼
            //cust.Salt = custDTO.Salt;
            cust.Email = custDTO.Email;
            cust.Phone = custDTO.Phone;

            // 更新 Car 的 LicensePlate
            car.LicensePlate = custDTO.LicensePlate;

            // 設定狀態為已修改
            _context.Entry(cust).State = EntityState.Modified;
            _context.Entry(car).State = EntityState.Modified;

            try
            {
                // 儲存修改
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
                {
                    return "修改失敗";
                }
                else
                {
                    throw;
                }
            }

            return "修改成功";
        }

        [HttpPut("password{id}")]
        public async Task<IActionResult> ChangePassword(int id, ChangePswDTO pswDto)
        {
            // 根據傳入的 id 找到用戶
            var customer = await _context.Customer.FindAsync(id);
            if (customer == null)
            {
                return NotFound("用戶不存在");
            }


            // 驗證舊密碼是否正確
            if (!_hash.VerifyPassword(pswDto.OldPassword, customer.Password, customer.Salt))
            {
                return BadRequest("舊密碼不正確");
            }

            // 使用新的密碼和鹽值覆蓋舊密碼
            var (newHashedPassword, newSalt) = _hash.HashPassword(pswDto.NewPassword);
            customer.Password = newHashedPassword;
            customer.Salt = newSalt;

            // 設置資料庫狀態為已修改
            _context.Entry(customer).State = EntityState.Modified;

            try
            {
                // 儲存更改
                await _context.SaveChangesAsync();
                return Ok("密碼更新成功");
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "更新失敗，請稍後再試");
            }
        }



        // POST: api/Customers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754

        [HttpPost("sign")]
        public async Task<ActionResult> PostCustomer(CustomerDTO custDTO)
        {
            // 檢查是否已有相同的Email或車牌
            var customer = await _context.Customer.FirstOrDefaultAsync(c => c.Email == custDTO.Email);
            var existingCar = await _context.Car.FirstOrDefaultAsync(c => c.LicensePlate == custDTO.LicensePlate);

            // 如果用戶不存在，並且車牌不存在
            if (customer == null && existingCar == null)
            {
                // 密碼加密與加鹽
                var (hashedPassword, salt) = _hash.HashPassword(custDTO.Password);
                custDTO.Password = hashedPassword;
                custDTO.Salt = salt;

                // 創建新用戶
                Customer cust = new Customer
                {
                    Username = custDTO.Username,
                    Password = custDTO.Password,
                    Salt = custDTO.Salt,
                    Email = custDTO.Email,
                    Phone = custDTO.Phone
                };

                // 將新用戶添加到資料庫
                _context.Customer.Add(cust);
                await _context.SaveChangesAsync();

                // 創建用戶的車輛資料
                Car car = new Car
                {
                    LicensePlate = custDTO.LicensePlate,
                    UserId = cust.UserId, // 使用剛創建的用戶ID
                    IsActive = true
                };

                // 將車輛資料添加到資料庫
                _context.Car.Add(car);
                await _context.SaveChangesAsync();

                // 發送歡迎郵件
                if (!string.IsNullOrEmpty(custDTO.Email))
                {
                    string subject = "歡迎加入 MyGoParking!";
                    string message = $"<p>親愛的用戶：感謝您註冊，您已成功加入！<br>敬祝順利<br>mygoParking團隊</p>";

                    try
                    {
                        await _sentmail.SendEmailAsync(custDTO.Email, subject, message);
                    }
                    catch (Exception ex)
                    {
                        // 錯誤處理
                        Console.WriteLine($"發送郵件失敗: {ex.Message}");
                    }
                }

                // 回傳註冊成功的完整用戶資料給前端
                return Ok(new
                {
                    exit = true,
                    message = "註冊成功!",
                    userId = cust.UserId,
                    username = cust.Username,
                    email = cust.Email,
                    phone = cust.Phone,
                    licensePlate = car.LicensePlate,
                    password = cust.Password, // 回傳加密後的密碼
                    salt = cust.Salt
                });
            }
            // 檢查帳號是否已存在
            else if (customer != null)
            {
                return Ok(new { message = "此帳號已註冊!" });
            }
            // 檢查車牌是否已存在
            else if (existingCar != null)
            {
                return Ok(new { message = "此車牌已存在!" });
            }
            else
            {
                return Ok(new { message = "請洽客服人員!" });
            }
        }


        // JWT web token 忘記密碼
        [HttpPost("forgot")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotDTO forgot)
        {
            var user = await _context.Customer.FirstOrDefaultAsync(u => u.Email == forgot.Email);
            if (user == null)
            {
                return Ok(new { message = "找不到該用戶" });
            }

            // 生成 JWT token
            var secretKey = _configuration["JwtSettings:SecretKey"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim("timestamp", DateTime.UtcNow.ToString()) // 增加時間戳作為 Claim
                }),
                Expires = DateTime.UtcNow.AddHours(1),  // Token 有效期
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["JwtSettings:Issuer"],  // 設置 Issuer
                Audience = _configuration["JwtSettings:Audience"]  // 設置 Audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var resetToken = tokenHandler.WriteToken(token);

            // 構建密碼重設連結
            var resetLink = $"https://www.mygoparking.com/reset?token={resetToken}";

            // 構建返回的 DTO，將 token 也傳回給前端
            var resetPasswordDto = new ResetDTO
            {
                Token = resetToken  // 將 token 傳回
            };

            string subject = "MyGoParking 忘記密碼";
            string message = $"<p>親愛的用戶：<br>請點擊以下連結重設您的新密碼：</p>" + $"<a href=\"{resetLink}\">" + "<p>此鏈接將在1小時內過期。<br>mygoParking團隊</p>";

            // 發送郵件 (這裡應該是使用郵件服務發送連結)
            await _sentmail.SendForgotEmailAsync(user.Email, subject, message, resetLink);

            return Ok(resetPasswordDto);
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetDTO reset)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JwtSettings:SecretKey"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            try
            {
                tokenHandler.ValidateToken(reset.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,  // 啟用 Issuer 驗證
                    ValidateAudience = true,  // 啟用 Audience 驗證
                    ValidIssuer = _configuration["JwtSettings:Issuer"],  // 從 appsettings 讀取 Issuer
                    ValidAudience = _configuration["JwtSettings:Audience"],  // 從 appsettings 讀取 Audience
                    ValidateLifetime = true,  // 驗證 token 的有效期
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                    
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "nameid");//找的到id 133了
                //var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;//原本寫法 抓不到

                var userId = userIdClaim.Value;
                var user = await _context.Customer.FindAsync(int.Parse(userId));


                // 加密新密碼
                var (hashedPassword, salt) = _hash.HashPassword(reset.NewPassword);
                user.Password = hashedPassword;
                user.Salt = salt;
                // 更新用戶的 Token（可選）
                user.Token = reset.Token;
              
                _context.Customer.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "密碼已成功重設" });
            }
            catch (Exception)
            {
                return Ok(new { message = "重設密碼鏈接無效或已過期" });
            }
        }



        [HttpPost("login")]
        public IActionResult Login(LoginsDTO login)

        {
            bool exit = false;
            string message = "";
            int UserId = 0;
            var member = _context.Customer.Where(m => m.Email.Equals(login.Email)).SingleOrDefault();
            
            if (member != null)
            {
               
                // 從數據庫中獲取已加密的密碼和鹽值
                var hashedPassword = member.Password;
                var salt = member.Salt;
                var isPasswordValid = _hash.VerifyPassword(login.Password, hashedPassword, salt);

                if (!isPasswordValid)
                {
                    
                    message = "登入失敗";
                }
                else
                {
                    exit = true;
                    message = "登入成功";
                    UserId = member.UserId;
                }
            }
            else
            {
                exit = false;
                message = "無此帳號";
            }
            ExitDTO exitDTO = new ExitDTO
            {
                Exit = exit,
                UserId = UserId,
                Message = message,
            };
            return Ok(exitDTO);

        }

        [HttpPost("coupon")]
        public async Task<ActionResult<CouponDTO>> AddCoupon([FromBody] int User)
        {
            var user = await _context.Customer.FirstOrDefaultAsync(u => u.UserId == User);

            if (user != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Guid g1 = Guid.NewGuid();//32位

                    Coupon coup = new Coupon
                    {
                        CouponCode = "#" + g1.ToString(),
                        DiscountAmount = 50,
                        ValidFrom = DateTime.Now,
                        ValidUntil = DateTime.Now.AddYears(1),//有效期一年
                        IsUsed = false,
                        UserId = user.UserId,
                    };
                    _context.Coupon.Add(coup);//加進資料庫 
                }
                await _context.SaveChangesAsync();//存檔 
                return Ok(new { message = "成功領取三張優惠券!", success = true });
            }

            else if(user == null)
            {
                return Ok(new { message = "領取失敗,您尚未註冊或登入", success = false});
            }
            return Ok(new { message = "領取失敗, 請洽客服人員", success = false });
        }

        [HttpGet("MapApiKey")]
        public async Task<IActionResult> GetGoogleMapKey([FromQuery] double Lat, [FromQuery] double lng)
        {
            string googleMapKey = Environment.GetEnvironmentVariable("GOOGLE_MAP_API_KEY");
            string googleMapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={Lat},{lng}&zoom=18&size=600x300&markers=color:red%7Clabel:P%7C{Lat},{lng}&key={googleMapKey}";

            using var client = new HttpClient();
            var response = await client.GetAsync(googleMapUrl);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to fetch map from Google API");
            }
            var content = await response.Content.ReadAsByteArrayAsync();
            return File(content, "image/png");

        }
        // DELETE: api/Customers/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteCustomer(int id)
        //{
        //    var customer = await _context.Customer.FindAsync(id);
        //    if (customer == null)
        //    {
        //        return NotFound();
        //    }

            //    _context.Customer.Remove(customer);
            //    await _context.SaveChangesAsync();

            //    return NoContent();
            //}

        private bool CustomerExists(int id)
        {
            return _context.Customer.Any(e => e.UserId == id);
        }
    }

}
