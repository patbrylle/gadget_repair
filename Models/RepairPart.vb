Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

Namespace Models

    <Table("RepairParts")>
    Public Class RepairPart

        <Key>
        Public Property RepairPartId As Integer

        Public Property RepairId As Integer

        Public Property InventoryId As Integer

        ' name and price are copied from inventory when the part is added
        <Required, MaxLength(150)>
        Public Property ItemName As String = String.Empty

        Public Property Quantity As Integer = 1

        Public Property UnitPrice As Decimal = 0D

        Public Property AddedAt As DateTime = DateTime.Now

    End Class

End Namespace