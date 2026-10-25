Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Public Class RepairHistoryForm
    Inherits Form

    ' ==========================================================
    ' WIN32 (drag borderless window)
    ' ==========================================================

    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As Integer, lParam As Integer) As IntPtr
    End Function

    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HTCAPTION As Integer = 2
    Private Const CornerRadius As Integer = 20


    ' ==========================================================
    ' FIELDS
    ' ==========================================================

    Private ReadOnly db As ApplicationDbContext
    Private ReadOnly customerId As Integer
    Private ReadOnly customerName As String

    Private card As NeonCard
    Private headerPanel As Panel
    Private dividerPanel As Panel
    Private footerPanel As Panel
    Private listPanel As Panel

    Private lblTitle As Label
    Private lblSubtitle As Label
    Private lblCustomer As Label
    Private lblCount As Label
    Private lblEmpty As Label

    Private btnClose As NeonButton


    ' ==========================================================
    ' CONSTRUCTOR
    ' ==========================================================

    Public Sub New(database As ApplicationDbContext, selectedCustomerId As Integer, selectedCustomerName As String)

        db = database
        customerId = selectedCustomerId
        customerName = selectedCustomerName

        InitializeForm()
        BuildInterface()
        LoadRepairHistory()

    End Sub


    ' ==========================================================
    ' FORM SETUP
    ' ==========================================================

    Private Sub InitializeForm()

        Me.Text = "Repair History"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.None
        Me.ShowInTaskbar = False
        Me.ClientSize = New Size(560, 660)
        Me.MinimumSize = New Size(480, 520)
        Me.Font = Theme.UiFont(9.0F)
        Me.DoubleBuffered = True
        Me.KeyPreview = True
        Me.BackColor = Theme.CardColor
        Me.Padding = New Padding(0)

        AddHandler Me.KeyDown,
            Sub(sender, e)
                If e.KeyCode = Keys.Escape Then
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                End If
            End Sub

        AddHandler Me.Resize, Sub() ApplyRoundedCorners()

    End Sub

    Private Sub ApplyRoundedCorners()

        If Me.Width <= 0 OrElse Me.Height <= 0 Then Return

        Dim d As Integer = CornerRadius * 2

        Using path As New GraphicsPath()
            path.AddArc(0, 0, d, d, 180, 90)
            path.AddArc(Me.Width - d, 0, d, d, 270, 90)
            path.AddArc(Me.Width - d, Me.Height - d, d, d, 0, 90)
            path.AddArc(0, Me.Height - d, d, d, 90, 90)
            path.CloseFigure()

            Dim old As Region = Me.Region
            Me.Region = New Region(path)
            If old IsNot Nothing Then old.Dispose()
        End Using

    End Sub

    Protected Overrides Sub OnHandleCreated(e As EventArgs)

        MyBase.OnHandleCreated(e)
        ApplyRoundedCorners()

    End Sub


    ' ==========================================================
    ' MAIN INTERFACE
    ' ==========================================================

    Private Sub BuildInterface()

        card = New NeonCard()
        card.Dock = DockStyle.Fill
        card.Padding = New Padding(26, 22, 26, 16)
        Me.Controls.Add(card)

        BuildList()
        BuildHeader()
        BuildFooter()

        listPanel.BringToFront()

    End Sub


    ' ==========================================================
    ' HEADER
    ' ==========================================================

    Private Sub BuildHeader()

        headerPanel = New Panel()
        headerPanel.Dock = DockStyle.Top
        headerPanel.Height = 96
        headerPanel.BackColor = Color.Transparent

        lblTitle = New Label()
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(0, 0)
        lblTitle.Font = Theme.UiFont(18.0F, FontStyle.Bold)
        lblTitle.ForeColor = Theme.TextWhite
        lblTitle.BackColor = Color.Transparent
        lblTitle.Text = "REPAIR HISTORY"

        lblSubtitle = New Label()
        lblSubtitle.AutoSize = True
        lblSubtitle.Location = New Point(2, 36)
        lblSubtitle.Font = Theme.UiFont(9.0F)
        lblSubtitle.ForeColor = Theme.TextMuted
        lblSubtitle.BackColor = Color.Transparent
        lblSubtitle.Text = "Complete repair records for this customer"

        lblCustomer = New Label()
        lblCustomer.AutoSize = True
        lblCustomer.Location = New Point(2, 62)
        lblCustomer.Font = Theme.UiFont(9.0F, FontStyle.Bold)
        lblCustomer.ForeColor = Theme.NeonCyan
        lblCustomer.BackColor = Color.Transparent
        lblCustomer.Text = "CUSTOMER  •  " & customerName.ToUpperInvariant()

        headerPanel.Controls.Add(lblTitle)
        headerPanel.Controls.Add(lblSubtitle)
        headerPanel.Controls.Add(lblCustomer)

        AddHandler headerPanel.MouseDown, AddressOf DragWindow
        AddHandler lblTitle.MouseDown, AddressOf DragWindow
        AddHandler lblSubtitle.MouseDown, AddressOf DragWindow
        AddHandler lblCustomer.MouseDown, AddressOf DragWindow

        dividerPanel = New Panel()
        dividerPanel.Dock = DockStyle.Top
        dividerPanel.Height = 1
        dividerPanel.BackColor = Theme.LineColor

        card.Controls.Add(dividerPanel)
        card.Controls.Add(headerPanel)

    End Sub

    Private Sub DragWindow(sender As Object, e As MouseEventArgs)

        If e.Button = MouseButtons.Left Then
            ReleaseCapture()
            SendMessage(Me.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0)
        End If

    End Sub


    ' ==========================================================
    ' LIST (scrollable stack of repair cards)
    ' ==========================================================

    Private Sub BuildList()

        listPanel = New Panel()
        listPanel.Dock = DockStyle.Fill
        listPanel.AutoScroll = True
        listPanel.BackColor = Theme.CardColor
        listPanel.Padding = New Padding(0, 16, 0, 0)

        lblEmpty = New Label()
        lblEmpty.Dock = DockStyle.Fill
        lblEmpty.TextAlign = ContentAlignment.MiddleCenter
        lblEmpty.Font = Theme.UiFont(10.5F)
        lblEmpty.ForeColor = Theme.TextMuted
        lblEmpty.BackColor = Theme.CardColor
        lblEmpty.Text = "No repair records found for this customer."
        lblEmpty.Visible = False

        listPanel.Controls.Add(lblEmpty)
        card.Controls.Add(listPanel)

    End Sub


    ' ==========================================================
    ' FOOTER
    ' ==========================================================

    Private Sub BuildFooter()

        footerPanel = New Panel()
        footerPanel.Dock = DockStyle.Bottom
        footerPanel.Height = 58
        footerPanel.BackColor = Color.Transparent
        footerPanel.Padding = New Padding(0, 14, 0, 0)

        btnClose = New NeonButton()
        btnClose.Text = "CLOSE"
        btnClose.Filled = False
        btnClose.Accent = Theme.NeonPurple
        btnClose.Dock = DockStyle.Right
        btnClose.Width = 110
        AddHandler btnClose.Click, AddressOf btnClose_Click

        lblCount = New Label()
        lblCount.Dock = DockStyle.Fill
        lblCount.AutoSize = False
        lblCount.TextAlign = ContentAlignment.MiddleLeft
        lblCount.ForeColor = Theme.TextMuted
        lblCount.BackColor = Color.Transparent
        lblCount.Font = Theme.UiFont(8.5F)
        lblCount.Text = "Loading repair records..."

        footerPanel.Controls.Add(lblCount)
        footerPanel.Controls.Add(btnClose)

        card.Controls.Add(footerPanel)

    End Sub


    ' ==========================================================
    ' LOAD REPAIR HISTORY
    ' ==========================================================

   Private Sub LoadRepairHistory()

    Try

        Dim query As IQueryable(Of Repair) =
            db.Repairs.
                AsNoTracking().
                Include(Function(r) r.Technician).
                Where(Function(r) r.CustomerId = customerId)

        If AppSession.IsTechnician Then
            Dim techId As Integer = AppSession.CurrentTechnicianId
            query = query.Where(
                Function(r) r.TechnicianId.HasValue AndAlso
                            r.TechnicianId.Value = techId)
        End If

        Dim repairs As List(Of Repair) =
            query.OrderByDescending(Function(r) r.RepairId).ToList()

        listPanel.SuspendLayout()

        ' dock-top stacks last-added first, so add in reverse
        For i As Integer = repairs.Count - 1 To 0 Step -1
            listPanel.Controls.Add(New RepairItemCard(repairs(i)))
        Next

        listPanel.ResumeLayout(True)

        Dim total As Integer = repairs.Count
        lblEmpty.Visible = (total = 0)

        If total = 0 Then
            lblCount.Text = "No repair records"
        Else
            Dim finishedTotal As Decimal =
                repairs.
                    Where(Function(r)
                              Return String.Equals(r.Status, "Completed", StringComparison.OrdinalIgnoreCase) OrElse
                                     String.Equals(r.Status, "Delivered", StringComparison.OrdinalIgnoreCase)
                          End Function).
                    Sum(Function(r) r.Cost)

            lblCount.Text =
                total.ToString() & If(total = 1, " repair", " repairs") &
                "  •  Finished total: " & Theme.FormatPeso(finishedTotal)
        End If

    Catch ex As Exception

        MessageBox.Show(
            "Unable to load repair history." & Environment.NewLine & Environment.NewLine & ex.Message,
            "Repair History",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error)

        lblCount.Text = "Failed to load repair records"

    End Try

