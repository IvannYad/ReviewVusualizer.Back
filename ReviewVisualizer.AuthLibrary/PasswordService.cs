namespace ReviewVisualizer.AuthLibrary
{
    public class PasswordService
    {
        private readonly string _passwordSecret;

        public PasswordService(string passwordSecret)
        {
            _passwordSecret = passwordSecret ?? throw new ArgumentNullException(nameof(passwordSecret));
        }

        public string HashPassword(string password)
        {
            var combined = password + _passwordSecret;
            return BCrypt.Net.BCrypt.HashPassword(combined);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var combined = providedPassword + _passwordSecret;
            return BCrypt.Net.BCrypt.Verify(combined, hashedPassword);
        }
    }
}
