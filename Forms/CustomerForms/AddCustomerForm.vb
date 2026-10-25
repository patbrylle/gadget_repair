Imports gadget_repair.Data
Imports gadget_repair.Models

Public Class AddCustomerForm
    Inherits CustomerFormBase

    Public Sub New(database As ApplicationDbContext)

        MyBase.New(database,
                   "ADD CUSTOMER",
                   "Enter the customer's information below.",
                   "SAVE CUSTOMER")

    End Sub

    Protected Overrides Sub SaveToDatabase(fullName As String,
                                           contactNumber As String,
                                           address As String)

        Dim customer As New Customer()

        customer.CustomerName = fullName
        customer.ContactNumber = contactNumber
        customer.Address = address

        customerController.Add(customer)

    End Sub

End Class