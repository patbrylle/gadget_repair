Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Data
Imports gadget_repair.Forms
Imports gadget_repair.Models

Public Class PaymentsForm
    Inherits UserControl

    Private ReadOnly db As ApplicationDbContext
    Private ReadOnly paymentController As PaymentController

    Private headerPanel As Panel
    Private lblHeading As Label
    Private lblSubheading As Label

    Private statsTable As TableLayoutPanel
    Private statTotal As StatCard
    Private statPaid As StatCard
    Private statPartial As StatCard
    Private statUnpaid As StatCard

    Private toolbarPanel As Panel
    Private searchHost As NeonInput
    Private txtSearch As TextBox
    Private filterHost As NeonInput
    Private cboFilter As ComboBox

    Private gridCard As NeonCard
    Private grid As SmoothDataGridView
    Private lblCount As Label

    Private loading As Boolean = False

    Private Const SearchHint As String = "Search customer, device or technician..."
    Private searchIsHint As Boolean = True

    ' hover: whole row (tableHoverRow) and the action button (hoverRow/hoverAction)
    Private hoverRow As Integer = -1
    Private hoverAction As Integer = 0
    Private tableHoverRow As Integer = -1

    Private ReadOnly StatusList As String() = {"Paid", "Partially Paid", "Unpaid"}

    Private Class PaymentRowData
        Public Property RepairId As Integer
        Public Property PaymentId As Integer
    End Class


    ' ============================================================
    ' CONSTRUCTOR
    ' ============================================================

    Public Sub New(database As ApplicationDbContext)

        db = database
        paymentController = New PaymentController(db)

        loading = True
        BuildInterface()
        loading = False

        AddHandler Me.Load, AddressOf PaymentsForm_Load

        ' reload every time the page is shown so new completed repairs appear
        AddHandler Me.VisibleChanged, AddressOf PaymentsForm_VisibleChanged

    End Sub

    Private Sub PaymentsForm_Load(sender As Object, e As EventArgs)
        LoadPayments()
    End Sub

    Private Sub PaymentsForm_VisibleChanged(sender As Object, e As EventArgs)

        If Me.Visible AndAlso Not loading Then
            LoadPayments()
        End If

    End Sub

    Public Sub RefreshData()
        LoadPayments()
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
        lblHeading.Text = "Payments"
        lblHeading.AutoSize = True
        lblHeading.Location = New Point(0, 0)
        lblHeading.Font = Theme.UiFont(23.0F, FontStyle.Bold)
        lblHeading.ForeColor = Theme.TextWhite

        lblSubheading = New Label()
        lblSubheading.Text = "Completed and delivered repairs, with their payment and balance"
        lblSubheading.AutoSize = True
        lblSubheading.Location = New Point(2, 42)
        lblSubheading.Font = Theme.UiFont(9.5F)
        lblSubheading.ForeColor = Theme.TextMuted

        headerPanel.Controls.Add(lblHeading)
        headerPanel.Controls.Add(lblSubheading)

    End Sub


    ' ============================================================
    ' STATISTICS
    ' ============================================================

    Private Sub BuildStatistics()

        statsTable = New TableLayoutPanel()
        statsTable.Dock = DockStyle.Top
        statsTable.Height = 108
        statsTable.ColumnCount = 4
        statsTable.RowCount = 1
        statsTable.BackColor = Theme.BackgroundColor
        statsTable.Margin = New Padding(0)

        For i As Integer = 1 To 4
            statsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))
        Next

        statsTable.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        statTotal = NewStatCard("TOTAL RECORDS", Theme.NeonCyan, New Padding(0, 0, 10, 16))
        statPaid = NewStatCard("PAID", Theme.NeonGreen, New Padding(5, 0, 5, 16))
        statPartial = NewStatCard("PARTIALLY PAID", Theme.NeonYellow, New Padding(5, 0, 5, 16))
        statUnpaid = NewStatCard("UNPAID", Theme.NeonPink, New Padding(10, 0, 0, 16))

        statsTable.Controls.Add(statTotal, 0, 0)
        statsTable.Controls.Add(statPaid, 1, 0)
        statsTable.Controls.Add(statPartial, 2, 0)
        statsTable.Controls.Add(statUnpaid, 3, 0)

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
    ' TOOLBAR (same pattern as the Repairs page)
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

        ' filter (NeonComboBox ignores the wheel while closed)
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

        AddHandler toolbarPanel.Resize, AddressOf ResizeToolbar

        ResizeToolbar(toolbarPanel, EventArgs.Empty)

    End Sub

    Private Sub ResizeToolbar(sender As Object, e As EventArgs)

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
        colCustomer.FillWeight = 19.0F

        Dim colDevice As New DataGridViewTextBoxColumn()
        colDevice.Name = "colDevice"
        colDevice.HeaderText = "DEVICE"
        colDevice.FillWeight = 18.0F

        Dim colTechnician As New DataGridViewTextBoxColumn()
        colTechnician.Name = "colTechnician"
        colTechnician.HeaderText = "TECHNICIAN"
        colTechnician.FillWeight = 14.0F
        colTechnician.DefaultCellStyle.ForeColor = Theme.NeonPurple

        Dim colDate As New DataGridViewTextBoxColumn()
        colDate.Name = "colDate"
        colDate.HeaderText = "COMPLETED"
        colDate.FillWeight = 12.0F
        colDate.DefaultCellStyle.ForeColor = Theme.TextMuted

        Dim colCost As New DataGridViewTextBoxColumn()
        colCost.Name = "colCost"
        colCost.HeaderText = "FINAL COST"
        colCost.FillWeight = 12.0F

        Dim colPaid As New DataGridViewTextBoxColumn()
        colPaid.Name = "colPaid"
        colPaid.HeaderText = "PAID"
        colPaid.FillWeight = 11.0F
        colPaid.DefaultCellStyle.ForeColor = Theme.NeonGreen

        Dim colBalance As New DataGridViewTextBoxColumn()
        colBalance.Name = "colBalance"
        colBalance.HeaderText = "BALANCE"
        colBalance.FillWeight = 11.0F

        Dim colStatus As New DataGridViewTextBoxColumn()
        colStatus.Name = "colStatus"
        colStatus.HeaderText = "STATUS"
        colStatus.FillWeight = 14.0F

        Dim colActions As New DataGridViewTextBoxColumn()
        colActions.Name = "colActions"
        colActions.HeaderText = "ACTIONS"
        colActions.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        colActions.Width = 130
        colActions.Resizable = DataGridViewTriState.False

        grid.Columns.AddRange(
            New DataGridViewColumn() {
                colId, colCustomer, colDevice, colTechnician, colDate,
                colCost, colPaid, colBalance, colStatus, colActions})

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

        LoadPayments()

    End Sub

    Private Sub Filter_Changed(sender As Object, e As EventArgs)

        If loading Then Return

        LoadPayments()

    End Sub


    ' ============================================================
    ' LOAD
    ' ============================================================

    Private Sub LoadPayments()

        If paymentController Is Nothing Then Return
        If grid Is Nothing Then Return
        If cboFilter Is Nothing Then Return
        If txtSearch Is Nothing Then Return

        loading = True

        Try

            grid.Rows.Clear()

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

            Dim payments As List(Of Payment) =
                paymentController.GetPayments(searchText, statusFilter)

            Dim customers As Dictionary(Of Integer, String) =
                paymentController.GetCustomerLookup()

            For Each payment As Payment In payments

                Dim repair As Repair = payment.Repair

                If repair Is Nothing Then Continue For

                Dim customerName As String = "Unknown Customer"

                If customers.ContainsKey(repair.CustomerId) Then
                    customerName = customers(repair.CustomerId)
                End If

                Dim balance As Decimal =
                    Math.Max(0D, repair.Cost - payment.AmountPaid)

                Dim completed As String = "-"

                If repair.CompletionDate.HasValue Then
                    completed = repair.CompletionDate.Value.ToString("MMM dd, yyyy")
                End If

                Dim rowIndex As Integer =
                    grid.Rows.Add(
                        payment.PaymentId,
                        customerName & ChrW(10) & "TICKET #" & repair.RepairId.ToString(),
                        BuildDeviceText(repair),
                        PaymentController.TechnicianName(repair),
                        completed,
                        Money.Peso(repair.Cost),
                        Money.Peso(payment.AmountPaid),
                        Money.Peso(balance),
                        PaymentController.ComputeStatus(repair.Cost, payment.AmountPaid),
                        "")

                grid.Rows(rowIndex).Tag =
                    New PaymentRowData With {
                        .RepairId = repair.RepairId,
                        .PaymentId = payment.PaymentId
                    }

            Next

            ' statistics always use the unfiltered list
            UpdateStatistics(paymentController.GetPayments("", "All Status"))

            lblCount.Text = "Showing " & grid.Rows.Count.ToString() & " record(s)"

        Catch ex As Exception

            MessageBox.Show(
                "Unable to load payments." &
                Environment.NewLine & Environment.NewLine & ex.Message,
                "Database Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        Finally

            loading = False

        End Try

    End Sub

    ' line 1 = brand + model, line 2 = device type
    Private Function BuildDeviceText(repair As Repair) As String

        Dim nameLine As String = paymentController.GetItemUsed(repair)
        Dim typeLine As String = If(repair.DeviceType, "").Trim()

        If nameLine = "-" OrElse nameLine = typeLine Then
            Return If(nameLine = "-", "Not provided", nameLine)
        End If

        Return nameLine & ChrW(10) & typeLine

    End Function
    Private Sub UpdateStatistics(all As List(Of Payment))

        Dim paid As Integer = 0
        Dim partiallyPaid As Integer = 0
        Dim unpaid As Integer = 0

        For Each p As Payment In all

            If p.Repair Is Nothing Then Continue For

            Select Case PaymentController.ComputeStatus(p.Repair.Cost, p.AmountPaid)

                Case "Paid"
                    paid += 1

                Case "Partially Paid"
                    partiallyPaid += 1

                Case Else
                    unpaid += 1

            End Select

        Next

        statTotal.Value = all.Count.ToString()
        statPaid.Value = paid.ToString()
        statPartial.Value = partiallyPaid.ToString()
        statUnpaid.Value = unpaid.ToString()

    End Sub


    ' ============================================================
    ' OPEN MODAL
    ' ============================================================

    Private Sub OpenPaymentModal(repairId As Integer)

        If repairId <= 0 Then Return

        Using modal As New PaymentEditForm(paymentController, repairId)

            If modal.ShowDialog(Me.FindForm()) = DialogResult.OK Then

                Dim firstRow As Integer = grid.FirstDisplayedScrollingRowIndex

                LoadPayments()

                ' keep the scroll position after a save
                Try
                    If firstRow >= 0 AndAlso firstRow < grid.RowCount Then
                        grid.FirstDisplayedScrollingRowIndex = firstRow
                    End If
                Catch
                End Try

            End If

        End Using

    End Sub

    Private Sub Grid_CellDoubleClick(
        sender As Object,
        e As DataGridViewCellEventArgs)

        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

        If grid.Columns(e.ColumnIndex).Name = "colActions" Then Return

        OpenRow(e.RowIndex)

    End Sub

    Private Sub OpenRow(rowIndex As Integer)

        Dim data As PaymentRowData =
            TryCast(grid.Rows(rowIndex).Tag, PaymentRowData)

        If data Is Nothing Then Return

        OpenPaymentModal(data.RepairId)

    End Sub


    ' ============================================================
    ' GRID MOUSE
    ' ============================================================

    Private Function IsOverButton(e As DataGridViewCellMouseEventArgs) As Boolean

        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return False

        If grid.Columns(e.ColumnIndex).Name <> "colActions" Then Return False

        Dim cell As New Rectangle(
            0, 0,
            grid.Columns(e.ColumnIndex).Width,
            grid.Rows(e.RowIndex).Height)

        Return NeonGridPaint.ActionButtonRect(cell).Contains(e.X, e.Y)

    End Function

    Private Sub Grid_CellMouseMove(
        sender As Object,
        e As DataGridViewCellMouseEventArgs)

        Dim newRow As Integer = If(e.RowIndex >= 0, e.RowIndex, -1)
        Dim newAction As Integer = If(IsOverButton(e), 1, 0)

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
        If Not IsOverButton(e) Then Return

        OpenRow(e.RowIndex)

    End Sub


    ' ============================================================
    ' GRID PAINTING
    ' ============================================================

    Private Sub Grid_CellPainting(
        sender As Object,
        e As DataGridViewCellPaintingEventArgs)

        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

        Dim hot As Boolean = (tableHoverRow = e.RowIndex)

        Select Case grid.Columns(e.ColumnIndex).Name

            Case "colActions"
                PaintActionsCell(e, hot)

            Case "colStatus"
                NeonGridPaint.PaintPill(
                    e, hot,
                    Function(s As String) NeonGridPaint.PayStatusColor(s))

            Case "colCustomer"
                NeonGridPaint.PaintTwoLine(e, hot, Theme.TextMuted)

            Case "colDevice"
                NeonGridPaint.PaintTwoLine(e, hot, Theme.NeonCyan)

            Case Else
                NeonGridPaint.PaintBase(e, hot)

                e.Paint(e.CellBounds, DataGridViewPaintParts.ContentForeground)

        End Select

    End Sub

    ' PAY for admin until fully paid, otherwise VIEW
    Private Sub PaintActionsCell(
        e As DataGridViewCellPaintingEventArgs,
        hot As Boolean)

        NeonGridPaint.PaintBase(e, hot)

        Dim status As String =
            Convert.ToString(grid.Rows(e.RowIndex).Cells("colStatus").Value)

        Dim caption As String =
            If(AppSession.IsAdmin AndAlso status <> "Paid", "PAY", "VIEW")

        Dim buttonHot As Boolean =
            hoverRow = e.RowIndex AndAlso hoverAction = 1

        NeonGridPaint.DrawActionButton(
            e.Graphics,
            NeonGridPaint.ActionButtonRect(e.CellBounds),
            caption,
            Theme.NeonCyan,
            buttonHot)

    End Sub

End Class