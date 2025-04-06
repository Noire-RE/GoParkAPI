namespace GoParkAPI.DTO
{
    public class CarsDTO
    {
        public int carId { get; set; }
        public string licensePlate { get; set; }
        public DateTime? registerDate { get; set; }
        public bool isActive { get; set; }
    }
}