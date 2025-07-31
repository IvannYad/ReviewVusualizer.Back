using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviewVisualizer.Data.Dto
{
    public record LoginRequest(string Username, string Password);
    public record LoginResponse(bool Success, string? Error = null);
    public record RegisterRequest(string Username, string Password, string PasswordConfirmation);
    public record RegisterResponse(bool Success, string? UserName = null, string? Error = null);
    public record LogoffRequest(string Username);
    public record LogoffResponse(bool Success, string? Error = null);
}
