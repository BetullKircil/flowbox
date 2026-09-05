using FlowBox.Api.Endpoints;

namespace FlowBox.Api.Registry;

public static class EndpointRouteBuilderExtensions
{
    public static void MapEndpoints(this IEndpointRouteBuilder routeBuilder, List<Type> endpointTypes)
    {
        foreach (var type in endpointTypes)
        {
            if (Activator.CreateInstance(type) is IEndpoint endpoint)
            {
                endpoint.MapEndpoint(routeBuilder);
            }
        }
    }
}
