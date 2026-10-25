Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Controllers

    Public Class InventoryController

        ' fixed category list (old categories in the database still show in the filter)
        Public Shared ReadOnly CategoryList As String() = {
            "LCD / Screen", "Battery", "Charger / Cable", "Storage",
            "Memory (RAM)", "Keyboard", "Motherboard / IC", "Casing / Housing",
            "Camera", "Speaker / Mic", "Accessories", "Tools", "Other"
        }

        Private ReadOnly db As ApplicationDbContext

        Public Sub New(context As ApplicationDbContext)
            db = context
        End Sub

        Private Shared Sub RequireAdmin()
            If Not AppSession.IsAdmin Then
                Throw New UnauthorizedAccessException(
                    "Only an administrator can manage inventory.")
            End If
        End Sub

        Private Shared Sub Validate(
            itemName As String,
            category As String,
            quantity As Integer,
            unitCost As Decimal,
            sellingPrice As Decimal,
            minimumStock As Integer)

            If String.IsNullOrWhiteSpace(itemName) Then
                Throw New ArgumentException("Please enter the item name.")
            End If

            If String.IsNullOrWhiteSpace(category) Then
                Throw New ArgumentException("Please choose a category.")
            End If

            If quantity < 0 OrElse minimumStock < 0 Then
                Throw New ArgumentException("Stock values cannot be negative.")
            End If

            If unitCost < 0D OrElse sellingPrice < 0D Then
                Throw New ArgumentException("Prices cannot be negative.")
            End If

        End Sub


        ' ============================================================
        ' READ
        ' ============================================================

        Public Function GetInventory(
            Optional search As String = "",
            Optional category As String = ""
        ) As List(Of Inventory)

            Dim query = db.Inventories.AsQueryable()

            If Not String.IsNullOrWhiteSpace(search) Then

                Dim searchText As String = search.Trim().ToLower()

                query = query.Where(
                    Function(i) i.ItemName.ToLower().Contains(searchText) OrElse
                                i.Category.ToLower().Contains(searchText))

            End If

            If Not String.IsNullOrWhiteSpace(category) AndAlso
               category <> "All Categories" Then

                query = query.Where(Function(i) i.Category = category)

            End If

            Return query.OrderBy(Function(i) i.ItemName).ToList()

        End Function

        Public Function GetInventoryItem(inventoryId As Integer) As Inventory

            Return db.Inventories.FirstOrDefault(
                Function(i) i.InventoryId = inventoryId)

        End Function

        ' categories found in the database
        Public Function GetCategories() As List(Of String)

            Return db.Inventories.
                Where(Function(i) Not String.IsNullOrWhiteSpace(i.Category)).
                Select(Function(i) i.Category).
                Distinct().
                OrderBy(Function(c) c).
                ToList()

        End Function

        ' fixed list first, then any old category that is not in it
        Public Function GetFilterCategories() As List(Of String)

            Dim result As New List(Of String)(CategoryList)

            For Each c As String In GetCategories()
                If Not result.Contains(c, StringComparer.OrdinalIgnoreCase) Then
                    result.Add(c)
                End If
            Next

            Return result

        End Function


        ' ============================================================
        ' WRITE (admin only)
        ' ============================================================

        Public Function CreateInventory(
            itemName As String,
            category As String,
            quantity As Integer,
            unitCost As Decimal,
            sellingPrice As Decimal,
            minimumStock As Integer
        ) As Inventory

            RequireAdmin()

            Validate(itemName, category, quantity, unitCost, sellingPrice, minimumStock)

            Dim item As New Inventory With {
                .ItemName = itemName.Trim(),
                .Category = category.Trim(),
                .Quantity = quantity,
                .UnitCost = unitCost,
                .SellingPrice = sellingPrice,
                .MinimumStock = minimumStock,
                .CreatedAt = DateTime.Now,
                .UpdatedAt = DateTime.Now
            }

            db.Inventories.Add(item)
            db.SaveChanges()

            Return item

        End Function

        Public Function UpdateInventory(
            inventoryId As Integer,
            itemName As String,
            category As String,
            quantity As Integer,
            unitCost As Decimal,
            sellingPrice As Decimal,
            minimumStock As Integer
        ) As Boolean

            RequireAdmin()

            Validate(itemName, category, quantity, unitCost, sellingPrice, minimumStock)

            Dim item As Inventory =
                db.Inventories.FirstOrDefault(Function(i) i.InventoryId = inventoryId)

            If item Is Nothing Then Return False

            item.ItemName = itemName.Trim()
            item.Category = category.Trim()
            item.Quantity = quantity
            item.UnitCost = unitCost
            item.SellingPrice = sellingPrice
            item.MinimumStock = minimumStock
            item.UpdatedAt = DateTime.Now

            db.SaveChanges()

            Return True

        End Function

        Public Function DeleteInventory(inventoryId As Integer) As Boolean

            RequireAdmin()

            Dim item As Inventory =
                db.Inventories.FirstOrDefault(Function(i) i.InventoryId = inventoryId)

            If item Is Nothing Then Return False

            db.Inventories.Remove(item)
            db.SaveChanges()

            Return True

        End Function


        ' ============================================================
        ' STOCK STATUS
        ' ============================================================

        Public Shared Function StockStatus(quantity As Integer, minimum As Integer) As String

            If quantity <= 0 Then Return "Out of Stock"
            If quantity <= minimum Then Return "Low Stock"

            Return "In Stock"

        End Function

        Public Shared Function GetStockStatus(item As Inventory) As String
            Return StockStatus(item.Quantity, item.MinimumStock)
        End Function


        ' ============================================================
        ' USAGE (from the items technicians added to repairs)
        ' ============================================================

        ' inventory id -> total pieces used
        Public Function GetUsageLookup() As Dictionary(Of Integer, Integer)

            Return db.RepairParts.
                ToList().
                GroupBy(Function(p) p.InventoryId).
                ToDictionary(
                    Function(g) g.Key,
                    Function(g) g.Sum(Function(p) p.Quantity))

        End Function

        ' name and pieces used of the most used item (Nothing when none yet)
        Public Function GetMostUsed() As Tuple(Of String, Integer)

            Dim top = db.RepairParts.
                ToList().
                GroupBy(Function(p) p.InventoryId).
                Select(Function(g) New With {
                    .Name = g.First().ItemName,
                    .Used = g.Sum(Function(p) p.Quantity)}).
                OrderByDescending(Function(x) x.Used).
                FirstOrDefault()

            If top Is Nothing Then Return Nothing

            Return Tuple.Create(top.Name, top.Used)

        End Function


        ' ============================================================
        ' STATISTICS
        ' ============================================================

        Public Function GetTotalItems() As Integer
            Return db.Inventories.Count()
        End Function

        Public Function GetTotalStock() As Integer

            Return db.Inventories.
                Select(Function(i) i.Quantity).
                DefaultIfEmpty(0).
                Sum()

        End Function

        Public Function GetLowStockCount() As Integer

            Return db.Inventories.Count(
                Function(i) i.Quantity > 0 AndAlso i.Quantity <= i.MinimumStock)

        End Function

        Public Function GetOutOfStockCount() As Integer
            Return db.Inventories.Count(Function(i) i.Quantity <= 0)
        End Function

        ' stock value at unit cost
        Public Function GetInventoryValue() As Decimal

            Return db.Inventories.
                Select(Function(i) i.Quantity * i.UnitCost).
                DefaultIfEmpty(0D).
                Sum()

        End Function

    End Class

End Namespace