Imports gadget_repair.Models

Public Enum UserRole
    None = 0
    Admin = 1
    Technician = 2
End Enum

Public Module AppSession

    Private displayNameOverride As String = ""

    Public ReadOnly Property Role As UserRole
        Get
            If AuthController.CurrentUser IsNot Nothing Then Return UserRole.Admin
            If TechnicianAuthController.CurrentTechnician IsNot Nothing Then Return UserRole.Technician
            Return UserRole.None
        End Get
    End Property

    Public ReadOnly Property IsAdmin As Boolean
        Get
            Return Role = UserRole.Admin
        End Get
    End Property

    Public ReadOnly Property IsTechnician As Boolean
        Get
            Return Role = UserRole.Technician
        End Get
    End Property

    Public ReadOnly Property CurrentTechnicianId As Integer
        Get
            If TechnicianAuthController.CurrentTechnician Is Nothing Then Return 0
            Return TechnicianAuthController.CurrentTechnician.TechnicianId
        End Get
    End Property

    Public ReadOnly Property DisplayName As String
        Get
            If displayNameOverride <> "" AndAlso Role <> UserRole.None Then Return displayNameOverride
            If AuthController.CurrentUser IsNot Nothing Then Return AuthController.CurrentUser.FullName
            If TechnicianAuthController.CurrentTechnician IsNot Nothing Then Return TechnicianAuthController.CurrentTechnician.TechnicianName
            Return ""
        End Get
    End Property

    ' used by the Profile modal so the new name shows without logging in again
    Public Sub SetDisplayName(name As String)
        displayNameOverride = If(name, "").Trim()
    End Sub

    Public Sub SignOut()
        displayNameOverride = ""
        AuthController.Logout()
        TechnicianAuthController.Logout()
    End Sub

    ' ---------------- PERMISSIONS ----------------

    Public ReadOnly Property CanManageRepairs As Boolean
        Get
            Return IsAdmin
        End Get
    End Property

    Public Function CanUpdateProgress(repair As Repair) As Boolean
        Return IsTechnician AndAlso
               repair IsNot Nothing AndAlso
               repair.TechnicianId.HasValue AndAlso
               repair.TechnicianId.Value = CurrentTechnicianId
    End Function

End Module