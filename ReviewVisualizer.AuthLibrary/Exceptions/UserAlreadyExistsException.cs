namespace ReviewVisualizer.AuthLibrary.Exceptions
{
    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException(string userName)
            : this(userName, null)
        {
        }

        public UserAlreadyExistsException(string userName, Exception? innerException)
            : base($"Cannot register user {userName}, because the user with given UserName already exists in the database",
                  innerException)
        {
        }
    }
}
