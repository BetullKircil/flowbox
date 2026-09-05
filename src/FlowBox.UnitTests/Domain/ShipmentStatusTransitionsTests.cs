using FlowBox.Api.Domain;
using FlowBox.Api.Enums;
using FluentAssertions;

namespace FlowBox.UnitTests.Domain;

public class ShipmentStatusTransitionsTests
{
    [Theory]
    [InlineData(ShipmentStatus.Created, ShipmentStatus.PickedUp)]
    [InlineData(ShipmentStatus.PickedUp, ShipmentStatus.ArrivedAtSortingCenter)]
    [InlineData(ShipmentStatus.ArrivedAtSortingCenter, ShipmentStatus.Sorted)]
    [InlineData(ShipmentStatus.Sorted, ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.ArrivedAtDistributionCenter)]
    [InlineData(ShipmentStatus.ArrivedAtDistributionCenter, ShipmentStatus.OutForDelivery)]
    [InlineData(ShipmentStatus.OutForDelivery, ShipmentStatus.Delivered)]
    public void IsValidTransition_FollowsHappyPath_ReturnsTrue(ShipmentStatus from, ShipmentStatus to)
    {
        ShipmentStatusTransitions.IsValidTransition(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(ShipmentStatus.Created, ShipmentStatus.Sorted)]
    [InlineData(ShipmentStatus.Created, ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.PickedUp, ShipmentStatus.OutForDelivery)]
    [InlineData(ShipmentStatus.OutForDelivery, ShipmentStatus.Created)]
    public void IsValidTransition_SkipsAheadOrGoesBackwards_ReturnsFalse(ShipmentStatus from, ShipmentStatus to)
    {
        ShipmentStatusTransitions.IsValidTransition(from, to).Should().BeFalse();
    }

    [Theory]
    [InlineData(ShipmentStatus.Created)]
    [InlineData(ShipmentStatus.PickedUp)]
    [InlineData(ShipmentStatus.ArrivedAtSortingCenter)]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.OutForDelivery)]
    public void IsValidTransition_ToFailed_IsAlwaysAllowedFromNonTerminalStatus(ShipmentStatus from)
    {
        ShipmentStatusTransitions.IsValidTransition(from, ShipmentStatus.Failed).Should().BeTrue();
    }

    [Theory]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.Failed)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.Created)]
    [InlineData(ShipmentStatus.Failed, ShipmentStatus.Created)]
    [InlineData(ShipmentStatus.Failed, ShipmentStatus.Delivered)]
    public void IsValidTransition_FromTerminalStatus_AlwaysReturnsFalse(ShipmentStatus from, ShipmentStatus to)
    {
        ShipmentStatusTransitions.IsValidTransition(from, to).Should().BeFalse();
    }

    [Fact]
    public void GetValidNextStatuses_FromNonTerminalStatus_IncludesHappyPathAndFailed()
    {
        var next = ShipmentStatusTransitions.GetValidNextStatuses(ShipmentStatus.Sorted);

        next.Should().BeEquivalentTo([ShipmentStatus.InTransit, ShipmentStatus.Failed]);
    }

    [Fact]
    public void GetValidNextStatuses_FromLastHappyPathStatus_OnlyIncludesFailed()
    {
        var next = ShipmentStatusTransitions.GetValidNextStatuses(ShipmentStatus.OutForDelivery);

        next.Should().BeEquivalentTo([ShipmentStatus.Delivered, ShipmentStatus.Failed]);
    }

    [Theory]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Failed)]
    public void GetValidNextStatuses_FromTerminalStatus_ReturnsEmpty(ShipmentStatus from)
    {
        ShipmentStatusTransitions.GetValidNextStatuses(from).Should().BeEmpty();
    }

    [Theory]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Failed)]
    public void IsTerminal_ForDeliveredOrFailed_ReturnsTrue(ShipmentStatus status)
    {
        ShipmentStatusTransitions.IsTerminal(status).Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_ForCreated_ReturnsFalse()
    {
        ShipmentStatusTransitions.IsTerminal(ShipmentStatus.Created).Should().BeFalse();
    }
}
