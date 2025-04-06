namespace GoParkAPI.DTO
{
    public class LoginDTO
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string LicensePlate { get; set; } = null!;
    }
}
