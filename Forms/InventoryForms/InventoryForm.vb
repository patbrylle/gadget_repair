Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Forms

    Public Class InventoryForm
        Inherits Form

        Private ReadOnly db As ApplicationDbContext
        Private ReadOnly inventoryController As InventoryController

        Private headerPanel As Panel
        Private lblHeading As Label
        Private lblSubheading As Label
        Private btnNew As NeonButton

        Private statsTable As TableLayoutPanel
        Private statUnits As StatCard
        Private statValue As StatCard
        Private statMost As StatCard
        Private statLow As StatCard
        Private statOut As StatCard

        Private toolbarPanel As Panel
        Private searchHost As NeonInput
        Private txtSearch As TextBox
        Private filterHost As NeonInput
        Private cboFilter As ComboBox

        Private gridCard As NeonCard
        Private grid As SmoothDataGridView
        Private lblCount As Label

        Private loading As Boolean = False

        Private Const SearchHint As String = "Search item or category..."
        Private searchIsHint As Boolean = True

        ' hover: whole row (tableHoverRow) and the VIEW button (hoverRow/hoverAction)
        Private hoverRow As Integer = -1
        Private hoverAction As Integer = 0
        Private tableHoverRow As Integer = -1


        ' ============================================================
        ' CONSTRUCTOR
        ' ============================================================

        Public Sub New(database As ApplicationDbContext)

            db = database
            inventoryController = New InventoryController(db)

            loading = True
            BuildInterface()
            loading = False

            AddHandler Me.Load, AddressOf InventoryForm_Load

        End Sub

        Private Sub InventoryForm_Load(sender As Object, e As EventArgs)

            Try
                FillCategories()
            Catch
                ' LoadInventory shows the database error
            End Try

            LoadInventory()

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
            lblHeading.Text = "Inventory"
            lblHeading.AutoSize = True
            lblHeading.Location = New Point(0, 0)
            lblHeading.Font = Theme.UiFont(23.0F, FontStyle.Bold)
            lblHeading.ForeColor = Theme.TextWhite

            lblSubheading = New Label()
lblSubheading.Text = If(AppSession.IsAdmin,
    "Manage parts, stock levels and item pricing",
    "Check which parts are in stock")
                lblSubheading.AutoSize = True
            lblSubheading.Location = New Point(2, 42)
            lblSubheading.Font = Theme.UiFont(9.5F)
            lblSubheading.ForeColor = Theme.TextMuted

            btnNew = New NeonButton()
            btnNew.Text = "+  ADD ITEM"
            btnNew.Filled = True
            btnNew.Accent = Theme.NeonPurple
            btnNew.Size = New Size(155, 44)
            btnNew.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            btnNew.Top = 2
            btnNew.Visible = AppSession.IsAdmin

            AddHandler btnNew.Click,
                Sub(sender As Object, e As EventArgs) OpenModal(0)

            headerPanel.Controls.Add(lblHeading)
            headerPanel.Controls.Add(lblSubheading)
            headerPanel.Controls.Add(btnNew)

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

            statUnits = NewStatCard("TOTAL UNITS", Theme.NeonPurple, New Padding(0, 0, 10, 16))
            statValue = NewStatCard("TOTAL VALUE", Theme.NeonGreen, New Padding(5, 0, 5, 16))
            statMost = NewStatCard("MOST USED ITEM", Theme.NeonCyan, New Padding(5, 0, 5, 16))
            statLow = NewStatCard("LOW STOCK", Color.Orange, New Padding(5, 0, 5, 16))
            statOut = NewStatCard("OUT OF STOCK", Theme.NeonPink, New Padding(5, 0, 0, 16))

            statsTable.Controls.Add(statUnits, 0, 0)
            statsTable.Controls.Add(statValue, 1, 0)
            statsTable.Controls.Add(statMost, 2, 0)
            statsTable.Controls.Add(statLow, 3, 0)
            statsTable.Controls.Add(statOut, 4, 0)

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

        Private Sub UpdateStatistics()
