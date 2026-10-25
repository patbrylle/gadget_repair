Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Public Enum LoginStatus
    Success
    InvalidCredentials
    AccountDisabled
    LockedOut
End Enum

Public Class LoginResult
    Public Property Status As LoginStatus
    Public Property User As AdminUser
    Public Property Message As String = ""
    Public Property LockSecondsLeft As Integer = 0
End Class

Public Class AuthController

    Private Const MaxAttempts As Integer = 5
    Private Const LockMinutes As Integer = 2

    Private Shared ReadOnly failedAttempts As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
    Private Shared ReadOnly lockedUntil As New Dictionary(Of String, DateTime)(StringComparer.OrdinalIgnoreCase)

    ' Currently signed-in admin: AuthController.CurrentUser
    Public Shared Property CurrentUser As AdminUser = Nothing

    ' ------------------------------------------------------
    ' LOGIN
    Public Function Login(email As String, password As String) As LoginResult

    email = If(email, "").Trim()

    ' lockout check
    Dim until As DateTime

    If lockedUntil.TryGetValue(email, until) Then

        If DateTime.Now < until Then

            Dim secs As Integer =
                CInt((until - DateTime.Now).TotalSeconds) + 1

            Return New LoginResult With {
                .Status = LoginStatus.LockedOut,
                .LockSecondsLeft = secs,
                .Message = "Too many attempts. Try again in " &
                           secs.ToString() & "s."
            }

        End If

        lockedUntil.Remove(email)
        failedAttempts.Remove(email)

    End If

    Dim user As AdminUser = FindUser(email)

    If user Is Nothing OrElse Not user.VerifyPassword(password) Then

        RegisterFailure(email)

        Return New LoginResult With {
            .Status = LoginStatus.InvalidCredentials,
            .Message = "Invalid email or password."
        }

    End If

    If Not user.IsActive Then

        Return New LoginResult With {
            .Status = LoginStatus.AccountDisabled,
            .Message = "This account is disabled."
        }

    End If

    failedAttempts.Remove(email)

    SaveLastLogin(user)

    CurrentUser = user

    Return New LoginResult With {
        .Status = LoginStatus.Success,
        .User = user
    }

End Function

    Public Shared Sub Logout()
        CurrentUser = Nothing
    End Sub

    Private Sub RegisterFailure(username As String)
        Dim count As Integer = 0
        failedAttempts.TryGetValue(username, count)
        count += 1
        failedAttempts(username) = count

        If count >= MaxAttempts Then
            lockedUntil(username) = DateTime.Now.AddMinutes(LockMinutes)
        End If
    End Sub
    Public Shared Sub ClearLockout(email As String)
        email = If(email, "").Trim()
        lockedUntil.Remove(email)
        failedAttempts.Remove(email)
    End Sub
    
    ' ------------------------------------------------------
    ' DATA ACCESS (EF Core)
    ' ------------------------------------------------------
   Private Function FindUser(email As String) As AdminUser

    Using db As ApplicationDbContext = DbFactory.Create()

        Return db.Admins.
            AsNoTracking().
            FirstOrDefault(
                Function(a) a.Email.ToLower() = email.ToLower()
            )

    End Using

End Function

    Private Sub SaveLastLogin(user As AdminUser)
        Using db As ApplicationDbContext = DbFactory.Create()
            Dim row As AdminUser = db.Admins.Find(user.Id)
            If row IsNot Nothing Then
                row.LastLogin = DateTime.Now
                db.SaveChanges()
            End If
        End Using
        user.LastLogin = DateTime.Now
    End Sub

    ' ------------------------------------------------------
    ' ADMIN MANAGEMENT
    ' ------------------------------------------------------
      Public Shared Sub CreateAdmin(username As String, email As String, fullName As String, password As String)
       Using db As ApplicationDbContext = DbFactory.Create()
           db.Admins.Add(New AdminUser With {
               .Username = username.Trim(),
               .Email = email.Trim(),
               .FullName = fullName.Trim(),
               .PasswordHash = AdminUser.HashPassword(password)
           })
           db.SaveChanges()
       End Using
   End Sub

    Public Shared Sub ChangePassword(userId As Integer, newPassword As String)
        Using db As ApplicationDbContext = DbFactory.Create()
            Dim row As AdminUser = db.Admins.Find(userId)
            If row Is Nothing Then Return
            row.PasswordHash = AdminUser.HashPassword(newPassword)
            db.SaveChanges()
        End Using
    End Sub

End Class