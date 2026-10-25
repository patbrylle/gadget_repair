Imports System.Linq
Imports gadget_repair.Data
Imports gadget_repair.Models
Imports Microsoft.EntityFrameworkCore

Namespace Controllers

    Public Class RepairController

        ' One shared status list for every form
        Public Shared ReadOnly StatusList As String() = {
            "Pending",
            "In Progress",
            "Waiting for Parts",
            "Completed",
            "Delivered",
            "Cancelled"
        }

        Private ReadOnly db As ApplicationDbContext

        Public Sub New(database As ApplicationDbContext)
            db = database
        End Sub

        ' technicians may only move a repair to these two
        Public Shared ReadOnly TechnicianStatuses As String() = {
            "In Progress",
            "Completed"
        }

        ' only an administrator may set these
        Public Shared ReadOnly AdminStatuses As String() = {
            "Waiting for Parts",
            "Cancelled",
            "Delivered"
        }

        ' nobody can change the status after these
        Public Shared Function IsFinal(status As String) As Boolean
            Return status = "Delivered" OrElse status = "Cancelled"
        End Function

        ' the status buttons an admin gets for a repair in this status
               ' the status buttons an admin gets for a repair in this status
        Public Shared Function AdminActionsFor(status As String) As String()

            If IsFinal(status) Then Return New String() {}

            Dim list As New List(Of String)

            ' waiting for parts: while the repair is not finished
            If status = "Pending" OrElse status = "In Progress" Then
                list.Add("Waiting for Parts")
            End If

            ' delivered: only after the technician completed it
            If status = "Completed" Then
                list.Add("Delivered")
            End If

            ' cancel: any time before it is final
            list.Add("Cancelled")

            Return list.ToArray()

        End Function
        ' menu / button wording for a status (one place for every form)
        Public Shared Function ActionLabel(status As String) As String

            Select Case status

                Case "Cancelled"
                    Return "Cancel Repair"

                Case "Delivered"
                    Return "Mark Delivered"

                Case Else
                    Return status

            End Select

        End Function

        ' confirmation message shown before an admin changes the status
        Public Shared Function ConfirmText(
            repairId As Integer,
            newStatus As String) As String

            Dim ticket As String = "ticket #" & repairId.ToString()

            Dim lockNote As String =
                Environment.NewLine &
                Environment.NewLine &
                "The status cannot be changed afterwards."

            Select Case newStatus

                Case "Delivered"
                    Return "Mark " & ticket &
                           " as delivered to the customer?" & lockNote

                Case "Cancelled"
                    Return "Cancel " & ticket & "?" & lockNote

                Case Else
                    Return "Set " & ticket & " to " &
                           newStatus.ToLower() & "?"

            End Select

        End Function

        ' shared by this controller and the technician web API
        Public Shared Sub RequireTechnicianStatus(
            currentStatus As String,
            newStatus As String)

            If IsFinal(currentStatus) Then
                Throw New InvalidOperationException(
                    "This repair is " & If(currentStatus, "").ToLower() &
                    " and can no longer be updated.")
            End If

            ' an unchanged status is always fine
            If newStatus <> currentStatus AndAlso
               Not TechnicianStatuses.Contains(newStatus) Then

                Throw New ArgumentException(
                    "Technicians can only set a repair to In Progress or Completed.")

            End If

        End Sub

        ' status + completion date rules in one place
        Public Shared Sub ApplyStatus(repair As Repair, status As String)

            repair.Status = status

            If status = "Completed" OrElse status = "Delivered" Then

                If Not repair.CompletionDate.HasValue Then
                    repair.CompletionDate = DateTime.Now
                End If

            Else

                repair.CompletionDate = Nothing

            End If

        End Sub
        
        ' items used on repairs (shares this controller's database context)
        Public Function PartsController() As RepairPartController
            Return New RepairPartController(db)
        End Function
        ' ============================================================
        ' ACCESS GUARDS
        ' ============================================================

        Private Shared Sub RequireSignedIn()
            If AppSession.Role = UserRole.None Then
                Throw New UnauthorizedAccessException("Please sign in first.")
            End If
        End Sub

        Private Shared Sub RequireAdmin()
            If Not AppSession.IsAdmin Then
                Throw New UnauthorizedAccessException(
                    "Only an administrator can do this.")
            End If
        End Sub


        ' ============================================================
        ' GET ALL REPAIRS (technicians only see their own)
        ' ============================================================

        Public Function GetRepairs(
            Optional searchText As String = "",
            Optional statusFilter As String = "All Status"
        ) As List(Of Repair)

            RequireSignedIn()

            Dim query = db.Repairs.
                Include(Function(r) r.Technician).
                AsQueryable()

            If AppSession.IsTechnician Then

                Dim techId As Integer = AppSession.CurrentTechnicianId

                query = query.Where(
                    Function(r) r.TechnicianId.HasValue AndAlso
                                r.TechnicianId.Value = techId
                )

            End If

            If Not String.IsNullOrWhiteSpace(statusFilter) AndAlso
               statusFilter <> "All Status" Then

                query = query.Where(
                    Function(r) r.Status = statusFilter
                )

            End If

            If Not String.IsNullOrWhiteSpace(searchText) Then

                Dim search As String = searchText.Trim().ToLower()

                Dim customerIds As List(Of Integer) =
                    db.Customers.
                        Where(
                            Function(c) c.CustomerName.ToLower().
                                Contains(search)
                        ).
                        Select(Function(c) c.CustomerId).
                        ToList()

                query = query.Where(
                    Function(r) _
                        customerIds.Contains(r.CustomerId) OrElse
                        (r.DeviceType IsNot Nothing AndAlso
                         r.DeviceType.ToLower().Contains(search)) OrElse
                        (r.Brand IsNot Nothing AndAlso
                         r.Brand.ToLower().Contains(search)) OrElse
                        (r.Model IsNot Nothing AndAlso
                         r.Model.ToLower().Contains(search)) OrElse
                        (r.SerialNumber IsNot Nothing AndAlso
                         r.SerialNumber.ToLower().Contains(search)) OrElse
                        (r.Technician IsNot Nothing AndAlso
                         r.Technician.TechnicianName.ToLower().
                            Contains(search))
                )

            End If

            Return query.
                OrderByDescending(Function(r) r.RepairDate).
                ToList()

        End Function


        ' ============================================================
        ' GET CUSTOMERS / TECHNICIANS
        ' ============================================================

        Public Function GetCustomers() As List(Of Customer)

            RequireSignedIn()

            Return db.Customers.
                OrderBy(Function(c) c.CustomerName).
                ToList()

        End Function

        Public Function GetTechnicians() As List(Of Technician)

            RequireSignedIn()

            Return db.Technicians.
                Where(Function(t) t.Status <> "Inactive").
                OrderBy(Function(t) t.TechnicianName).
                ToList()

        End Function


        ' ============================================================
        ' GET ONE REPAIR
        ' ============================================================

        Public Function GetRepair(repairId As Integer) As Repair

            RequireSignedIn()

            Dim repair As Repair =
                db.Repairs.
                    Include(Function(r) r.Technician).
                    FirstOrDefault(Function(r) r.RepairId = repairId)

            ' a technician cannot open someone else's repair
            If repair IsNot Nothing AndAlso
               AppSession.IsTechnician AndAlso
               Not AppSession.CanUpdateProgress(repair) Then

                Return Nothing

            End If

            Return repair

        End Function


        ' ============================================================
        ' VALIDATION HELPERS
        ' ============================================================

        Private Shared Sub ValidateDeviceInfo(
            customerId As Integer,
            deviceType As String,
            problemDescription As String)

            If customerId <= 0 Then
                Throw New ArgumentException("Please select a customer.")
            End If

            If String.IsNullOrWhiteSpace(deviceType) Then
                Throw New ArgumentException("Device type is required.")
            End If

            If String.IsNullOrWhiteSpace(problemDescription) Then
                Throw New ArgumentException("Problem description is required.")
            End If

        End Sub

        Private Sub ValidateTechnician(technicianId As Integer?)

            If Not technicianId.HasValue Then Return

            Dim id As Integer = technicianId.Value

            Dim ok As Boolean =
                db.Technicians.Any(
                    Function(t) t.TechnicianId = id AndAlso
                                t.Status <> "Inactive")

            If Not ok Then
                Throw New ArgumentException(
                    "The selected technician is not available.")
            End If

        End Sub


        ' ============================================================
        ' CREATE REPAIR (admin only)
        ' status and cost are ignored: a new repair is always Pending
        ' with no cost; the technician sets those later.
        ' ============================================================

        Public Sub CreateRepair(
            customerId As Integer,
            technicianId As Integer?,
            deviceType As String,
            brand As String,
            model As String,
            serialNumber As String,
            problemDescription As String,
            repairDescription As String,
            status As String,
            repairDate As DateTime,
            Optional cost As Decimal = 0D
        )

            RequireAdmin()

            ValidateDeviceInfo(customerId, deviceType, problemDescription)
            ValidateTechnician(technicianId)

            Dim repair As New Repair With {
                .CustomerId = customerId,
                .TechnicianId = technicianId,
                .DeviceType = deviceType.Trim(),
                .Brand = If(brand, "").Trim(),
                .Model = If(model, "").Trim(),
                .SerialNumber = If(serialNumber, "").Trim(),
                .ProblemDescription = problemDescription.Trim(),
                .RepairDescription = "",
                .Status = "Pending",
                .RepairDate = repairDate,
                .Cost = 0D
            }

            db.Repairs.Add(repair)
            db.SaveChanges()

        End Sub


        ' ============================================================
        ' UPDATE REPAIR
        '   Admin      -> customer, technician, device, brand, model,
        '                 serial, problem, date
        '   Technician -> status, final cost, repair description
        '                 (only on repairs assigned to them)
        ' ============================================================

        Public Sub UpdateRepair(
            repairId As Integer,
            customerId As Integer,
            technicianId As Integer?,
            deviceType As String,
            brand As String,
            model As String,
            serialNumber As String,
            problemDescription As String,
            repairDescription As String,
            status As String,
            Optional cost As Decimal? = Nothing,
            Optional repairDate As DateTime? = Nothing
        )

            RequireSignedIn()

            Dim repair As Repair =
                db.Repairs.FirstOrDefault(
                    Function(r) r.RepairId = repairId)

            If repair Is Nothing Then
                Throw New InvalidOperationException(
                    "Repair record not found.")
            End If

            If AppSession.IsAdmin Then

                ValidateDeviceInfo(customerId, deviceType, problemDescription)

                If Not Nullable.Equals(technicianId, repair.TechnicianId) Then
                    ValidateTechnician(technicianId)
                End If

                repair.CustomerId = customerId
                repair.TechnicianId = technicianId
                repair.DeviceType = deviceType.Trim()
                repair.Brand = If(brand, "").Trim()
                repair.Model = If(model, "").Trim()
                repair.SerialNumber = If(serialNumber, "").Trim()
                repair.ProblemDescription = problemDescription.Trim()

                If repairDate.HasValue Then
                    repair.RepairDate = repairDate.Value
                End If

            Else

                ApplyTechnicianProgress(repair, status, cost, repairDescription)

            End If

            db.SaveChanges()

        End Sub


            Private Sub ApplyTechnicianProgress(
            repair As Repair,
            status As String,
            cost As Decimal?,
            repairDescription As String)

            If Not AppSession.CanUpdateProgress(repair) Then
                Throw New UnauthorizedAccessException(
                    "You can only update repairs assigned to you.")
            End If

            ' the form sends the current status back when it was not changed
            Dim target As String =
                If(String.IsNullOrWhiteSpace(status), repair.Status, status)

            RequireTechnicianStatus(repair.Status, target)

            Dim newCost As Decimal = repair.Cost

            If cost.HasValue Then

                If cost.Value < 0D Then
                    Throw New ArgumentException("Cost cannot be negative.")
                End If

                newCost = cost.Value

            End If

            ' cannot drop the cost below what the customer already paid
            Dim alreadyPaid As Decimal =
                db.Payments.
                    Where(Function(p) p.RepairId = repair.RepairId).
                    Select(Function(p) p.AmountPaid).
                    ToList().
                    Sum()

            If newCost < alreadyPaid Then
                Throw New ArgumentException(
                    "The cost cannot be lower than the amount already paid.")
            End If

            If target = "Completed" AndAlso newCost <= 0D Then
                Throw New ArgumentException(
                    "Set the final cost before marking this repair as completed.")
            End If

            ApplyStatus(repair, target)

            repair.Cost = newCost
            repair.RepairDescription = If(repairDescription, "").Trim()

        End Sub
        ' ============================================================
        ' SET STATUS (admin only)
        '   Waiting for Parts, Cancelled and Delivered
        ' ============================================================

        Public Sub SetStatusAsAdmin(
            repairId As Integer,
            newStatus As String)

            RequireAdmin()

            If Not AdminStatuses.Contains(newStatus) Then
                Throw New ArgumentException(
                    "Admin can only set Waiting for Parts, Cancelled or Delivered.")
            End If

            Dim repair As Repair =
                db.Repairs.FirstOrDefault(
                    Function(r) r.RepairId = repairId)

            If repair Is Nothing Then
                Throw New InvalidOperationException(
                    "Repair record not found.")
            End If

            If Not AdminActionsFor(repair.Status).Contains(newStatus) Then
                Throw New InvalidOperationException(
                    "This repair is " & If(repair.Status, "").ToLower() &
                    ", so it cannot be set to " & newStatus.ToLower() & ".")
            End If

            If newStatus = "Delivered" AndAlso repair.Cost <= 0D Then
                Throw New ArgumentException(
                    "Set the final cost before marking this repair as delivered.")
            End If

            ApplyStatus(repair, newStatus)

            db.SaveChanges()

        End Sub
        
        ' ============================================================
        ' DELETE REPAIR (admin only)
        ' ============================================================
Public Function DeleteRepair(
    repairId As Integer
) As Boolean

    RequireAdmin()

    Dim repair = db.Repairs.
        FirstOrDefault(Function(r) r.RepairId = repairId)

    If repair Is Nothing Then
        Return False
    End If

    Dim parts As List(Of RepairPart) =
        db.RepairParts.Where(Function(p) p.RepairId = repairId).ToList()

    ' finished repairs really used the parts; otherwise give the stock back
    If Not (repair.Status = "Completed" OrElse repair.Status = "Delivered") Then

        For Each line As RepairPart In parts

            Dim invId As Integer = line.InventoryId

            Dim item As Inventory =
                db.Inventories.FirstOrDefault(Function(i) i.InventoryId = invId)

            If item IsNot Nothing Then
                item.Quantity += line.Quantity
                item.UpdatedAt = DateTime.Now
            End If

        Next

    End If

    db.RepairParts.RemoveRange(parts)
    db.Repairs.Remove(repair)
    db.SaveChanges()

    Return True

End Function

    End Class

End Namespace