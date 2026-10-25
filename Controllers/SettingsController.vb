Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Controllers

    Public Class ShopInfo
        Public Property Name As String = "Gadget Repair"
        Public Property Address As String = ""
        Public Property Contact As String = ""
        Public Property Email As String = ""
    End Class

    Public Class SettingsController

        Public Const KeyShopName As String = "shop.name"
        Public Const KeyShopAddress As String = "shop.address"
        Public Const KeyShopContact As String = "shop.contact"
        Public Const KeyShopEmail As String = "shop.email"
        Public Const KeyDefaultPeriod As String = "report.defaultPeriod"
        Public Const KeyNotifyRepairs As String = "notify.repairs"
        Public Const KeyNotifyStock As String = "notify.stock"
        Public Const KeyNotifyPayments As String = "notify.payments"

        Private ReadOnly db As ApplicationDbContext

        Public Sub New(context As ApplicationDbContext)
            db = context
            SchemaBootstrap.EnsureSupportTables(db)
        End Sub

        Public Function GetValue(key As String, Optional fallback As String = "") As String

            Dim row As AppSetting =
                db.AppSettings.AsNoTracking().
                    FirstOrDefault(Function(s) s.SettingKey = key)

            If row Is Nothing Then Return fallback

            Return row.SettingValue

        End Function

        Public Function GetBool(key As String, fallback As Boolean) As Boolean

            Dim value As String = GetValue(key, "")

            If value = "" Then Return fallback

            Return value = "1"

        End Function

        Public Sub SetValues(values As Dictionary(Of String, String))

            If Not AppSession.IsAdmin Then
                Throw New UnauthorizedAccessException(
                    "Only an administrator can change settings.")
            End If

            For Each pair As KeyValuePair(Of String, String) In values

                Dim k As String = pair.Key
                Dim v As String = If(pair.Value, "").Trim()

                If v.Length > 1000 Then v = v.Substring(0, 1000)

                Dim row As AppSetting =
                    db.AppSettings.FirstOrDefault(Function(s) s.SettingKey = k)

                If row Is Nothing Then
                    db.AppSettings.Add(New AppSetting With {.SettingKey = k, .SettingValue = v})
                Else
                    row.SettingValue = v
                End If

            Next

            db.SaveChanges()

        End Sub

        Public Function GetShop() As ShopInfo

            Dim info As New ShopInfo()

            info.Name = GetValue(KeyShopName, "Gadget Repair")

            If String.IsNullOrWhiteSpace(info.Name) Then info.Name = "Gadget Repair"

            info.Address = GetValue(KeyShopAddress, "")
            info.Contact = GetValue(KeyShopContact, "")
            info.Email = GetValue(KeyShopEmail, "")

            Return info

        End Function

    End Class

End Namespace