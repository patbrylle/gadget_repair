Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Windows.Forms
Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Controllers
Imports gadget_repair.Models

Public Class CustomersForm
    Inherits UserControl

    Private ReadOnly db As ApplicationDbContext
    Private ReadOnly customerController As CustomerController

    Private headerPanel As Panel
    Private btnAdd As NeonButton

    Private statsTable As TableLayoutPanel
    Private statTotal As StatCard
    Private statShowing As StatCard
    Private statActive As StatCard

    Private toolbarPanel As Panel
    Private WithEvents txtSearch As TextBox
    Private lblSearchHint As Label
    Private searchHost As NeonInput

    Private gridCard As NeonCard
    Private WithEvents grid As DataGridView
    Private lblCount As Label

    ' ==========================================================
    ' HOVER STATE
    ' ==========================================================

    Private hoverRow As Integer = -1
    Private hoverAction As Integer = 0
    Private tableHoverRow As Integer = -1


    ' ==========================================================
    ' CONSTRUCTOR
    ' ==========================================================

    Public Sub New(database As ApplicationDbContext)

        db = database
        customerController = New CustomerController(db)

        BuildInterface()
        LoadCustomers()

    End Sub


    ' ==========================================================
    ' MAIN INTERFACE
    ' ==========================================================

    Private Sub BuildInterface()

        Me.Dock = DockStyle.Fill
        Me.BackColor = Theme.BackgroundColor
        Me.Padding = New Padding(30, 28, 30, 26)
        Me.Font = Theme.UiFont(9.0F)
        Me.DoubleBuffered = True

        BuildHeader()
        BuildStats()
        BuildToolbar()
        BuildGrid()

        Me.Controls.Add(gridCard)
        Me.Controls.Add(toolbarPanel)
        Me.Controls.Add(statsTable)
        Me.Controls.Add(headerPanel)

    End Sub


    ' ==========================================================
    ' HEADER
    ' ==========================================================

    Private Sub BuildHeader()

        headerPanel = New Panel()

        headerPanel.Dock = DockStyle.Top
        headerPanel.Height = 84
        headerPanel.BackColor = Theme.BackgroundColor


        ' ------------------------------------------------------
        ' TITLE
        ' ------------------------------------------------------

        Dim title As New Label()

        title.Text = "Customers"
        title.AutoSize = True
        title.Location = New Point(0, 0)
        title.Font = Theme.UiFont(23.0F, FontStyle.Bold)
        title.ForeColor = Theme.TextWhite


        ' ------------------------------------------------------
        ' SUBTITLE
        ' ------------------------------------------------------

        Dim subtitle As New Label()

        subtitle.Text =
            "Manage customers registered in the repair system"

        subtitle.AutoSize = True
        subtitle.Location = New Point(2, 42)
        subtitle.Font = Theme.UiFont(9.5F)
        subtitle.ForeColor = Theme.TextMuted


        ' ------------------------------------------------------
        ' ADD CUSTOMER BUTTON
        ' ------------------------------------------------------

        btnAdd = New NeonButton()

        btnAdd.Text = "+  ADD CUSTOMER"
        btnAdd.Filled = True
        btnAdd.Accent = Theme.NeonPurple
        btnAdd.Size = New Size(180, 44)
        btnAdd.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnAdd.Top = 2

        AddHandler btnAdd.Click,
            AddressOf AddCustomer


        headerPanel.Controls.Add(title)
        headerPanel.Controls.Add(subtitle)
        headerPanel.Controls.Add(btnAdd)


        ' ------------------------------------------------------
        ' KEEP BUTTON ALIGNED
        ' ------------------------------------------------------

        AddHandler headerPanel.Resize,
            Sub(sender, e)

                btnAdd.Left =
                    headerPanel.ClientSize.Width -
                    btnAdd.Width

            End Sub

    End Sub


    ' ==========================================================
    ' STATISTICS
    ' ==========================================================

    Private Sub BuildStats()

        statsTable = New TableLayoutPanel()

        statsTable.Dock = DockStyle.Top
        statsTable.Height = 108
        statsTable.ColumnCount = 3
        statsTable.RowCount = 1
        statsTable.BackColor = Theme.BackgroundColor
        statsTable.Margin = New Padding(0)


        ' ------------------------------------------------------
        ' COLUMNS
        ' ------------------------------------------------------

        statsTable.ColumnStyles.Add(
            New ColumnStyle(
                SizeType.Percent,
                33.33F))

        statsTable.ColumnStyles.Add(
            New ColumnStyle(
                SizeType.Percent,
                33.33F))

        statsTable.ColumnStyles.Add(
            New ColumnStyle(
                SizeType.Percent,
                33.34F))


        statsTable.RowStyles.Add(
            New RowStyle(
                SizeType.Percent,
                100.0F))


        ' ------------------------------------------------------
        ' TOTAL CUSTOMERS
        ' ------------------------------------------------------

        statTotal = New StatCard()

        statTotal.Caption = "TOTAL CUSTOMERS"
        statTotal.Accent = Theme.NeonPurple
        statTotal.Dock = DockStyle.Fill
        statTotal.Margin =
            New Padding(
                0,
                0,
                10,
                16)


        ' ------------------------------------------------------
        ' SHOWING
        ' ------------------------------------------------------

        statShowing = New StatCard()

        statShowing.Caption = "SHOWING"
        statShowing.Accent = Theme.NeonCyan
        statShowing.Dock = DockStyle.Fill
        statShowing.Margin =
            New Padding(
                5,
                0,
                5,
                16)


        ' ------------------------------------------------------
        ' WITH REPAIR TICKETS
        ' ------------------------------------------------------

        statActive = New StatCard()

        statActive.Caption = "WITH REPAIR TICKETS"
        statActive.Accent = Theme.NeonGreen
        statActive.Dock = DockStyle.Fill
        statActive.Margin =
            New Padding(
                10,
                0,
                0,
                16)


        statsTable.Controls.Add(
            statTotal,
            0,
            0)

        statsTable.Controls.Add(
            statShowing,
            1,
            0)

        statsTable.Controls.Add(
            statActive,
            2,
            0)

    End Sub


    ' ==========================================================
    ' SEARCH TOOLBAR
    ' ==========================================================

    Private Sub BuildToolbar()

        toolbarPanel = New Panel()

        toolbarPanel.Dock = DockStyle.Top
        toolbarPanel.Height = 68
        toolbarPanel.BackColor = Theme.BackgroundColor


        ' ======================================================
        ' SEARCH CONTAINER
        ' ======================================================

        searchHost = New NeonInput()

        searchHost.Location =
            New Point(0, 6)

        searchHost.Size =
            New Size(560, 46)

        searchHost.Anchor =
            AnchorStyles.Top Or
            AnchorStyles.Left

        searchHost.SearchIcon = True

        searchHost.Padding =
            New Padding(
                42,
                0,
                14,
                0)


        ' ======================================================
        ' SEARCH TEXTBOX
        ' ======================================================
