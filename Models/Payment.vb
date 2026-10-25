Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

Namespace Models

    Public Class Payment

        <Key>
        Public Property PaymentId As Integer

        ' ==========================================================
        ' REPAIR
        ' ==========================================================

        Public Property RepairId As Integer

        <ForeignKey(NameOf(RepairId))>
        Public Overridable Property Repair As Repair


        ' ==========================================================
        ' PAYMENT
        ' ==========================================================

        Public Property AmountPaid As Decimal = 0D

        Public Property PaymentDate As DateTime =
            DateTime.Now


        ' ==========================================================
        ' NOTES
        ' ==========================================================

        <MaxLength(255)>
        Public Property Notes As String = String.Empty


        ' ==========================================================
        ' NAVIGATION
        ' ==========================================================

        Public ReadOnly Property TotalCost As Decimal
            Get
                If Repair Is Nothing Then
                    Return 0D
                End If

                Return Repair.Cost
            End Get
        End Property


       Public ReadOnly Property Balance As Decimal
    Get

        Dim remainingAmount As Decimal =
            TotalCost - AmountPaid

        If remainingAmount < 0D Then
            Return 0D
        End If

        Return remainingAmount

    End Get
End Property

        Public ReadOnly Property Status As String
            Get

                If AmountPaid <= 0D Then
                    Return "Unpaid"

                ElseIf AmountPaid < TotalCost Then
                    Return "Partially Paid"

                Else
                    Return "Paid"

                End If

            End Get
        End Property

    End Class

End Namespace