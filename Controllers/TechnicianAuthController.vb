
Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Public Class TechnicianLoginResult
    Public Property Status As LoginStatus
    Public Property Technician As Technician
    Public Property Message As String = ""
End Class

Public Class TechnicianAuthController

    ' Currently signed-in technician
    Public Shared Property CurrentTechnician As Technician = Nothing

    Public Function Login(
        email As String,
        password As String
    ) As TechnicianLoginResult

        email = If(email, "").Trim()

        If String.IsNullOrWhiteSpace(email) OrElse
           String.IsNullOrEmpty(password) Then

            Return New TechnicianLoginResult With {
                .Status = LoginStatus.InvalidCredentials,
                .Message = "Please enter your email and password."
            }
        End If

        Using db As ApplicationDbContext = DbFactory.Create()

            Dim account = db.TechnicianAccounts _
                .Include(Function(a) a.Technician) _
                .FirstOrDefault(
                    Function(a) a.Technician.Email.ToLower() =
                        email.ToLower()
                )

            If account Is Nothing OrElse
               Not account.VerifyPassword(password) Then

                Return New TechnicianLoginResult With {
                    .Status = LoginStatus.InvalidCredentials,
                    .Message = "Invalid email or password."
                }
            End If

            If Not account.IsActive Then
                Return New TechnicianLoginResult With {
                    .Status = LoginStatus.AccountDisabled,
                    .Message = "This technician account is disabled."
                }
            End If

            If account.Technician Is Nothing Then
                Return New TechnicianLoginResult With {
                    .Status = LoginStatus.InvalidCredentials,
                    .Message = "Technician profile was not found."
                }
            End If

            account.LastLogin = DateTime.Now
            db.SaveChanges()

            CurrentTechnician = account.Technician

            Return New TechnicianLoginResult With {
                .Status = LoginStatus.Success,
                .Technician = account.Technician,
                .Message = "Login successful."
            }

        End Using

    End Function

    Public Shared Sub Logout()
        CurrentTechnician = Nothing
    End Sub

End Class