txtSearch = New TextBox()
txtSearch.PlaceholderText = "Search name, contact number or address..."
txtSearch.BackColor = Theme.InputColor
txtSearch.ForeColor = Theme.TextWhite
txtSearch.BorderStyle = BorderStyle.None
txtSearch.Font = Theme.UiFont(9.5F)
txtSearch.Anchor = AnchorStyles.Left Or AnchorStyles.Right

searchHost.Controls.Add(txtSearch)
toolbarPanel.Controls.Add(searchHost)


        ' ======================================================
        ' SEARCH STATUS
        ' ======================================================
lblSearchHint = New Label()
lblSearchHint.Text = "All customers"
lblSearchHint.AutoSize = False
lblSearchHint.Size = New Size(130, 46)
lblSearchHint.TextAlign = ContentAlignment.MiddleRight
lblSearchHint.Anchor = AnchorStyles.Top Or AnchorStyles.Right
lblSearchHint.Font = Theme.UiFont(8.5F, FontStyle.Bold)
lblSearchHint.ForeColor = Theme.TextMuted
lblSearchHint.Top = 6


        toolbarPanel.Controls.Add(
            lblSearchHint)


        ' ======================================================
        ' RESPONSIVE SEARCH LAYOUT
        ' ======================================================

        AddHandler toolbarPanel.Resize,
            AddressOf ResizeSearchToolbar


        ' Initial positioning
        ResizeSearchToolbar(
            toolbarPanel,
            EventArgs.Empty)

    End Sub


    ' ==========================================================
    ' RESIZE SEARCH TOOLBAR
    ' ==========================================================

    Private Sub ResizeSearchToolbar(
        sender As Object,
        e As EventArgs)

        If toolbarPanel Is Nothing OrElse
           searchHost Is Nothing OrElse
           lblSearchHint Is Nothing Then

            Return

        End If


        If toolbarPanel.ClientSize.Width <= 0 Then
            Return
        End If


        ' ------------------------------------------------------
        ' RIGHT STATUS POSITION
        ' ------------------------------------------------------

        lblSearchHint.Left =
            toolbarPanel.ClientSize.Width -
            lblSearchHint.Width


        ' ------------------------------------------------------
        ' SEARCH WIDTH
        ' ------------------------------------------------------

        Dim spacing As Integer = 22

        Dim availableWidth As Integer =
            toolbarPanel.ClientSize.Width -
            lblSearchHint.Width -
            spacing


        Dim minimumWidth As Integer = 360
        Dim maximumWidth As Integer = 620


        Dim finalWidth As Integer =
            Math.Min(
                maximumWidth,
                Math.Max(
                    minimumWidth,
                    availableWidth))


        ' ------------------------------------------------------
        ' SMALL WINDOW PROTECTION
        ' ------------------------------------------------------

        If toolbarPanel.ClientSize.Width <
           minimumWidth +
           lblSearchHint.Width +
           spacing Then

            finalWidth =
                Math.Max(
                    220,
                    availableWidth)

        End If


        searchHost.Width =
            finalWidth

    End Sub


    ' ==========================================================
    ' CUSTOMER GRID
    ' ==========================================================

    Private Sub BuildGrid()

        gridCard = New NeonCard()

        gridCard.Dock = DockStyle.Fill

        gridCard.Padding =
            New Padding(
                16,
                14,
                16,
                10)


        ' ------------------------------------------------------
        ' GRID
        ' ------------------------------------------------------

        grid = New DataGridView()


        With grid

            .Dock =
                DockStyle.Fill

            .AutoGenerateColumns =
                False

            .AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill

            .ReadOnly =
                True

            .AllowUserToAddRows =
                False

            .AllowUserToDeleteRows =
                False

            .AllowUserToResizeRows =
                False

            .AllowUserToResizeColumns =
                False

            .MultiSelect =
                False

            .RowHeadersVisible =
                False

            .SelectionMode =
                DataGridViewSelectionMode.FullRowSelect

            .BackgroundColor =
                Theme.CardColor

            .GridColor =
                Theme.LineColor

            .BorderStyle =
                BorderStyle.None

            .CellBorderStyle =
                DataGridViewCellBorderStyle.SingleHorizontal

            .ColumnHeadersBorderStyle =
                DataGridViewHeaderBorderStyle.None

            .ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing

            .EnableHeadersVisualStyles =
                False

            .ColumnHeadersHeight =
                48

            .RowTemplate.Height =
                68

            .AllowUserToOrderColumns =
                False

            .DefaultCellStyle.NullValue =
                ""

        End With


        ' ------------------------------------------------------
        ' HEADER STYLE
        ' ------------------------------------------------------

        With grid.ColumnHeadersDefaultCellStyle

            .BackColor =
                Theme.CardColor

            .ForeColor =
                Theme.TextMuted

            .SelectionBackColor =
                Theme.CardColor

            .SelectionForeColor =
                Theme.TextMuted

            .Font =
                Theme.UiFont(
                    8.0F,
                    FontStyle.Bold)

            .Padding =
                New Padding(
                    12,
                    0,
                    0,
                    0)

        End With


        ' ------------------------------------------------------
        ' NORMAL CELL STYLE
        ' ------------------------------------------------------

        With grid.DefaultCellStyle

            .BackColor =
                Theme.CardColor

            .ForeColor =
                Theme.TextWhite

            .SelectionBackColor =
                Theme.CardColor

            .SelectionForeColor =
                Theme.TextWhite

            .Font =
                Theme.UiFont(9.5F)

            .Padding =
                New Padding(
                    12,
                    0,
                    0,
                    0)

        End With


        ' ------------------------------------------------------
        ' ALTERNATING ROW STYLE
        ' ------------------------------------------------------

        With grid.AlternatingRowsDefaultCellStyle

            .BackColor =
                Theme.Lerp(
                    Theme.CardColor,
                    Theme.BackgroundColor,
                    0.16F)

            .ForeColor =
                Theme.TextWhite

            .SelectionBackColor =
                Theme.CardColor

            .SelectionForeColor =
                Theme.TextWhite

        End With


        ' ======================================================
        ' ID
        ' ======================================================

        Dim colId As New DataGridViewTextBoxColumn()

        colId.Name =
            "CustomerId"

        colId.DataPropertyName =
            "CustomerId"

        colId.Visible =
            False


        ' ======================================================
        ' CUSTOMER
        ' ======================================================

        Dim colName As New DataGridViewTextBoxColumn()

        colName.Name =
            "FullName"

        colName.DataPropertyName =
            "FullName"

        colName.HeaderText =
            "CUSTOMER"

        colName.FillWeight =
            34.0F


        ' ======================================================
        ' CONTACT
        ' ======================================================

        Dim colContact As New DataGridViewTextBoxColumn()

        colContact.Name =
            "ContactNumber"

        colContact.DataPropertyName =
            "ContactNumber"

        colContact.HeaderText =
            "CONTACT NUMBER"

        colContact.FillWeight =
            22.0F

        colContact.DefaultCellStyle.ForeColor =
            Theme.NeonCyan


        ' ======================================================
        ' ADDRESS
        ' ======================================================

        Dim colAddress As New DataGridViewTextBoxColumn()

        colAddress.Name =
            "Address"

        colAddress.DataPropertyName =
            "Address"

        colAddress.HeaderText =
            "ADDRESS"

        colAddress.FillWeight =
            34.0F

        colAddress.DefaultCellStyle.ForeColor =
            Theme.TextMuted


        ' ======================================================
        ' ACTIONS
        ' ======================================================

        Dim colActions As New DataGridViewTextBoxColumn()

        colActions.Name =
            "Actions"

        colActions.HeaderText =
            "ACTIONS"

        colActions.ReadOnly =
            True

        colActions.AutoSizeMode =
            DataGridViewAutoSizeColumnMode.None

        colActions.Width =
            280

        colActions.Resizable =
            DataGridViewTriState.False


        grid.Columns.AddRange(
            New DataGridViewColumn() {
                colId,
                colName,
                colContact,
                colAddress,
                colActions
            })


        ' ======================================================
        ' EVENTS
        ' ======================================================

        AddHandler grid.CellPainting,
            AddressOf Grid_CellPainting

        AddHandler grid.CellMouseClick,
            AddressOf Grid_CellMouseClick

        AddHandler grid.CellMouseMove,
            AddressOf Grid_CellMouseMove

        AddHandler grid.CellMouseLeave,
            AddressOf Grid_CellMouseLeave


        ' ======================================================
        ' FOOTER COUNT
        ' ======================================================

        lblCount = New Label()

        lblCount.Dock =
            DockStyle.Bottom

        lblCount.Height =
            32

        lblCount.TextAlign =
            ContentAlignment.MiddleLeft

        lblCount.ForeColor =
            Theme.TextMuted

        lblCount.Font =
            Theme.UiFont(8.5F)

        lblCount.Padding =
            New Padding(
                6,
                0,
                0,
                0)


        gridCard.Controls.Add(grid)
        gridCard.Controls.Add(lblCount)

    End Sub


    ' ==========================================================
    ' LOAD CUSTOMERS
    ' ==========================================================
