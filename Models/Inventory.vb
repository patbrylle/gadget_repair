Imports System
Imports System.ComponentModel.DataAnnotations

Namespace Models

    Public Class Inventory

        <Key>
        Public Property InventoryId As Integer

        <Required>
        <MaxLength(150)>
        Public Property ItemName As String = String.Empty

        <MaxLength(100)>
        Public Property Category As String = String.Empty

        Public Property Quantity As Integer = 0

        Public Property UnitCost As Decimal = 0D

        Public Property SellingPrice As Decimal = 0D

        Public Property MinimumStock As Integer = 5

        Public Property CreatedAt As DateTime = DateTime.Now

        Public Property UpdatedAt As DateTime = DateTime.Now

    End Class

End Namespace