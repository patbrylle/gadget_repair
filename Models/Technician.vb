Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

Namespace Models

    Public Class Technician

        <Key>
        Public Property TechnicianId As Integer

        <Required>
        <MaxLength(100)>
        Public Property TechnicianName As String = String.Empty

        <MaxLength(100)>
        Public Property Specialization As String = String.Empty

        <MaxLength(30)>
        Public Property ContactNumber As String = String.Empty

        <MaxLength(50)>
        Public Property Status As String = "Available"

        <MaxLength(255)>
        Public Property Email As String = String.Empty

        Public Property CreatedAt As DateTime = DateTime.Now

        ' Navigation property
        Public Overridable Property Repairs As ICollection(Of Repair) =
            New List(Of Repair)

    End Class

End Namespace