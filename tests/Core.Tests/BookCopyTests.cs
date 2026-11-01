using Core.Domain;

namespace Core.Tests;

public class BookCopyTests
{
    [Fact]
    public void Issue_WhenAlreadyIssued_ThrowsDomainException_AndKeepsState()
    {
        var copy = BookCopy.Create("C-1", "978-966-03-1234-5");
        copy.Issue();

        Assert.Throws<DomainException>(() => copy.Issue());

        Assert.True(copy.IsIssued);
    }

    [Fact]
    public void Return_WhenNotIssued_ThrowsDomainException()
    {
        var copy = BookCopy.Create("C-1", "978-966-03-1234-5");

        Assert.Throws<DomainException>(() => copy.Return());

        Assert.False(copy.IsIssued);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankIsbn_ThrowsArgumentException(string isbn)
    {
        Assert.Throws<ArgumentException>(() => BookCopy.Create("C-1", isbn));
    }
}