End Sub


    ' ==========================================================
    ' CLOSE
    ' ==========================================================

    Private Sub btnClose_Click(sender As Object, e As EventArgs)

        Me.DialogResult = DialogResult.Cancel
        Me.Close()

    End Sub


    ' ==========================================================
    ' REPAIR ITEM CARD (custom painted, no table)
    ' ==========================================================

    Private Class RepairItemCard
        Inherits Control

        Private Const GapBottom As Integer = 10
        Private Const PadX As Integer = 20
        Private Const PadTop As Integer = 16
        Private Const PadBottom As Integer = 16
        Private Const InfoRowHeight As Integer = 46

        Private ReadOnly titleText As String
        Private ReadOnly subtitleText As String
        Private ReadOnly statusText As String
        Private ReadOnly dateText As String
        Private ReadOnly costText As String
        Private ReadOnly info As New List(Of Tuple(Of String, String))
        Private ReadOnly sections As New List(Of Tuple(Of String, String))
        Private ReadOnly accent As Color

        Private ReadOnly fTitle As Font
        Private ReadOnly fSub As Font
        Private ReadOnly fLabel As Font
        Private ReadOnly fBody As Font
        Private ReadOnly fPill As Font
        Private ReadOnly fFoot As Font
        Private ReadOnly fCost As Font

        Private Const TextFlags As TextFormatFlags =
            TextFormatFlags.WordBreak Or TextFormatFlags.NoPadding Or TextFormatFlags.Left Or TextFormatFlags.Top


        Public Sub New(repair As Object)

            Me.SetStyle(ControlStyles.UserPaint Or
                        ControlStyles.AllPaintingInWmPaint Or
                        ControlStyles.OptimizedDoubleBuffer Or
                        ControlStyles.ResizeRedraw, True)

            fTitle = Theme.UiFont(11.0F, FontStyle.Bold)
            fSub = Theme.UiFont(8.5F)
            fLabel = Theme.UiFont(7.5F, FontStyle.Bold)
            fBody = Theme.UiFont(9.0F)
            fPill = Theme.UiFont(8.0F, FontStyle.Bold)
            fFoot = Theme.UiFont(8.5F)
            fCost = Theme.UiFont(10.0F, FontStyle.Bold)

            Me.Dock = DockStyle.Top
            Me.Height = 150
            Me.BackColor = Theme.CardColor

            ' ---- basic values ----
            Dim brand As String = ToStr(Pick(repair, "Brand"))
            Dim model As String = ToStr(Pick(repair, "Model"))
            Dim serial As String = ToStr(Pick(repair, "SerialNumber"))
            Dim device As String = ToStr(Pick(repair, "DeviceType"))
            Dim id As String = ToStr(Pick(repair, "RepairId"))

            ' ---- title and subtitle ----
            Dim name As String = device

            If name.Length = 0 Then name = (brand & " " & model).Trim()
            If name.Length = 0 Then name = "Repair"

            titleText = name
            subtitleText = "REPAIR #" & id
            statusText = ToStr(Pick(repair, "Status")).ToUpperInvariant()

            ' ---- brand / model / serial number ----
            info.Add(Tuple.Create("BRAND", If(brand.Length > 0, brand, "—")))
            info.Add(Tuple.Create("MODEL", If(model.Length > 0, model, "—")))
            info.Add(Tuple.Create("SERIAL NUMBER", If(serial.Length > 0, serial, "—")))

            ' ---- problem, repair done, technician (hidden when empty) ----
            Dim problem As String = ToStr(Pick(repair, "ProblemDescription"))
            Dim repairDesc As String = ToStr(Pick(repair, "RepairDescription"))
            Dim techName As String = ToStr(Pick(Pick(repair, "Technician"), "TechnicianName"))

            If problem.Length > 0 Then sections.Add(Tuple.Create("PROBLEM", problem))
            If repairDesc.Length > 0 Then sections.Add(Tuple.Create("REPAIR DONE", repairDesc))
            If techName.Length > 0 Then sections.Add(Tuple.Create("TECHNICIAN", techName))

            ' ---- dates ----
            Dim received As Object = Pick(repair, "RepairDate")
            Dim completed As Object = Pick(repair, "CompletionDate")

            Dim d As String = ""

            If TypeOf received Is DateTime Then
                d = "Received " & CType(received, DateTime).ToString("MMM dd, yyyy")
            End If

            If TypeOf completed Is DateTime Then
                If d.Length > 0 Then d &= "  •  "
                d &= "Completed " & CType(completed, DateTime).ToString("MMM dd, yyyy")
            End If

                         dateText = d
