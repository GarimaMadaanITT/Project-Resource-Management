using Prm.Application.Validation;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class EmailValidatorTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("name.surname@company.co.uk")]
    public void ValidateAndNormalize_Accepts_Valid_Emails(string email)
    {
        var result = EmailValidator.ValidateAndNormalize(email);
        Assert.Equal(email, result);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@missing.com")]
    [InlineData("")]
    public void ValidateAndNormalize_Rejects_Invalid_Emails(string email)
    {
        Assert.Throws<DomainException>(() => EmailValidator.ValidateAndNormalize(email));
    }
}
