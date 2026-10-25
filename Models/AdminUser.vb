Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Security.Cryptography

Namespace Models

    <Table("Admins")>
    Public Class AdminUser

        <Key>
        Public Property Id As Integer

        <Required, MaxLength(50)>
        Public Property Username As String = ""

        <Required, MaxLength(200)>
        <EmailAddress>
        Public Property Email As String = ""

        <Required, MaxLength(100)>
        Public Property FullName As String = ""

        <Required, MaxLength(200)>
        Public Property PasswordHash As String = ""

        Public Property IsActive As Boolean = True

        Public Property LastLogin As DateTime? = Nothing

        Public Property CreatedAt As DateTime = DateTime.Now

        ' ============================================================
        ' PASSWORD HASHING
        ' NOTE: keep the Rfc2898DeriveBytes constructor. The static
        ' Pbkdf2 method fails under Wine (CryptographicException 0xc1000008).
        ' ============================================================

        Private Const Iterations As Integer = 100000
        Private Const SaltSize As Integer = 16
        Private Const HashSize As Integer = 32

        Public Shared Function HashPassword(password As String) As String

            Dim salt(SaltSize - 1) As Byte

            Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
                rng.GetBytes(salt)
            End Using

            Using kdf As New Rfc2898DeriveBytes(password, salt, Iterations)

                Dim hash() As Byte = kdf.GetBytes(HashSize)

                Return Iterations.ToString() & "." &
                       Convert.ToBase64String(salt) & "." &
                       Convert.ToBase64String(hash)

            End Using

        End Function

        Public Function VerifyPassword(password As String) As Boolean

            Try

                Dim parts() As String = PasswordHash.Split("."c)

                If parts.Length <> 3 Then Return False

                Dim iter As Integer = Integer.Parse(parts(0))
                Dim salt() As Byte = Convert.FromBase64String(parts(1))
                Dim expected() As Byte = Convert.FromBase64String(parts(2))

                Using kdf As New Rfc2898DeriveBytes(password, salt, iter)

                    Dim actual() As Byte = kdf.GetBytes(expected.Length)

                    Dim diff As Integer = 0

                    For i As Integer = 0 To expected.Length - 1
                        diff = diff Or (expected(i) Xor actual(i))
                    Next

                    Return diff = 0

                End Using

            Catch
                Return False
            End Try

        End Function

    End Class

End Namespace