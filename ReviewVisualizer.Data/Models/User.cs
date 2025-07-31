namespace ReviewVisualizer.Data.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public bool Blocked { get; set; }
    }
}
