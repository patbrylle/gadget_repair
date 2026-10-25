Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Controllers

    Public Class DashboardStats

        ' ============================================================
        ' MAIN STATISTICS
        ' ============================================================

        Public Property CustomerCount As Integer
        Public Property TotalRepairs As Integer
        Public Property ActiveRepairs As Integer
        Public Property CompletedCount As Integer

        Public Property LowStockCount As Integer
        Public Property OutOfStockCount As Integer

        ' ============================================================
        ' PAYMENT STATISTICS
        ' ============================================================

        Public Property TotalCollected As Decimal
        Public Property TotalOutstanding As Decimal
        Public Property PaidPayments As Integer
        Public Property PartialPayments As Integer
        Public Property UnpaidPayments As Integer

        ' ============================================================
        ' REPAIR STATUS CHART
        ' ============================================================

        Public Property PendingRepairs As Integer
        Public Property InProgressRepairs As Integer
        Public Property WaitingForPartsRepairs As Integer
        Public Property CancelledRepairs As Integer

        ' ============================================================
        ' MONTHLY REPAIR CHART
        ' ============================================================

        Public Property MonthlyRepairs As List(Of MonthlyRepairStat) =
            New List(Of MonthlyRepairStat)

    End Class


    Public Class MonthlyRepairStat

        Public Property MonthLabel As String = String.Empty

        Public Property Year As Integer

        Public Property Month As Integer

        Public Property RepairCount As Integer

    End Class


    Public Class DashboardController

        Private ReadOnly db As ApplicationDbContext


        ' ============================================================
        ' CONSTRUCTOR
        ' ============================================================

        Public Sub New(database As ApplicationDbContext)

            db = database

        End Sub


        ' ============================================================
        ' GET ALL DASHBOARD STATISTICS
        ' ============================================================

        Public Function GetStats() As DashboardStats

            Dim stats As New DashboardStats()


            ' ========================================================
            ' CUSTOMERS
            ' ========================================================

            stats.CustomerCount =
                db.Customers.Count()


            ' ========================================================
            ' REPAIRS
            ' ========================================================

            stats.TotalRepairs =
                db.Repairs.Count()


            stats.PendingRepairs =
                db.Repairs.Count(
                    Function(r) _
                        r.Status = "Pending"
                )


            stats.InProgressRepairs =
                db.Repairs.Count(
                    Function(r) _
                        r.Status = "In Progress"
                )


            stats.WaitingForPartsRepairs =
                db.Repairs.Count(
                    Function(r) _
                        r.Status = "Waiting for Parts"
                )


            stats.CancelledRepairs =
                db.Repairs.Count(
                    Function(r) _
                        r.Status = "Cancelled"
                )


            stats.CompletedCount =
                db.Repairs.Count(
                    Function(r) _
                        r.Status = "Completed" OrElse
                        r.Status = "Delivered"
                )


            stats.ActiveRepairs =
                db.Repairs.Count(
                    Function(r) _
                        r.Status = "In Progress" OrElse
                        r.Status = "Waiting for Parts"
                )


            ' ========================================================
            ' INVENTORY
            ' ========================================================

            stats.LowStockCount =
                db.Inventories.Count(
                    Function(i) _
                        i.Quantity > 0 AndAlso
                        i.Quantity <= i.MinimumStock
                )


            stats.OutOfStockCount =
                db.Inventories.Count(
                    Function(i) _
                        i.Quantity <= 0
                )


            ' ========================================================
            ' PAYMENTS
            '
            ' Load payment records first.
            ' The calculations are then done in VB instead of
            ' asking EF/MySQL to translate the whole expression.
            ' ========================================================

            Dim paymentRows As List(Of Payment) =
                db.Payments _
                    .Include(Function(p) p.Repair) _
                    .ToList()


            ' ========================================================
            ' TOTAL COLLECTED
            ' ========================================================

            stats.TotalCollected =
                paymentRows.Sum(
                    Function(p) _
                        p.AmountPaid
                )


            ' ========================================================
            ' TOTAL OUTSTANDING
            ' ========================================================

            stats.TotalOutstanding = 0D

            For Each payment As Payment In paymentRows

                If payment.Repair Is Nothing Then
                    Continue For
                End If

                Dim balance As Decimal =
                    payment.Repair.Cost - payment.AmountPaid

                If balance > 0D Then
                    stats.TotalOutstanding += balance
                End If

            Next


            ' ========================================================
            ' PAID PAYMENTS
            ' ========================================================

         stats.PaidPayments =
    paymentRows _
        .Where(
            Function(p) _
                p.Repair IsNot Nothing AndAlso
                p.AmountPaid >= p.Repair.Cost
        ) _
        .Count()


            ' ========================================================
            ' PARTIALLY PAID
            ' ========================================================

          stats.PartialPayments =
    paymentRows _
        .Where(
            Function(p) _
                p.Repair IsNot Nothing AndAlso
                p.AmountPaid > 0D AndAlso
                p.AmountPaid < p.Repair.Cost
        ) _
        .Count()


            ' ========================================================
            ' UNPAID
            ' ========================================================
stats.UnpaidPayments =
    paymentRows _
        .Where(
            Function(p) _
                p.AmountPaid <= 0D
        ) _
        .Count()

            ' ========================================================
            ' MONTHLY REPAIR DATA
            ' ========================================================

            Dim sixMonthsAgo As DateTime =
                New DateTime(
                    DateTime.Now.Year,
                    DateTime.Now.Month,
                    1
                ).AddMonths(-5)


            Dim repairDates As List(Of DateTime) =
                db.Repairs _
                    .Where(
                        Function(r) _
                            r.RepairDate >= sixMonthsAgo
                    ) _
                    .Select(
                        Function(r) _
                            r.RepairDate
                    ) _
                    .ToList()


            ' ========================================================
            ' BUILD SIX MONTHS
            ' ========================================================

            For monthOffset As Integer = 0 To 5

                Dim currentMonth As DateTime =
                    sixMonthsAgo.AddMonths(monthOffset)


                Dim nextMonth As DateTime =
                    currentMonth.AddMonths(1)


                Dim count As Integer =
                    repairDates _
                        .Where(
                            Function(dateValue) _
                                dateValue >= currentMonth AndAlso
                                dateValue < nextMonth
                        ) _
                        .Count()


                stats.MonthlyRepairs.Add(
                    New MonthlyRepairStat With {
                        .MonthLabel =
                            currentMonth.ToString("MMM"),
                        .Year =
                            currentMonth.Year,
                        .Month =
                            currentMonth.Month,
                        .RepairCount =
                            count
                    }
                )

            Next


            ' ========================================================
            ' RETURN RESULTS
            ' ========================================================

            Return stats

        End Function

    End Class

End Namespace