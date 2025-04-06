namespace GoParkAPI.DTO
{
    public class ChangePswDTO
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
    public class ForgotDTO
    {
        public string Email { get; set; }
    }

    public class ResetDTO
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }

}
