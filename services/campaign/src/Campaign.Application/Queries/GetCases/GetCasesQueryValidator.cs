using FluentValidation;

namespace Campaign.Application.Queries.GetCases;

public sealed class GetCasesQueryValidator : AbstractValidator<GetCasesQuery>
{
    public GetCasesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa 1 veya daha buyuk olmalidir.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1-100 arasinda olmalidir.");
    }
}
