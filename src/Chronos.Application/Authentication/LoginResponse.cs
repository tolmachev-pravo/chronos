namespace Chronos.Application.Authentication
{
    public class LoginResponse
    {
        public LoginResponse(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }

        public LoginResponse(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string Username { get; set; }
    }
}
