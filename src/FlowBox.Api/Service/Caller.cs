using FlowBox.Api.Enums;

namespace FlowBox.Api.Service;

public record Caller(Guid UserId, UserRole Role);
