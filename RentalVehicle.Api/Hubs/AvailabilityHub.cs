using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RentalVehicle.Api.Hubs;

[Authorize]
public class AvailabilityHub : Hub
{
    public Task WatchVehicle(int vehicleId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"vehicle-{vehicleId}");

    public Task UnwatchVehicle(int vehicleId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"vehicle-{vehicleId}");
}
