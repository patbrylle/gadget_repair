Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text.RegularExpressions
Imports gadget_repair.Data
Imports gadget_repair.Models
Imports Microsoft.EntityFrameworkCore

Namespace Controllers

    Public Class TechnicianController

        Public Shared ReadOnly StatusList As String() = {
            "Available", "Busy", "Inactive"
        }

        Public Shared ReadOnly SpecializationList As String() = {
            "Smartphone Repair",
            "Laptop Repair",
            "Desktop / PC Repair",
            "Tablet Repair",
            "Printer Repair",
            "Monitor / Display Repair",
            "Console / Gaming Repair",
            "Networking",
            "Data Recovery",
            "General Repair"
        }

        Private ReadOnly db As ApplicationDbContext

        Public Sub New(context As ApplicationDbContext)
            db = context
        End Sub


        ' ============================================================
        ' VALIDATION HELPERS (also used by the dialog)
        ' ============================================================

        Public Shared Function IsValidName(name As String) As Boolean

            If String.IsNullOrWhiteSpace(name) Then Return False

            Return Regex.IsMatch(name.Trim(), "^\p{L}[\p{L} .'\-]{1,99}$")

        End Function

        ' Accepts 09171234567, 0917 123 4567, +63 917 123 4567.
        ' Returns the number as 09XXXXXXXXX, or "" when invalid.
        Public Shared Function NormalizeContact(raw As String) As String

            If String.IsNullOrWhiteSpace(raw) Then Return ""

            Dim digits As String =
                Regex.Replace(raw.Trim(), "[\s\-\(\)]", "")

            If digits.StartsWith("+63") Then

                digits = "0" & digits.Substring(3)

            ElseIf digits.StartsWith("63") AndAlso digits.Length = 12 Then

                digits = "0" & digits.Substring(2)

            End If

            If Regex.IsMatch(digits, "^09\d{9}$") Then Return digits

            Return ""

        End Function

        Public Shared Function IsValidEmail(email As String) As Boolean

            If String.IsNullOrWhiteSpace(email) Then Return False

            Dim value As String = email.Trim()

            If value.Length > 255 Then Return False

            Return Regex.IsMatch(
                value,
                "^[A-Za-z0-9._%+\-]+@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,}$")

        End Function

        ' Returns "" when the password is acceptable, otherwise the reason.
        Public Shared Function PasswordProblem(password As String) As String

            If String.IsNullOrEmpty(password) Then
                Return "Password is required."
            End If

            If password.Length < 8 Then
                Return "Use at least 8 characters."
            End If

            If password.Length > 128 Then
                Return "Use 128 characters or fewer."
            End If

            If Not Regex.IsMatch(password, "[A-Za-z]") OrElse
               Not Regex.IsMatch(password, "\d") Then
                Return "Include at least one letter and one number."
            End If

            Return ""

        End Function

        Private Shared Function ValidateProfile(
            name As String,
            specialization As String,
            contact As String,
            email As String,
            status As String) As String

            If Not IsValidName(name) Then
                Throw New ArgumentException(
                    "Enter a valid technician name (letters, spaces, . ' - only).")
            End If

            If String.IsNullOrWhiteSpace(specialization) OrElse
               specialization.Trim().Length > 100 Then
                Throw New ArgumentException("Please choose a specialization.")
            End If

            Dim cleanContact As String = NormalizeContact(contact)

            If cleanContact = "" Then
                Throw New ArgumentException(
                    "Enter a valid mobile number, for example 0917 123 4567.")
            End If

            If Not IsValidEmail(email) Then
                Throw New ArgumentException("Please enter a valid technician email.")
            End If

            If String.IsNullOrWhiteSpace(status) OrElse
               Not StatusList.Contains(status.Trim()) Then
                Throw New ArgumentException("Please choose a valid status.")
            End If

            Return cleanContact

        End Function


        ' ============================================================
        ' GET ALL TECHNICIANS
        ' ============================================================

        Public Function GetTechnicians(
            Optional search As String = Nothing,
            Optional status As String = Nothing
        ) As List(Of Technician)

            Dim query = db.Technicians _
                .Include(Function(t) t.Repairs) _
                .AsQueryable()

            If Not String.IsNullOrWhiteSpace(search) Then

                search = search.Trim().ToLower()

                query = query.Where(
                    Function(t) _
                        t.TechnicianName.ToLower().Contains(search) OrElse _
                        t.Specialization.ToLower().Contains(search) OrElse _
                        t.ContactNumber.ToLower().Contains(search) OrElse _
                        t.Email.ToLower().Contains(search)
                )

            End If

            If Not String.IsNullOrWhiteSpace(status) AndAlso
               status <> "All Status" Then

                query = query.Where(
                    Function(t) t.Status = status
                )

            End If

            Return query _
                .OrderBy(Function(t) t.TechnicianName) _
                .ToList()

        End Function


        ' ============================================================
        ' GET TECHNICIAN BY ID
        ' ============================================================

        Public Function GetTechnician(id As Integer) As Technician

            Return db.Technicians _
                .Include(Function(t) t.Repairs) _
                .FirstOrDefault(
                    Function(t) t.TechnicianId = id
                )

        End Function


        ' ============================================================
        ' GET AVAILABLE TECHNICIANS
        ' ============================================================

        Public Function GetAvailableTechnicians() As List(Of Technician)

            Return db.Technicians _
                .Where(
                    Function(t) _
                        t.Status = "Available" OrElse _
                        t.Status = "Busy"
                ) _
                .OrderBy(Function(t) t.TechnicianName) _
                .ToList()

        End Function


        ' ============================================================
        ' CREATE TECHNICIAN
        ' ============================================================

        Public Function CreateTechnician(
            technicianName As String,
            specialization As String,
            contactNumber As String,
            email As String,
            status As String,
            temporaryPassword As String
        ) As Technician

            Dim cleanContact As String =
                ValidateProfile(
                    technicianName, specialization,
                    contactNumber, email, status)

            Dim passwordIssue As String = PasswordProblem(temporaryPassword)

            If passwordIssue <> "" Then
                Throw New ArgumentException(
                    "The temporary password is not valid. " & passwordIssue)
            End If

            Dim normalizedEmail As String = email.Trim()

            If db.Technicians.Any(
                Function(t) t.Email.ToLower() = normalizedEmail.ToLower()) Then
                Throw New ArgumentException(
                    "A technician with this email already exists.")
            End If

            Dim technician As New Technician With {
                .TechnicianName = technicianName.Trim(),
                .Specialization = specialization.Trim(),
                .ContactNumber = cleanContact,
                .Email = normalizedEmail,
                .Status = status.Trim(),
                .CreatedAt = DateTime.Now
            }

            Dim account As New TechnicianAccount With {
                .Technician = technician,
                .PasswordHash = TechnicianAccount.HashPassword(temporaryPassword),
                .IsActive = (technician.Status <> "Inactive"),
                .CreatedAt = DateTime.Now
            }

            db.Technicians.Add(technician)
            db.TechnicianAccounts.Add(account)
            db.SaveChanges()

            Return technician

        End Function


        ' ============================================================
        ' UPDATE TECHNICIAN
        ' ============================================================

        Public Function UpdateTechnician(
            id As Integer,
            technicianName As String,
            specialization As String,
            contactNumber As String,
            email As String,
            status As String
        ) As Boolean

            Dim technician =
                db.Technicians.FirstOrDefault(
                    Function(t) t.TechnicianId = id)

            If technician Is Nothing Then Return False

            Dim cleanContact As String =
                ValidateProfile(
                    technicianName, specialization,
                    contactNumber, email, status)

            Dim normalizedEmail As String = email.Trim()

            If db.Technicians.Any(
                Function(t) t.TechnicianId <> id AndAlso
                            t.Email.ToLower() = normalizedEmail.ToLower()) Then
                Throw New ArgumentException(
                    "A technician with this email already exists.")
            End If

            technician.TechnicianName = technicianName.Trim()
            technician.Specialization = specialization.Trim()
            technician.ContactNumber = cleanContact
            technician.Email = normalizedEmail
            technician.Status = status.Trim()      ' FIX: status was never saved

            Dim account = db.TechnicianAccounts.FirstOrDefault(
                Function(a) a.TechnicianId = id)

            If account IsNot Nothing Then
                account.IsActive = (technician.Status <> "Inactive")
            End If

            db.SaveChanges()

            Return True

        End Function


        ' ============================================================
        ' DELETE TECHNICIAN
        ' ============================================================

        Public Function DeleteTechnician(id As Integer) As Boolean

            Dim technician =
                db.Technicians _
                    .Include(Function(t) t.Repairs) _
                    .FirstOrDefault(
                        Function(t) t.TechnicianId = id)

            If technician Is Nothing Then Return False

            If technician.Repairs IsNot Nothing AndAlso
               technician.Repairs.Any() Then

                Throw New InvalidOperationException(
                    "This technician cannot be deleted because " &
                    "they have assigned repairs.")

            End If

            db.Technicians.Remove(technician)
            db.SaveChanges()

            Return True

        End Function


        ' ============================================================
        ' STATISTICS
        ' ============================================================

        Public Function GetTotalTechnicians() As Integer
            Return db.Technicians.Count()
        End Function

        Public Function GetAvailableCount() As Integer
            Return db.Technicians.Count(Function(t) t.Status = "Available")
        End Function

        Public Function GetBusyCount() As Integer
            Return db.Technicians.Count(Function(t) t.Status = "Busy")
        End Function

        Public Function GetInactiveCount() As Integer
            Return db.Technicians.Count(Function(t) t.Status = "Inactive")
        End Function


        ' ============================================================
        ' ASSIGNED REPAIRS
        ' FIX: Repair has no "Customer" navigation, so Include("Customer")
        ' threw at runtime. Customers are looked up separately instead.
        ' ============================================================

        Public Function GetAssignedRepairs(
            technicianId As Integer
        ) As List(Of Repair)

            Return db.Repairs _
                .AsNoTracking() _
                .Where(Function(r) r.TechnicianId.HasValue AndAlso
                                   r.TechnicianId.Value = technicianId) _
                .OrderByDescending(Function(r) r.RepairDate) _
                .ToList()

        End Function

        Public Function GetCustomerLookup() As Dictionary(Of Integer, Customer)

            Return db.Customers _
                .AsNoTracking() _
                .ToList() _
                .ToDictionary(Function(c) c.CustomerId)

        End Function

    End Class

End Namespace