' ---- cost (only for completed / delivered repairs) ----
Dim costValue As Object = Pick(repair, "Cost")
Dim isFinished As Boolean =
    (statusText = "COMPLETED" OrElse statusText = "DELIVERED")

If isFinished AndAlso costValue IsNot Nothing AndAlso
   Convert.ToDecimal(costValue) > 0D Then
    costText = "Total  " & Theme.FormatPeso(Convert.ToDecimal(costValue))
Else
    costText = ""
End If

accent = StatusColor(statusText)

        End Sub


        ' ---- helpers ----

        Private Shared Function Pick(source As Object, ParamArray names() As String) As Object

            If source Is Nothing Then Return Nothing

            For Each n As String In names
                Dim p As PropertyInfo = source.GetType().GetProperty(
                    n, BindingFlags.Public Or BindingFlags.Instance Or BindingFlags.IgnoreCase)

                If p IsNot Nothing Then
                    Dim v As Object = p.GetValue(source)
                    If v IsNot Nothing Then Return v
                End If
            Next

            Return Nothing

        End Function

        Private Shared Function ToStr(value As Object) As String

            If value Is Nothing Then Return ""
            Return value.ToString().Trim()

        End Function

        Private Shared Function StatusColor(status As String) As Color

            Dim key As String = status.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant()

            Select Case key

                Case "completed", "done", "repaired", "released", "delivered"
                    Return Theme.NeonGreen
                                Case "inprogress", "ongoing", "repairing"
                    Return Theme.NeonCyan
                Case "waitingforparts", "pending", "onhold"
                    Return Theme.NeonYellow
                Case "cancelled", "canceled", "failed"
                    Return Theme.NeonRed
                Case Else
                    Return Theme.NeonPurple
            End Select

        End Function


        ' ---- layout ----

        Private Function TextWidth() As Integer

            Return Math.Max(40, Me.Width - 1 - (PadX * 2))

        End Function

        Private Function ComputeHeight() As Integer

            If fBody Is Nothing Then Return Me.Height

            Dim h As Integer = PadTop + 46 + InfoRowHeight

            For Each s In sections
                Dim sz As Size = TextRenderer.MeasureText(
                    s.Item2, fBody, New Size(TextWidth(), 0), TextFlags)

                h += 16 + sz.Height + 10
            Next

            h += 14 + 20 + PadBottom + GapBottom

            Return h

        End Function

        Protected Overrides Sub OnResize(e As EventArgs)

            MyBase.OnResize(e)

            Dim h As Integer = ComputeHeight()

            If Me.Height <> h Then Me.Height = h

        End Sub


        ' ---- paint ----

        Protected Overrides Sub OnPaint(e As PaintEventArgs)

            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias

            g.Clear(Theme.CardColor)

            Dim body As New Rectangle(0, 0, Me.Width - 1, Me.Height - GapBottom - 1)

            Dim fill As Color = Theme.Lerp(Theme.CardColor, Theme.NeonPurple, 0.08F)
            Dim border As Color = Theme.Lerp(Theme.LineColor, Theme.NeonPurple, 0.25F)

            Using path As GraphicsPath = RoundRect(body, 14)

                Using b As New SolidBrush(fill)
                    g.FillPath(b, path)
                End Using

                ' accent strip on the left edge
                Dim oldClip As Region = g.Clip
                g.SetClip(path, CombineMode.Intersect)

                Using b As New SolidBrush(accent)
                    g.FillRectangle(b, 0, 0, 4, body.Height + 1)
                End Using

                g.Clip = oldClip

                Using p As New Pen(border, 1.0F)
                    g.DrawPath(p, path)
                End Using

            End Using

            Dim x As Integer = PadX
            Dim right As Integer = body.Right - PadX
            Dim y As Integer = PadTop

            ' status pill
            Dim pillW As Integer = 0

            If statusText.Length > 0 Then

                Dim ts As Size = TextRenderer.MeasureText(statusText, fPill, New Size(400, 30), TextFormatFlags.NoPadding)
                pillW = ts.Width + 24

                Dim pill As New Rectangle(right - pillW, y, pillW, 24)

                Using path As GraphicsPath = RoundRect(pill, 12)
                    Using b As New SolidBrush(Color.FromArgb(38, accent))
                        g.FillPath(b, path)
                    End Using
                    Using p As New Pen(Color.FromArgb(120, accent), 1.0F)
                        g.DrawPath(p, path)
                    End Using
                End Using

                TextRenderer.DrawText(g, statusText, fPill, pill, accent,
                    TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)

            End If

            ' title
            Dim titleRect As New Rectangle(x, y, right - x - If(pillW > 0, pillW + 12, 0), 24)

            TextRenderer.DrawText(g, titleText, fTitle, titleRect, Theme.TextWhite,
                TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)

            y += 24

            ' subtitle
            TextRenderer.DrawText(g, subtitleText, fSub, New Rectangle(x, y, right - x, 18), Theme.TextMuted,
                TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)

            y += 22

            ' brand / model / serial number row
            Dim colWidth As Integer = (right - x) \ info.Count
            Dim cx As Integer = x

            For Each item In info

                TextRenderer.DrawText(g, item.Item1, fLabel, New Rectangle(cx, y + 6, colWidth - 8, 14), Theme.NeonCyan,
                    TextFormatFlags.Left Or TextFormatFlags.NoPadding)

                TextRenderer.DrawText(g, item.Item2, fBody, New Rectangle(cx, y + 22, colWidth - 8, 18), Theme.TextWhite,
                    TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)

                cx += colWidth

            Next

            y += InfoRowHeight

            ' problem / repair done / technician (only when they have text)
            For Each s In sections

                TextRenderer.DrawText(g, s.Item1, fLabel, New Rectangle(x, y, right - x, 14), Theme.NeonCyan,
                    TextFormatFlags.Left Or TextFormatFlags.NoPadding)

                y += 16

                Dim sz As Size = TextRenderer.MeasureText(
                    s.Item2, fBody, New Size(TextWidth(), 0), TextFlags)

                TextRenderer.DrawText(g, s.Item2, fBody, New Rectangle(x, y, TextWidth(), sz.Height), Theme.TextWhite, TextFlags)

                y += sz.Height + 10

            Next

            ' divider
            y += 2

            Using p As New Pen(border, 1.0F)
                g.DrawLine(p, x, y, right, y)
            End Using

            y += 12

            ' footer: dates left, cost right
            If dateText.Length > 0 Then
                TextRenderer.DrawText(g, dateText, fFoot, New Rectangle(x, y, right - x, 20), Theme.TextMuted,
                    TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
            End If

            If costText.Length > 0 Then
                TextRenderer.DrawText(g, costText, fCost, New Rectangle(x, y, right - x, 20), Theme.NeonCyan,
                    TextFormatFlags.Right Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
            End If

        End Sub

        Private Shared Function RoundRect(r As Rectangle, radius As Integer) As GraphicsPath

            Dim d As Integer = radius * 2
            Dim path As New GraphicsPath()

            path.AddArc(r.X, r.Y, d, d, 180, 90)
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90)
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
            path.CloseFigure()

            Return path

        End Function
        Protected Overrides Sub Dispose(disposing As Boolean)

            ' fonts come from the shared theme, so they must not be disposed here
            MyBase.Dispose(disposing)

        End Sub
    End Class

End Class