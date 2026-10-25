Namespace Models

    Public Class Customer

        Public Property CustomerId As Integer

        Public Property CustomerName As String = ""

        Public ReadOnly Property FullName As String
            Get
                Return If(String.IsNullOrWhiteSpace(CustomerName), "", CustomerName.Trim())
            End Get
        End Property

        Public Property ContactNumber As String = ""

        Public Property Address As String = ""

    End Class

End Namespace