If Not AppSession.IsAdmin Then statValue.Value = "-"
            Try

                statUnits.Value = inventoryController.GetTotalStock().ToString("N0")
                statValue.Value = Money.Peso(inventoryController.GetInventoryValue())
                statLow.Value = inventoryController.GetLowStockCount().ToString()
                statOut.Value = inventoryController.GetOutOfStockCount().ToString()

                Dim most As Tuple(Of String, Integer) = inventoryController.GetMostUsed()

                If most Is Nothing Then

                    statMost.Caption = "MOST USED ITEM"
                    statMost.Value = "-"

                Else

                    statMost.Caption = "MOST USED  -  " & most.Item2.ToString() & " PCS"
                    statMost.Value = most.Item1

                End If

            Catch

                statUnits.Value = "0"
                statValue.Value = "0"
                statMost.Value = "-"
                statLow.Value = "0"
                statOut.Value = "0"

            End Try

        End Sub


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

            ' category filter (NeonComboBox ignores the wheel while closed)
            filterHost = New NeonInput()
            filterHost.Location = New Point(0, 6)
            filterHost.Size = New Size(220, 46)
            filterHost.Padding = New Padding(12, 0, 8, 0)

            cboFilter = New NeonComboBox()
            cboFilter.Font = Theme.UiFont(9.5F)
            cboFilter.Items.Add("All Categories")
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

        ' rebuilds the category list and keeps the current choice
        Private Sub FillCategories()

            Dim keep As String = "All Categories"

            If cboFilter.SelectedItem IsNot Nothing Then
                keep = cboFilter.SelectedItem.ToString()
            End If

            Dim wasLoading As Boolean = loading

            loading = True

            cboFilter.Items.Clear()
            cboFilter.Items.Add("All Categories")

            For Each c As String In inventoryController.GetFilterCategories()
                cboFilter.Items.Add(c)
            Next

            Dim index As Integer = cboFilter.Items.IndexOf(keep)

            cboFilter.SelectedIndex = If(index >= 0, index, 0)

            loading = wasLoading

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

            Dim colItem As New DataGridViewTextBoxColumn()
            colItem.Name = "colItem"
            colItem.HeaderText = "ITEM"
            colItem.FillWeight = 30.0F

            Dim colStock As New DataGridViewTextBoxColumn()
            colStock.Name = "colStock"
            colStock.HeaderText = "IN STOCK"
            colStock.FillWeight = 11.0F

            Dim colCost As New DataGridViewTextBoxColumn()
            colCost.Name = "colCost"
            colCost.HeaderText = "UNIT COST"
            colCost.FillWeight = 14.0F
            colCost.DefaultCellStyle.ForeColor = Theme.NeonCyan

            Dim colPrice As New DataGridViewTextBoxColumn()
            colPrice.Name = "colPrice"
            colPrice.HeaderText = "SELLING PRICE"
            colPrice.FillWeight = 14.0F
            colPrice.DefaultCellStyle.ForeColor = Theme.NeonGreen

            Dim colUsed As New DataGridViewTextBoxColumn()
            colUsed.Name = "colUsed"
            colUsed.HeaderText = "USED"
            colUsed.FillWeight = 10.0F
            colUsed.DefaultCellStyle.ForeColor = Theme.TextMuted

            Dim colStatus As New DataGridViewTextBoxColumn()
            colStatus.Name = "colStatus"
            colStatus.HeaderText = "STATUS"
            colStatus.FillWeight = 16.0F

            Dim colActions As New DataGridViewTextBoxColumn()
            colActions.Name = "colActions"
            colActions.HeaderText = "ACTIONS"
            colActions.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            colActions.Width = 130
            colActions.Resizable = DataGridViewTriState.False

            grid.Columns.AddRange(
                New DataGridViewColumn() {
                    colId, colItem, colStock, colCost,
                    colPrice, colUsed, colStatus, colActions})

            AddHandler grid.CellPainting, AddressOf Grid_CellPainting
            AddHandler grid.CellMouseMove, AddressOf Grid_CellMouseMove
            AddHandler grid.CellMouseLeave, AddressOf Grid_CellMouseLeave
            AddHandler grid.CellMouseClick, AddressOf Grid_CellMouseClick
            AddHandler grid.CellDoubleClick, AddressOf Grid_CellDoubleClick
            AddHandler grid.MouseEnter, AddressOf Grid_MouseEnter
            AddHandler grid.Scroll, AddressOf Grid_Scroll
