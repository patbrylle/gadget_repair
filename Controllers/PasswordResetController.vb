Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Controllers

    Public Class PasswordResetResult
        Public Property Success As Boolean
        Public Property Message As String = ""
    End Class


    Public Class PasswordResetController

        Private Const MaxAttempts As Integer = 5
        Private Const LockMinutes As Integer = 2

        Private Shared ReadOnly sync As New Object()
        Private Shared ReadOnly failed As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        Private Shared ReadOnly lockedUntil As New Dictionary(Of String, DateTime)(StringComparer.OrdinalIgnoreCase)


        Public Function ResetPassword(email As String,
                                      verifier As String,
                                      newPassword As String,
                                      confirmPassword As String) As PasswordResetResult

            email = If(email, "").Trim()
            verifier = If(verifier, "").Trim()

            If email.Length = 0 Then
                Return Fail("Please enter your email.")
            End If

            If verifier.Length = 0 Then
                Return Fail("Please enter your contact number (technician) or username (admin).")
            End If

            Dim pwError As String = ValidatePassword(newPassword, confirmPassword)

            If pwError.Length > 0 Then
                Return Fail(pwError)
            End If

            Dim wait As Integer = SecondsLocked(email)

            If wait > 0 Then
                Return Fail("Too many attempts. Try again in " & wait.ToString() & "s.")
            End If

            Dim lower As String = email.ToLower()

            Using db As ApplicationDbContext = DbFactory.Create()

                ' ---- technician first (same order as sign in) ----
                Dim account = db.TechnicianAccounts.
                    Include(Function(a) a.Technician).
                    FirstOrDefault(Function(a) a.Technician.Email.ToLower() = lower)

                If account IsNot Nothing AndAlso
                   account.Technician IsNot Nothing AndAlso
                   SamePhone(account.Technician.ContactNumber, verifier) Then

                    If Not account.IsActive Then
                        Return Fail("This technician account is disabled.")
                    End If

                    account.PasswordHash = TechnicianAccount.HashPassword(newPassword)
                    db.SaveChanges()

                    ClearFailures(email)
                    Return Ok()

                End If

                ' ---- admin ----
                Dim admin = db.Admins.FirstOrDefault(Function(a) a.Email.ToLower() = lower)

                If admin IsNot Nothing AndAlso
                   String.Equals(admin.Username, verifier, StringComparison.OrdinalIgnoreCase) Then

                    If Not admin.IsActive Then
                        Return Fail("This account is disabled.")
                    End If

                    admin.PasswordHash = AdminUser.HashPassword(newPassword)
                    db.SaveChanges()

                    ClearFailures(email)
                    AuthController.ClearLockout(email)
                    Return Ok()

                End If

            End Using

            RegisterFailure(email)

            ' same message whether the email exists or not
            Return Fail("The details you entered do not match our records.")

        End Function


        ' Returns "" when valid, otherwise the error text.
        Public Shared Function ValidatePassword(newPassword As String, confirmPassword As String) As String

            If String.IsNullOrEmpty(newPassword) Then
                Return "Please enter a new password."
            End If

            If newPassword.Length < 8 Then
                Return "Password must be at least 8 characters."
            End If

            If newPassword.Length > 128 Then
                Return "Password must not exceed 128 characters."
            End If

            If Not newPassword.Any(Function(c) Char.IsLetter(c)) OrElse
               Not newPassword.Any(Function(c) Char.IsDigit(c)) Then
                Return "Password must contain at least one letter and one number."
            End If

            If newPassword <> confirmPassword Then
                Return "Passwords do not match."
            End If

            Return ""

        End Function


        Private Shared Function SamePhone(stored As String, entered As String) As Boolean

            Dim a As String = CustomerValidator.NormalizeContact(stored)
            Dim b As String = CustomerValidator.NormalizeContact(entered)

            Return a.Length > 0 AndAlso a = b

        End Function


        Private Shared Function Ok() As PasswordResetResult
            Return New PasswordResetResult With {.Success = True, .Message = "Password updated."}
        End Function

        Private Shared Function Fail(text As String) As PasswordResetResult
            Return New PasswordResetResult With {.Success = False, .Message = text}
        End Function


        ' ---------------- attempt limiting ----------------

        Private Shared Function SecondsLocked(key As String) As Integer

            SyncLock sync

                Dim until As DateTime

                If lockedUntil.TryGetValue(key, until) Then

                    If DateTime.Now < until Then
                        Return CInt((until - DateTime.Now).TotalSeconds) + 1
                    End If

                    lockedUntil.Remove(key)
                    failed.Remove(key)

                End If

            End SyncLock

            Return 0

        End Function

        Private Shared Sub RegisterFailure(key As String)

            SyncLock sync

                Dim count As Integer = 0
                failed.TryGetValue(key, count)
                count += 1
                failed(key) = count

                If count >= MaxAttempts Then
                    lockedUntil(key) = DateTime.Now.AddMinutes(LockMinutes)
                End If

            End SyncLock

        End Sub

        Private Shared Sub ClearFailures(key As String)

            SyncLock sync
                failed.Remove(key)
                lockedUntil.Remove(key)
            End SyncLock

        End Sub

    End Class

End Namespace