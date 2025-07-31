using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviewVisualizer.Data.Dto
{
    public record LoginRequest
    {
        [Required]
        public string Username { get; init; }

        [Required]
        [MinLength(5, ErrorMessage = "Password is too short")]
        public string Password { get; init; }
    }

    public record LoginResponse(bool Success, string? Error = null);

    public record RegisterRequest
    {
        [Required]
        public string Username { get; init; }

        [Required]
        [MinLength(5, ErrorMessage = "Password is too short")]
        public string Password { get; init; }

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string PasswordConfirmation { get; init; }
    }

    public record RegisterResponse(bool Success, string? UserName = null, string? Error = null);

    public record LogoffRequest
    {
        [Required]
        public string Username { get; init; }
    }

    public record LogoffResponse(bool Success, string? Error = null);
}
