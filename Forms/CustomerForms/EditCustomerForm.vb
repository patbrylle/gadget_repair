Imports gadget_repair.Data
Imports gadget_repair.Models

Public Class EditCustomerForm
    Inherits CustomerFormBase

    Private ReadOnly customer As Customer

    Public Sub New(database As ApplicationDbContext, customerToEdit As Customer)

        MyBase.New(database,
                   "EDIT CUSTOMER",
                   "Update the customer's information below.",
                   "SAVE CHANGES")

        customer = customerToEdit

        txtFullName.Text = customer.CustomerName
        txtContactNumber.Text = customer.ContactNumber
        txtAddress.Text = customer.Address

    End Sub

    Protected Overrides Sub SaveToDatabase(fullName As String,
                                           contactNumber As String,
                                           address As String)

        ' Check duplicates BEFORE touching the tracked entity,
        ' so a failed save never leaves it half-modified.
        customerController.EnsureUnique(customer.CustomerId, contactNumber)

        customer.CustomerName = fullName
        customer.ContactNumber = contactNumber
        customer.Address = address

        customerController.Update(customer)

    End Sub

End Class