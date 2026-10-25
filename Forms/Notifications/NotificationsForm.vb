Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Models

Public Class NotificationsForm
    Inherits Form

    Private ReadOnly controller As New NotificationController()
    Private target As String = ""

    Private lblSub As Label
    Private list As Panel
    Private btnAll As NeonButton

    Public ReadOnly Property NavigateTarget As String
        Get
            Return target
        End Get
    End Property

    Public Sub New()

        FormBorderStyle = FormBorderStyle.None
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        BackColor = Theme.LineColor
        Padding = New Padding(1)
        ClientSize = New Size(560, 640)
        KeyPreview = True
        Font = Theme.UiFont(9.0F)

        BuildUi()

        AddHandler Me.Shown, AddressOf NotificationsForm_Shown

    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            Close()
            Return True
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub NotificationsForm_Shown (sender As Object, e As EventArgs)

        Try
            controller.Sync()
        Catch ex As Exception
            Debug.WriteLine(ex)
        End Try

        Reload()

    End Sub

    Private Sub BuildUi()

        Dim root As New Panel()
        root.Dock = DockStyle.Fill
        root.BackColor = Theme.CardColor

        ' ---------- header ----------
        Dim header As New Panel()
        header.Dock = DockStyle.Top
        header.Height = 80
        header.BackColor = Theme.CardColor

        AddHandler header.Paint,
            Sub(s As Object, e As PaintEventArgs)
                Using lg As New LinearGradientBrush(
                    New Rectangle(0, header.Height - 2, Math.Max(2, header.Width), 2),
                    Theme.NeonCyan, Theme.NeonPurple, LinearGradientMode.Horizontal)
                    e.Graphics.FillRectangle(lg, 0, header.Height - 2, header.Width, 2)
                End Using
            End Sub

        Dim lblTitle As New Label()
        lblTitle.Text = "Notifications"
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(24, 14)
        lblTitle.Font = Theme.UiFont(15.0F, FontStyle.Bold)
        lblTitle.ForeColor = Theme.TextWhite

        lblSub = New Label()
        lblSub.AutoSize = True
        lblSub.Location = New Point(26, 48)
        lblSub.Font = Theme.UiFont(9.0F)
        lblSub.ForeColor = Theme.TextMuted

        Dim btnClose As New NeonCloseButton()
        btnClose.Location = New Point(560 - 2 - 32 - 14, 14)
        AddHandler btnClose.Click, Sub(s As Object, e As EventArgs) Close()

        header.Controls.Add(lblTitle)
        header.Controls.Add(lblSub)
        header.Controls.Add(btnClose)

        NeonLayout.MakeDraggable(Me, header, lblTitle, lblSub)

        ' ---------- footer ----------
        Dim footer As New Panel()
        footer.Dock = DockStyle.Bottom
        footer.Height = 66
        footer.BackColor = Theme.CardColor

        btnAll = New NeonButton()
        btnAll.Text = "Mark all as read"
        btnAll.Accent = Theme.NeonCyan
        btnAll.KeepAccentOnHover = True
        btnAll.Size = New Size(170, 38)
        btnAll.Location = New Point(20, 14)
        AddHandler btnAll.Click, AddressOf MarkAll_Click

        Dim btnDone As New NeonButton()
        btnDone.Text = "Close"
        btnDone.Filled = True
        btnDone.Size = New Size(110, 38)
        btnDone.Location = New Point(560 - 2 - 110 - 20, 14)
        AddHandler btnDone.Click, Sub(s As Object, e As EventArgs) Close()

        footer.Controls.Add(btnAll)
        footer.Controls.Add(btnDone)

        ' ---------- list ----------
        list = New Panel()
        list.Dock = DockStyle.Fill
        list.AutoScroll = True
        list.BackColor = Theme.CardColor
        list.Padding = New Padding(0, 8, 0, 8)

        root.Controls.Add(list)
        root.Controls.Add(footer)
        root.Controls.Add(header)

        Controls.Add(root)

    End Sub

    Private Sub Reload()

        Dim items As List(Of AppNotification)

        Try
            items = controller.GetNotifications()
        Catch ex As Exception
            MessageBox.Show("Unable to load notifications." & Environment.NewLine & ex.Message,
                            "Notifications", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        list.SuspendLayout()

        For i As Integer = list.Controls.Count - 1 To 0 Step -1
            Dim old As Control = list.Controls(i)
            list.Controls.RemoveAt(i)
            old.Dispose()
        Next

        Dim unread As Integer = 0

        For Each n As AppNotification In items
            If Not n.IsRead Then unread += 1
        Next

        lblSub.Text = If(unread > 0, unread.ToString() & " unread of " & items.Count.ToString(),
                         If(items.Count > 0, "You are all caught up", ""))

        btnAll.Enabled = unread > 0

        If items.Count = 0 Then

            Dim empty As New Label()
            empty.Text = "You're all caught up." & Environment.NewLine & "New repairs, stock and payment alerts will appear here."
            empty.Dock = DockStyle.Fill
            empty.TextAlign = ContentAlignment.MiddleCenter
            empty.ForeColor = Theme.TextMuted
            empty.Font = Theme.UiFont(10.0F)
            list.Controls.Add(empty)

        Else

            Dim y As Integer = 4

            For Each n As AppNotification In items

                Dim row As New NotifRow(n)
                row.Left = 16
                row.Top = y
                row.Width = Math.Max(100, list.ClientSize.Width - 32)
                row.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right

                AddHandler row.Activated, AddressOf Row_Activated
                AddHandler row.Dismissed, AddressOf Row_Dismissed

                list.Controls.Add(row)

                y += row.Height + 8

            Next

        End If

        list.ResumeLayout(True)

    End Sub

    Private Sub Row_Activated(item As AppNotification)

        Try
            controller.MarkRead(item.NotificationId)
        Catch ex As Exception
            Debug.WriteLine(ex)
        End Try

        target = NotificationController.TargetMenu(item.Category)
        Close()

    End Sub

    Private Sub Row_Dismissed(item As AppNotification)

        Try
            controller.Dismiss(item.NotificationId)
        Catch ex As Exception
            Debug.WriteLine(ex)
        End Try

        Reload()

    End Sub

    Private Sub MarkAll_Click(sender As Object, e As EventArgs)

        Try
            controller.MarkAllRead()
        Catch ex As Exception
            Debug.WriteLine(ex)
        End Try

        Reload()

    End Sub

    ' ============================================================
    ' ONE ROW
    ' ============================================================
    Private Class NotifRow
        Inherits Control

        Public ReadOnly Item As AppNotification
        Public Event Activated(item As AppNotification)
        Public Event Dismissed(item As AppNotification)

        Private hot As Boolean = False
        Private hotX As Boolean = False

        Public Sub New(n As AppNotification)

            Item = n

            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)

            Height = 78
            Cursor = Cursors.Hand

        End Sub

        Private Function Accent() As Color
            Select Case Item.Category
                Case "stock" : Return Theme.NeonYellow
                Case "payments" : Return Theme.NeonPink
                Case "assigned" : Return Theme.NeonGreen
                Case Else : Return Theme.NeonCyan
            End Select
        End Function

        Private Function CloseRect() As Rectangle
            Return New Rectangle(Width - 34, 40, 20, 20)
        End Function

        Private Shared Function Ago(t As DateTime) As String

            Dim span As TimeSpan = DateTime.Now - t

            If span.TotalMinutes < 1 Then Return "just now"
            If span.TotalMinutes < 60 Then Return CInt(span.TotalMinutes).ToString() & " min ago"
            If span.TotalHours < 24 Then Return CInt(span.TotalHours).ToString() & " hr ago"
            If span.TotalDays < 7 Then Return CInt(span.TotalDays).ToString() & " day(s) ago"

            Return t.ToString("MMM dd")

        End Function

        Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
            e.Graphics.Clear(If(Parent IsNot Nothing, Parent.BackColor, Theme.CardColor))
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)

            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias

            Dim ac As Color = Accent()
            Dim back As Color = If(hot, Theme.Lerp(Theme.BackgroundColor, ac, 0.10F), Theme.BackgroundColor)

            Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)

            Using path As GraphicsPath = Theme.RoundedRect(rect, 10)
                Using b As New SolidBrush(back)
                    g.FillPath(b, path)
                End Using
                Using p As New Pen(If(hot, ac, Theme.LineColor), 1.0F)
                    g.DrawPath(p, path)
                End Using
            End Using

            Using b As New SolidBrush(ac)
                g.FillRectangle(b, 0, 14, 3, Height - 28)
                If Not Item.IsRead Then g.FillEllipse(b, 16, 20, 9, 9)
            End Using

            Dim titleColor As Color = If(Item.IsRead, Theme.TextMuted, Theme.TextWhite)

            TextRenderer.DrawText(g, Item.Title, Theme.UiFont(9.5F, FontStyle.Bold),
                New Rectangle(34, 12, Width - 34 - 110, 22), titleColor,
                TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
                TextFormatFlags.NoPadding Or TextFormatFlags.EndEllipsis)

            TextRenderer.DrawText(g, Ago(Item.CreatedAt), Theme.UiFont(8.0F),
                New Rectangle(Width - 100, 12, 86, 22), Theme.TextMuted,
                TextFormatFlags.Right Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)

            TextRenderer.DrawText(g, Item.Message, Theme.UiFont(9.0F),
                New Rectangle(34, 38, Width - 34 - 48, 34), Theme.TextMuted,
                TextFormatFlags.Left Or TextFormatFlags.Top Or TextFormatFlags.WordBreak Or
                TextFormatFlags.NoPadding Or TextFormatFlags.EndEllipsis)

            If hot Then

                Dim cr As Rectangle = CloseRect()

                Using p As New Pen(If(hotX, Theme.NeonRed, Theme.TextMuted), 1.6F)
                    p.StartCap = LineCap.Round
                    p.EndCap = LineCap.Round
                    g.DrawLine(p, cr.X + 6, cr.Y + 6, cr.Right - 6, cr.Bottom - 6)
                    g.DrawLine(p, cr.X + 6, cr.Bottom - 6, cr.Right - 6, cr.Y + 6)
                End Using

            End If

        End Sub

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            hot = True : Invalidate() : MyBase.OnMouseEnter(e)
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            hot = False : hotX = False : Invalidate() : MyBase.OnMouseLeave(e)
        End Sub

        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            Dim over As Boolean = CloseRect().Contains(e.Location)
            If over <> hotX Then
                hotX = over
                Invalidate()
            End If
            MyBase.OnMouseMove(e)
        End Sub

        Protected Overrides Sub OnMouseClick(e As MouseEventArgs)

            MyBase.OnMouseClick(e)

            If e.Button <> MouseButtons.Left Then Return

            If CloseRect().Contains(e.Location) Then
                RaiseEvent Dismissed(Item)
            Else
                RaiseEvent Activated(Item)
            End If

        End Sub

    End Class

End Class