Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Models

Namespace Data

    Public Class ApplicationDbContext
        Inherits DbContext

        Public Sub New(
            options As DbContextOptions(Of ApplicationDbContext)
        )
            MyBase.New(options)
        End Sub

        ' ============================================================
        ' DATABASE TABLES
        ' ============================================================

        Public Property Customers As DbSet(Of Customer)
        Public Property Repairs As DbSet(Of Repair)
        Public Property Technicians As DbSet(Of Technician)
        Public Property Inventories As DbSet(Of Inventory)
        Public Property Payments As DbSet(Of Payment)
        Public Property Admins As DbSet(Of AdminUser)
        Public Property TechnicianAccounts As DbSet(Of TechnicianAccount)
        Public Property RepairParts As DbSet(Of RepairPart)
                Public Property Notifications As DbSet(Of AppNotification)
        Public Property AppSettings As DbSet(Of AppSetting)

        ' ============================================================
        ' MODEL CONFIGURATION
        ' ============================================================

        Protected Overrides Sub OnModelCreating(
            modelBuilder As ModelBuilder
        )

            MyBase.OnModelCreating(modelBuilder)

            ' CUSTOMER → REPAIR
            modelBuilder.Entity(Of Repair)() _
                .HasOne(Of Customer)() _
                .WithMany() _
                .HasForeignKey(Function(r) r.CustomerId) _
                .OnDelete(DeleteBehavior.Cascade)

            ' ADMINS: unique username
            modelBuilder.Entity(Of AdminUser)() _
                .HasIndex(Function(a) a.Username) _
                .IsUnique()

            ' TECHNICIAN ACCOUNT: one account per technician
            modelBuilder.Entity(Of TechnicianAccount)() _
                .HasOne(Function(a) a.Technician) _
                .WithOne() _
                .HasForeignKey(Of TechnicianAccount)(
                    Function(a) a.TechnicianId
                ) _
                .OnDelete(DeleteBehavior.Cascade)

            modelBuilder.Entity(Of TechnicianAccount)() _
                .HasIndex(Function(a) a.TechnicianId) _
                .IsUnique()

            ' REPAIR → PARTS USED (deleting a repair deletes its part lines)
            modelBuilder.Entity(Of RepairPart)() _
                .HasOne(Of Repair)() _
                .WithMany() _
                .HasForeignKey(Function(p) p.RepairId) _
                .OnDelete(DeleteBehavior.Cascade)
                ' stock changes are checked against the quantity that was read
modelBuilder.Entity(Of Inventory)() _
    .Property(Function(i) i.Quantity) _
    .IsConcurrencyToken()

        End Sub

    End Class

End Namespace