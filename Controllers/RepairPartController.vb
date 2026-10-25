Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports gadget_repair.Data
Imports gadget_repair.Models
Imports Microsoft.EntityFrameworkCore

Namespace Controllers

    Public Class RepairPartController

        Private ReadOnly db As ApplicationDbContext

        Public Sub New(database As ApplicationDbContext)
            db = database
        End Sub

        ' only the assigned technician, and only until the repair is final
        Public Shared Function CanEditItems(repair As Repair) As Boolean
            Return repair IsNot Nothing AndAlso
                   AppSession.CanUpdateProgress(repair) AndAlso
                   Not RepairController.IsFinal(repair.Status)
        End Function

        Public Shared Function PartsTotal(parts As IEnumerable(Of RepairPart)) As Decimal

            Dim total As Decimal = 0D

            For Each p As RepairPart In parts
                total += p.Quantity * p.UnitPrice
            Next

            Return total

        End Function

        ' one line per item: name  xQty  amount
        Public Shared Function Summary(parts As IEnumerable(Of RepairPart)) As String

            Dim lines As New List(Of String)

            For Each p As RepairPart In parts
                lines.Add(p.ItemName & "  x" & p.Quantity.ToString() &
                          "   " & Money.Peso(p.Quantity * p.UnitPrice))
            Next

            Return String.Join(Environment.NewLine, lines)

        End Function

        Private Function FindRepair(repairId As Integer) As Repair

            If AppSession.Role = UserRole.None Then
                Throw New UnauthorizedAccessException("Please sign in first.")
            End If

            Return db.Repairs.FirstOrDefault(Function(r) r.RepairId = repairId)

        End Function

        Public Function GetParts(repairId As Integer) As List(Of RepairPart)

            Dim repair As Repair = FindRepair(repairId)

            If repair Is Nothing Then Return New List(Of RepairPart)()

            If AppSession.IsTechnician AndAlso
               Not AppSession.CanUpdateProgress(repair) Then
                Return New List(Of RepairPart)()
            End If

            Return db.RepairParts.
                Where(Function(p) p.RepairId = repairId).
                OrderBy(Function(p) p.AddedAt).
                ToList()

        End Function

        ' items that still have stock
        Public Function GetAvailableItems() As List(Of Inventory)

    Return db.Inventories.
        AsNoTracking().
        Where(Function(i) i.Quantity > 0).
        OrderBy(Function(i) i.ItemName).
        ToList()

End Function

        Public Sub AddPart(repairId As Integer, inventoryId As Integer, quantity As Integer)

            Dim repair As Repair = FindRepair(repairId)

            If repair Is Nothing Then
                Throw New InvalidOperationException("Repair record not found.")
            End If

            If Not CanEditItems(repair) Then
                Throw New UnauthorizedAccessException(
                    "Only the assigned technician can change the items, and not after the repair is final.")
            End If

            If quantity <= 0 Then
                Throw New ArgumentException("Enter a quantity above 0.")
            End If

            Dim item As Inventory =
                db.Inventories.FirstOrDefault(Function(i) i.InventoryId = inventoryId)

            If item Is Nothing Then
                Throw New InvalidOperationException("Inventory item not found.")
            End If

            If quantity > item.Quantity Then
                Throw New ArgumentException(
                    "Only " & item.Quantity.ToString() & " in stock.")
            End If

            ' the same item on the same repair becomes one line
            Dim line As RepairPart =
                db.RepairParts.FirstOrDefault(
                    Function(p) p.RepairId = repairId AndAlso p.InventoryId = inventoryId)

            If line Is Nothing Then

                line = New RepairPart With {
                    .RepairId = repairId,
                    .InventoryId = inventoryId,
                    .ItemName = item.ItemName,
                    .Quantity = quantity,
                    .UnitPrice = item.SellingPrice,
                    .AddedAt = DateTime.Now
                }

                db.RepairParts.Add(line)

            Else

                line.Quantity += quantity

            End If

            item.Quantity -= quantity
            item.UpdatedAt = DateTime.Now

Try
    db.SaveChanges()
Catch ex As DbUpdateConcurrencyException
    db.ChangeTracker.Clear()
    Throw New InvalidOperationException(
        "The stock changed while you were adding this item. Please try again.")
End Try
        End Sub

        ' removes the line and puts the stock back
        Public Sub RemovePart(partId As Integer)

            Dim line As RepairPart =
                db.RepairParts.FirstOrDefault(Function(p) p.RepairPartId = partId)

            If line Is Nothing Then Return

            Dim repair As Repair = FindRepair(line.RepairId)

            If Not CanEditItems(repair) Then
                Throw New UnauthorizedAccessException(
                    "Only the assigned technician can change the items, and not after the repair is final.")
            End If

            Dim item As Inventory =
                db.Inventories.FirstOrDefault(Function(i) i.InventoryId = line.InventoryId)

            If item IsNot Nothing Then
                item.Quantity += line.Quantity
                item.UpdatedAt = DateTime.Now
            End If

            db.RepairParts.Remove(line)

Try
    db.SaveChanges()
Catch ex As DbUpdateConcurrencyException
    db.ChangeTracker.Clear()
    Throw New InvalidOperationException(
        "The stock changed while you were adding this item. Please try again.")
End Try
        End Sub

    End Class

End Namespace