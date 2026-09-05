using FlowBox.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowBox.Api.Data;

public class FlowBoxDbContext(DbContextOptions<FlowBoxDbContext> options) : DbContext(options)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Courier> Couriers => Set<Courier>();
}