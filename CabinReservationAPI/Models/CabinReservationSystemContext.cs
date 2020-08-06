using CabinReservationAPI.Models;
using First.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CabinReservationSystemAPI.Models
{
    public class CabinReservationSystemContext : DbContext
    {
        public CabinReservationSystemContext(DbContextOptions<CabinReservationSystemContext> options) : base(options) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new Exception("Database not configured");
            }
        }
        public virtual DbSet<Invoice> Invoice { get; set; }
        public virtual DbSet<Cabin> Cabin { get; set; }
        public virtual DbSet<CabinReservation> CabinReservation { get; set; }
        public virtual DbSet<Person> Person { get; set; }
        public virtual DbSet<Post> Post { get; set; }
        public virtual DbSet<Resort> Resort { get; set; }
        public virtual DbSet<Activity> Activity { get; set; }
        public virtual DbSet<ActivityReservation> ActivityReservation { get; set; }
        public virtual DbSet<CabinImage> CabinImage { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cabin>(entity =>
            {
                entity.HasOne(cabin => cabin.Resort).WithMany(resort => resort.Cabins).HasForeignKey(cabin => cabin.ResortId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(cabin => cabin.Person).WithMany(person => person.Cabins).HasForeignKey(cabin => cabin.PersonId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(cabin => cabin.Post).WithMany(post => post.Cabins).HasForeignKey(cabin => cabin.PostalCode).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<Activity>(entity =>
            {
                entity.HasOne(activity => activity.Resort).WithMany(office => office.Activities).HasForeignKey(activity => activity.ResortId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(activity => activity.Post).WithMany(post => post.Activities).HasForeignKey(activity => activity.PostalCode);
            });
            modelBuilder.Entity<Person>(entity =>
            {
                entity.HasOne(person => person.Post).WithMany(post => post.Persons).HasForeignKey(person => person.PostalCode).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<CabinReservation>(entity =>
            {
                entity.HasOne(cabinReservation => cabinReservation.Cabin).WithMany(cabin => cabin.CabinReservations).HasForeignKey(cabinReservation => cabinReservation.CabinId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(cabinReservation => cabinReservation.Person).WithMany(person => person.CabinReservations).HasForeignKey(cabinReservation => cabinReservation.PersonId).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<ActivityReservation>(entity =>
            {
                entity.HasOne(activityReservation => activityReservation.CabinReservation).WithMany(activityReservation => activityReservation.ActivityReservations).HasForeignKey(activityReservation => activityReservation.CabinReservationId);
                entity.HasOne(activityReservation => activityReservation.Activity).WithMany(service => service.ActivityReservations).HasForeignKey(activityReservation => activityReservation.ActivityId).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasOne(invoice => invoice.CabinReservation).WithMany(cabinReservation => cabinReservation.Invoices).HasForeignKey(invoice => invoice.CabinReservationId);
            });
            modelBuilder.Entity<Resort>(entity =>
            {
                entity.HasIndex(resort => resort.ResortName).IsUnique();
            });
            modelBuilder.Entity<CabinImage>(entity =>
            {
                entity.HasOne(cabinImage => cabinImage.Cabin).WithMany(cabin => cabin.CabinImages).HasForeignKey(cabinImage => cabinImage.CabinId);
            });
            modelBuilder.Entity<CabinImage>(entity =>
            {
                entity.HasIndex(cabinImge => cabinImge.ImageUrl).IsUnique();
            });

            // Adding Post and Admin-Person
            modelBuilder.Entity<Post>().HasData(
                new Post { PostalCode = "00100", City = "Helsinki" }
                );
            modelBuilder.Entity<Person>().HasData(
                new Person { 
                    PersonId = 1,
                    PostalCode = "00100", 
                    SocialSecurityNumber= "admin@localhost.org", 
                    FirstName= "admin@localhost.org", 
                    LastName = "admin@localhost.org", 
                    PhoneNumber= "admin@localhost.org", 
                    Address = "admin@localhost.org", 
                    Email= "admin@localhost.org" }
                );
        }
    }
}
