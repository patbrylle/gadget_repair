Imports System
Imports System.ComponentModel.DataAnnotations.Schema

Namespace Models

    Public Class Repair

        Public Property RepairId As Integer

        Public Property TechnicianId As Integer?

        <ForeignKey(NameOf(TechnicianId))>
        Public Overridable Property Technician As Technician

        Public Property CustomerId As Integer

        Public Property DeviceType As String = ""

        Public Property Brand As String = ""

        Public Property Model As String = ""

        Public Property SerialNumber As String = ""

        Public Property ProblemDescription As String = ""

        Public Property RepairDescription As String = ""

        Public Property Status As String = "Pending"

        Public Property RepairDate As DateTime = DateTime.Now

        Public Property CompletionDate As DateTime?

        Public Property Cost As Decimal

    End Class

End Namespace