using CoachTraining.Api.DTOs.Auth;

namespace CoachTraining.Api.Services;

public interface IPasswordService
{
    Task<string?> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task RequestResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<string?> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
