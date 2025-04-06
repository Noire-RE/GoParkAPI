namespace GoParkAPI.DTO
{
    public class LoginsDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        
    }

    public class ExitDTO
    {
        public bool Exit { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }
       

    }



}
