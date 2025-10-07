using Microsoft.EntityFrameworkCore;
using SFTracker.Models;

namespace SFTracker.Data;

public class SFTrackerDbContext(DbContextOptions<SFTrackerDbContext> options) : DbContext(options) {
	public DbSet<DbPart> Parts { get; set; }
	public DbSet<Factory> Factories { get; set; }
	public DbSet<FactoryInput> FactoryInputs { get; set; }
	public DbSet<FactoryOutput> FactoryOutputs { get; set; }
	public DbSet<Network> Networks { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		/*
		// Configure Factory relationships
		modelBuilder.Entity<FactoryInput>()
			.HasOne(fi => fi.Factory)
			.WithMany(f => f.Inputs)
			.HasForeignKey(fi => fi.FactoryId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<FactoryInput>()
			.HasOne(fi => fi.Part)
			.WithMany(p => p.FactoryInputs)
			.HasForeignKey(fi => fi.PartId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<FactoryOutput>()
			.HasOne(fo => fo.Factory)
			.WithMany(f => f.Outputs)
			.HasForeignKey(fo => fo.FactoryId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<FactoryOutput>()
			.HasOne(fo => fo.Part)
			.WithMany(p => p.FactoryOutputs)
			.HasForeignKey(fo => fo.PartId)
			.OnDelete(DeleteBehavior.Restrict);

		// Ensure part names are unique
		modelBuilder.Entity<DbPart>()
			.HasIndex(p => p.Name)
			.IsUnique();
		*/
	}
}
