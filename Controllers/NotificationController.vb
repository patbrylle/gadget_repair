Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Controllers

    Public Class NotificationController

        Public Const CatRepairs As String = "repairs"
        Public Const CatStock As String = "stock"
        Public Const CatPayments As String = "payments"
        Public Const CatAssigned As String = "assigned"

        Private Class Wanted
            Public RefKey As String = ""
            Public Category As String = ""
            Public Title As String = ""
            Public Message As String = ""
            Public CreatedAt As DateTime = DateTime.Now
        End Class

        Private Shared Sub RequireSignedIn()
            If AppSession.Role = UserRole.None Then
                Throw New UnauthorizedAccessException("Please sign in first.")
            End If
        End Sub

        ' rows that belong to the signed-in user
        Private Shared Function Mine(ctx As ApplicationDbContext) As IQueryable(Of AppNotification)

            If AppSession.IsAdmin Then
                Return ctx.Notifications.Where(Function(n) n.Audience = "Admin")
            End If

            Dim techId As Integer = AppSession.CurrentTechnicianId

            Return ctx.Notifications.Where(
                Function(n) n.Audience = "Technician" AndAlso
                            n.TechnicianId.HasValue AndAlso
                            n.TechnicianId.Value = techId)

        End Function

        Public Shared Function TargetMenu(category As String) As String

            Select Case category
                Case CatStock : Return If(AppSession.IsAdmin, "Inventory", "Repairs")
                Case CatPayments : Return If(AppSession.IsAdmin, "Payments", "Repairs")
                Case Else : Return "Repairs"
            End Select

        End Function

        ' ============================================================
        ' SYNC: builds notifications from live data
        ' ============================================================

        Public Sub Sync()

            RequireSignedIn()

            Using ctx As ApplicationDbContext = DbFactory.Create()

                SchemaBootstrap.EnsureSupportTables(ctx)

                Dim isAdmin As Boolean = AppSession.IsAdmin

                Dim wanted As List(Of Wanted) =
                    If(isAdmin, BuildAdmin(ctx), BuildTechnician(ctx))

                Dim byKey As New Dictionary(Of String, AppNotification)()

                For Each n As AppNotification In Mine(ctx).ToList()
                    If byKey.ContainsKey(n.RefKey) Then
                        ctx.Notifications.Remove(n)
                    Else
                        byKey(n.RefKey) = n
                    End If
                Next

                Dim keys As New HashSet(Of String)(wanted.Select(Function(w) w.RefKey))

                ' conditions that no longer apply disappear
                For Each n As AppNotification In byKey.Values.ToList()
                    If Not keys.Contains(n.RefKey) Then ctx.Notifications.Remove(n)
                Next

                Dim owner As Integer? = Nothing

                If Not isAdmin Then owner = AppSession.CurrentTechnicianId

                For Each w As Wanted In wanted

                    Dim row As AppNotification = Nothing

                    If byKey.TryGetValue(w.RefKey, row) Then

                        If row.Title <> w.Title OrElse row.Message <> w.Message Then
                            row.Title = w.Title
                            row.Message = w.Message
                        End If

                    Else

                        ctx.Notifications.Add(New AppNotification With {
                            .Audience = If(isAdmin, "Admin", "Technician"),
                            .TechnicianId = owner,
                            .Category = w.Category,
                            .Title = w.Title,
                            .Message = w.Message,
                            .RefKey = w.RefKey,
                            .IsRead = False,
                            .IsDismissed = False,
                            .CreatedAt = w.CreatedAt})

                    End If

                Next

                ctx.SaveChanges()

            End Using

        End Sub

        Private Shared Function Clip(text As String, max As Integer) As String
            If text Is Nothing Then Return ""
            If text.Length <= max Then Return text
            Return text.Substring(0, max)
        End Function

        Private Shared Function CustomerName(map As Dictionary(Of Integer, String), id As Integer) As String
            Dim name As String = Nothing
            If map.TryGetValue(id, name) AndAlso Not String.IsNullOrWhiteSpace(name) Then Return name
            Return "Unknown customer"
        End Function

        Private Shared Function DeviceText(r As Repair) As String
            Dim d As String = (If(r.Brand, "") & " " & If(r.Model, "")).Trim()
            If d = "" Then d = If(r.DeviceType, "").Trim()
            If d = "" Then d = "device"
            Return d
        End Function

        Private Shared Function BuildAdmin(ctx As ApplicationDbContext) As List(Of Wanted)

            Dim list As New List(Of Wanted)()
            Dim settings As New SettingsController(ctx)

            Dim customers As Dictionary(Of Integer, String) =
                ctx.Customers.AsNoTracking().ToList().
                    ToDictionary(Function(c) c.CustomerId, Function(c) If(c.CustomerName, ""))

            ' ---- repairs that need attention ----
            If settings.GetBool(SettingsController.KeyNotifyRepairs, True) Then

                Dim openRepairs As List(Of Repair) =
                    ctx.Repairs.AsNoTracking().
                        Where(Function(r) r.Status = "Pending" OrElse r.Status = "Waiting for Parts").
                        OrderByDescending(Function(r) r.RepairDate).
                        Take(60).ToList()

                For Each r As Repair In openRepairs

                    Dim w As New Wanted()
                    w.Category = CatRepairs
                    w.CreatedAt = r.RepairDate

                    If r.Status = "Waiting for Parts" Then
                        w.RefKey = "repair-parts:" & r.RepairId
                        w.Title = "Repair waiting for parts"
                    ElseIf r.TechnicianId.HasValue Then
                        w.RefKey = "repair-pending:" & r.RepairId
                        w.Title = "Repair waiting to start"
                    Else
                        w.RefKey = "repair-unassigned:" & r.RepairId
                        w.Title = "Repair needs a technician"
                    End If

                    w.Message = Clip("Ticket #" & r.RepairId & " - " &
                                     CustomerName(customers, r.CustomerId) & " - " &
                                     DeviceText(r), 500)
                    list.Add(w)

                Next

            End If

            ' ---- low and out of stock ----
            If settings.GetBool(SettingsController.KeyNotifyStock, True) Then

                Dim items As List(Of Inventory) =
                    ctx.Inventories.AsNoTracking().
                        Where(Function(i) i.Quantity <= i.MinimumStock).
                        OrderBy(Function(i) i.ItemName).
                        Take(60).ToList()

                For Each i As Inventory In items

                    Dim w As New Wanted()
                    w.Category = CatStock
                    w.CreatedAt = i.UpdatedAt

                    If i.Quantity <= 0 Then
                        w.RefKey = "stock-out:" & i.InventoryId
                        w.Title = "Out of stock"
                        w.Message = Clip(i.ItemName & " is out of stock.", 500)
                    Else
                        w.RefKey = "stock-low:" & i.InventoryId
                        w.Title = "Low stock"
                        w.Message = Clip(i.ItemName & ": " & i.Quantity & " left (minimum " &
                                         i.MinimumStock & ").", 500)
                    End If

                    list.Add(w)

                Next

            End If

            ' ---- unpaid balances ----
            If settings.GetBool(SettingsController.KeyNotifyPayments, True) Then

                Dim payable As List(Of Repair) =
                    ctx.Repairs.AsNoTracking().
                        Where(Function(r) (r.Status = "Completed" OrElse r.Status = "Delivered") AndAlso
                                          r.Cost > 0D).
                        OrderByDescending(Function(r) r.CompletionDate).
                        ToList()

                Dim paid As Dictionary(Of Integer, Decimal) =
                    ctx.Payments.AsNoTracking().ToList().
                        GroupBy(Function(p) p.RepairId).
                        ToDictionary(Function(g) g.Key, Function(g) g.Sum(Function(p) p.AmountPaid))

                Dim added As Integer = 0

                For Each r As Repair In payable

                    If added >= 60 Then Exit For

                    Dim got As Decimal = 0D
                    paid.TryGetValue(r.RepairId, got)

                    Dim balance As Decimal = r.Cost - got

                    If balance <= 0D Then Continue For

                    Dim w As New Wanted()
                    w.Category = CatPayments
                    w.RefKey = "unpaid:" & r.RepairId
                    w.Title = If(got > 0D, "Partial payment - balance due", "Payment due")
                    w.Message = Clip("Ticket #" & r.RepairId & " - " &
                                     CustomerName(customers, r.CustomerId) &
                                     " owes " & Theme.FormatPeso(balance), 500)
                    w.CreatedAt = If(r.CompletionDate.HasValue, r.CompletionDate.Value, r.RepairDate)
                    list.Add(w)
                    added += 1

                Next

            End If

            Return list

        End Function

        Private Shared Function BuildTechnician(ctx As ApplicationDbContext) As List(Of Wanted)

            Dim list As New List(Of Wanted)()
            Dim techId As Integer = AppSession.CurrentTechnicianId

            Dim customers As Dictionary(Of Integer, String) =
                ctx.Customers.AsNoTracking().ToList().
                    ToDictionary(Function(c) c.CustomerId, Function(c) If(c.CustomerName, ""))

            Dim assigned As List(Of Repair) =
                ctx.Repairs.AsNoTracking().
                    Where(Function(r) r.TechnicianId.HasValue AndAlso
                                      r.TechnicianId.Value = techId AndAlso
                                      r.Status = "Pending").
                    OrderByDescending(Function(r) r.RepairDate).
                    Take(60).ToList()

            For Each r As Repair In assigned

                Dim w As New Wanted()
                w.Category = CatAssigned
                w.RefKey = "assigned:" & r.RepairId
                w.Title = "New repair assigned to you"
                w.Message = Clip("Ticket #" & r.RepairId & " - " &
                                 CustomerName(customers, r.CustomerId) & " - " &
                                 DeviceText(r), 500)
                w.CreatedAt = r.RepairDate
                list.Add(w)

            Next

            Return list

        End Function

        ' ============================================================
        ' READ / UPDATE
        ' ============================================================

        Public Function GetNotifications() As List(Of AppNotification)

            RequireSignedIn()

            Using ctx As ApplicationDbContext = DbFactory.Create()
                Return Mine(ctx).AsNoTracking().
                    Where(Function(n) Not n.IsDismissed).
                    OrderBy(Function(n) n.IsRead).
                    ThenByDescending(Function(n) n.CreatedAt).
                    ToList()
            End Using

        End Function

        Public Function GetUnreadCount() As Integer

            RequireSignedIn()

            Using ctx As ApplicationDbContext = DbFactory.Create()
                Return Mine(ctx).Count(Function(n) Not n.IsDismissed AndAlso Not n.IsRead)
            End Using

        End Function

        Public Sub MarkRead(id As Integer)

            RequireSignedIn()

            Using ctx As ApplicationDbContext = DbFactory.Create()
                Dim row As AppNotification = Mine(ctx).FirstOrDefault(Function(n) n.NotificationId = id)
                If row Is Nothing Then Return
                row.IsRead = True
                ctx.SaveChanges()
            End Using

        End Sub

        Public Sub MarkAllRead()

            RequireSignedIn()

            Using ctx As ApplicationDbContext = DbFactory.Create()
                For Each n As AppNotification In Mine(ctx).Where(Function(x) Not x.IsRead).ToList()
                    n.IsRead = True
                Next
                ctx.SaveChanges()
            End Using

        End Sub

        Public Sub Dismiss(id As Integer)

            RequireSignedIn()

            Using ctx As ApplicationDbContext = DbFactory.Create()
                Dim row As AppNotification = Mine(ctx).FirstOrDefault(Function(n) n.NotificationId = id)
                If row Is Nothing Then Return
                row.IsDismissed = True
                row.IsRead = True
                ctx.SaveChanges()
            End Using

        End Sub

    End Class

End Namespace