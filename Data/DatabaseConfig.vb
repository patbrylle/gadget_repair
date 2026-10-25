Namespace Data

    Public Module DatabaseConfig

        Public ReadOnly ConnectionString As String = GetConnectionString()

        Private Function GetConnectionString() As String
            Dim configuredConnectionString As String =
                Environment.GetEnvironmentVariable("GADGET_REPAIR_CONNECTION_STRING")

            If Not String.IsNullOrWhiteSpace(configuredConnectionString) Then
                Return configuredConnectionString
            End If

            Return "Server=127.0.0.1;Port=3306;Database=gadget_repairdb;User=root;Password=Brylle_121105;SslMode=None;"
        End Function

    End Module

End Namespace