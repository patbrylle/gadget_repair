Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design

Namespace Data

    Public Class ApplicationDbContextFactory
        Implements IDesignTimeDbContextFactory(Of ApplicationDbContext)

        Public Function CreateDbContext(args As String()) As ApplicationDbContext _
            Implements IDesignTimeDbContextFactory(Of ApplicationDbContext).CreateDbContext

            Dim optionsBuilder = New DbContextOptionsBuilder(Of ApplicationDbContext)()

            optionsBuilder.UseMySql(
                DatabaseConfig.ConnectionString,
                ServerVersion.AutoDetect(DatabaseConfig.ConnectionString)
            )

            Return New ApplicationDbContext(optionsBuilder.Options)

        End Function

    End Class

End Namespace