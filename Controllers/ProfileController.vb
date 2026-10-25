Imports System
Imports System.Linq
Imports System.Text.RegularExpressions
Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Controllers

    Public Class ProfileInfo
        Public Property IsAdmin As Boolean
        Public Property FullName As String = ""
        Public Property Username As String = ""
        Public Property Specialization As String = ""
        Public Property ContactNumber As String = ""
        Public Property Email As String = ""
        Public Property RoleText As String = ""
    End Class

    Public Class ProfileController

        Private ReadOnly db As ApplicationDbContext

        Public Sub New(context As ApplicationDbContext)
            db = context
        End Sub

        Private Shared Sub RequireSignedIn()
            If AppSession.Role = UserRole.None Then
                Throw New UnauthorizedAccessException("Please sign in first.")
            End If
        End Sub

        ' ---- reflection keeps this independent of the exact AdminUser shape ----
        Private Shared Function ReadText(target As Object, name As String) As String
            Dim p As System.Reflection.PropertyInfo = target.GetType().GetProperty(name)
            If p Is Nothing OrElse Not p.CanRead Then Return ""
            Return Convert.ToString(p.GetValue(target))
        End Function

        Private Shared Function TrySet(target As Object, name As String, value As String) As Boolean
            Dim p As System.Reflection.PropertyInfo = target.GetType().GetProperty(name)
            If p Is Nothing OrElse Not p.CanWrite Then Return False
            p.SetValue(target, value)
            Return True
        End Function

        Private Function FindAdmin() As AdminUser

            Dim current = AuthController.CurrentUser

            If current Is Nothing Then Return Nothing

            Dim username As String = current.Username

            Return db.Admins.FirstOrDefault(Function(a) a.Username = username)

        End Function

        ' ============================================================
        ' READ
        ' ============================================================

        Public Function GetProfile() As ProfileInfo

            RequireSignedIn()

            If AppSession.IsTechnician Then

                Dim id As Integer = AppSession.CurrentTechnicianId

                Dim t As Technician =
                    db.Technicians.AsNoTracking().
                        FirstOrDefault(Function(x) x.TechnicianId = id)

                If t Is Nothing Then
                    Throw New InvalidOperationException("Technician record not found.")
                End If

                Return New ProfileInfo With {
                    .IsAdmin = False,
                    .FullName = t.TechnicianName,
                    .Username = t.Email,
                    .Specialization = t.Specialization,
                    .ContactNumber = t.ContactNumber,
                    .Email = t.Email,
                    .RoleText = "Technician"}

            End If

            Dim admin As AdminUser = FindAdmin()

            If admin Is Nothing Then
                Throw New InvalidOperationException("Administrator account not found.")
            End If

            Dim name As String = ReadText(admin, "FullName")

            If String.IsNullOrWhiteSpace(name) Then name = AppSession.DisplayName

            Return New ProfileInfo With {
                .IsAdmin = True,
                .FullName = name,
                .Username = ReadText(admin, "Username"),
                .RoleText = "Administrator"}

        End Function

        ' ============================================================
        ' UPDATE PROFILE
        ' ============================================================

        Public Sub UpdateProfile(
            fullName As String,
            specialization As String,
            contact As String,
            email As String)

            RequireSignedIn()

            If AppSession.IsTechnician Then
                UpdateTechnician(fullName, specialization, contact, email)
            Else
                UpdateAdmin(fullName)
            End If

        End Sub

        Private Sub UpdateAdmin(fullName As String)

            Dim name As String = If(fullName, "").Trim()

            If name = "" Then Throw New ArgumentException("Please enter your name.")
            If name.Length > 100 Then Throw New ArgumentException("Name is too long (100 characters max).")

            Dim admin As AdminUser = FindAdmin()

            If admin Is Nothing Then
                Throw New InvalidOperationException("Administrator account not found.")
            End If

            If Not TrySet(admin, "FullName", name) Then
                Throw New InvalidOperationException(
                    "The administrator name cannot be changed because AdminUser.FullName is read-only.")
            End If

            db.SaveChanges()

            AppSession.SetDisplayName(name)

        End Sub

        Private Sub UpdateTechnician(
            fullName As String,
            specialization As String,
            contact As String,
            email As String)

            If Not TechnicianController.IsValidName(fullName) Then
                Throw New ArgumentException("Enter a valid name (letters, spaces, . ' - only).")
            End If

            If String.IsNullOrWhiteSpace(specialization) Then
                Throw New ArgumentException("Please choose a specialization.")
            End If

            Dim cleanContact As String = TechnicianController.NormalizeContact(contact)

            If cleanContact = "" Then
                Throw New ArgumentException("Enter a valid mobile number, for example 0917 123 4567.")
            End If

            If Not TechnicianController.IsValidEmail(email) Then
                Throw New ArgumentException("Please enter a valid email address.")
            End If

            Dim id As Integer = AppSession.CurrentTechnicianId
            Dim normalized As String = email.Trim()

            If db.Technicians.Any(
                Function(t) t.TechnicianId <> id AndAlso
                            t.Email.ToLower() = normalized.ToLower()) Then
                Throw New ArgumentException("Another technician already uses this email.")
            End If

            Dim tech As Technician =
                db.Technicians.FirstOrDefault(Function(t) t.TechnicianId = id)

            If tech Is Nothing Then
                Throw New InvalidOperationException("Technician record not found.")
            End If

            tech.TechnicianName = fullName.Trim()
            tech.Specialization = specialization.Trim()
            tech.ContactNumber = cleanContact
            tech.Email = normalized

            db.SaveChanges()

            Dim sessionTech As Technician = TechnicianAuthController.CurrentTechnician

            If sessionTech IsNot Nothing Then
                sessionTech.TechnicianName = tech.TechnicianName
                sessionTech.Specialization = tech.Specialization
                sessionTech.ContactNumber = tech.ContactNumber
                sessionTech.Email = tech.Email
            End If

            AppSession.SetDisplayName(tech.TechnicianName)

        End Sub

        ' ============================================================
        ' CHANGE PASSWORD
        ' ============================================================

        Public Sub ChangePassword(
            currentPassword As String,
            newPassword As String,
            confirmPassword As String)

            RequireSignedIn()

            If String.IsNullOrEmpty(currentPassword) Then
                Throw New ArgumentException("Enter your current password.")
            End If

            Dim problem As String = TechnicianController.PasswordProblem(newPassword)

            If problem <> "" Then
                Throw New ArgumentException("New password: " & problem)
            End If

            If newPassword <> confirmPassword Then
                Throw New ArgumentException("The new password and its confirmation do not match.")
            End If

            If newPassword = currentPassword Then
                Throw New ArgumentException("Choose a password different from the current one.")
            End If

            If AppSession.IsTechnician Then
                ChangeTechnicianPassword(currentPassword, newPassword)
            Else
                ChangeAdminPassword(currentPassword, newPassword)
            End If

        End Sub

        Private Sub ChangeTechnicianPassword(currentPassword As String, newPassword As String)

            Dim id As Integer = AppSession.CurrentTechnicianId

            Dim account As TechnicianAccount =
                db.TechnicianAccounts.FirstOrDefault(Function(a) a.TechnicianId = id)

            If account Is Nothing Then
                Throw New InvalidOperationException("Your login account was not found.")
            End If

            If Not account.VerifyPassword(currentPassword) Then
                Throw New ArgumentException("The current password is incorrect.")
            End If

            account.PasswordHash = TechnicianAccount.HashPassword(newPassword)

            db.SaveChanges()

        End Sub

        Private Sub ChangeAdminPassword(currentPassword As String, newPassword As String)

            Dim admin As AdminUser = FindAdmin()

            If admin Is Nothing Then
                Throw New InvalidOperationException("Administrator account not found.")
            End If

            Dim stored As String = ReadText(admin, "PasswordHash")

            ' safety: only touch hashes in the same PBKDF2 format the technician accounts use,
            ' so the admin can never be locked out by a format mismatch
            If Not Regex.IsMatch(stored, "^\d+\.[A-Za-z0-9+/=]+\.[A-Za-z0-9+/=]+$") Then
                Throw New InvalidOperationException(
                    "The administrator password uses a storage format this screen does not support yet.")
            End If

            Dim checker As New TechnicianAccount With {.PasswordHash = stored}

            If Not checker.VerifyPassword(currentPassword) Then
                Throw New ArgumentException("The current password is incorrect.")
            End If

            If Not TrySet(admin, "PasswordHash", TechnicianAccount.HashPassword(newPassword)) Then
                Throw New InvalidOperationException("Could not save the new password.")
            End If

            db.SaveChanges()

        End Sub

    End Class

End Namespace