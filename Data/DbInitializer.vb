Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Models

Namespace Data

    Public Module DbInitializer

        Public Sub Initialize()

            Using db As ApplicationDbContext = DbFactory.Create()

                db.Database.Migrate()

                If Not db.Admins.Any() Then

                    db.Admins.Add(New AdminUser With {
                        .Username = "admin",
                        .Email = "admin@gmail.com",
                        .FullName = "System Administrator",
                        .PasswordHash = AdminUser.HashPassword("admin123"),
                        .IsActive = True
                    })

                    db.SaveChanges()

                End If

            End Using

        End Sub

    End Module

End Namespace