If Not AppSession.IsAdmin Then
    colCost.Visible = False          ' unit cost is private
    colUsed.Visible = False
    colActions.Visible = False       ' no VIEW / edit
End If
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

            LoadInventory()

        End Sub

        Private Sub Filter_Changed(sender As Object, e As EventArgs)

            If loading Then Return

            LoadInventory()

        End Sub


        ' ============================================================
        ' LOAD
        ' ============================================================

        Private Sub LoadInventory()

            If inventoryController Is Nothing Then Return
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

                Dim categoryFilter As String = "All Categories"

                If cboFilter.SelectedItem IsNot Nothing Then
                    categoryFilter = cboFilter.SelectedItem.ToString()
                End If

                Dim items As List(Of Inventory) =
                    inventoryController.GetInventory(searchText, categoryFilter)

                Dim usage As Dictionary(Of Integer, Integer) =
                    inventoryController.GetUsageLookup()

                For Each item As Inventory In items

                    Dim used As Integer = 0

                    usage.TryGetValue(item.InventoryId, used)

                    Dim rowIndex As Integer =
                        grid.Rows.Add(
                            item.InventoryId,
                            item.ItemName & ChrW(10) & If(item.Category, ""),
                            item.Quantity.ToString(),
                            Money.Peso(item.UnitCost),
                            Money.Peso(item.SellingPrice),
                            used.ToString(),
                            InventoryController.GetStockStatus(item),
                            "")

                    grid.Rows(rowIndex).Tag = item.InventoryId

                Next

                UpdateStatistics()

                lblCount.Text = "Showing " & grid.Rows.Count.ToString() & " item(s)"

            Catch ex As Exception

                MessageBox.Show(
                    "Unable to load inventory." &
                    Environment.NewLine & Environment.NewLine & ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

            Finally

                loading = False

            End Try

        End Sub


        ' ============================================================
        ' MODAL
        ' ============================================================

        Private Sub OpenModal(inventoryId As Integer)

            Using modal As New InventoryDetailForm(inventoryController, inventoryId)

                modal.ShowDialog(Me)

                If modal.Changed Then

                    Dim firstRow As Integer = grid.FirstDisplayedScrollingRowIndex

                    FillCategories()
                    LoadInventory()

                    ' keep the scroll position after a change
                    Try
                        If firstRow >= 0 AndAlso firstRow < grid.RowCount Then
                            grid.FirstDisplayedScrollingRowIndex = firstRow
                        End If
                    Catch
                    End Try

                End If

            End Using

        End Sub

        Private Sub OpenRow(rowIndex As Integer)
If Not AppSession.IsAdmin Then Return
            If rowIndex < 0 OrElse rowIndex >= grid.RowCount Then Return

            Dim tag As Object = grid.Rows(rowIndex).Tag

            If tag Is Nothing Then Return

            OpenModal(Convert.ToInt32(tag))

        End Sub

        Private Sub Grid_CellDoubleClick(
            sender As Object,
            e As DataGridViewCellEventArgs)

            If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

            If grid.Columns(e.ColumnIndex).Name = "colActions" Then Return

            OpenRow(e.RowIndex)

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
        ' GRID PAINTING (shared helpers in NeonGridPaint)
        ' ============================================================

        Private Sub Grid_CellPainting(
            sender As Object,
            e As DataGridViewCellPaintingEventArgs)

            If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

            Dim hot As Boolean = (tableHoverRow = e.RowIndex)

            Select Case grid.Columns(e.ColumnIndex).Name

                Case "colActions"

                    NeonGridPaint.PaintBase(e, hot)

                    NeonGridPaint.DrawActionButton(
                        e.Graphics,
                        NeonGridPaint.ActionButtonRect(e.CellBounds),
                        "VIEW",
                        Theme.NeonPurple,
                        hoverRow = e.RowIndex AndAlso hoverAction = 1)

                Case "colStatus"

                    NeonGridPaint.PaintPill(
                        e, hot,
                        Function(s As String) NeonGridPaint.StockStatusColor(s))

                Case "colItem"

                    NeonGridPaint.PaintTwoLine(e, hot, Theme.NeonCyan)

                Case Else

                    NeonGridPaint.PaintBase(e, hot)

                    e.Paint(e.CellBounds, DataGridViewPaintParts.ContentForeground)

            End Select

        End Sub

    End Class

End Namespace