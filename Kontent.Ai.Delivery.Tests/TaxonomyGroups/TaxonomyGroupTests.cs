using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.TaxonomyGroups;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.TaxonomyGroups;

public class TaxonomyGroupTests
{
    [Fact]
    public void System_AccessedViaITaxonomyGroup_ReturnsSameInstance()
    {
        var sut = CreateTaxonomyGroup();

        var system = ((ITaxonomyGroup)sut).System;

        Assert.Same(sut.System, system);
    }

    [Fact]
    public void Terms_AccessedViaITaxonomyGroup_ReturnsSameInstance()
    {
        var sut = CreateTaxonomyGroup();

        var terms = ((ITaxonomyGroup)sut).Terms;

        Assert.Same(sut.Terms, terms);
    }

    [Fact]
    public void Terms_AccessedViaITaxonomyTermDetails_ReturnsSameInstance()
    {
        var child = new TaxonomyTermDetails
        {
            Name = "Child",
            Codename = "child",
            Terms = []
        };
        var parent = new TaxonomyTermDetails
        {
            Name = "Parent",
            Codename = "parent",
            Terms = [child]
        };

        var terms = ((ITaxonomyTermDetails)parent).Terms;

        Assert.Same(parent.Terms, terms);
    }

    private static TaxonomyGroup CreateTaxonomyGroup() => new()
    {
        System = new TaxonomyGroupSystemAttributes
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Codename = "test_group"
        },
        Terms =
        [
            new TaxonomyTermDetails
            {
                Name = "Term 1",
                Codename = "term_1",
                Terms = []
            }
        ]
    };
}
