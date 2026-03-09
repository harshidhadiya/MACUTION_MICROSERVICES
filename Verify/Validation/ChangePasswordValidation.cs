using System.Data;
using FluentValidation;
using VERIFY.Data.Dto;

namespace VERIFY.Validation
{
    public class ChangePasswordValidation:AbstractValidator<changePasswordDto>
    {
        public ChangePasswordValidation()
        {
            RuleFor(x => x.Password).NotEmpty().WithMessage("Old Password is required");
            RuleFor(x => x.ConfirmPassword).NotEmpty().WithMessage("New Password is required");
            RuleFor(x=> x.ConfirmPassword).Equal(x => x.Password).WithMessage("New Password and Confirm Password must match");
        }
    }
}