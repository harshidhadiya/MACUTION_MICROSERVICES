using FluentValidation;
using PRODUCT.Data.Dto;

namespace PRODUCT.Validation
{
    public class productCreateValidation:AbstractValidator<createProduct>
    {
        public productCreateValidation()
        {
            RuleFor(x => x.product_name).NotEmpty().WithMessage("Product name is required.");
            RuleFor(x => x.Buy_Date).NotEmpty().WithMessage("Buy date is required.");
        }
    }
}