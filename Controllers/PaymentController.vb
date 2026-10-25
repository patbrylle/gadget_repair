Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Controllers

    Public Class PaymentController

        Private ReadOnly db As ApplicationDbContext

        Public Sub New(context As ApplicationDbContext)
            db = context
        End Sub


        ' ============================================================
        ' SHARED RULES (forms reuse these)
        ' ============================================================

        ' repairs in these statuses appear on the Payments page
        Public Shared Function IsPayable(status As String) As Boolean
            Return status = "Completed" OrElse status = "Delivered"
        End Function

        Public Shared Function ComputeStatus(
            cost As Decimal,
            paid As Decimal) As String

            If paid <= 0D Then
                Return "Unpaid"
            ElseIf paid < cost Then
                Return "Partially Paid"
            Else
                Return "Paid"
            End If

        End Function

        Public Shared Function TechnicianName(repair As Repair) As String

            If repair Is Nothing OrElse
               repair.Technician Is Nothing OrElse
               String.IsNullOrWhiteSpace(repair.Technician.TechnicianName) Then

                Return "Unassigned"

            End If

            Return repair.Technician.TechnicianName

        End Function

        Private Shared Sub RequireSignedIn()
            If AppSession.Role = UserRole.None Then
                Throw New UnauthorizedAccessException("Please sign in first.")
            End If
        End Sub

        Private Shared Sub RequireAdmin()
            If Not AppSession.IsAdmin Then
                Throw New UnauthorizedAccessException(
                    "Only an administrator can manage payments.")
            End If
        End Sub

        ' completed / delivered repairs (technicians only see their own)
        Private Function PayableRepairs() As IQueryable(Of Repair)

            RequireSignedIn()

            Dim query As IQueryable(Of Repair) =
                db.Repairs.Where(
                    Function(r) r.Status = "Completed" OrElse
                                r.Status = "Delivered")

            If AppSession.IsTechnician Then

                Dim techId As Integer = AppSession.CurrentTechnicianId

                query = query.Where(
                    Function(r) r.TechnicianId.HasValue AndAlso
                                r.TechnicianId.Value = techId)

            End If

            Return query

        End Function


        ' ============================================================
        ' ITEM USED IN REPAIR
        ' ============================================================

        Public Function GetItemUsed(repair As Repair) As String

            If repair Is Nothing Then
                Return "-"
            End If

            Dim brand As String = If(repair.Brand, "").Trim()
            Dim model As String = If(repair.Model, "").Trim()

            Dim item As String = (brand & " " & model).Trim()

            If item = "" Then
                item = If(repair.DeviceType, "").Trim()
            End If

            If item = "" Then
                Return "-"
            End If

            Return item

        End Function


        ' ============================================================
        ' LIST: every payable repair, with its payment (or an empty one)
        ' ============================================================

        Public Function GetPayments(
            Optional search As String = "",
            Optional status As String = "All Status"
        ) As List(Of Payment)

            Dim repairs As List(Of Repair) =
                PayableRepairs().
                    Include(Function(r) r.Technician).
                    OrderByDescending(Function(r) r.CompletionDate).
                    ToList()

            Dim repairIds As List(Of Integer) =
                repairs.Select(Function(r) r.RepairId).ToList()

            Dim existing As Dictionary(Of Integer, Payment) =
                db.Payments.
                    Where(Function(p) repairIds.Contains(p.RepairId)).
                    ToList().
                    GroupBy(Function(p) p.RepairId).
                    ToDictionary(Function(g) g.Key, Function(g) g.First())

            Dim customers As Dictionary(Of Integer, String) = GetCustomerLookup()

            Dim searchText As String = ""

            If Not String.IsNullOrWhiteSpace(search) Then
                searchText = search.Trim().ToLower()
            End If

            Dim result As New List(Of Payment)

            For Each repair As Repair In repairs

                If searchText <> "" Then

                    Dim customerName As String = ""
                    customers.TryGetValue(repair.CustomerId, customerName)

                    Dim haystack As String =
                        (If(customerName, "") & "|" &
                         If(repair.DeviceType, "") & "|" &
                         If(repair.Brand, "") & "|" &
                         If(repair.Model, "") & "|" &
                         If(repair.SerialNumber, "") & "|" &
                         TechnicianName(repair)).ToLower()

                    If Not haystack.Contains(searchText) Then
                        Continue For
                    End If

                End If

                Dim payment As Payment = Nothing

                If Not existing.TryGetValue(repair.RepairId, payment) Then

                    payment = New Payment With {
                        .PaymentId = 0,
                        .RepairId = repair.RepairId,
                        .AmountPaid = 0D,
                        .PaymentDate = If(repair.CompletionDate.HasValue,
                                          repair.CompletionDate.Value,
                                          DateTime.Now),
                        .Notes = String.Empty
                    }

                End If

                payment.Repair = repair

                If status <> "All Status" AndAlso
                   ComputeStatus(repair.Cost, payment.AmountPaid) <> status Then

                    Continue For

                End If

                result.Add(payment)

            Next

            Return result

        End Function


        ' ============================================================
        ' SINGLE RECORDS
        ' ============================================================

        Public Function GetPayment(paymentId As Integer) As Payment

            Return db.Payments.
                Include(Function(p) p.Repair).
                FirstOrDefault(Function(p) p.PaymentId = paymentId)

        End Function

        Public Function GetPaymentByRepair(repairId As Integer) As Payment

            Return db.Payments.
                Include(Function(p) p.Repair).
                FirstOrDefault(Function(p) p.RepairId = repairId)

        End Function

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

        Public Function GetRepairs() As List(Of Repair)

            Return PayableRepairs().
                Where(Function(r) r.Cost > 0D).
                OrderByDescending(Function(r) r.CompletionDate).
                ToList()

        End Function


        ' ============================================================
        ' CREATE / UPDATE / DELETE (admin only)
        ' amountPaid is the TOTAL paid so far, for create and update
        ' ============================================================

        Public Function CreatePayment(
            repairId As Integer,
            amountPaid As Decimal,
            paymentDate As DateTime,
            notes As String
        ) As Payment

            RequireAdmin()

            Dim repair As Repair =
                db.Repairs.FirstOrDefault(
                    Function(r) r.RepairId = repairId)

            If repair Is Nothing Then
                Throw New InvalidOperationException("Repair record not found.")
            End If

            If Not IsPayable(repair.Status) Then
                Throw New InvalidOperationException(
                    "Only completed or delivered repairs can be paid.")
            End If

            If repair.Cost <= 0D Then
                Throw New InvalidOperationException(
                    "The technician has not set the final cost yet.")
            End If

            If amountPaid <= 0D Then
                Throw New ArgumentException("Enter an amount above 0.")
            End If

            If amountPaid > repair.Cost Then
                Throw New ArgumentException(
                    "Payment cannot be greater than the repair cost.")
            End If

            Dim payment As Payment =
                db.Payments.FirstOrDefault(
                    Function(p) p.RepairId = repairId)

            If payment Is Nothing Then

                payment = New Payment With {
                    .RepairId = repairId
                }

                db.Payments.Add(payment)

            End If

            payment.AmountPaid = amountPaid
            payment.PaymentDate = paymentDate
            payment.Notes = If(notes, String.Empty).Trim()

            db.SaveChanges()

            Return payment

        End Function

        Public Function UpdatePayment(
            paymentId As Integer,
            amountPaid As Decimal,
            paymentDate As DateTime,
            notes As String
        ) As Boolean

            RequireAdmin()

            Dim payment As Payment =
                db.Payments.
                    Include(Function(p) p.Repair).
                    FirstOrDefault(Function(p) p.PaymentId = paymentId)

            If payment Is Nothing Then
                Return False
            End If

            If amountPaid < 0D Then
                amountPaid = 0D
            End If

            If payment.Repair IsNot Nothing AndAlso
               amountPaid > payment.Repair.Cost Then

                Throw New ArgumentException(
                    "Payment cannot be greater than the repair cost.")

            End If

            payment.AmountPaid = amountPaid
            payment.PaymentDate = paymentDate
            payment.Notes = If(notes, String.Empty).Trim()

            db.SaveChanges()

            Return True

        End Function

        Public Function DeletePayment(paymentId As Integer) As Boolean

            RequireAdmin()

            Dim payment As Payment =
                db.Payments.FirstOrDefault(
                    Function(p) p.PaymentId = paymentId)

            If payment Is Nothing Then
                Return False
            End If

            db.Payments.Remove(payment)
            db.SaveChanges()

            Return True

        End Function


        ' ============================================================
        ' STATISTICS (all payable repairs)
        ' ============================================================

        Private Function GetBalances() As List(Of Tuple(Of Decimal, Decimal))

            Dim repairs = PayableRepairs().
                Select(Function(r) New With {r.RepairId, r.Cost}).
                ToList()

            Dim paidByRepair As Dictionary(Of Integer, Decimal) =
                db.Payments.
                    ToList().
                    GroupBy(Function(p) p.RepairId).
                    ToDictionary(
                        Function(g) g.Key,
                        Function(g) g.Sum(Function(p) p.AmountPaid))

            Dim list As New List(Of Tuple(Of Decimal, Decimal))

            For Each r In repairs

                Dim paid As Decimal = 0D

                If paidByRepair.ContainsKey(r.RepairId) Then
                    paid = paidByRepair(r.RepairId)
                End If

                list.Add(Tuple.Create(r.Cost, paid))

            Next

            Return list

        End Function

        Private Function CountByStatus(targetStatus As String) As Integer

            Dim total As Integer = 0

            For Each t As Tuple(Of Decimal, Decimal) In GetBalances()

                If ComputeStatus(t.Item1, t.Item2) = targetStatus Then
                    total += 1
                End If

            Next

            Return total

        End Function

        Public Function GetTotalPayments() As Integer
            Return GetBalances().Count
        End Function

        Public Function GetPaidCount() As Integer
            Return CountByStatus("Paid")
        End Function

        Public Function GetPartiallyPaidCount() As Integer
            Return CountByStatus("Partially Paid")
        End Function

        Public Function GetUnpaidCount() As Integer
            Return CountByStatus("Unpaid")
        End Function

        Public Function GetTotalCollected() As Decimal

            Return db.Payments.
                Select(Function(p) p.AmountPaid).
                DefaultIfEmpty(0D).
                Sum()

        End Function

        Public Function GetTotalOutstanding() As Decimal

            Dim total As Decimal = 0D

            For Each t As Tuple(Of Decimal, Decimal) In GetBalances()

                Dim balance As Decimal = t.Item1 - t.Item2

                If balance > 0D Then
                    total += balance
                End If

            Next

            Return total

        End Function


        ' ============================================================
        ' CUSTOMERS
        ' ============================================================

        Public Function GetCustomerName(customerId As Integer) As String

            Dim customer As Customer =
                db.Customers.FirstOrDefault(
                    Function(c) c.CustomerId = customerId)

            If customer Is Nothing Then
                Return "Unknown Customer"
            End If

            Return customer.CustomerName

        End Function

        ' one query for the whole list instead of one per row
        Public Function GetCustomerLookup() As Dictionary(Of Integer, String)

            Return db.Customers.
                AsNoTracking().
                ToList().
                ToDictionary(
                    Function(c) c.CustomerId,
                    Function(c) If(c.CustomerName, ""))

        End Function

    End Class

End Namespace