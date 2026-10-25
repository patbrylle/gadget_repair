Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Security.Cryptography

Namespace Models

    <Table("TechnicianAccounts")>
    Public Class TechnicianAccount

        <Key>
        Public Property Id As Integer

        <Required>
        Public Property TechnicianId As Integer

        <Required, MaxLength(255)>
        Public Property PasswordHash As String = String.Empty

        Public Property IsActive As Boolean = True

        Public Property LastLogin As DateTime? = Nothing

        Public Property CreatedAt As DateTime = DateTime.Now

        <ForeignKey(NameOf(TechnicianId))>
        Public Overridable Property Technician As Technician

        ' Password hashing settings
        Private Const Iterations As Integer = 100000
        Private Const SaltSize As Integer = 16
        Private Const HashSize As Integer = 32

        Public Shared Function HashPassword(password As String) As String
            If String.IsNullOrEmpty(password) Then
                Throw New ArgumentException("Password cannot be empty.")
            End If

            Dim salt(SaltSize - 1) As Byte
            RandomNumberGenerator.Fill(salt)

            Dim hash As Byte() =
                Pbkdf2Sha256(password, salt, Iterations, HashSize)

            Return Iterations.ToString() & "." &
                   Convert.ToBase64String(salt) & "." &
                   Convert.ToBase64String(hash)
        End Function

        Public Function VerifyPassword(password As String) As Boolean
            Try
                If String.IsNullOrEmpty(password) OrElse
                   String.IsNullOrWhiteSpace(PasswordHash) Then
                    Return False
                End If

                Dim parts() As String = PasswordHash.Split("."c)

                If parts.Length <> 3 Then Return False

                Dim iterations As Integer

                If Not Integer.TryParse(parts(0), iterations) OrElse
                   iterations < 1 OrElse iterations > 1000000 Then
                    Return False
                End If

                Dim salt() As Byte = Convert.FromBase64String(parts(1))
                Dim expected() As Byte = Convert.FromBase64String(parts(2))

                If salt.Length <> SaltSize OrElse
                   expected.Length <> HashSize Then
                    Return False
                End If

                Dim actual() As Byte =
                    Pbkdf2Sha256(password, salt, iterations, expected.Length)

                Return CryptographicOperations.FixedTimeEquals(actual, expected)

            Catch
                Return False
            End Try
        End Function

        ' PBKDF2-HMAC-SHA256 (RFC 8018). Same output as
        ' Rfc2898DeriveBytes.Pbkdf2(..., HashAlgorithmName.SHA256, ...)
        ' but built on plain HMAC, which also works under Wine.
        Private Shared Function Pbkdf2Sha256(
            password As String,
            salt As Byte(),
            iterations As Integer,
            length As Integer) As Byte()

            Dim key As Byte() = System.Text.Encoding.UTF8.GetBytes(password)
            Dim output(length - 1) As Byte
            Dim blockCount As Integer = CInt(Math.Ceiling(length / 32.0))

            Using hmac As New HMACSHA256(key)

                For block As Integer = 1 To blockCount

                    Dim saltBlock(salt.Length + 3) As Byte
                    Buffer.BlockCopy(salt, 0, saltBlock, 0, salt.Length)
                    saltBlock(salt.Length) = CByte((block >> 24) And &HFF)
                    saltBlock(salt.Length + 1) = CByte((block >> 16) And &HFF)
                    saltBlock(salt.Length + 2) = CByte((block >> 8) And &HFF)
                    saltBlock(salt.Length + 3) = CByte(block And &HFF)

                    Dim u As Byte() = hmac.ComputeHash(saltBlock)
                    Dim t As Byte() = CType(u.Clone(), Byte())

                    For i As Integer = 2 To iterations
                        u = hmac.ComputeHash(u)
                        For j As Integer = 0 To t.Length - 1
                            t(j) = t(j) Xor u(j)
                        Next
                    Next

                    Dim offset As Integer = (block - 1) * 32
                    Dim count As Integer = Math.Min(32, length - offset)
                    Buffer.BlockCopy(t, 0, output, offset, count)

                Next

            End Using

            Return output

        End Function

    End Class

End Namespace