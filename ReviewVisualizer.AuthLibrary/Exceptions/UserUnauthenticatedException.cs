namespace ReviewVisualizer.AuthLibrary.Exceptions
{
    public class UserUnauthenticatedException : Exception
    {
        public UserUnauthenticatedException(string userName, string password)
            : this(userName, password, null)
        {
        }

        public UserUnauthenticatedException(string userName, string password, Exception? innerException)
            : base($"User not found with credentials: [UserName: {userName};Password: {password}]. Either username or password is incorrect",
                  innerException)
        {
        }
    }
}