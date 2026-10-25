Imports System.Text.RegularExpressions

Namespace Controllers

    Public Class CustomerValidationException
        Inherits Exception

        Public ReadOnly Property Field As String

        Public Sub New(fieldKey As String, text As String)
            MyBase.New(text)
            Field = fieldKey
        End Sub

    End Class


    Public Class CustomerValidationResult

        Public ReadOnly Property IsValid As Boolean
        Public ReadOnly Property Field As String
        Public ReadOnly Property Message As String

        Private Sub New(valid As Boolean, fieldKey As String, text As String)
            IsValid = valid
            Field = fieldKey
            Message = text
        End Sub

        Public Shared Function Ok() As CustomerValidationResult
            Return New CustomerValidationResult(True, "", "")
        End Function

        Public Shared Function Bad(fieldKey As String, text As String) As CustomerValidationResult
            Return New CustomerValidationResult(False, fieldKey, text)
        End Function

    End Class


    Public Module CustomerValidator

        Public Const NameField As String = "Name"
        Public Const ContactField As String = "Contact"
        Public Const AddressField As String = "Address"

        Private ReadOnly NamePattern As New Regex("^\p{L}[\p{L} .'\-]*$")
        Private ReadOnly PhoneChars As New Regex("^[0-9+\s\-()]+$")
        Private ReadOnly MobilePattern As New Regex("^09\d{9}$")
        Private ReadOnly LandlinePattern As New Regex("^0(2\d{8}|[3-9]\d{8})$")


        ' "  Juan   Dela  Cruz " -> "Juan Dela Cruz"
        Public Function NormalizeName(value As String) As String
            Return Regex.Replace(If(value, "").Trim(), "\s+", " ")
        End Function


        ' "+63 917 123 4567" / "63917..." / "0917-123-4567" -> "09171234567"
        Public Function NormalizeContact(value As String) As String

            Dim s As String = Regex.Replace(If(value, ""), "[\s\-()]", "")

            If s.StartsWith("+63") Then
                s = "0" & s.Substring(3)
            ElseIf s.StartsWith("63") AndAlso s.Length = 12 Then
                s = "0" & s.Substring(2)
            End If

            Return s

        End Function


        Public Function Validate(fullName As String,
                                 contactNumber As String,
                                 address As String) As CustomerValidationResult

            ' ---------------- name ----------------
            Dim name As String = NormalizeName(fullName)

            If name.Length = 0 Then
                Return CustomerValidationResult.Bad(NameField, "Please enter the customer's full name.")
            End If

            If name.Length < 2 OrElse name.Length > 100 Then
                Return CustomerValidationResult.Bad(NameField, "Name must be 2 to 100 characters long.")
            End If

            If Not NamePattern.IsMatch(name) Then
                Return CustomerValidationResult.Bad(NameField,
                    "Name may only contain letters, spaces, periods, apostrophes and hyphens.")
            End If

            ' ---------------- contact ----------------
            Dim rawContact As String = If(contactNumber, "").Trim()

            If rawContact.Length = 0 Then
                Return CustomerValidationResult.Bad(ContactField, "Please enter the customer's contact number.")
            End If

            Dim phone As String = NormalizeContact(rawContact)

            If Not PhoneChars.IsMatch(rawContact) OrElse
               Not (MobilePattern.IsMatch(phone) OrElse LandlinePattern.IsMatch(phone)) Then

                Return CustomerValidationResult.Bad(ContactField,
                    "Enter a valid PH number, e.g. 09171234567, +639171234567 or 028 123 4567.")
            End If

            ' ---------------- address ----------------
            Dim addr As String = If(address, "").Trim()

            If addr.Length = 0 Then
                Return CustomerValidationResult.Bad(AddressField, "Please enter the customer's address.")
            End If

            If addr.Length < 5 OrElse addr.Length > 200 Then
                Return CustomerValidationResult.Bad(AddressField, "Address must be 5 to 200 characters long.")
            End If

            Return CustomerValidationResult.Ok()

        End Function

    End Module

End Namespace