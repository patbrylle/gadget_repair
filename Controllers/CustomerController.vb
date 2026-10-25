Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Controllers

    Public Class CustomerController

        Private ReadOnly db As ApplicationDbContext

        Public Sub New(database As ApplicationDbContext)
            db = database
        End Sub

        ' Get all customers
        Public Function GetAll() As List(Of Customer)

            Return db.Customers.
                AsNoTracking().
                OrderBy(Function(c) c.CustomerName).
                ToList()

        End Function

        ' Search customers
        Public Function Search(searchText As String) As List(Of Customer)

            If String.IsNullOrWhiteSpace(searchText) Then
                Return GetAll()
            End If

            Dim searchTerm As String = searchText.Trim()

            Return db.Customers.
                AsNoTracking().
                Where(
                    Function(c) c.CustomerName.Contains(searchTerm) OrElse
                               c.ContactNumber.Contains(searchTerm) OrElse
                               c.Address.Contains(searchTerm)
                ).
                OrderBy(Function(c) c.CustomerName).
                ToList()

        End Function

        ' Get one customer
        Public Function GetById(customerId As Integer) As Customer

            Return db.Customers.Find(customerId)

        End Function

        ' Add customer
        Public Sub Add(customer As Customer)

            Prepare(customer)
            EnsureUnique(0, customer.ContactNumber)

            db.Customers.Add(customer)

            Try
                db.SaveChanges()
            Catch
                db.Entry(customer).State = EntityState.Detached
                Throw
            End Try

        End Sub

        ' Update customer
        Public Sub Update(customer As Customer)

            Prepare(customer)
            EnsureUnique(customer.CustomerId, customer.ContactNumber)

            Try
                db.Customers.Update(customer)
                db.SaveChanges()
            Catch
                db.Entry(customer).Reload()
                Throw
            End Try

        End Sub

        ' Delete customer
        Public Sub Delete(customerId As Integer)

            Dim customer As Customer = db.Customers.Find(customerId)

            If customer Is Nothing Then Return

            If db.Repairs.Any(Function(r) r.CustomerId = customerId) Then
                Throw New InvalidOperationException(
                    "This customer has repair records and cannot be deleted.")
            End If

            db.Customers.Remove(customer)
            db.SaveChanges()

        End Sub


        ' ==========================================================
        ' VALIDATION
        ' ==========================================================

        Private Shared Sub Prepare(customer As Customer)

            customer.CustomerName = CustomerValidator.NormalizeName(customer.CustomerName)
            customer.ContactNumber = CustomerValidator.NormalizeContact(customer.ContactNumber)
            customer.Address = If(customer.Address, "").Trim()

            Dim result As CustomerValidationResult =
                CustomerValidator.Validate(customer.CustomerName,
                                           customer.ContactNumber,
                                           customer.Address)

            If Not result.IsValid Then
                Throw New CustomerValidationException(result.Field, result.Message)
            End If

        End Sub

        ' customerId = 0 when adding. Pass the real id when editing.
        Public Sub EnsureUnique(customerId As Integer, contactNumber As String)

            Dim phone As String = CustomerValidator.NormalizeContact(contactNumber)

            If phone.Length > 0 AndAlso
               db.Customers.AsNoTracking().Any(
                   Function(c) c.CustomerId <> customerId AndAlso c.ContactNumber = phone) Then

                Throw New CustomerValidationException(CustomerValidator.ContactField,
                    "This contact number is already registered to another customer.")
            End If

        End Sub

    End Class

End Namespace