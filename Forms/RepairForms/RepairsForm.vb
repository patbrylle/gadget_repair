Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Forms

    ' double buffered grid
    ' (if another form also declares this class, keep only one copy)
    Friend Class SmoothDataGridView
        Inherits DataGridView

        Public Sub New()

            DoubleBuffered = True

            SetStyle(
                ControlStyles.OptimizedDoubleBuffer Or
                ControlStyles.AllPaintingInWmPaint Or
                ControlStyles.UserPaint,
                True)

            UpdateStyles()

        End Sub

    End Class


    Partial Public Class RepairsForm
        Inherits Form

        Private ReadOnly db As ApplicationDbContext
        Private ReadOnly repairController As RepairController

        Private headerPanel As Panel
        Private lblHeading As Label
        Private lblSubheading As Label
        Private btnNew As NeonButton

        Private statsTable As TableLayoutPanel
        Private statTotal As StatCard
        Private statPending As StatCard
        Private statActive As StatCard
        Private statWaiting As StatCard
        Private statDone As StatCard

        Private toolbarPanel As Panel
        Private searchHost As NeonInput
        Private txtSearch As TextBox
        Private filterHost As NeonInput
        Private cboFilter As ComboBox

        Private gridCard As NeonCard
        Private grid As SmoothDataGridView
        Private lblCount As Label

        Private loading As Boolean = False

        Private Const SearchHint As String = "Search customer, device or serial..."
        Private searchIsHint As Boolean = True

        ' hover: whole row (tableHoverRow) and the VIEW button (hoverRow/hoverAction)
        Private hoverRow As Integer = -1
        Private hoverAction As Integer = 0
        Private tableHoverRow As Integer = -1

        Private ReadOnly StatusList As String() = RepairController.StatusList

        ' ============================================================
        ' CONSTRUCTOR
        ' ============================================================

        Public Sub New(database As ApplicationDbContext)

            InitializeComponent()

            db = database
            repairController = New RepairController(db)

            loading = True
            BuildInterface()
            loading = False

            AddHandler Me.Load, AddressOf RepairsForm_Load

        End Sub

        Private Sub RepairsForm_Load(sender As Object, e As EventArgs)

            If repairController Is Nothing Then

                MessageBox.Show(
                    "Repair controller is not initialized.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

                Return

            End If

            LoadRepairs()

        End Sub

        Private Sub BuildInterface()

            Me.Dock = DockStyle.Fill
            Me.BackColor = Theme.BackgroundColor
            Me.Padding = New Padding(30, 28, 30, 26)
            Me.Font = Theme.UiFont(9.0F)
            Me.DoubleBuffered = True

            BuildHeader()
            BuildStatistics()
            BuildToolbar()
            BuildGrid()

            Me.Controls.Add(gridCard)
            Me.Controls.Add(toolbarPanel)
            Me.Controls.Add(statsTable)
            Me.Controls.Add(headerPanel)

        End Sub

        ' ============================================================
        ' HEADER
        ' ============================================================

        Private Sub BuildHeader()

            headerPanel = New Panel()
            headerPanel.Dock = DockStyle.Top
            headerPanel.Height = 84
            headerPanel.BackColor = Theme.BackgroundColor

            lblHeading = New Label()
            lblHeading.Text = "Repair Tickets"
            lblHeading.AutoSize = True
            lblHeading.Location = New Point(0, 0)
            lblHeading.Font = Theme.UiFont(23.0F, FontStyle.Bold)
            lblHeading.ForeColor = Theme.TextWhite

            lblSubheading = New Label()
            lblSubheading.Text = "Create, track and update device repairs"
            lblSubheading.AutoSize = True
            lblSubheading.Location = New Point(2, 42)
            lblSubheading.Font = Theme.UiFont(9.5F)
            lblSubheading.ForeColor = Theme.TextMuted

            btnNew = New NeonButton()
            btnNew.Text = "+  ADD REPAIR"
            btnNew.Filled = True
            btnNew.Accent = Theme.NeonPurple
            btnNew.Size = New Size(165, 44)
            btnNew.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            btnNew.Top = 2

            AddHandler btnNew.Click, AddressOf btnNew_Click

            headerPanel.Controls.Add(lblHeading)
            headerPanel.Controls.Add(lblSubheading)
            headerPanel.Controls.Add(btnNew)

            btnNew.Visible = AppSession.CanManageRepairs

            AddHandler headerPanel.Resize,
                Sub(sender As Object, e As EventArgs)

                    btnNew.Left = headerPanel.ClientSize.Width - btnNew.Width

                End Sub

        End Sub

        ' ============================================================
        ' STATISTICS
        ' ============================================================

        Private Sub BuildStatistics()

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

            statTotal = NewStatCard("TOTAL TICKETS", Theme.NeonPurple, New Padding(0, 0, 10, 16))
            statPending = NewStatCard("PENDING", Color.Gold, New Padding(5, 0, 5, 16))
            statActive = NewStatCard("IN PROGRESS", Theme.NeonCyan, New Padding(5, 0, 5, 16))
            statWaiting = NewStatCard("WAITING FOR PARTS", Color.Orange, New Padding(5, 0, 5, 16))
            statDone = NewStatCard("COMPLETED", Theme.NeonGreen, New Padding(5, 0, 0, 16))

            statsTable.Controls.Add(statTotal, 0, 0)
            statsTable.Controls.Add(statPending, 1, 0)
            statsTable.Controls.Add(statActive, 2, 0)
            statsTable.Controls.Add(statWaiting, 3, 0)
            statsTable.Controls.Add(statDone, 4, 0)

        End Sub

        Private Function NewStatCard(
            caption As String,
            accent As Color,
            margin As Padding) As StatCard

            Dim card As New StatCard()

            card.Caption = caption
            card.Accent = accent
            card.Dock = DockStyle.Fill
            card.Margin = margin

            Return card

        End Function

        ' ============================================================
        ' TOOLBAR
        ' ============================================================

        Private Sub BuildToolbar()

            toolbarPanel = New Panel()
            toolbarPanel.Dock = DockStyle.Top
            toolbarPanel.Height = 68
            toolbarPanel.BackColor = Theme.BackgroundColor

            ' search (NeonInput centers the textbox vertically)
            searchHost = New NeonInput()
            searchHost.Location = New Point(0, 6)
            searchHost.Size = New Size(420, 46)
            searchHost.SearchIcon = True
            searchHost.Padding = New Padding(42, 0, 14, 0)

            txtSearch = New TextBox()
            txtSearch.BackColor = Theme.InputColor
            txtSearch.ForeColor = Theme.TextMuted
            txtSearch.BorderStyle = BorderStyle.None
            txtSearch.Font = Theme.UiFont(9.5F)
            txtSearch.Text = SearchHint

            searchHost.Controls.Add(txtSearch)

            AddHandler txtSearch.Enter, AddressOf Search_Enter
            AddHandler txtSearch.Leave, AddressOf Search_Leave
            AddHandler txtSearch.TextChanged, AddressOf Search_TextChanged

            ' filter (NeonComboBox draws its own dark list and chevron)
            filterHost = New NeonInput()
            filterHost.Location = New Point(0, 6)
            filterHost.Size = New Size(220, 46)
            filterHost.Padding = New Padding(12, 0, 8, 0)

            cboFilter = New NeonComboBox()
            cboFilter.Font = Theme.UiFont(9.5F)
            cboFilter.Items.Add("All Status")

            For Each status As String In StatusList
                cboFilter.Items.Add(status)
            Next

            cboFilter.SelectedIndex = 0

            AddHandler cboFilter.SelectedIndexChanged, AddressOf Filter_Changed

            filterHost.Controls.Add(cboFilter)

            toolbarPanel.Controls.Add(searchHost)
            toolbarPanel.Controls.Add(filterHost)

            AddHandler toolbarPanel.Resize, AddressOf ResizeRepairToolbar

            ResizeRepairToolbar(toolbarPanel, EventArgs.Empty)

        End Sub

        Private Sub ResizeRepairToolbar(sender As Object, e As EventArgs)

            If toolbarPanel Is Nothing OrElse
               searchHost Is Nothing OrElse
               filterHost Is Nothing Then
                Return
            End If

            If toolbarPanel.ClientSize.Width <= 0 Then Return

            filterHost.Left = toolbarPanel.ClientSize.Width - filterHost.Width

            Dim spacing As Integer = 22
            Dim availableWidth As Integer = filterHost.Left - spacing
            Dim minimumWidth As Integer = 300
            Dim maximumWidth As Integer = 560

            Dim finalWidth As Integer =
                Math.Min(maximumWidth, Math.Max(minimumWidth, availableWidth))

            If toolbarPanel.ClientSize.Width <
               minimumWidth + filterHost.Width + spacing Then

                finalWidth = Math.Max(220, availableWidth)

            End If

            searchHost.Width = finalWidth

        End Sub

        ' ============================================================
        ' GRID
        ' ============================================================

        Private Sub BuildGrid()

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
                .AllowUserToOrderColumns = False
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
                .ColumnHeadersHeight = 48
                .RowTemplate.Height = 68
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

            With grid.AlternatingRowsDefaultCellStyle

                .BackColor = Theme.Lerp(Theme.CardColor, Theme.BackgroundColor, 0.16F)
                .ForeColor = Theme.TextWhite
                .SelectionBackColor = Theme.CardColor
                .SelectionForeColor = Theme.TextWhite

            End With

            Dim colId As New DataGridViewTextBoxColumn()
            colId.Name = "colId"
            colId.HeaderText = "ID"
            colId.Visible = False

            Dim colCustomer As New DataGridViewTextBoxColumn()
            colCustomer.Name = "colCustomer"
            colCustomer.HeaderText = "CUSTOMER"
            colCustomer.FillWeight = 22.0F

            Dim colDevice As New DataGridViewTextBoxColumn()
            colDevice.Name = "colDevice"
            colDevice.HeaderText = "DEVICE"
            colDevice.FillWeight = 20.0F

            Dim colTechnician As New DataGridViewTextBoxColumn()
            colTechnician.Name = "colTechnician"
            colTechnician.HeaderText = "ASSIGNED TECHNICIAN"
            colTechnician.FillWeight = 19.0F
            colTechnician.DefaultCellStyle.ForeColor = Theme.NeonPurple

            Dim colDate As New DataGridViewTextBoxColumn()
            colDate.Name = "colDate"
            colDate.HeaderText = "DATE"
            colDate.FillWeight = 13.0F
            colDate.DefaultCellStyle.ForeColor = Theme.TextMuted

            Dim colCost As New DataGridViewTextBoxColumn()
            colCost.Name = "colCost"
            colCost.HeaderText = "COST"
            colCost.FillWeight = 13.0F
            colCost.DefaultCellStyle.ForeColor = Theme.NeonGreen

            Dim colStatus As New DataGridViewTextBoxColumn()
            colStatus.Name = "colStatus"
            colStatus.HeaderText = "STATUS"
            colStatus.FillWeight = 16.0F

            Dim colActions As New DataGridViewTextBoxColumn()
            colActions.Name = "colActions"
            colActions.HeaderText = "ACTIONS"
            colActions.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
colActions.Width = If(AppSession.IsTechnician, 190, 130)
            colActions.Resizable = DataGridViewTriState.False

            grid.Columns.AddRange(
                New DataGridViewColumn() {
                    colId, colCustomer, colDevice, colTechnician,
                    colDate, colCost, colStatus, colActions})

            AddHandler grid.CellPainting, AddressOf Grid_CellPainting
            AddHandler grid.CellMouseMove, AddressOf Grid_CellMouseMove
            AddHandler grid.CellMouseLeave, AddressOf Grid_CellMouseLeave
            AddHandler grid.CellMouseClick, AddressOf Grid_CellMouseClick
            AddHandler grid.CellDoubleClick, AddressOf Grid_CellDoubleClick
            AddHandler grid.MouseEnter, AddressOf Grid_MouseEnter
            AddHandler grid.Scroll, AddressOf Grid_Scroll

            lblCount = New Label()
            lblCount.Dock = DockStyle.Bottom
            lblCount.Height = 32
            lblCount.TextAlign = ContentAlignment.MiddleLeft
            lblCount.ForeColor = Theme.TextMuted
            lblCount.Font = Theme.UiFont(8.5F)
            lblCount.Padding = New Padding(6, 0, 0, 0)

            gridCard.Controls.Add(grid)
            gridCard.Controls.Add(lblCount)

        End Sub

        ' take focus from the filter combo so the wheel scrolls the table
        Private Sub Grid_MouseEnter(sender As Object, e As EventArgs)

            If cboFilter IsNot Nothing AndAlso cboFilter.Focused Then
                grid.Focus()
            End If

        End Sub

        ' the mouse does not move while the wheel scrolls, so reset hover
        Private Sub Grid_Scroll(sender As Object, e As ScrollEventArgs)

            If e.ScrollOrientation <> ScrollOrientation.VerticalScroll Then Return

            ResetHover()
            grid.Invalidate()

        End Sub

        Private Sub ResetHover()

            tableHoverRow = -1
            hoverRow = -1
            hoverAction = 0

            If grid IsNot Nothing Then
                grid.Cursor = Cursors.Default
            End If

        End Sub

        ' ============================================================
        ' SEARCH / FILTER
        ' ============================================================

        Private Sub Search_Enter(sender As Object, e As EventArgs)

            If searchIsHint Then

                searchIsHint = False
                txtSearch.Text = ""
                txtSearch.ForeColor = Theme.TextWhite

            End If

        End Sub

        Private Sub Search_Leave(sender As Object, e As EventArgs)

            If String.IsNullOrWhiteSpace(txtSearch.Text) Then

                searchIsHint = True
                txtSearch.Text = SearchHint
                txtSearch.ForeColor = Theme.TextMuted

            End If

        End Sub

        Private Sub Search_TextChanged(sender As Object, e As EventArgs)

            If loading Then Return
            If searchIsHint Then Return

            LoadRepairs()

        End Sub

        Private Sub Filter_Changed(sender As Object, e As EventArgs)

            If loading Then Return

            LoadRepairs()

        End Sub

        ' ============================================================
        ' LOAD REPAIRS
        ' ============================================================

        Private Sub LoadRepairs()

            If repairController Is Nothing Then Return
            If grid Is Nothing Then Return
            If cboFilter Is Nothing Then Return
            If txtSearch Is Nothing Then Return

            loading = True

            Try

                grid.Rows.Clear()

                ' old hover indexes are invalid after the rows are rebuilt
                ResetHover()

                Dim searchText As String = ""

                If Not searchIsHint AndAlso
                   Not String.IsNullOrWhiteSpace(txtSearch.Text) Then

                    searchText = txtSearch.Text.Trim()

                End If

                Dim statusFilter As String = "All Status"

                If cboFilter.SelectedItem IsNot Nothing Then
                    statusFilter = cboFilter.SelectedItem.ToString()
                End If

                Dim repairs As List(Of Repair) =
                    repairController.GetRepairs(searchText, statusFilter)

                Dim customers As List(Of Customer) =
                    repairController.GetCustomers()

                For Each repair As Repair In repairs

                    Dim repairId As Integer = repair.RepairId

                    Dim customer As Customer =
                        customers.FirstOrDefault(
                            Function(c As Customer) c.CustomerId = repair.CustomerId)

                    Dim customerName As String = "Unknown Customer"

                    If customer IsNot Nothing Then
                        customerName = customer.CustomerName
                    End If

                    Dim rowIndex As Integer =
                        grid.Rows.Add(
                            repairId,
                            customerName & ChrW(10) & "TICKET #" & repairId.ToString(),
                            BuildDeviceText(repair),
                            GetTechnicianName(repair),
                            repair.RepairDate.ToString("MMM dd, yyyy"),
                            Money.Peso(repair.Cost),
                            repair.Status,
                            "")

                    grid.Rows(rowIndex).Tag = repairId

                Next

                ' statistics always use the unfiltered list
                UpdateStatistics(repairController.GetRepairs("", "All Status"))

                lblCount.Text = "Showing " & grid.Rows.Count.ToString() & " repair(s)"

            Catch ex As Exception

                MessageBox.Show(
                    "Unable to load repairs." &
                    Environment.NewLine & Environment.NewLine & ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

            Finally

                loading = False

            End Try

        End Sub

        ' line 1 = brand + model (the name), line 2 = device type
        Private Function BuildDeviceText(repair As Repair) As String

            Dim nameLine As String =
                (If(repair.Brand, "") & " " & If(repair.Model, "")).Trim()

            Dim typeLine As String = If(repair.DeviceType, "").Trim()

            If nameLine = "" Then
                nameLine = typeLine
                typeLine = ""
            End If

            If nameLine = "" Then nameLine = "Not provided"

            Return nameLine & ChrW(10) & typeLine

        End Function

        Private Function GetTechnicianName(repair As Repair) As String

            If repair Is Nothing OrElse
               repair.Technician Is Nothing OrElse
               String.IsNullOrWhiteSpace(repair.Technician.TechnicianName) Then

                Return "Unassigned"

            End If

            Return repair.Technician.TechnicianName

        End Function
        Private Sub UpdateStatistics(repairs As List(Of Repair))

            If repairs Is Nothing Then Return

            statTotal.Value = repairs.Count.ToString()

            statPending.Value =
                repairs.Where(Function(r As Repair) r.Status = "Pending").Count().ToString()

            statActive.Value =
                repairs.Where(Function(r As Repair) r.Status = "In Progress").Count().ToString()

            statWaiting.Value =
                repairs.Where(Function(r As Repair) r.Status = "Waiting for Parts").Count().ToString()

            statDone.Value =
                repairs.Where(
                    Function(r As Repair)
                        Return r.Status = "Completed" OrElse
                               r.Status = "Delivered"
                    End Function).Count().ToString()

        End Sub

        ' ============================================================
        ' ADD / VIEW
        ' ============================================================

        Private Sub btnNew_Click(sender As Object, e As EventArgs)

            OpenRepairModal(0)

        End Sub

        Private Sub OpenRepairModal(repairId As Integer)

            Using modal As New RepairEditForm(repairController, repairId)

                If modal.ShowDialog(Me) = DialogResult.OK Then
                    LoadRepairs()
                End If

            End Using

        End Sub

        Private Sub OpenViewModal(repairId As Integer)

            Using modal As New RepairViewForm(repairController, repairId)

                modal.ShowDialog(Me)

                If modal.Changed Then

                    Dim firstRow As Integer = grid.FirstDisplayedScrollingRowIndex

                    LoadRepairs()

                    ' keep the scroll position after an edit / status change
                    Try
                        If firstRow >= 0 AndAlso firstRow < grid.RowCount Then
                            grid.FirstDisplayedScrollingRowIndex = firstRow
                        End If
                    Catch
                    End Try

                End If

            End Using

        End Sub
        Private Sub OpenItemsModal(repairId As Integer)

    Dim repair As Repair = repairController.GetRepair(repairId)

    If repair Is Nothing Then Return

    Using modal As New RepairPartsForm(repairController.PartsController(), repair)
        modal.ShowDialog(Me)
    End Using

End Sub

        Private Sub Grid_CellDoubleClick(
            sender As Object,
            e As DataGridViewCellEventArgs)

            If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

            If grid.Columns(e.ColumnIndex).Name = "colActions" Then Return

            Dim row As DataGridViewRow = grid.Rows(e.RowIndex)

            If row.Tag Is Nothing Then Return

            OpenViewModal(Convert.ToInt32(row.Tag))

        End Sub

        ' ============================================================
        ' VIEW BUTTON GEOMETRY (one source for paint + mouse)
        ' ============================================================
Private Function ActionButtonRect(cell As Rectangle, index As Integer) As Rectangle

    Const padding As Integer = 8
    Const gap As Integer = 6
    Const buttonHeight As Integer = 32

    Dim count As Integer = If(AppSession.IsTechnician, 2, 1)
    Dim w As Integer = (cell.Width - padding * 2 - gap * (count - 1)) \ count

    Return New Rectangle(
        cell.X + padding + index * (w + gap),
        cell.Y + (cell.Height - buttonHeight) \ 2,
        w,
        buttonHeight)

End Function

' 0 = none, 1 = VIEW, 2 = ITEMS
Private Function HitAction(e As DataGridViewCellMouseEventArgs) As Integer

    If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return 0

    If grid.Columns(e.ColumnIndex).Name <> "colActions" Then Return 0

    Dim cell As New Rectangle(
        0, 0,
        grid.Columns(e.ColumnIndex).Width,
        grid.Rows(e.RowIndex).Height)

    Dim count As Integer = If(AppSession.IsTechnician, 2, 1)

    For i As Integer = 0 To count - 1
        If ActionButtonRect(cell, i).Contains(e.X, e.Y) Then Return i + 1
    Next

    Return 0

End Function
        ' ============================================================
        ' GRID MOUSE
        ' ============================================================

        Private Sub Grid_CellMouseMove(
            sender As Object,
            e As DataGridViewCellMouseEventArgs)

            Dim newRow As Integer = If(e.RowIndex >= 0, e.RowIndex, -1)
Dim newAction As Integer = HitAction(e)

            grid.Cursor = If(newAction <> 0, Cursors.Hand, Cursors.Default)

            If newRow <> tableHoverRow Then

                Dim oldRow As Integer = tableHoverRow

                tableHoverRow = newRow

                If oldRow >= 0 AndAlso oldRow < grid.RowCount Then
                    grid.InvalidateRow(oldRow)
                End If

                If newRow >= 0 AndAlso newRow < grid.RowCount Then
                    grid.InvalidateRow(newRow)
                End If

            End If

            If newRow <> hoverRow OrElse newAction <> hoverAction Then

                Dim oldActionRow As Integer = hoverRow

                hoverRow = newRow
                hoverAction = newAction

                InvalidateActionCell(oldActionRow)
                InvalidateActionCell(newRow)

            End If

        End Sub

        Private Sub Grid_CellMouseLeave(
            sender As Object,
            e As DataGridViewCellEventArgs)

            Dim oldRow As Integer = tableHoverRow

            ResetHover()

            If oldRow >= 0 AndAlso oldRow < grid.RowCount Then
                grid.InvalidateRow(oldRow)
            End If

            InvalidateActionCell(oldRow)

        End Sub

        Private Sub InvalidateActionCell(rowIndex As Integer)

            If rowIndex < 0 OrElse rowIndex >= grid.RowCount Then Return

            grid.InvalidateCell(grid.Columns("colActions").Index, rowIndex)

        End Sub

       Private Sub Grid_CellMouseClick(
    sender As Object,
    e As DataGridViewCellMouseEventArgs)

    If e.Button <> MouseButtons.Left Then Return

    Dim action As Integer = HitAction(e)

    If action = 0 Then Return

    Dim row As DataGridViewRow = grid.Rows(e.RowIndex)

    If row.Tag Is Nothing Then Return

    If action = 2 Then
        OpenItemsModal(Convert.ToInt32(row.Tag))
    Else
        OpenViewModal(Convert.ToInt32(row.Tag))
    End If

End Sub
        ' ============================================================
        ' GRID PAINTING
        ' ============================================================

        Private Sub Grid_CellPainting(
            sender As Object,
            e As DataGridViewCellPaintingEventArgs)

            If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

            Select Case grid.Columns(e.ColumnIndex).Name

                Case "colActions"
                    PaintActionsCell(e)

                Case "colStatus"
                    PaintStatusCell(e)

                Case "colCustomer"
                    PaintTwoLineCell(e, Theme.TextMuted)

                Case "colDevice"
                    PaintTwoLineCell(e, Theme.NeonCyan)

                Case Else
                    PaintBaseCell(e)

                    e.Paint(e.CellBounds, DataGridViewPaintParts.ContentForeground)

            End Select

        End Sub

        ' row background (with hover tint) + bottom line, shared by every cell
        Private Sub PaintBaseCell(e As DataGridViewCellPaintingEventArgs)

            e.Handled = True

            Dim rowColor As Color = Theme.CardColor

            If tableHoverRow = e.RowIndex Then
                rowColor = Theme.Lerp(Theme.CardColor, Theme.NeonPurple, 0.08F)
            End If

            Using brush As New SolidBrush(rowColor)
                e.Graphics.FillRectangle(brush, e.CellBounds)
            End Using

            Using pen As New Pen(Theme.LineColor, 1.0F)
                e.Graphics.DrawLine(
                    pen,
                    e.CellBounds.Left, e.CellBounds.Bottom - 1,
                    e.CellBounds.Right, e.CellBounds.Bottom - 1)
            End Using

        End Sub

        ' bold name on top, small caps line below
        ' (customer + ticket number, device name + device type)
        Private Sub PaintTwoLineCell(
            e As DataGridViewCellPaintingEventArgs,
            secondLineColor As Color)

            PaintBaseCell(e)

            Dim raw As String = If(e.Value Is Nothing, "", e.Value.ToString())
            Dim parts() As String = raw.Split(ChrW(10))

            Dim firstLine As String = parts(0)
            Dim secondLine As String = If(parts.Length > 1, parts(1), "")

            Dim x As Integer = e.CellBounds.X + 12
            Dim w As Integer = Math.Max(10, e.CellBounds.Width - 20)

            Dim flags As TextFormatFlags =
                TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
                TextFormatFlags.NoPadding Or TextFormatFlags.EndEllipsis

            If secondLine = "" Then

                TextRenderer.DrawText(
                    e.Graphics, firstLine, Theme.UiFont(9.5F, FontStyle.Bold),
                    New Rectangle(x, e.CellBounds.Y, w, e.CellBounds.Height - 1),
                    Theme.TextWhite, flags)

            Else

                TextRenderer.DrawText(
                    e.Graphics, firstLine, Theme.UiFont(9.5F, FontStyle.Bold),
                    New Rectangle(x, e.CellBounds.Y + 12, w, 22),
                    Theme.TextWhite, flags)

                TextRenderer.DrawText(
                    e.Graphics, secondLine.ToUpperInvariant(),
                    Theme.UiFont(7.5F, FontStyle.Bold),
                    New Rectangle(x, e.CellBounds.Y + 35, w, 18),
                    secondLineColor, flags)

            End If

        End Sub

        Private Sub PaintStatusCell(e As DataGridViewCellPaintingEventArgs)

            PaintBaseCell(e)

            Dim status As String = If(e.Value Is Nothing, "", e.Value.ToString())

            If String.IsNullOrWhiteSpace(status) Then Return

            Dim statusColor As Color = Theme.StatusColor(status)

            Dim pillWidth As Integer =
                Math.Min(
                    e.CellBounds.Width - 20,
                    Math.Max(82, status.Length * 7 + 28))

            Dim pillHeight As Integer = 28

            Dim pillRect As New Rectangle(
                e.CellBounds.X + (e.CellBounds.Width - pillWidth) \ 2,
                e.CellBounds.Y + (e.CellBounds.Height - pillHeight) \ 2,
                pillWidth,
                pillHeight)

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

            Using path As GraphicsPath = Theme.RoundedRect(pillRect, 10)

                Using brush As New SolidBrush(Color.FromArgb(35, statusColor))
                    e.Graphics.FillPath(brush, path)
                End Using

                Using pen As New Pen(Color.FromArgb(90, statusColor), 1.0F)
                    e.Graphics.DrawPath(pen, path)
                End Using

            End Using

            TextRenderer.DrawText(
                e.Graphics,
                status,
                Theme.UiFont(8.0F, FontStyle.Bold),
                pillRect,
                statusColor,
                TextFormatFlags.HorizontalCenter Or
                TextFormatFlags.VerticalCenter Or
                TextFormatFlags.NoPadding)

        End Sub

      Private Sub PaintActionsCell(e As DataGridViewCellPaintingEventArgs)

    PaintBaseCell(e)

    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

    Dim rowHot As Boolean = (hoverRow = e.RowIndex)

    DrawActionButton(
        e.Graphics, ActionButtonRect(e.CellBounds, 0),
        "VIEW", Theme.NeonPurple,
        rowHot AndAlso hoverAction = 1)

    If AppSession.IsTechnician Then

        DrawActionButton(
            e.Graphics, ActionButtonRect(e.CellBounds, 1),
            "ITEMS", Theme.NeonGreen,
            rowHot AndAlso hoverAction = 2)

    End If

End Sub

        Private Sub DrawActionButton(
            g As Graphics,
            rect As Rectangle,
            caption As String,
            accent As Color,
            hot As Boolean)

            Dim alpha As Integer = If(hot, 80, 24)
            Dim borderAlpha As Integer = If(hot, 255, 150)

            Dim drawRect As Rectangle = rect

            If hot Then
                drawRect = New Rectangle(
                    rect.X - 1, rect.Y - 1,
                    rect.Width + 2, rect.Height + 2)
            End If

            Using path As GraphicsPath = Theme.RoundedRect(drawRect, 8)

                Using brush As New SolidBrush(Color.FromArgb(alpha, accent))
                    g.FillPath(brush, path)
                End Using

                Using pen As New Pen(Color.FromArgb(borderAlpha, accent), 1.2F)
                    g.DrawPath(pen, path)
                End Using

            End Using

            TextRenderer.DrawText(
                g,
                caption,
                Theme.UiFont(7.5F, FontStyle.Bold),
                rect,
                accent,
                TextFormatFlags.HorizontalCenter Or
                TextFormatFlags.VerticalCenter Or
                TextFormatFlags.NoPadding)

        End Sub

    End Class

End Namespace