Private Sub LoadCustomers()

    Try

        Dim search As String = ""

        If txtSearch IsNot Nothing Then
            search = txtSearch.Text.Trim()
        End If

        Dim customers As List(Of Customer) =
            customerController.Search(search)

        grid.DataSource = customers
        grid.ClearSelection()

        UpdateStats(customers.Count)

    Catch ex As Exception

        MessageBox.Show(
            "Unable to load customers." &
            Environment.NewLine & Environment.NewLine &
            ex.Message,
            "Database Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error)

    End Try

End Sub

    ' ==========================================================
    ' STATISTICS
    ' ==========================================================

    Private Sub UpdateStats(
        showing As Integer)

        Dim total As Integer =
            db.Customers.Count()


        Dim withRepairs As Integer =
            db.Repairs.
            Select(Function(r) r.CustomerId).
            Distinct().
            Count()


        statTotal.Value =
            total.ToString()

        statShowing.Value =
            showing.ToString()

        statActive.Value =
            withRepairs.ToString()


        lblCount.Text =
            showing.ToString() &
            " of " &
            total.ToString() &
            " customers"


        If lblSearchHint IsNot Nothing Then

            If String.IsNullOrWhiteSpace(
                txtSearch.Text) Then

                lblSearchHint.Text =
                    "All customers"

            Else

                lblSearchHint.Text =
                    "Filtered results"

            End If

        End If

    End Sub


    ' ==========================================================
    ' SEARCH
    ' ==========================================================

    Private Sub txtSearch_TextChanged(
        sender As Object,
        e As EventArgs) Handles txtSearch.TextChanged

        LoadCustomers()

    End Sub


    ' ==========================================================
    ' GRID CELL PAINTING
    ' ==========================================================

    Private Sub Grid_CellPainting(
        sender As Object,
        e As DataGridViewCellPaintingEventArgs)

        If e.RowIndex < 0 OrElse
           e.ColumnIndex < 0 Then

            Return

        End If


        Dim columnName As String =
            grid.Columns(
                e.ColumnIndex).Name


        ' ------------------------------------------------------
        ' CUSTOMER NAME
        ' ------------------------------------------------------

        If columnName = "FullName" Then

            PaintNameCell(e)

            Return

        End If


        ' ------------------------------------------------------
        ' ACTIONS
        ' ------------------------------------------------------

        If columnName = "Actions" Then

            PaintActionsCell(e)

            Return

        End If


        ' ------------------------------------------------------
        ' NORMAL CELL HOVER
        ' ------------------------------------------------------

        If tableHoverRow = e.RowIndex Then

            e.Handled = True

            e.Graphics.SmoothingMode =
                SmoothingMode.AntiAlias


            Using hoverBrush As New SolidBrush(
                Theme.Lerp(
                    Theme.CardColor,
                    Theme.NeonPurple,
                    0.08F))

                e.Graphics.FillRectangle(
                    hoverBrush,
                    e.CellBounds)

            End Using


            e.Paint(
                e.CellBounds,
                DataGridViewPaintParts.ContentForeground Or
                DataGridViewPaintParts.Border)

        End If

    End Sub


    ' ==========================================================
    ' CUSTOMER NAME CELL
    ' ==========================================================

    Private Sub PaintNameCell(
        e As DataGridViewCellPaintingEventArgs)

        e.Handled = True

        Dim rowColor As Color =
            Theme.CardColor


        If tableHoverRow = e.RowIndex Then

            rowColor =
                Theme.Lerp(
                    Theme.CardColor,
                    Theme.NeonPurple,
                    0.08F)

        End If


        Using brush As New SolidBrush(
            rowColor)

            e.Graphics.FillRectangle(
                brush,
                e.CellBounds)

        End Using


        Using pen As New Pen(
            Theme.LineColor,
            1.0F)

            e.Graphics.DrawLine(
                pen,
                e.CellBounds.Left,
                e.CellBounds.Bottom - 1,
                e.CellBounds.Right,
                e.CellBounds.Bottom - 1)

        End Using


        Dim fullName As String =
            Convert.ToString(e.Value)


        Dim diameter As Integer =
            38


        Dim circle As New Rectangle(
            e.CellBounds.X + 14,
            e.CellBounds.Y +
                (e.CellBounds.Height -
                 diameter) \ 2,
            diameter,
            diameter)


        e.Graphics.SmoothingMode =
            SmoothingMode.AntiAlias


        Using brush As New LinearGradientBrush(
            circle,
            Theme.NeonCyan,
            Theme.NeonPurple,
            45.0F)

            e.Graphics.FillEllipse(
                brush,
                circle)

        End Using


        TextRenderer.DrawText(
            e.Graphics,
            Initials(fullName),
            Theme.UiFont(
                8.5F,
                FontStyle.Bold),
            circle,
            Theme.BackgroundColor,
            TextFormatFlags.HorizontalCenter Or
            TextFormatFlags.VerticalCenter Or
            TextFormatFlags.NoPadding)


        Dim nameRect As New Rectangle(
            circle.Right + 13,
            e.CellBounds.Y,
            Math.Max(
                10,
                e.CellBounds.Right -
                circle.Right -
                24),
            e.CellBounds.Height)


        TextRenderer.DrawText(
            e.Graphics,
            fullName,
            Theme.UiFont(
                9.5F,
                FontStyle.Bold),
            nameRect,
            Theme.TextWhite,
            TextFormatFlags.Left Or
            TextFormatFlags.VerticalCenter Or
            TextFormatFlags.NoPadding Or
            TextFormatFlags.EndEllipsis)

    End Sub


    ' ==========================================================
    ' INITIALS
    ' ==========================================================

    Private Function Initials(
        fullName As String) As String

        If String.IsNullOrWhiteSpace(fullName) Then
            Return "?"
        End If


        Dim parts() As String =
            fullName.Trim().
            Split(
                New Char() {" "c},
                StringSplitOptions.RemoveEmptyEntries)


        If parts.Length = 1 Then

            Return parts(0).
                Substring(0, 1).
                ToUpper()

        End If


        Return (
            parts(0).Substring(0, 1) &
            parts(parts.Length - 1).Substring(0, 1)
        ).ToUpper()

    End Function


' ==========================================================
' ACTION BUTTON RECTANGLES
' ==========================================================

Private Function ViewRect(
    cellWidth As Integer,
    cellHeight As Integer) As Rectangle

    Return New Rectangle(
        10,
        (cellHeight - 32) \ 2,
        74,
        32)

End Function


Private Function EditRect(
    cellWidth As Integer,
    cellHeight As Integer) As Rectangle

    Return New Rectangle(
        94,
        (cellHeight - 32) \ 2,
        74,
        32)

End Function


Private Function DeleteRect(
    cellWidth As Integer,
    cellHeight As Integer) As Rectangle

    Return New Rectangle(
        178,
        (cellHeight - 32) \ 2,
        82,
        32)

End Function


Private Function HitAction(
    pt As Point,
    cellWidth As Integer,
    cellHeight As Integer) As Integer

    If ViewRect(
        cellWidth,
        cellHeight).Contains(pt) Then

        Return 1

    End If


    If EditRect(
        cellWidth,
        cellHeight).Contains(pt) Then

        Return 2

    End If


    If DeleteRect(
        cellWidth,
        cellHeight).Contains(pt) Then

        Return 3

    End If


    Return 0

End Function

' ==========================================================
' ACTION BUTTONS
' ==========================================================

Private Sub PaintActionsCell(
    e As DataGridViewCellPaintingEventArgs)

    e.Handled = True


    Dim rowColor As Color =
        Theme.CardColor


    If tableHoverRow = e.RowIndex Then

        rowColor =
            Theme.Lerp(
                Theme.CardColor,
                Theme.NeonPurple,
                0.08F)

    End If


    Using brush As New SolidBrush(
        rowColor)

        e.Graphics.FillRectangle(
            brush,
            e.CellBounds)

    End Using


    Using pen As New Pen(
        Theme.LineColor,
        1.0F)

        e.Graphics.DrawLine(
            pen,
            e.CellBounds.Left,
            e.CellBounds.Bottom - 1,
            e.CellBounds.Right,
            e.CellBounds.Bottom - 1)

    End Using


    e.Graphics.SmoothingMode =
        SmoothingMode.AntiAlias


    ' ------------------------------------------------------
    ' VIEW
    ' ------------------------------------------------------

    Dim viewButton As Rectangle =
        ViewRect(
            e.CellBounds.Width,
            e.CellBounds.Height)

    viewButton.Offset(
        e.CellBounds.X,
        e.CellBounds.Y)


    ' ------------------------------------------------------
    ' EDIT
    ' ------------------------------------------------------

    Dim editButton As Rectangle =
        EditRect(
            e.CellBounds.Width,
            e.CellBounds.Height)

    editButton.Offset(
        e.CellBounds.X,
        e.CellBounds.Y)


    ' ------------------------------------------------------
    ' DELETE
    ' ------------------------------------------------------

    Dim deleteButton As Rectangle =
        DeleteRect(
            e.CellBounds.Width,
            e.CellBounds.Height)

    deleteButton.Offset(
        e.CellBounds.X,
        e.CellBounds.Y)


    ' ------------------------------------------------------
    ' DRAW BUTTONS
    ' ------------------------------------------------------

    DrawActionButton(
        e.Graphics,
        viewButton,
        "VIEW",
        Theme.NeonGreen,
        hoverRow = e.RowIndex AndAlso
        hoverAction = 1)


    DrawActionButton(
        e.Graphics,
        editButton,
        "EDIT",
        Theme.NeonCyan,
        hoverRow = e.RowIndex AndAlso
        hoverAction = 2)


    DrawActionButton(
        e.Graphics,
        deleteButton,
        "DELETE",
        Theme.NeonPink,
        hoverRow = e.RowIndex AndAlso
        hoverAction = 3)

End Sub

    ' ==========================================================
    ' DRAW ACTION BUTTON
    ' ==========================================================

    Private Sub DrawActionButton(
        g As Graphics,
        rect As Rectangle,
        caption As String,
        accent As Color,
        hot As Boolean)

        Dim alpha As Integer

        If hot Then
            alpha = 80
        Else
            alpha = 24
        End If


        Dim borderAlpha As Integer

        If hot Then
            borderAlpha = 255
        Else
            borderAlpha = 150
        End If


        Dim drawRect As Rectangle =
            rect


        If hot Then

            drawRect =
                New Rectangle(
                    rect.X - 1,
                    rect.Y - 1,
                    rect.Width + 2,
                    rect.Height + 2)

        End If


        Using path As GraphicsPath =
            Theme.RoundedRect(
                drawRect,
                8)

            Using brush As New SolidBrush(
                Color.FromArgb(
                    alpha,
                    accent))

                g.FillPath(
                    brush,
                    path)

            End Using


            Using pen As New Pen(
                Color.FromArgb(
                    borderAlpha,
                    accent),
                1.2F)

                g.DrawPath(
                    pen,
                    path)

            End Using

        End Using


        TextRenderer.DrawText(
            g,
            caption,
            Theme.UiFont(
                8.0F,
                FontStyle.Bold),
            rect,
            accent,
            TextFormatFlags.HorizontalCenter Or
            TextFormatFlags.VerticalCenter Or
            TextFormatFlags.NoPadding)

    End Sub


    ' ==========================================================
    ' MOUSE HOVER
    ' ==========================================================

    Private Sub Grid_CellMouseMove(
        sender As Object,
        e As DataGridViewCellMouseEventArgs)

        Dim newRow As Integer =
            -1

        Dim newAction As Integer =
            0


        If e.RowIndex >= 0 Then

            newRow =
                e.RowIndex


            If e.ColumnIndex >= 0 AndAlso
               grid.Columns(
                   e.ColumnIndex).Name =
                   "Actions" Then

                newAction =
                    HitAction(
                        e.Location,
                        grid.Columns(
                            e.ColumnIndex).Width,
                        grid.Rows(
                            e.RowIndex).Height)

            End If

        End If


        If newAction <> 0 Then

            grid.Cursor =
                Cursors.Hand

        Else

            grid.Cursor =
                Cursors.Default

        End If


        ' ------------------------------------------------------
        ' WHOLE ROW HOVER
        ' ------------------------------------------------------

        If newRow <> tableHoverRow Then

            Dim oldRow As Integer =
                tableHoverRow


            tableHoverRow =
                newRow


            If oldRow >= 0 AndAlso
               oldRow < grid.RowCount Then

                grid.InvalidateRow(
                    oldRow)

            End If


            If newRow >= 0 AndAlso
               newRow < grid.RowCount Then

                grid.InvalidateRow(
                    newRow)

            End If

        End If


        ' ------------------------------------------------------
        ' ACTION BUTTON HOVER
        ' ------------------------------------------------------

        If newRow <> hoverRow OrElse
           newAction <> hoverAction Then

            Dim oldActionRow As Integer =
                hoverRow


            hoverRow =
                newRow

            hoverAction =
                newAction


            InvalidateActionCell(
                oldActionRow)

            InvalidateActionCell(
                newRow)

        End If

    End Sub


    ' ==========================================================
    ' MOUSE LEAVE
    ' ==========================================================

    Private Sub Grid_CellMouseLeave(
        sender As Object,
        e As DataGridViewCellEventArgs)

        grid.Cursor =
            Cursors.Default


        Dim oldRow As Integer =
            tableHoverRow


        tableHoverRow =
            -1

        hoverRow =
            -1

        hoverAction =
            0


        If oldRow >= 0 AndAlso
           oldRow < grid.RowCount Then

            grid.InvalidateRow(
                oldRow)

        End If


        InvalidateActionCell(
            oldRow)

    End Sub


    ' ==========================================================
    ' INVALIDATE ACTION CELL
    ' ==========================================================

    Private Sub InvalidateActionCell(
        rowIndex As Integer)

        If rowIndex >= 0 AndAlso
           rowIndex < grid.RowCount Then

            grid.InvalidateCell(
                grid.Columns(
                    "Actions").Index,
                rowIndex)

        End If

    End Sub


    ' ==========================================================
    ' ACTION CLICK
    ' ==========================================================

    Private Sub Grid_CellMouseClick(
        sender As Object,
        e As DataGridViewCellMouseEventArgs)

        If e.RowIndex < 0 OrElse
           e.ColumnIndex < 0 Then

            Return

        End If


        If e.Button <>
           MouseButtons.Left Then

            Return

        End If


        If grid.Columns(
            e.ColumnIndex).Name <>
            "Actions" Then

            Return

        End If


        Dim action As Integer =
            HitAction(
                e.Location,
                grid.Columns(
                    e.ColumnIndex).Width,
                grid.Rows(
                    e.RowIndex).Height)

Select Case action

    Case 1

        OpenRepairHistory(
            e.RowIndex)


    Case 2

        OpenEditCustomer(
            e.RowIndex)


    Case 3

        DeleteCustomer(
            e.RowIndex)

End Select


    End Sub


    ' ==========================================================
    ' ADD CUSTOMER
    ' ==========================================================

    Private Sub AddCustomer(
        sender As Object,
        e As EventArgs)

        Using form As New AddCustomerForm(db)

            Dim parentForm As Form =
                Me.FindForm()


            If form.ShowDialog(parentForm) =
               DialogResult.OK Then

                ReloadAfterChange()

            End If

        End Using

    End Sub
' ==========================================================
' VIEW REPAIR HISTORY
' ==========================================================

Private Sub OpenRepairHistory(
    rowIndex As Integer)

    Try

        Dim customerId As Integer =
            Convert.ToInt32(
                grid.Rows(
                    rowIndex).
                Cells(
                    "CustomerId").
                Value)


        Dim customerName As String =
            Convert.ToString(
                grid.Rows(
                    rowIndex).
                Cells(
                    "FullName").
                Value)


        Using historyForm As New RepairHistoryForm(
            db,
            customerId,
            customerName)

            Dim parentForm As Form =
                Me.FindForm()


            historyForm.ShowDialog(
                parentForm)

        End Using


    Catch ex As Exception

        MessageBox.Show(
            "Unable to open repair history." &
            Environment.NewLine &
            Environment.NewLine &
            ex.Message,
            "Repair History",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error)

    End Try

End Sub


    ' ==========================================================
    ' EDIT CUSTOMER
    ' ==========================================================

    Private Sub OpenEditCustomer(
        rowIndex As Integer)

        Try

            Dim customerId As Integer =
                Convert.ToInt32(
                    grid.Rows(
                        rowIndex).
                    Cells(
                        "CustomerId").
                    Value)


            Dim customer As Customer =
                customerController.GetById(
                    customerId)


            If customer Is Nothing Then

                MessageBox.Show(
                    "Customer was not found.",
                    "Edit Customer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

                Return

            End If


            Using editForm As New EditCustomerForm(
                db,
                customer)

                Dim parentForm As Form =
                    Me.FindForm()


                Dim result As DialogResult =
                    editForm.ShowDialog(
                        parentForm)


                If result =
                   DialogResult.OK Then

                    ReloadAfterChange()

                End If

            End Using


        Catch ex As Exception

            MessageBox.Show(
                "Unable to open customer." &
                Environment.NewLine &
                Environment.NewLine &
                ex.Message,
                "Edit Customer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        End Try

    End Sub



    ' ==========================================================
    ' DELETE CUSTOMER
    ' ==========================================================

    Private Sub DeleteCustomer(
        rowIndex As Integer)

        Dim customerId As Integer =
            Convert.ToInt32(
                grid.Rows(
                    rowIndex).
                Cells(
                    "CustomerId").
                Value)


        Dim customerName As String =
            Convert.ToString(
                grid.Rows(
                    rowIndex).
                Cells(
                    "FullName").
                Value)


        Dim result As DialogResult =
            MessageBox.Show(
                "Delete " &
                customerName &
                "?" &
                Environment.NewLine &
                Environment.NewLine &
                "This action cannot be undone.",
                "Delete Customer",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning)


        If result <>
           DialogResult.Yes Then

            Return

        End If


        Try

            customerController.Delete(
                customerId)

            ReloadAfterChange()


        Catch ex As Exception

            MessageBox.Show(
                "Unable to delete customer." &
                Environment.NewLine &
                Environment.NewLine &
                ex.Message,
                "Database Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        End Try

    End Sub


    ' ==========================================================
    ' RELOAD
    ' ==========================================================

  Private Sub ReloadAfterChange()
    LoadCustomers()
End Sub

End Class