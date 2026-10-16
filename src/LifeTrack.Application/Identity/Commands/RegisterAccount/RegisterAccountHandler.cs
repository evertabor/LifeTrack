using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Application.Finance;
using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Identity;

public sealed class RegisterAccountHandler(
    IAccountService accountService,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterAccountCommand, RegisterAccountResult>
{

    public async Task<RegisterAccountResult> Handle(RegisterAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {

            var result = await accountService.RegisterAsync(request.Email, request.Password, request.DisplayName, request.PhoneNumber, cancellationToken);
            if (!result.Succeeded && result.Errors is not null)
            {
                foreach (var error in result.Errors)
                {
                    if (error.Contains("email", StringComparison.OrdinalIgnoreCase) &&
                        error.Contains("already", StringComparison.OrdinalIgnoreCase))
                    {
                        return new RegisterAccountResult(false, new[] { "Ya existe una cuenta con ese correo." });
                    }
                }
            }
            if (result.Succeeded && result.UserId.HasValue)
            {
                await SeedDefaultCategories(result.UserId.Value, cancellationToken);
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

    private async Task SeedDefaultCategories(Guid userId, CancellationToken cancellationToken)
    {
        foreach (var defaults in DefaultCategories.All)
        {
            if (await categories.ExistsAsync(defaults.Name, userId, cancellationToken))
            {
                continue;
            }
            await categories.AddAsync(Category.Create(defaults.Name, userId, defaults.Type), cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
