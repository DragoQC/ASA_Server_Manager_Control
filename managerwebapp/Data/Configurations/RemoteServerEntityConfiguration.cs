using managerwebapp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace managerwebapp.Data.Configurations;

public sealed class RemoteServerEntityConfiguration : IEntityTypeConfiguration<RemoteServerEntity>
{
    public void Configure(EntityTypeBuilder<RemoteServerEntity> builder)
    {
        builder.ToTable("RemoteServers");

        builder.HasKey(remoteServer => remoteServer.Id);

        builder.Property(remoteServer => remoteServer.Id)
            .ValueGeneratedOnAdd();

        builder.Property(remoteServer => remoteServer.CreatedAtUtc);

        builder.Property(remoteServer => remoteServer.ModifiedAtUtc);

        builder.Property(remoteServer => remoteServer.IpAddress)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(remoteServer => remoteServer.ApiKeyHash)
            .HasMaxLength(512)
            .IsRequired();
    }
}
