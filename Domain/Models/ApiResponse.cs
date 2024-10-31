namespace PikaNoteAPI.Models
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Payload { get; set; }
    }
}