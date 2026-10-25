Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

Namespace Models

    <Table("Notifications")>
    Public Class AppNotification
        <Key>
        Public Property NotificationId As Integer
        <Required, MaxLength(20)>
        Public Property Audience As String = "Admin"
        Public Property TechnicianId As Integer?
        <Required, MaxLength(30)>
        Public Property Category As String = ""
        <Required, MaxLength(150)>
        Public Property Title As String = ""
        <MaxLength(500)>
        Public Property Message As String = ""
        <Required, MaxLength(100)>
        Public Property RefKey As String = ""
        Public Property IsRead As Boolean
        Public Property IsDismissed As Boolean
        Public Property CreatedAt As DateTime = DateTime.Now
    End Class

    <Table("AppSettings")>
    Public Class AppSetting
        <Key, MaxLength(100)>
        Public Property SettingKey As String = ""
        <MaxLength(1000)>
        Public Property SettingValue As String = ""
    End Class

End Namespace