Imports Microsoft.EntityFrameworkCore

Namespace Data

    Public Module DbFactory

        Public Function Create() As ApplicationDbContext
            Dim builder As New DbContextOptionsBuilder(Of ApplicationDbContext)()
            builder.UseMySql(
                DatabaseConfig.ConnectionString,
                ServerVersion.AutoDetect(DatabaseConfig.ConnectionString))
            Return New ApplicationDbContext(builder.Options)
        End Function

    End Module

End Namespace