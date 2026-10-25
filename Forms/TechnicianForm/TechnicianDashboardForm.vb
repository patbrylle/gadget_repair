Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Forms

    ' what a technician sees first: their own jobs at a glance
    Public Class TechnicianDashboardForm
        Inherits UserControl

        Public Event NavigateRequested(menuName As String)

        Private ReadOnly repairController As RepairController

        Private headerPanel As Panel
        Private lblSub As Label
        Private btnOpen As NeonButton
        Private statsTable As TableLayoutPanel
        Private statAssigned As StatCard
        Private statPending As StatCard
        Private statActive As StatCard
        Private statWaiting As StatCard
        Private statDone As StatCard
        Private gridCard As NeonCard
        Private grid As SmoothDataGridView
        Private lblCount As Label

        Public Sub New(database As ApplicationDbContext)

            repairController = New RepairController(database)

            BuildInterface()

            AddHandler Me.Load, Sub(s As Object, e As EventArgs) LoadData()

        End Sub

        Private Sub BuildInterface()

            Me.Dock = DockStyle.Fill
            Me.BackColor = Theme.BackgroundColor
            Me.Padding = New Padding(30, 28, 30, 26)
            Me.Font = Theme.UiFont(9.0F)
            Me.DoubleBuffered = True

            ' ---------- header ----------
            headerPanel = New Panel()
            headerPanel.Dock = DockStyle.Top
            headerPanel.Height = 84
            headerPanel.BackColor = Theme.BackgroundColor

            Dim lblHeading As New Label()
            lblHeading.Text = "My Work"
            lblHeading.AutoSize = True
            lblHeading.Location = New Point(0, 0)
            lblHeading.Font = Theme.UiFont(23.0F, FontStyle.Bold)
            lblHeading.ForeColor = Theme.TextWhite

            lblSub = New Label()
            lblSub.AutoSize = True
            lblSub.Location = New Point(2, 42)
            lblSub.Font = Theme.UiFont(9.5F)
            lblSub.ForeColor = Theme.TextMuted

            btnOpen = New NeonButton()
            btnOpen.Text = "OPEN MY REPAIRS"
            btnOpen.Filled = True
            btnOpen.Size = New Size(175, 44)
            btnOpen.Top = 2

            AddHandler btnOpen.Click,
                Sub(s As Object, e As EventArgs) RaiseEvent NavigateRequested("Repairs")

            headerPanel.Controls.Add(lblHeading)
            headerPanel.Controls.Add(lblSub)
            headerPanel.Controls.Add(btnOpen)

            AddHandler headerPanel.Resize,
                Sub(s As Object, e As EventArgs)
                    btnOpen.Left = headerPanel.ClientSize.Width - btnOpen.Width
                End Sub

            ' ---------- stat cards ----------
            statsTable = New TableLayoutPanel()
            statsTable.Dock = DockStyle.Top
            statsTable.Height = 108
            statsTable.ColumnCount = 5
            statsTable.RowCount = 1
            statsTable.BackColor = Theme.BackgroundColor
            statsTable.Margin = New Padding(0)

            For i As Integer = 1 To 5
                statsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20.0F))
            Next

            statsTable.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

            statAssigned = NewCard("ASSIGNED TO ME", Theme.NeonPurple, New Padding(0, 0, 10, 16))
            statPending = NewCard("PENDING", Color.Gold, New Padding(5, 0, 5, 16))
            statActive = NewCard("IN PROGRESS", Theme.NeonCyan, New Padding(5, 0, 5, 16))
            statWaiting = NewCard("WAITING FOR PARTS", Color.Orange, New Padding(5, 0, 5, 16))
            statDone = NewCard("DONE THIS WEEK", Theme.NeonGreen, New Padding(5, 0, 0, 16))

            statsTable.Controls.Add(statAssigned, 0, 0)
            statsTable.Controls.Add(statPending, 1, 0)
            statsTable.Controls.Add(statActive, 2, 0)
            statsTable.Controls.Add(statWaiting, 3, 0)
            statsTable.Controls.Add(statDone, 4, 0)

            ' ---------- open jobs list ----------
            gridCard = New NeonCard()
            gridCard.Dock = DockStyle.Fill
            gridCard.Padding = New Padding(16, 14, 16, 10)

            grid = New SmoothDataGridView()

            With grid
                .Dock = DockStyle.Fill
                .AutoGenerateColumns = False
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                .ReadOnly = True
                .AllowUserToAddRows = False
                .AllowUserToDeleteRows = False
                .AllowUserToResizeRows = False
                .AllowUserToResizeColumns = False
                .MultiSelect = False
                .RowHeadersVisible = False
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect
                .BackgroundColor = Theme.CardColor
                .GridColor = Theme.LineColor
                .BorderStyle = BorderStyle.None
                .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
                .ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
                .ColumnHeadersHeightSizeMode =
                    DataGridViewColumnHeadersHeightSizeMode.DisableResizing
                .EnableHeadersVisualStyles = False
                .ColumnHeadersHeight = 44
                .RowTemplate.Height = 52
                .DefaultCellStyle.NullValue = ""
            End With

            With grid.ColumnHeadersDefaultCellStyle
                .BackColor = Theme.CardColor
                .ForeColor = Theme.TextMuted
                .SelectionBackColor = Theme.CardColor
                .SelectionForeColor = Theme.TextMuted
                .Font = Theme.UiFont(8.0F, FontStyle.Bold)
                .Padding = New Padding(12, 0, 0, 0)
            End With

            With grid.DefaultCellStyle
                .BackColor = Theme.CardColor
                .ForeColor = Theme.TextWhite
                .SelectionBackColor = Theme.CardColor
                .SelectionForeColor = Theme.TextWhite
                .Font = Theme.UiFont(9.5F)
                .Padding = New Padding(12, 0, 0, 0)
            End With

            AddColumn("colTicket", "TICKET", 12.0F)
            AddColumn("colDevice", "DEVICE", 34.0F)
            AddColumn("colDate", "RECEIVED", 20.0F)
            AddColumn("colStatus", "STATUS", 22.0F)

            AddHandler grid.CellPainting, AddressOf Grid_CellPainting
            AddHandler grid.CellDoubleClick,
                Sub(s As Object, e As DataGridViewCellEventArgs)
                    If e.RowIndex >= 0 Then RaiseEvent NavigateRequested("Repairs")
                End Sub

            lblCount = New Label()
            lblCount.Dock = DockStyle.Bottom
            lblCount.Height = 30
            lblCount.TextAlign = ContentAlignment.MiddleLeft
            lblCount.ForeColor = Theme.TextMuted
            lblCount.Font = Theme.UiFont(8.5F)
            lblCount.Padding = New Padding(6, 0, 0, 0)

            gridCard.Controls.Add(grid)
            gridCard.Controls.Add(lblCount)

            Me.Controls.Add(gridCard)
            Me.Controls.Add(statsTable)
            Me.Controls.Add(headerPanel)

        End Sub

        Private Function NewCard(caption As String, accent As Color, margin As Padding) As StatCard

            Dim card As New StatCard()
            card.Caption = caption
            card.Accent = accent
            card.Dock = DockStyle.Fill
            card.Margin = margin
            card.Value = "0"

            Return card

        End Function

        Private Sub AddColumn(name As String, header As String, weight As Single)

            Dim c As New DataGridViewTextBoxColumn()
            c.Name = name
            c.HeaderText = header
            c.FillWeight = weight
            c.SortMode = DataGridViewColumnSortMode.NotSortable
            grid.Columns.Add(c)

        End Sub

        Private Sub LoadData()

            Try

                ' GetRepairs already limits a technician to their own repairs
                Dim all As List(Of Repair) = repairController.GetRepairs("", "All Status")

                Dim weekStart As DateTime =
                    DateTime.Today.AddDays(-((CInt(DateTime.Today.DayOfWeek) + 6) Mod 7))

                statAssigned.Value =
                    all.Where(Function(r As Repair) r.Status <> "Cancelled").Count().ToString()

                statPending.Value =
                    all.Where(Function(r As Repair) r.Status = "Pending").Count().ToString()

                statActive.Value =
                    all.Where(Function(r As Repair) r.Status = "In Progress").Count().ToString()

                statWaiting.Value =
                    all.Where(Function(r As Repair) r.Status = "Waiting for Parts").Count().ToString()

                statDone.Value =
                    all.Where(Function(r As Repair)
                                  Return (r.Status = "Completed" OrElse r.Status = "Delivered") AndAlso
                                         r.CompletionDate.HasValue AndAlso
                                         r.CompletionDate.Value >= weekStart
                              End Function).Count().ToString()

                ' open jobs: not finished, oldest first
                Dim openJobs As List(Of Repair) =
                    all.Where(Function(r As Repair) Not RepairController.IsFinal(r.Status) AndAlso
                                                    r.Status <> "Completed").
                        OrderBy(Function(r As Repair) r.RepairDate).
                        Take(10).
                        ToList()

                grid.Rows.Clear()

                For Each r As Repair In openJobs

                    Dim device As String =
                        (If(r.Brand, "") & " " & If(r.Model, "")).Trim()

                    If device = "" Then device = If(r.DeviceType, "Not provided")

                    grid.Rows.Add(
                        "#" & r.RepairId.ToString(),
                        device,
                        r.RepairDate.ToString("MMM dd, yyyy"),
                        r.Status)

                Next

                lblSub.Text =
                    "Hello " & If(String.IsNullOrWhiteSpace(AppSession.DisplayName), "", AppSession.DisplayName & ", ") &
                    "these are your open jobs, oldest first"

                lblCount.Text =
                    If(openJobs.Count = 0,
                       "No open jobs. You are all caught up.",
                       "Showing " & openJobs.Count.ToString() & " open job(s)  |  double-click to open Repairs")

            Catch ex As Exception

                lblSub.Text = "Unable to load your jobs: " & ex.Message

            End Try

        End Sub

        Private Sub Grid_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)

            If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

            If grid.Columns(e.ColumnIndex).Name = "colStatus" Then

                NeonGridPaint.PaintPill(e, False,
                    Function(s As String) Theme.StatusColor(s))

            Else

                NeonGridPaint.PaintBase(e, False)
                e.Paint(e.CellBounds, DataGridViewPaintParts.ContentForeground)

            End If

        End Sub

    End Class

End Namespace