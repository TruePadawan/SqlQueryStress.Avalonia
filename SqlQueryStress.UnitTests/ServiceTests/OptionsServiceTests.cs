using FluentAssertions;
using SqlQueryStress.Services.OptionsService;
using SqlQueryStress.ViewModels;

namespace SqlQueryStress.UnitTests.ServiceTests;

public class OptionsServiceTests
{
    [Fact]
    public void Defaults_MatchOriginalApplication()
    {
        var options = new OptionsService();

        options.EnableConnectionPooling.Should().BeTrue();
        options.CollectIOStatistics.Should().BeTrue();
        options.CollectTimeStatistics.Should().BeTrue();
        options.KillQueriesOnCancel.Should().BeTrue();
        options.ForceClientRetrievalOfData.Should().BeFalse();
        options.ConnectionTimeout.Should().Be(15);
        options.CommandTimeout.Should().Be(0);
    }

    [Fact]
    public void OptionsViewModel_IsInitialisedFromServiceDefaults()
    {
        var viewModel = new OptionsViewModel(new OptionsService());

        viewModel.EnableConnectionPooling.Should().BeTrue();
        viewModel.CollectIOStatistics.Should().BeTrue();
        viewModel.CollectTimeStatistics.Should().BeTrue();
        viewModel.KillQueriesOnCancel.Should().BeTrue();
        viewModel.ForceClientRetrievalOfData.Should().BeFalse();
    }

    [Fact]
    public void ApplyChanges_PersistsEditsToService()
    {
        var service = new OptionsService();
        var viewModel = new OptionsViewModel(service)
        {
            CollectIOStatistics = false,
            CollectTimeStatistics = false,
        };

        viewModel.ApplyChanges();

        service.CollectIOStatistics.Should().BeFalse();
        service.CollectTimeStatistics.Should().BeFalse();
    }
}
