using FluentValidation;
using VERIFY.Data.Dto;


namespace VERIFY.Validation
{
    public class UserCreateValidation : AbstractValidator<UserCreateDto>
    {
        public UserCreateValidation()
        {
            RuleFor(x => x.Name).Custom((name, context) =>
{

    if (String.IsNullOrEmpty(name))
    {
        context.AddFailure("Name", "Empty CanNot Be Accepted Here");
        return;
    }
    if (name.Length < 3)
    {
        context.AddFailure("Lenght Must contain more then 3 lenght ");
    }
    return;
});
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email format is not valid");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Mobile number is required");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required");

            RuleFor(x => x.Password).NotEmpty().WithMessage("password must filled up");
            RuleFor(x => x.Role)
                                .NotEmpty()
                                .Must(role => role == "USER" || role == "SELLER" || role == "ADMIN")
                                .WithMessage("Role must be either USER or SELLER");
        }
    }
}