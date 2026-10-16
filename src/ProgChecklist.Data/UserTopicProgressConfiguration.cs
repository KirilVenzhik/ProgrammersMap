using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProgChecklist.Data;

internal sealed class UserTopicProgressConfiguration : IEntityTypeConfiguration<UserTopicProgress>
{
    public void Configure(EntityTypeBuilder<UserTopicProgress> builder)
    {
        builder.ToTable("UserTopicProgress");
        builder.HasKey(p => new { p.UserId, p.TopicKey });
        builder.Property(p => p.TopicKey).IsRequired().HasMaxLength(UserTopicProgress.TopicKeyMaxLength);
        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
