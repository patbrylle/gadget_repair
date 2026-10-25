Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Forms

    Partial Public Class TechnicianForm
        Inherits Form

        ' ============================================================
        ' CONTROLLER
        ' ============================================================

        Private ReadOnly db As ApplicationDbContext
        Private ReadOnly technicianController As TechnicianController


        ' ============================================================
        ' MAIN UI
        ' ============================================================

        Private mainPanel As Panel
        Private headerPanel As Panel

        Private lblHeading As Label
        Private lblSubheading As Label
        Private btnNew As NeonButton


        ' ============================================================
        ' STATISTICS
        ' ============================================================

        Private statsTable As TableLayoutPanel

        Private statTotal As StatCard
        Private statAvailable As StatCard
        Private statBusy As StatCard
        Private statInactive As StatCard


        ' ============================================================
        ' TOOLBAR
        ' ============================================================

        Private toolbarPanel As Panel

        Private searchHost As NeonInput
        Private txtSearch As TextBox

        Private filterHost As NeonInput
        Private cboFilter As ComboBox


        ' ============================================================
        ' GRID
        ' ============================================================

        Private gridCard As NeonCard
        Private grid As SmoothDataGridView

        Private lblCount As Label


        ' ============================================================
        ' STATE
        ' ============================================================

        Private loading As Boolean = False

        Private Const SearchHint As String =
            "Search technician, specialization or contact..."

        Private searchIsHint As Boolean = True


        ' ============================================================
        ' HOVER STATE
        ' ============================================================

        Private hoverRow As Integer = -1
        Private hoverAction As Integer = 0

        Private tableHoverRow As Integer = -1


        ' ============================================================
        ' STATUS LIST
        ' ============================================================

        Private ReadOnly StatusList As String() = {
            "Available",
            "Busy",
            "Inactive"
        }


        ' ============================================================
        ' CONSTRUCTOR
        ' ============================================================

        Public Sub New(database As ApplicationDbContext)


            db = database

            technicianController =
                New TechnicianController(db)

            loading = True

            BuildInterface()

            loading = False

            AddHandler Me.Load,
                AddressOf TechnicianForm_Load

        End Sub


        ' ============================================================
        ' FORM LOAD
        ' ============================================================

        Private Sub TechnicianForm_Load(
            sender As Object,
            e As EventArgs)

            If technicianController Is Nothing Then

                MessageBox.Show(
                    "Technician controller is not initialized.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

                Return

            End If

            LoadTechnicians()

        End Sub


        ' ============================================================
        ' MAIN INTERFACE
        ' ============================================================

        Private Sub BuildInterface()

            Me.Dock =
                DockStyle.Fill

            Me.BackColor =
                Theme.BackgroundColor

            Me.Padding =
                New Padding(
                    30,
                    28,
                    30,
                    26)

            Me.Font =
                Theme.UiFont(9.0F)

            Me.DoubleBuffered =
                True


            BuildHeader()

            BuildStatistics()

            BuildToolbar()

            BuildGrid()


            Me.Controls.Add(
                gridCard)

            Me.Controls.Add(
                toolbarPanel)

            Me.Controls.Add(
                statsTable)

            Me.Controls.Add(
                headerPanel)

        End Sub


        ' ============================================================
        ' HEADER
        ' ============================================================

        Private Sub BuildHeader()

            headerPanel =
                New Panel()

            headerPanel.Dock =
                DockStyle.Top

            headerPanel.Height =
                84

            headerPanel.BackColor =
                Theme.BackgroundColor


            ' --------------------------------------------------------
            ' TITLE
            ' --------------------------------------------------------

            lblHeading =
                New Label()

            lblHeading.Text =
                "Technicians"

            lblHeading.AutoSize =
                True

            lblHeading.Location =
                New Point(
                    0,
                    0)

            lblHeading.Font =
                Theme.UiFont(
                    23.0F,
                    FontStyle.Bold)

            lblHeading.ForeColor =
                Theme.TextWhite


            ' --------------------------------------------------------
            ' SUBTITLE
            ' --------------------------------------------------------

            lblSubheading =
                New Label()

            lblSubheading.Text =
                "Manage technicians, availability and assigned repairs"

            lblSubheading.AutoSize =
                True

            lblSubheading.Location =
                New Point(
                    2,
                    42)

            lblSubheading.Font =
                Theme.UiFont(
                    9.5F)

            lblSubheading.ForeColor =
                Theme.TextMuted


            ' --------------------------------------------------------
            ' ADD TECHNICIAN
            ' --------------------------------------------------------

            btnNew =
                New NeonButton()

            btnNew.Text =
                "+  ADD TECHNICIAN"

            btnNew.Filled =
                True

            btnNew.Accent =
                Theme.NeonPurple

            btnNew.Size =
                New Size(
                    185,
                    44)

            btnNew.Anchor =
                AnchorStyles.Top Or
                AnchorStyles.Right

            btnNew.Top =
                2


            AddHandler btnNew.Click,
                AddressOf btnNew_Click


            headerPanel.Controls.Add(
                lblHeading)

            headerPanel.Controls.Add(
                lblSubheading)

            headerPanel.Controls.Add(
                btnNew)


            ' --------------------------------------------------------
            ' RESPONSIVE BUTTON
            ' --------------------------------------------------------

            AddHandler headerPanel.Resize,
                Sub(sender, e)

                    btnNew.Left =
                        headerPanel.ClientSize.Width -
                        btnNew.Width

                End Sub

        End Sub


        ' ============================================================
        ' STATISTICS
        ' ============================================================

        Private Sub BuildStatistics()

            statsTable =
                New TableLayoutPanel()

            statsTable.Dock =
                DockStyle.Top

            statsTable.Height =
                108

            statsTable.ColumnCount =
                4

            statsTable.RowCount =
                1

            statsTable.BackColor =
                Theme.BackgroundColor

            statsTable.Margin =
                New Padding(0)


            ' --------------------------------------------------------
            ' COLUMNS
            ' --------------------------------------------------------

            For i As Integer = 1 To 4

                statsTable.ColumnStyles.Add(
                    New ColumnStyle(
                        SizeType.Percent,
                        25.0F))

            Next


            statsTable.RowStyles.Add(
                New RowStyle(
                    SizeType.Percent,
                    100.0F))


            ' --------------------------------------------------------
            ' TOTAL
            ' --------------------------------------------------------

            statTotal =
                New StatCard()

            statTotal.Caption =
                "TOTAL TECHNICIANS"

            statTotal.Accent =
                Theme.NeonPurple

            statTotal.Dock =
                DockStyle.Fill

            statTotal.Margin =
                New Padding(
                    0,
                    0,
                    10,
                    16)


            ' --------------------------------------------------------
            ' AVAILABLE
            ' --------------------------------------------------------

            statAvailable =
                New StatCard()

            statAvailable.Caption =
                "AVAILABLE"

            statAvailable.Accent =
                Theme.NeonGreen

            statAvailable.Dock =
                DockStyle.Fill

            statAvailable.Margin =
                New Padding(
                    5,
                    0,
                    5,
                    16)


            ' --------------------------------------------------------
            ' BUSY
            ' --------------------------------------------------------

            statBusy =
                New StatCard()

            statBusy.Caption =
                "BUSY"

            statBusy.Accent =
                Theme.NeonCyan

            statBusy.Dock =
                DockStyle.Fill

            statBusy.Margin =
                New Padding(
                    5,
                    0,
                    5,
                    16)


            ' --------------------------------------------------------
            ' INACTIVE
            ' --------------------------------------------------------

            statInactive =
                New StatCard()

            statInactive.Caption =
                "INACTIVE"

            statInactive.Accent =
                Theme.NeonPink

            statInactive.Dock =
                DockStyle.Fill

            statInactive.Margin =
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
                statAvailable,
                1,
                0)

            statsTable.Controls.Add(
                statBusy,
                2,
                0)

            statsTable.Controls.Add(
                statInactive,
                3,
                0)

        End Sub


        ' ============================================================
        ' TOOLBAR
        ' ============================================================
        Private Sub BuildToolbar()

            toolbarPanel = New Panel()

            toolbarPanel.Dock = DockStyle.Top
            toolbarPanel.Height = 68
            toolbarPanel.BackColor = Theme.BackgroundColor


            ' SEARCH
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


            ' FILTER
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

            AddHandler toolbarPanel.Resize, AddressOf ResizeTechnicianToolbar

            ResizeTechnicianToolbar(toolbarPanel, EventArgs.Empty)

        End Sub


        ' ============================================================
        ' RESPONSIVE TOOLBAR
        ' ============================================================

        Private Sub ResizeTechnicianToolbar(
            sender As Object,
            e As EventArgs)

            If toolbarPanel Is Nothing OrElse
               searchHost Is Nothing OrElse
               filterHost Is Nothing Then

                Return

            End If


            If toolbarPanel.ClientSize.Width <= 0 Then
                Return
            End If


            filterHost.Left =
                toolbarPanel.ClientSize.Width -
                filterHost.Width


            Dim spacing As Integer =
                22

            Dim availableWidth As Integer =
                filterHost.Left -
                spacing

            Dim minimumWidth As Integer =
                300

            Dim maximumWidth As Integer =
                560


            Dim finalWidth As Integer =
                Math.Min(
                    maximumWidth,
                    Math.Max(
                        minimumWidth,
                        availableWidth))


            If toolbarPanel.ClientSize.Width <
               minimumWidth +
               filterHost.Width +
               spacing Then

                finalWidth =
                    Math.Max(
                        220,
                        availableWidth)

            End If


            searchHost.Width =
                finalWidth

        End Sub


        ' ============================================================
        ' GRID
        ' ============================================================

        Private Sub BuildGrid()

            gridCard =
                New NeonCard()

            gridCard.Dock =
                DockStyle.Fill

            gridCard.Padding =
                New Padding(
                    16,
                    14,
                    16,
                    10)

            grid = New SmoothDataGridView()
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


            ' ========================================================
            ' HEADER STYLE
            ' ========================================================

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


            ' ========================================================
            ' NORMAL CELL STYLE
            ' ========================================================

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
                    Theme.UiFont(
                        9.5F)

                .Padding =
                    New Padding(
                        12,
                        0,
                        0,
                        0)

            End With


            ' ========================================================
            ' ALTERNATING ROW
            ' ========================================================

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


            ' ========================================================
            ' ID
            ' ========================================================

            Dim colId As New DataGridViewTextBoxColumn()

            colId.Name =
                "colId"

            colId.HeaderText =
                "ID"

            colId.Visible =
                False


            ' ========================================================
            ' TECHNICIAN
            ' ========================================================

            Dim colTechnician As New DataGridViewTextBoxColumn()

            colTechnician.Name =
                "colTechnician"

            colTechnician.HeaderText =
                "TECHNICIAN"

            colTechnician.FillWeight =
                22.0F


            ' ========================================================
            ' SPECIALIZATION
            ' ========================================================

            Dim colSpecialization As New DataGridViewTextBoxColumn()

            colSpecialization.Name =
                "colSpecialization"

            colSpecialization.HeaderText =
                "SPECIALIZATION"

            colSpecialization.FillWeight =
                20.0F


            ' ========================================================
            ' CONTACT
            ' ========================================================

            Dim colContact As New DataGridViewTextBoxColumn()

            colContact.Name =
                "colContact"

            colContact.HeaderText =
                "CONTACT"

            colContact.FillWeight =
                16.0F


            ' ========================================================
            ' EMAIL
            ' ========================================================

            Dim colEmail As New DataGridViewTextBoxColumn()

            colEmail.Name =
                "colEmail"

            colEmail.HeaderText =
                "EMAIL"

            colEmail.FillWeight =
                22.0F

            colEmail.DefaultCellStyle.ForeColor =
                Theme.NeonCyan


            ' ========================================================
            ' STATUS
            ' ========================================================

            Dim colStatus As New DataGridViewTextBoxColumn()

            colStatus.Name =
                "colStatus"

            colStatus.HeaderText =
                "STATUS"

            colStatus.FillWeight =
                17.0F


            ' ========================================================
            ' REPAIRS
            ' ========================================================

            Dim colRepairs As New DataGridViewTextBoxColumn()

            colRepairs.Name =
                "colRepairs"

            colRepairs.HeaderText =
                "REPAIRS"

            colRepairs.FillWeight =
                12.0F

            colRepairs.DefaultCellStyle.ForeColor =
                Theme.NeonGreen


            ' ========================================================
            ' ACTIONS
            ' ========================================================

            Dim colActions As New DataGridViewTextBoxColumn()

            colActions.Name =
                "colActions"

            colActions.HeaderText =
                "ACTIONS"

            colActions.AutoSizeMode =
                DataGridViewAutoSizeColumnMode.None

            colActions.Width =
                290

            colActions.Resizable =
                DataGridViewTriState.False


            grid.Columns.AddRange(
                New DataGridViewColumn() {
                    colId,
                    colTechnician,
                    colSpecialization,
                    colContact,
                    colEmail,
                    colStatus,
                    colRepairs,
                    colActions
                })


            ' ========================================================
            ' EVENTS
            ' ========================================================

            AddHandler grid.CellPainting,
                AddressOf Grid_CellPainting

            AddHandler grid.CellMouseMove,
                AddressOf Grid_CellMouseMove

            AddHandler grid.CellMouseLeave,
                AddressOf Grid_CellMouseLeave

            AddHandler grid.CellMouseClick,
                AddressOf Grid_CellMouseClick

            AddHandler grid.CellDoubleClick,
                AddressOf Grid_CellDoubleClick

                            AddHandler grid.MouseEnter, AddressOf Grid_MouseEnter
            AddHandler grid.Scroll, AddressOf Grid_Scroll


            ' ========================================================
            ' FOOTER
            ' ========================================================

            lblCount =
                New Label()

            lblCount.Dock =
                DockStyle.Bottom

            lblCount.Height =
                32

            lblCount.TextAlign =
                ContentAlignment.MiddleLeft

            lblCount.ForeColor =
                Theme.TextMuted

            lblCount.Font =
                Theme.UiFont(
                    8.5F)

            lblCount.Padding =
                New Padding(
                    6,
                    0,
                    0,
                    0)


            gridCard.Controls.Add(
                grid)

            gridCard.Controls.Add(
                lblCount)

        End Sub


        ' ============================================================
        ' LOAD TECHNICIANS
        ' ============================================================

        Private Sub LoadTechnicians()

            If technicianController Is Nothing Then
                Return
            End If

            If grid Is Nothing Then
                Return
            End If

            If cboFilter Is Nothing Then
                Return
            End If

            If txtSearch Is Nothing Then
                Return
            End If


            loading =
                True


            Try

                grid.Rows.Clear()


                ' ====================================================
                ' SEARCH
                ' ====================================================

                Dim searchText As String =
                    ""


                If Not searchIsHint AndAlso
                   Not String.IsNullOrWhiteSpace(
                       txtSearch.Text) Then

                    searchText =
                        txtSearch.Text.Trim()

                End If


                ' ====================================================
                ' STATUS FILTER
                ' ====================================================

                Dim statusFilter As String =
                    "All Status"


                If cboFilter.SelectedItem IsNot Nothing Then

                    statusFilter =
                        cboFilter.SelectedItem.ToString()

                End If


                ' ====================================================
                ' CONTROLLER
                ' ====================================================

                Dim technicians As List(Of Technician) =
                    technicianController.GetTechnicians(
                        searchText,
                        statusFilter)


                ' ====================================================
                ' DISPLAY
                ' ====================================================

                For Each technician As Technician In technicians

                    Dim repairCount As Integer =
                        0


                    If technician.Repairs IsNot Nothing Then

                        repairCount =
                            technician.Repairs.Count

                    End If


                    Dim specialization As String =
                        If(
                            String.IsNullOrWhiteSpace(
                                technician.Specialization),
                            "—",
                            technician.Specialization)


                    Dim contact As String =
                        If(
                            String.IsNullOrWhiteSpace(
                                technician.ContactNumber),
                            "—",
                            technician.ContactNumber)


                    Dim email As String =
                        If(
                            String.IsNullOrWhiteSpace(
                                technician.Email),
                            "—",
                            technician.Email)


                    Dim status As String =
                        If(
                            String.IsNullOrWhiteSpace(
                                technician.Status),
                            "Inactive",
                            technician.Status)


                    Dim rowIndex As Integer =
                        grid.Rows.Add(
                            technician.TechnicianId,
                            technician.TechnicianName,
                            specialization,
                            contact,
                            email,
                            status,
                            repairCount.ToString(),
                            "")


                    grid.Rows(
                        rowIndex).Tag =
                        technician.TechnicianId

                Next


                ' ====================================================
                ' STATISTICS
                ' ====================================================

                UpdateStatistics(
                    technicians)


                ' ====================================================
                ' FOOTER
                ' ====================================================

                lblCount.Text =
                    technicians.Count.ToString() &
                    If(
                        technicians.Count = 1,
                        " technician",
                        " technicians")


            Catch ex As Exception

                MessageBox.Show(
                    "Unable to load technicians." &
                    Environment.NewLine &
                    Environment.NewLine &
                    ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

            Finally

                loading =
                    False

            End Try

        End Sub


        ' ============================================================
        ' STATISTICS
        ' ============================================================

               Private Sub UpdateStatistics(
            technicians As List(Of Technician))

            statTotal.Value = technicianController.GetTotalTechnicians().ToString()
            statAvailable.Value = technicianController.GetAvailableCount().ToString()
            statBusy.Value = technicianController.GetBusyCount().ToString()
            statInactive.Value = technicianController.GetInactiveCount().ToString()

        End Sub

        ' ============================================================
        ' ADD TECHNICIAN
        ' ============================================================

        Private Sub btnNew_Click(
            sender As Object,
            e As EventArgs)

            OpenTechnicianModal(
                0)

        End Sub


        ' ============================================================
        ' OPEN TECHNICIAN MODAL
        ' ============================================================

        Private Sub OpenTechnicianModal(
            technicianId As Integer)

            Using modal As New TechnicianEditDialog(
                technicianController,
                technicianId)

                Dim result As DialogResult =
                    modal.ShowDialog(
                        Me)


                If result =
                   DialogResult.OK Then

                    LoadTechnicians()

                End If

            End Using

        End Sub


        ' ============================================================
        ' DOUBLE CLICK EDIT
        ' ============================================================

        Private Sub Grid_CellDoubleClick(
            sender As Object,
            e As DataGridViewCellEventArgs)

            If e.RowIndex < 0 Then
                Return
            End If

            If e.ColumnIndex < 0 Then
                Return
            End If


            If grid.Columns(
                e.ColumnIndex).Name =
                "colActions" Then

                Return

            End If


            Dim row As DataGridViewRow =
                grid.Rows(
                    e.RowIndex)


            If row.Tag Is Nothing Then
                Return
            End If


            Dim technicianId As Integer =
                Convert.ToInt32(
                    row.Tag)


            OpenTechnicianModal(
                technicianId)

        End Sub


        ' ============================================================
        ' DELETE TECHNICIAN
        ' ============================================================

        Private Sub DeleteTechnician(
            technicianId As Integer)

            Dim result As DialogResult =
                MessageBox.Show(
                    "Are you sure you want to delete this technician?" &
                    Environment.NewLine &
                    Environment.NewLine &
                    "This action cannot be undone.",
                    "Delete Technician",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning)


            If result <>
               DialogResult.Yes Then

                Return

            End If


            Try

                Dim success As Boolean =
                    technicianController.DeleteTechnician(
                        technicianId)


                If Not success Then

                    MessageBox.Show(
                        "Technician could not be found.",
                        "Delete Technician",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)

                    Return

                End If


                LoadTechnicians()


            Catch ex As InvalidOperationException

                MessageBox.Show(
                    ex.Message,
                    "Cannot Delete Technician",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)


            Catch ex As Exception

                MessageBox.Show(
                    "Unable to delete technician." &
                    Environment.NewLine &
                    Environment.NewLine &
                    ex.Message,
                    "Delete Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

            End Try

        End Sub


        ' ============================================================
        ' SEARCH ENTER
        ' ============================================================

                Private Sub Search_Enter(sender As Object, e As EventArgs)

            If searchIsHint Then

                searchIsHint = False

                loading = True
                txtSearch.Text = ""
                loading = False

                txtSearch.ForeColor = Theme.TextWhite

            End If

        End Sub


        Private Sub Search_Leave(sender As Object, e As EventArgs)

            If String.IsNullOrWhiteSpace(txtSearch.Text) Then

                searchIsHint = True

                loading = True
                txtSearch.Text = SearchHint
                loading = False

                txtSearch.ForeColor = Theme.TextMuted

            End If

        End Sub

        ' ============================================================
        ' SEARCH TEXT CHANGED
        ' ============================================================

        Private Sub Search_TextChanged(
            sender As Object,
            e As EventArgs)

            If loading Then
                Return
            End If

            If searchIsHint Then
                Return
            End If


            LoadTechnicians()

        End Sub


        ' ============================================================
        ' FILTER CHANGED
        ' ============================================================

        Private Sub Filter_Changed(
            sender As Object,
            e As EventArgs)

            If loading Then
                Return
            End If


            LoadTechnicians()

        End Sub


        ' ============================================================
        ' FILTER DRAW
        ' ============================================================

        Private Sub Filter_DrawItem(
            sender As Object,
            e As DrawItemEventArgs)

            If e.Index < 0 Then
                Return
            End If


            Dim combo As ComboBox =
                DirectCast(
                    sender,
                    ComboBox)


            Dim selected As Boolean =
                (e.State And
                 DrawItemState.Selected) =
                DrawItemState.Selected


            Dim background As Color =
                If(
                    selected,
                    Theme.Lerp(
                        Theme.InputColor,
                        Theme.NeonPurple,
                        0.20F),
                    Theme.InputColor)


            Using brush As New SolidBrush(
                background)

                e.Graphics.FillRectangle(
                    brush,
                    e.Bounds)

            End Using


            TextRenderer.DrawText(
                e.Graphics,
                combo.Items(
                    e.Index).ToString(),
                combo.Font,
                New Rectangle(
                    e.Bounds.X + 8,
                    e.Bounds.Y,
                    e.Bounds.Width - 8,
                    e.Bounds.Height),
                Theme.TextWhite,
                TextFormatFlags.Left Or
                TextFormatFlags.VerticalCenter Or
                TextFormatFlags.NoPadding)


            e.DrawFocusRectangle()

        End Sub


       

        ' ============================================================
        ' GRID MOUSE LEAVE
        ' ============================================================

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


        ' ============================================================
        ' INVALIDATE ACTION CELL
        ' ============================================================

        Private Sub InvalidateActionCell(
            rowIndex As Integer)

            If rowIndex >= 0 AndAlso
               rowIndex < grid.RowCount Then

                Dim actionColumn As Integer =
                    grid.Columns(
                        "colActions").Index


                grid.InvalidateCell(
                    actionColumn,
                    rowIndex)

            End If

        End Sub



        ' ============================================================
        ' GRID PAINTING
        ' ============================================================

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


            ' ========================================================
            ' ACTIONS
            ' ========================================================

            If columnName =
               "colActions" Then

                PaintActionsCell(
                    e)

                Return

            End If


            ' ========================================================
            ' STATUS
            ' ========================================================

            If columnName =
               "colStatus" Then

                PaintStatusCell(
                    e)

                Return

            End If


            ' ========================================================
            ' NORMAL CELL
            ' ========================================================

            Dim rowColor As Color =
                Theme.CardColor


            If tableHoverRow =
               e.RowIndex Then

                rowColor =
                    Theme.Lerp(
                        Theme.CardColor,
                        Theme.NeonPurple,
                        0.08F)

            End If


            e.Handled =
                True


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


            e.Paint(
                e.CellBounds,
                DataGridViewPaintParts.ContentForeground)

        End Sub


        ' ============================================================
        ' STATUS CELL
        ' ============================================================

        Private Sub PaintStatusCell(
            e As DataGridViewCellPaintingEventArgs)

            e.Handled =
                True


            Dim rowColor As Color =
                Theme.CardColor


            If tableHoverRow =
               e.RowIndex Then

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


            Dim status As String =
                If(
                    e.Value Is Nothing,
                    "",
                    e.Value.ToString())


            If String.IsNullOrWhiteSpace(
                status) Then

                Return

            End If


            Dim statusColor As Color =
                GetStatusColor(
                    status)


            Dim pillWidth As Integer =
                Math.Min(
                    e.CellBounds.Width - 20,
                    Math.Max(
                        82,
                        status.Length * 7 + 28))


            Dim pillHeight As Integer =
                28


            Dim pillX As Integer =
                e.CellBounds.X +
                (e.CellBounds.Width -
                 pillWidth) \ 2


            Dim pillY As Integer =
                e.CellBounds.Y +
                (e.CellBounds.Height -
                 pillHeight) \ 2


            Dim pillRect As New Rectangle(
                pillX,
                pillY,
                pillWidth,
                pillHeight)


            e.Graphics.SmoothingMode =
                SmoothingMode.AntiAlias


            Using path As GraphicsPath =
                CreateRoundedPath(
                    pillRect,
                    10)

                Using brush As New SolidBrush(
                    Color.FromArgb(
                        35,
                        statusColor))

                    e.Graphics.FillPath(
                        brush,
                        path)

                End Using


                Using pen As New Pen(
                    Color.FromArgb(
                        90,
                        statusColor),
                    1.0F)

                    e.Graphics.DrawPath(
                        pen,
                        path)

                End Using

            End Using


            TextRenderer.DrawText(
                e.Graphics,
                status,
                Theme.UiFont(
                    8.0F,
                    FontStyle.Bold),
                pillRect,
                statusColor,
                TextFormatFlags.HorizontalCenter Or
                TextFormatFlags.VerticalCenter Or
                TextFormatFlags.NoPadding)

        End Sub


        ' ============================================================
        ' STATUS COLOR
        ' ============================================================

        Private Function GetStatusColor(
            status As String) As Color

            Select Case status

                Case "Available"

                    Return Theme.NeonGreen


                Case "Busy"

                    Return Theme.NeonCyan


                Case "Inactive"

                    Return Theme.NeonPink


                Case Else

                    Return Theme.TextMuted

            End Select

        End Function

        ' 1 = REPAIRS, 2 = EDIT, 3 = DELETE
        Private Function ActionAt(x As Integer, cellWidth As Integer) As Integer

            Dim pad As Integer = 8
            Dim gap As Integer = 6
            Dim bw As Integer = (cellWidth - pad * 2 - gap * 2) \ 3

            If x >= pad AndAlso x < pad + bw Then Return 1

            If x >= pad + bw + gap AndAlso
               x < pad + bw * 2 + gap Then Return 2

            If x >= pad + bw * 2 + gap * 2 AndAlso
               x < cellWidth - pad Then Return 3

            Return 0

        End Function


        Private Sub Grid_MouseEnter(sender As Object, e As EventArgs)

            If cboFilter IsNot Nothing AndAlso cboFilter.Focused Then
                grid.Focus()
            End If

        End Sub


        Private Sub Grid_Scroll(sender As Object, e As ScrollEventArgs)

            If e.ScrollOrientation <> ScrollOrientation.VerticalScroll Then
                Return
            End If

            tableHoverRow = -1
            hoverRow = -1
            hoverAction = 0
            grid.Cursor = Cursors.Default
            grid.Invalidate()

        End Sub


        Private Sub OpenAssignedRepairs(technicianId As Integer)

            Dim technician As Technician =
                technicianController.GetTechnician(technicianId)

            If technician Is Nothing Then Return

            Using dlg As New TechnicianRepairsDialog(
                technicianController, technician)

                dlg.ShowDialog(Me)

            End Using

        End Sub


        Private Sub Grid_CellMouseMove(
            sender As Object,
            e As DataGridViewCellMouseEventArgs)

            Dim newRow As Integer = -1
            Dim newAction As Integer = 0

            If e.RowIndex >= 0 Then

                newRow = e.RowIndex

                If e.ColumnIndex >= 0 AndAlso
                   grid.Columns(e.ColumnIndex).Name = "colActions" Then

                    newAction = ActionAt(
                        e.X, grid.Columns(e.ColumnIndex).Width)

                End If

            End If

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


        Private Sub Grid_CellMouseClick(
            sender As Object,
            e As DataGridViewCellMouseEventArgs)

            If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
            If e.Button <> MouseButtons.Left Then Return

            If grid.Columns(e.ColumnIndex).Name <> "colActions" Then Return

            Dim row As DataGridViewRow = grid.Rows(e.RowIndex)

            If row.Tag Is Nothing Then Return

            Dim technicianId As Integer = Convert.ToInt32(row.Tag)

            Select Case ActionAt(e.X, grid.Columns(e.ColumnIndex).Width)

                Case 1
                    OpenAssignedRepairs(technicianId)

                Case 2
                    OpenTechnicianModal(technicianId)

                Case 3
                    DeleteTechnician(technicianId)

            End Select

        End Sub


        Private Sub PaintActionsCell(
            e As DataGridViewCellPaintingEventArgs)

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

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

            Dim pad As Integer = 8
            Dim gap As Integer = 6
            Dim bw As Integer = (e.CellBounds.Width - pad * 2 - gap * 2) \ 3
            Dim bh As Integer = 32
            Dim y As Integer = e.CellBounds.Y + (e.CellBounds.Height - bh) \ 2

            Dim repairsRect As New Rectangle(e.CellBounds.X + pad, y, bw, bh)
            Dim editRect As New Rectangle(e.CellBounds.X + pad + bw + gap, y, bw, bh)
            Dim deleteRect As New Rectangle(e.CellBounds.X + pad + bw * 2 + gap * 2, y, bw, bh)

            DrawActionButton(e.Graphics, repairsRect, "REPAIRS", Theme.NeonPurple,
                             hoverRow = e.RowIndex AndAlso hoverAction = 1)

            DrawActionButton(e.Graphics, editRect, "EDIT", Theme.NeonCyan,
                             hoverRow = e.RowIndex AndAlso hoverAction = 2)

            DrawActionButton(e.Graphics, deleteRect, "DELETE", Theme.NeonPink,
                             hoverRow = e.RowIndex AndAlso hoverAction = 3)

        End Sub
       


        ' ============================================================
        ' DRAW ACTION BUTTON
        ' ============================================================

        Private Sub DrawActionButton(
            g As Graphics,
            rect As Rectangle,
            caption As String,
            accent As Color,
            hot As Boolean)

            Dim alpha As Integer

            If hot Then

                alpha =
                    80

            Else

                alpha =
                    24

            End If


            Dim borderAlpha As Integer

            If hot Then

                borderAlpha =
                    255

            Else

                borderAlpha =
                    150

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


        ' ============================================================
        ' ROUNDED PATH
        ' ============================================================

        Private Function CreateRoundedPath(
            rect As Rectangle,
            radius As Integer) As GraphicsPath

            Dim path As New GraphicsPath()


            Dim diameter As Integer =
                radius * 2


            Dim arcRect As New Rectangle(
                rect.X,
                rect.Y,
                diameter,
                diameter)


            path.AddArc(
                arcRect,
                180,
                90)


            arcRect.X =
                rect.Right -
                diameter


            path.AddArc(
                arcRect,
                270,
                90)


            arcRect.Y =
                rect.Bottom -
                diameter


            path.AddArc(
                arcRect,
                0,
                90)


            arcRect.X =
                rect.X


            path.AddArc(
                arcRect,
                90,
                90)


            path.CloseFigure()


            Return path

        End Function


    End Class


    ' =================================================================
    ' TECHNICIAN EDIT DIALOG
    ' =================================================================
    ' =================================================================
    ' SHARED DIALOG FRAME (border, accent strip, header, footer, drag)
    ' =================================================================

    Public MustInherit Class NeonDialogBase
        Inherits Form

        Protected ReadOnly Body As Panel
        Protected ReadOnly Footer As Panel

        Private ReadOnly lblSubtitle As Label
        Private dragging As Boolean = False
        Private dragOffset As Point

        Protected Sub New(
            title As String,
            subtitle As String,
            formWidth As Integer,
            formHeight As Integer)

            Me.Text = title
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.None
            Me.MinimizeBox = False
            Me.MaximizeBox = False
            Me.ShowInTaskbar = False
            Me.Font = Theme.UiFont(9.0F)
            Me.DoubleBuffered = True
            Me.BackColor = Theme.LineColor
            Me.Padding = New Padding(1)
            Me.Size = New Size(formWidth, formHeight)


            Dim accentStrip As New Panel()
            accentStrip.Dock = DockStyle.Top
            accentStrip.Height = 2
            accentStrip.BackColor = Theme.NeonPurple


            Dim header As New Panel()
            header.Dock = DockStyle.Top
            header.Height = 60
            header.BackColor = Theme.BackgroundColor

            Dim lblTitle As New Label()
            lblTitle.AutoSize = True
            lblTitle.Location = New Point(24, 10)
            lblTitle.Font = Theme.UiFont(13.5F, FontStyle.Bold)
            lblTitle.ForeColor = Theme.TextWhite
            lblTitle.Text = title

            lblSubtitle = New Label()
            lblSubtitle.AutoSize = True
            lblSubtitle.Location = New Point(25, 36)
            lblSubtitle.Font = Theme.UiFont(8.5F)
            lblSubtitle.ForeColor = Theme.TextMuted
            lblSubtitle.Text = subtitle

            Dim btnClose As New NeonCloseButton()
            btnClose.Top = 14

            AddHandler btnClose.Click,
                Sub(s As Object, e As EventArgs)
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                End Sub

            Dim headerLine As New Panel()
            headerLine.Dock = DockStyle.Bottom
            headerLine.Height = 1
            headerLine.BackColor = Theme.LineColor

            header.Controls.Add(lblTitle)
            header.Controls.Add(lblSubtitle)
            header.Controls.Add(btnClose)
            header.Controls.Add(headerLine)

            AddHandler header.Resize,
                Sub(s As Object, e As EventArgs)
                    btnClose.Left = header.ClientSize.Width - btnClose.Width - 14
                End Sub

            For Each ctl As Control In New Control() {header, lblTitle, lblSubtitle}
                AddHandler ctl.MouseDown, AddressOf Header_MouseDown
                AddHandler ctl.MouseMove, AddressOf Header_MouseMove
                AddHandler ctl.MouseUp, AddressOf Header_MouseUp
            Next


            Footer = New Panel()
            Footer.Dock = DockStyle.Bottom
            Footer.Height = 62
            Footer.BackColor = Theme.BackgroundColor

            Dim footerLine As New Panel()
            footerLine.Dock = DockStyle.Top
            footerLine.Height = 1
            footerLine.BackColor = Theme.LineColor

            Footer.Controls.Add(footerLine)


            Body = New Panel()
            Body.Dock = DockStyle.Fill
            Body.BackColor = Theme.BackgroundColor


            Me.Controls.Add(Body)
            Me.Controls.Add(Footer)
            Me.Controls.Add(header)
            Me.Controls.Add(accentStrip)

        End Sub

        ' right-aligns the buttons in the footer, last one on the far right
        Protected Sub PlaceFooterButtons(ParamArray buttons As Control())

            For Each b As Control In buttons
                b.Top = 12
                Footer.Controls.Add(b)
            Next

            Dim layout As Action =
                Sub()
                    Dim x As Integer = Footer.ClientSize.Width - 24

                    For i As Integer = buttons.Length - 1 To 0 Step -1
                        x -= buttons(i).Width
                        buttons(i).Left = x
                        x -= 10
                    Next
                End Sub

            AddHandler Footer.Resize, Sub(s As Object, e As EventArgs) layout()

            layout()

        End Sub

        Private Sub Header_MouseDown(sender As Object, e As MouseEventArgs)
            If e.Button <> MouseButtons.Left Then Return
            dragging = True
            dragOffset = New Point(
                System.Windows.Forms.Cursor.Position.X - Me.Left,
                System.Windows.Forms.Cursor.Position.Y - Me.Top)
        End Sub

        Private Sub Header_MouseMove(sender As Object, e As MouseEventArgs)
            If Not dragging Then Return
            Me.Location = New Point(
                System.Windows.Forms.Cursor.Position.X - dragOffset.X,
                System.Windows.Forms.Cursor.Position.Y - dragOffset.Y)
        End Sub

        Private Sub Header_MouseUp(sender As Object, e As MouseEventArgs)
            dragging = False
        End Sub

        Protected Overrides Function ProcessCmdKey(
            ByRef msg As Message,
            keyData As Keys) As Boolean

            If keyData = Keys.Escape Then

                Dim combo = TryCast(Me.ActiveControl, ComboBox)

                If combo IsNot Nothing AndAlso combo.DroppedDown Then
                    Return MyBase.ProcessCmdKey(msg, keyData)
                End If

                Me.DialogResult = DialogResult.Cancel
                Me.Close()

                Return True

            End If

            Return MyBase.ProcessCmdKey(msg, keyData)

        End Function

    End Class


    ' =================================================================
    ' TECHNICIAN ADD / EDIT DIALOG
    ' =================================================================

    Public Class TechnicianEditDialog
        Inherits NeonDialogBase

        Private Const ColA As Integer = 24
        Private Const ColB As Integer = 312
        Private Const Width2 As Integer = 262
        Private Const WidthFull As Integer = 550

        Private ReadOnly technicianController As TechnicianController
        Private ReadOnly technicianId As Integer
        Private ReadOnly isNew As Boolean

        Private txtName As TextBox
        Private cboSpecialization As NeonComboBox
        Private cboStatus As NeonComboBox
        Private txtContact As TextBox
        Private txtEmail As TextBox
        Private txtPassword As TextBox
        Private txtConfirm As TextBox

        Private errName As Label
        Private errSpecialization As Label
        Private errContact As Label
        Private errEmail As Label
        Private errPassword As Label
        Private errConfirm As Label

        Private btnCancel As NeonButton
        Private btnSave As NeonButton


        Public Sub New(
            controller As TechnicianController,
            id As Integer)

            MyBase.New(
                If(id = 0, "New Technician", "Edit Technician"),
                If(id = 0,
                   "Create a technician profile and login account",
                   "Update technician information"),
                600,
                If(id = 0, 430, 364))

            technicianController = controller
            technicianId = id
            isNew = (id = 0)

            BuildFields()
            BuildButtons()

            If Not isNew Then LoadTechnician()

        End Sub


        ' ------------------------------------------------------------
        ' BUILD
        ' ------------------------------------------------------------

        Private Sub BuildFields()

            ' row 1: name
            txtName = NewTextBox()
            txtName.MaxLength = 100
            NeonLayout.AddField(Body, "TECHNICIAN NAME", txtName, ColA, 18, WidthFull)
            errName = NewErrorLabel(ColA, 18, WidthFull)
            Watch(txtName, errName)

            ' row 2: specialization / status
            cboSpecialization = NewCombo()

            For Each item As String In TechnicianController.SpecializationList
                cboSpecialization.Items.Add(item)
            Next

            NeonLayout.AddField(Body, "SPECIALIZATION", cboSpecialization, ColA, 84, Width2)
            errSpecialization = NewErrorLabel(ColA, 84, Width2)
            Watch(cboSpecialization, errSpecialization)

            cboStatus = NewCombo()

            For Each item As String In TechnicianController.StatusList
                cboStatus.Items.Add(item)
            Next

            cboStatus.SelectedIndex = 0
            NeonLayout.AddField(Body, "STATUS", cboStatus, ColB, 84, Width2)

            ' row 3: contact / email
            txtContact = NewTextBox()
            txtContact.MaxLength = 16

            AddHandler txtContact.KeyPress,
                Sub(s As Object, e As KeyPressEventArgs)

                    If Not Char.IsControl(e.KeyChar) AndAlso
                       Not Char.IsDigit(e.KeyChar) AndAlso
                       "+- ()".IndexOf(e.KeyChar) < 0 Then

                        e.Handled = True

                    End If

                End Sub

            NeonLayout.AddField(Body, "CONTACT NUMBER", txtContact, ColA, 150, Width2)
            errContact = NewErrorLabel(ColA, 150, Width2)
            Watch(txtContact, errContact)

            txtEmail = NewTextBox()
            txtEmail.MaxLength = 255
            NeonLayout.AddField(Body, "EMAIL", txtEmail, ColB, 150, Width2)
            errEmail = NewErrorLabel(ColB, 150, Width2)
            Watch(txtEmail, errEmail)

            ' row 4: password (new technicians only)
            txtPassword = NewTextBox()
            txtPassword.UseSystemPasswordChar = True
            txtPassword.MaxLength = 128

            txtConfirm = NewTextBox()
            txtConfirm.UseSystemPasswordChar = True
            txtConfirm.MaxLength = 128

            errPassword = NewErrorLabel(ColA, 216, Width2)
            errConfirm = NewErrorLabel(ColB, 216, Width2)

            If isNew Then

                NeonLayout.AddField(
                    Body, "TEMPORARY PASSWORD (8+, LETTERS + NUMBERS)",
                    txtPassword, ColA, 216, Width2)

                NeonLayout.AddField(
                    Body, "CONFIRM PASSWORD",
                    txtConfirm, ColB, 216, Width2)

                Watch(txtPassword, errPassword)
                Watch(txtConfirm, errConfirm)

            End If

        End Sub

        Private Sub BuildButtons()

            btnCancel = New NeonButton()
            btnCancel.Text = "CANCEL"
            btnCancel.Filled = False
            btnCancel.Accent = Theme.TextMuted
            btnCancel.Size = New Size(100, 38)

            AddHandler btnCancel.Click,
                Sub(s As Object, e As EventArgs)
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                End Sub

            btnSave = New NeonButton()
            btnSave.Text = If(isNew, "CREATE TECHNICIAN", "SAVE CHANGES")
            btnSave.Filled = True
            btnSave.Accent = Theme.NeonPurple
            btnSave.Size = New Size(If(isNew, 190, 160), 38)

            AddHandler btnSave.Click, AddressOf Save_Click

            PlaceFooterButtons(btnCancel, btnSave)

        End Sub

        Private Shared Function NewTextBox() As TextBox

            Dim box As New TextBox()

            box.BorderStyle = BorderStyle.None
            box.BackColor = Theme.InputColor
            box.ForeColor = Theme.TextWhite
            box.Font = Theme.UiFont(9.5F)

            Return box

        End Function

        Private Shared Function NewCombo() As NeonComboBox

            Dim combo As New NeonComboBox()

            combo.Font = Theme.UiFont(9.5F)

            Return combo

        End Function

        ' small red message under a field
        Private Function NewErrorLabel(
            x As Integer,
            y As Integer,
            w As Integer) As Label

            Dim lbl As New Label()

            lbl.AutoSize = False
            lbl.Location = New Point(
                x + 2,
                y + NeonLayout.CaptionGap + NeonLayout.HostHeight + 2)
            lbl.Size = New Size(w, 13)
            lbl.Font = Theme.UiFont(7.5F)
            lbl.ForeColor = Theme.NeonRed
            lbl.Text = ""

            Body.Controls.Add(lbl)

            Return lbl

        End Function

        ' clears the message as soon as the user edits the field
        Private Shared Sub Watch(ctl As Control, lbl As Label)

            AddHandler ctl.TextChanged,
                Sub(s As Object, e As EventArgs)
                    lbl.Text = ""
                End Sub

        End Sub


        ' ------------------------------------------------------------
        ' LOAD
        ' ------------------------------------------------------------

        Private Sub LoadTechnician()

            Try

                Dim technician As Technician =
                    technicianController.GetTechnician(technicianId)

                If technician Is Nothing Then

                    MessageBox.Show(
                        "Technician could not be found.",
                        "Technician",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)

                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                    Return

                End If

                txtName.Text = If(technician.TechnicianName, "")
                txtContact.Text = If(technician.ContactNumber, "")
                txtEmail.Text = If(technician.Email, "")

                Dim spec As String = If(technician.Specialization, "").Trim()

                If spec <> "" Then

                    Dim idx As Integer = cboSpecialization.FindStringExact(spec)

                    If idx < 0 Then idx = cboSpecialization.Items.Add(spec)

                    cboSpecialization.SelectedIndex = idx

                End If

                Dim status As String = If(technician.Status, "Available")

                If cboStatus.Items.Contains(status) Then
                    cboStatus.SelectedItem = status
                End If

            Catch ex As Exception

                MessageBox.Show(
                    "Unable to load technician." &
                    Environment.NewLine & Environment.NewLine & ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

                Me.DialogResult = DialogResult.Cancel
                Me.Close()

            End Try

        End Sub


        ' ------------------------------------------------------------
        ' SAVE (validates every field, shows all problems at once)
        ' ------------------------------------------------------------

        Private Sub Save_Click(sender As Object, e As EventArgs)

            Dim firstBad As Control = Nothing

            Dim fail As Action(Of Label, String, Control) =
                Sub(lbl As Label, msg As String, ctl As Control)
                    lbl.Text = msg
                    If firstBad Is Nothing Then firstBad = ctl
                End Sub

            errName.Text = ""
            errSpecialization.Text = ""
            errContact.Text = ""
            errEmail.Text = ""
            errPassword.Text = ""
            errConfirm.Text = ""


            Dim name As String = txtName.Text.Trim()

            If name = "" Then
                fail(errName, "Technician name is required.", txtName)
            ElseIf Not TechnicianController.IsValidName(name) Then
                fail(errName, "Letters, spaces, . ' - only (2 to 100 characters).", txtName)
            End If


            If cboSpecialization.SelectedIndex < 0 Then
                fail(errSpecialization, "Please choose a specialization.", cboSpecialization)
            End If


            Dim contact As String =
                TechnicianController.NormalizeContact(txtContact.Text)

            If txtContact.Text.Trim() = "" Then
                fail(errContact, "Contact number is required.", txtContact)
            ElseIf contact = "" Then
                fail(errContact, "Use a mobile number like 0917 123 4567.", txtContact)
            End If


            Dim email As String = txtEmail.Text.Trim()

            If email = "" Then
                fail(errEmail, "Email is required.", txtEmail)
            ElseIf Not TechnicianController.IsValidEmail(email) Then
                fail(errEmail, "Enter a valid email, e.g. name@example.com.", txtEmail)
            End If


            If isNew Then

                Dim problem As String =
                    TechnicianController.PasswordProblem(txtPassword.Text)

                If problem <> "" Then
                    fail(errPassword, problem, txtPassword)
                End If

                If txtConfirm.Text <> txtPassword.Text Then
                    fail(errConfirm, "Passwords do not match.", txtConfirm)
                End If

            End If


            If firstBad IsNot Nothing Then
                firstBad.Focus()
                Return
            End If


            Dim specialization As String = cboSpecialization.SelectedItem.ToString()
            Dim status As String = cboStatus.SelectedItem.ToString()

            Try

                If isNew Then

                    technicianController.CreateTechnician(
                        name, specialization, contact,
                        email, status, txtPassword.Text)

                Else

                    Dim success As Boolean =
                        technicianController.UpdateTechnician(
                            technicianId, name, specialization,
                            contact, email, status)

                    If Not success Then

                        MessageBox.Show(
                            "Technician could not be found.",
                            "Update Technician",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)

                        Return

                    End If

                End If

                Me.DialogResult = DialogResult.OK
                Me.Close()

            Catch ex As ArgumentException

                MessageBox.Show(
                    ex.Message,
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

            Catch ex As Exception

                Dim detail As String = ex.Message
                Dim inner As Exception = ex.InnerException

                While inner IsNot Nothing
                    detail &= Environment.NewLine & inner.Message
                    inner = inner.InnerException
                End While

                MessageBox.Show(
                    "Unable to save technician." &
                    Environment.NewLine & Environment.NewLine & detail,
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

            End Try

        End Sub

    End Class


    ' =================================================================
    ' ASSIGNED REPAIRS DIALOG (customer + device per repair)
    ' =================================================================

    Public Class TechnicianRepairsDialog
        Inherits NeonDialogBase

        Private ReadOnly grid As SmoothDataGridView
        Private ReadOnly lblCount As Label

        Public Sub New(
            controller As TechnicianController,
            technician As Technician)

            MyBase.New(
                "Assigned Repairs",
                technician.TechnicianName &
                If(String.IsNullOrWhiteSpace(technician.Specialization),
                   "",
                   "  -  " & technician.Specialization),
                900,
                560)

            Body.Padding = New Padding(20, 16, 20, 16)

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
                .ColumnHeadersHeight = 42
                .RowTemplate.Height = 48
            End With

            With grid.ColumnHeadersDefaultCellStyle
                .BackColor = Theme.CardColor
                .ForeColor = Theme.TextMuted
                .SelectionBackColor = Theme.CardColor
                .SelectionForeColor = Theme.TextMuted
                .Font = Theme.UiFont(8.0F, FontStyle.Bold)
                .Padding = New Padding(10, 0, 0, 0)
            End With

            With grid.DefaultCellStyle
                .BackColor = Theme.CardColor
                .ForeColor = Theme.TextWhite
                .SelectionBackColor = Theme.CardColor
                .SelectionForeColor = Theme.TextWhite
                .Font = Theme.UiFont(9.0F)
                .Padding = New Padding(10, 0, 0, 0)
            End With

            AddColumn("colCustomer", "CUSTOMER", 20.0F)
            AddColumn("colContact", "CUSTOMER CONTACT", 16.0F)
            AddColumn("colDevice", "DEVICE", 20.0F)
            AddColumn("colType", "TYPE", 11.0F)
            AddColumn("colStatus", "STATUS", 14.0F)
            AddColumn("colDate", "DATE", 11.0F)
            AddColumn("colCost", "COST", 10.0F)

            AddHandler grid.CellFormatting,
                Sub(s As Object, e As DataGridViewCellFormattingEventArgs)

                    If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

                    Dim col As String = grid.Columns(e.ColumnIndex).Name

                    If col = "colStatus" Then
                        e.CellStyle.ForeColor =
                            StatusColor(Convert.ToString(e.Value))
                    ElseIf col = "colType" OrElse col = "colContact" Then
                        e.CellStyle.ForeColor = Theme.TextMuted
                    ElseIf col = "colCost" Then
                        e.CellStyle.ForeColor = Theme.NeonGreen
                    End If

                End Sub

            Body.Controls.Add(grid)


            lblCount = New Label()
            lblCount.AutoSize = True
            lblCount.Location = New Point(24, 22)
            lblCount.Font = Theme.UiFont(8.5F)
            lblCount.ForeColor = Theme.TextMuted
            Footer.Controls.Add(lblCount)

            Dim btnClose As New NeonButton()
            btnClose.Text = "CLOSE"
            btnClose.Filled = False
            btnClose.Accent = Theme.NeonCyan
            btnClose.Size = New Size(110, 38)

            AddHandler btnClose.Click,
                Sub(s As Object, e As EventArgs)
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                End Sub

            PlaceFooterButtons(btnClose)

            LoadRepairs(controller, technician.TechnicianId)

        End Sub

        Private Sub AddColumn(name As String, header As String, weight As Single)

            Dim col As New DataGridViewTextBoxColumn()

            col.Name = name
            col.HeaderText = header
            col.FillWeight = weight

            grid.Columns.Add(col)

        End Sub

        Private Sub LoadRepairs(
            controller As TechnicianController,
            technicianId As Integer)

            Try

                Dim repairs As List(Of Repair) =
                    controller.GetAssignedRepairs(technicianId)

                Dim customers As Dictionary(Of Integer, Customer) =
                    controller.GetCustomerLookup()

                For Each repair As Repair In repairs

                    Dim customer As Customer = Nothing

                    Dim customerName As String = "Unknown Customer"
                    Dim customerContact As String = "-"

                    If customers.TryGetValue(repair.CustomerId, customer) Then

                        customerName = customer.CustomerName

                        If Not String.IsNullOrWhiteSpace(customer.ContactNumber) Then
                            customerContact = customer.ContactNumber
                        End If

                    End If

                    Dim deviceName As String =
                        (If(repair.Brand, "") & " " & If(repair.Model, "")).Trim()

                    If deviceName = "" Then deviceName = "Not provided"

                    grid.Rows.Add(
                        customerName,
                        customerContact,
                        deviceName,
                        If(String.IsNullOrWhiteSpace(repair.DeviceType),
                           "-", repair.DeviceType),
                        repair.Status,
                        repair.RepairDate.ToString("MMM dd, yyyy"),
                        If(repair.Cost > 0D, Money.Peso(repair.Cost), "-"))

                Next

                lblCount.Text =
                    If(repairs.Count = 0,
                       "No repairs assigned to this technician yet.",
                       repairs.Count.ToString() &
                       If(repairs.Count = 1,
                          " assigned repair", " assigned repairs"))

            Catch ex As Exception

                MessageBox.Show(
                    "Unable to load assigned repairs." &
                    Environment.NewLine & Environment.NewLine & ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

            End Try

        End Sub

        Private Shared Function StatusColor(status As String) As Color

            Select Case status

                Case "Pending"
                    Return Color.Gold

                Case "In Progress", "Delivered"
                    Return Theme.NeonCyan

                Case "Waiting for Parts"
                    Return Color.Orange

                Case "Completed"
                    Return Theme.NeonGreen

                Case "Cancelled"
                    Return Theme.NeonRed

                Case Else
                    Return Theme.TextMuted

            End Select

        End Function

    End Class


End Namespace