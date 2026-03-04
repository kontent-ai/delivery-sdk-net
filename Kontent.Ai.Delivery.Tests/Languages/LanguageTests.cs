using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Languages;

namespace Kontent.Ai.Delivery.Tests.Languages;

public class LanguageTests
{
    [Fact]
    public void System_AccessedViaILanguage_ReturnsSameInstance()
    {
        var sut = new Language
        {
            System = new LanguageSystemAttributes
            {
                Id = Guid.NewGuid(),
                Codename = "en-US",
                Name = "English"
            }
        };

        var system = ((ILanguage)sut).System;

        Assert.Same(sut.System, system);
    }

    [Fact]
    public void SystemAttributes_PropertiesRoundTrip()
    {
        var id = Guid.NewGuid();
        var sut = new LanguageSystemAttributes
        {
            Id = id,
            Codename = "es-ES",
            Name = "Spanish"
        };

        Assert.Equal(id, sut.Id);
        Assert.Equal("es-ES", sut.Codename);
        Assert.Equal("Spanish", sut.Name);
    }

    [Fact]
    public void Language_WithExpression_CreatesClone()
    {
        var sut = new Language
        {
            System = new LanguageSystemAttributes
            {
                Id = Guid.NewGuid(),
                Codename = "en-US",
                Name = "English"
            }
        };

        var clone = sut with { };

        Assert.NotSame(sut, clone);
        Assert.Same(sut.System, clone.System);
    }

    [Fact]
    public void LanguageSystemAttributes_WithExpression_CreatesClone()
    {
        var sut = new LanguageSystemAttributes
        {
            Id = Guid.NewGuid(),
            Codename = "fr-FR",
            Name = "French"
        };

        var clone = sut with { };

        Assert.NotSame(sut, clone);
        Assert.Equal(sut.Id, clone.Id);
        Assert.Equal(sut.Codename, clone.Codename);
        Assert.Equal(sut.Name, clone.Name);
    }
}
