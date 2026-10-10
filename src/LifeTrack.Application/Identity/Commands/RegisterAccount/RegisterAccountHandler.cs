using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using MediatR;

namespace LifeTrack.Application.Identity;

public sealed class RegisterAccountHandler(
    IAccountService accountService) : IRequestHandler<RegisterAccountCommand, RegisterAccountResult>
{

    public async Task<RegisterAccountResult> Handle(RegisterAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await accountService.RegisterAsync(request.Email, request.Password, cancellationToken);
            if (!result.Succeeded && result.Errors is not null)
            {
                foreach (var error in result.Errors)
                {
                    if (error.Contains("email", StringComparison.OrdinalIgnoreCase) &&
                        error.Contains("already", StringComparison.OrdinalIgnoreCase))
                    {
                        return new RegisterAccountResult(false, new[] { "An account with this email already exists." });
                    }
                }
            }
            return new RegisterAccountResult(result.Succeeded, result.Errors);
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
        catch (NotFoundException ex)
        {
            throw new ResourceNotFoundException(ex.Message);
        }
    }
}
