namespace UserService.Dtos
{
    public class AuthResultDto
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? UserId { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
