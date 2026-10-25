Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Data
Imports gadget_repair.Models
Imports gadget_repair.Services
Imports gadget_repair.Forms

Public Class ReportsForm
    Inherits UserControl

    Private ReadOnly db As ApplicationDbContext
    Private ReadOnly reportController As ReportController
    Private current As ReportData
    Private loading As Boolean = True

    Private headerPanel As Panel
    Private btnPrint As NeonButton
    Private btnExcel As NeonButton
    Private btnPdf As NeonButton

    Private filterFlow As FlowLayoutPanel
    Private cboReport As NeonComboBox
    Private cboPeriod As NeonComboBox
    Private cboStatus As NeonComboBox
    Private dtFrom As NeonDateTimeField
    Private dtTo As NeonDateTimeField
    Private btnGenerate As NeonButton
    Private wrapPeriod As Panel
    Private wrapFrom As Panel
    Private wrapTo As Panel
    Private wrapStatus As Panel

    Private statsTable As TableLayoutPanel
    Private gridCard As NeonCard
    Private grid As SmoothDataGridView
    Private lblCount As Label

    Public Sub New(database As ApplicationDbContext)

        db = database
        reportController = New ReportController(db)

        BuildInterface()

        AddHandler Me.Load, AddressOf ReportsForm_Load

    End Sub

    Private Sub ReportsForm_Load(sender As Object, e As EventArgs)

        loading = True

        Try
            Dim def As String =
                New SettingsController(db).GetValue(SettingsController.KeyDefaultPeriod, "This Month")

            Dim idx As Integer = cboPeriod.Items.IndexOf(def)

            cboPeriod.SelectedIndex = If(idx >= 0, idx, 2)
        Catch
            cboPeriod.SelectedIndex = 2
        End Try

        cboReport.SelectedIndex = 0

        loading = False

        ApplyReportOptions()
        Generate()

    End Sub

    ' ============================================================
    ' UI
    ' ============================================================

    Private Sub BuildInterface()

        Me.Dock = DockStyle.Fill
        Me.BackColor = Theme.BackgroundColor
        Me.Padding = New Padding(30, 28, 30, 26)
        Me.Font = Theme.UiFont(9.0F)
        Me.DoubleBuffered = True

        BuildHeader()
        BuildFilters()
        BuildStats()
        BuildGrid()

        Me.Controls.Add(gridCard)
        Me.Controls.Add(statsTable)
        Me.Controls.Add(filterFlow)
        Me.Controls.Add(headerPanel)

    End Sub

    Private Sub BuildHeader()

        headerPanel = New Panel()
        headerPanel.Dock = DockStyle.Top
        headerPanel.Height = 84
        headerPanel.BackColor = Theme.BackgroundColor

        Dim lblHeading As New Label()
        lblHeading.Text = "Reports"
        lblHeading.AutoSize = True
        lblHeading.Location = New Point(0, 0)
        lblHeading.Font = Theme.UiFont(23.0F, FontStyle.Bold)
        lblHeading.ForeColor = Theme.TextWhite

        Dim lblSub As New Label()
        lblSub.Text = "Generate, print and export reports from your shop data"
        lblSub.AutoSize = True
        lblSub.Location = New Point(2, 42)
        lblSub.Font = Theme.UiFont(9.5F)
        lblSub.ForeColor = Theme.TextMuted

        btnPrint = NewHeaderButton("Print", Theme.NeonPurple, False)
        btnExcel = NewHeaderButton("Export Excel", Theme.NeonGreen, False)
        btnPdf = NewHeaderButton("Export PDF", Theme.NeonCyan, True)

        AddHandler btnPrint.Click, AddressOf Print_Click
        AddHandler btnExcel.Click, AddressOf Excel_Click
        AddHandler btnPdf.Click, AddressOf Pdf_Click

        headerPanel.Controls.Add(lblHeading)
        headerPanel.Controls.Add(lblSub)
        headerPanel.Controls.Add(btnPrint)
        headerPanel.Controls.Add(btnExcel)
        headerPanel.Controls.Add(btnPdf)

        AddHandler headerPanel.Resize, AddressOf LayoutHeaderButtons

        LayoutHeaderButtons(Nothing, EventArgs.Empty)

    End Sub

    Private Function NewHeaderButton(text As String, accent As Color, filled As Boolean) As NeonButton

        Dim b As New NeonButton()
        b.Text = text
        b.Accent = accent
        b.Filled = filled
        b.KeepAccentOnHover = Not filled
        b.Size = New Size(112, 40)
        b.Top = 6

        Return b

    End Function

    Private Sub LayoutHeaderButtons(sender As Object, e As EventArgs)

        If btnPdf Is Nothing Then Return

        Dim right As Integer = headerPanel.ClientSize.Width

        btnPdf.Left = right - btnPdf.Width
        btnExcel.Left = btnPdf.Left - btnExcel.Width - 10
        btnPrint.Left = btnExcel.Left - btnPrint.Width - 10

    End Sub

    Private Function MakeField(caption As String, child As Control, width As Integer) As Panel

        Dim wrap As New Panel()
        wrap.Size = New Size(width + 16, 56)
        wrap.Margin = New Padding(0)
        wrap.BackColor = Theme.BackgroundColor

        If TypeOf child Is NeonDateTimeField Then
            NeonLayout.AddCaption(wrap, caption, 0, 0)
            child.Location = New Point(0, NeonLayout.CaptionGap)
            child.Size = New Size(width, NeonLayout.HostHeight)
            wrap.Controls.Add(child)
        Else
            NeonLayout.AddField(wrap, caption, child, 0, 0, width)
        End If

        Return wrap

    End Function

    Private Sub BuildFilters()

        filterFlow = New FlowLayoutPanel()
        filterFlow.Dock = DockStyle.Top
        filterFlow.AutoSize = True
        filterFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink
        filterFlow.WrapContents = True
        filterFlow.FlowDirection = FlowDirection.LeftToRight
        filterFlow.BackColor = Theme.BackgroundColor
        filterFlow.Padding = New Padding(0)
        filterFlow.MinimumSize = New Size(0, 60)

        cboReport = New NeonComboBox()
        cboReport.Font = Theme.UiFont(9.5F)

        For Each name As String In ReportController.ReportNames
            cboReport.Items.Add(name)
        Next

        cboPeriod = New NeonComboBox()
        cboPeriod.Font = Theme.UiFont(9.5F)

        For Each p As String In ReportController.PeriodList
            cboPeriod.Items.Add(p)
        Next

        dtFrom = New NeonDateTimeField()
        dtFrom.Value = New DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)

        dtTo = New NeonDateTimeField()
        dtTo.Value = DateTime.Today

        cboStatus = New NeonComboBox()
        cboStatus.Font = Theme.UiFont(9.5F)

        btnGenerate = New NeonButton()
        btnGenerate.Text = "Generate"
        btnGenerate.Filled = True
        btnGenerate.Size = New Size(110, NeonLayout.HostHeight)
        btnGenerate.Location = New Point(0, NeonLayout.CaptionGap)

        Dim wrapGen As New Panel()
        wrapGen.Size = New Size(126, 56)
        wrapGen.Margin = New Padding(0)
        wrapGen.BackColor = Theme.BackgroundColor
        wrapGen.Controls.Add(btnGenerate)

        Dim wrapReport As Panel = MakeField("REPORT", cboReport, 230)
        wrapPeriod = MakeField("PERIOD", cboPeriod, 150)
        wrapFrom = MakeField("FROM", dtFrom, 190)
        wrapTo = MakeField("TO", dtTo, 190)
        wrapStatus = MakeField("STATUS", cboStatus, 150)

        filterFlow.Controls.Add(wrapReport)
        filterFlow.Controls.Add(wrapPeriod)
        filterFlow.Controls.Add(wrapFrom)
        filterFlow.Controls.Add(wrapTo)
        filterFlow.Controls.Add(wrapStatus)
        filterFlow.Controls.Add(wrapGen)

        AddHandler cboReport.SelectedIndexChanged, AddressOf Report_Changed
        AddHandler cboPeriod.SelectedIndexChanged, AddressOf Period_Changed
        AddHandler cboStatus.SelectedIndexChanged, AddressOf Status_Changed
        AddHandler btnGenerate.Click, Sub(s As Object, e As EventArgs) Generate()

    End Sub

    Private Sub BuildStats()

        statsTable = New TableLayoutPanel()
        statsTable.Dock = DockStyle.Top
        statsTable.Height = 108
        statsTable.RowCount = 1
        statsTable.BackColor = Theme.BackgroundColor
        statsTable.Margin = New Padding(0)

    End Sub

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
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            .EnableHeadersVisualStyles = False
            .ColumnHeadersHeight = 46
            .RowTemplate.Height = 42
            .DefaultCellStyle.NullValue = ""
        End With

        With grid.ColumnHeadersDefaultCellStyle
            .BackColor = Theme.CardColor
            .ForeColor = Theme.TextMuted
            .SelectionBackColor = Theme.CardColor
            .SelectionForeColor = Theme.TextMuted
            .Font = Theme.UiFont(8.0F, FontStyle.Bold)
            .Padding = New Padding(10, 0, 10, 0)
        End With

        With grid.DefaultCellStyle
            .BackColor = Theme.CardColor
            .ForeColor = Theme.TextWhite
            .SelectionBackColor = Theme.Lerp(Theme.CardColor, Theme.NeonCyan, 0.10F)
            .SelectionForeColor = Theme.TextWhite
            .Font = Theme.UiFont(9.5F)
            .Padding = New Padding(10, 0, 10, 0)
        End With

        With grid.AlternatingRowsDefaultCellStyle
            .BackColor = Theme.Lerp(Theme.CardColor, Theme.BackgroundColor, 0.16F)
            .ForeColor = Theme.TextWhite
            .SelectionBackColor = Theme.Lerp(Theme.CardColor, Theme.NeonCyan, 0.10F)
            .SelectionForeColor = Theme.TextWhite
        End With

        AddHandler grid.CellFormatting, AddressOf Grid_CellFormatting

        lblCount = New Label()
        lblCount.Dock = DockStyle.Bottom
        lblCount.Height = 30
        lblCount.TextAlign = ContentAlignment.MiddleLeft
        lblCount.ForeColor = Theme.TextMuted
        lblCount.Font = Theme.UiFont(8.5F)
        lblCount.Padding = New Padding(6, 0, 0, 0)

        gridCard.Controls.Add(grid)
        gridCard.Controls.Add(lblCount)

    End Sub

    ' ============================================================
    ' FILTER EVENTS
    ' ============================================================

    Private Function SelectedKind() As ReportKind
        Return CType(Math.Max(0, cboReport.SelectedIndex), ReportKind)
    End Function

    Private Sub ApplyReportOptions()

        Dim kind As ReportKind = SelectedKind()
        Dim options As String() = ReportController.StatusOptions(kind)
        Dim wasLoading As Boolean = loading

        loading = True

        cboStatus.Items.Clear()

        For Each o As String In options
            cboStatus.Items.Add(o)
        Next

        If options.Length > 0 Then cboStatus.SelectedIndex = 0

        wrapStatus.Visible = options.Length > 0

        Dim usesPeriod As Boolean = (kind <> ReportKind.Inventory)
        Dim custom As Boolean = usesPeriod AndAlso Convert.ToString(cboPeriod.SelectedItem) = "Custom"

        wrapPeriod.Visible = usesPeriod
        wrapFrom.Visible = custom
        wrapTo.Visible = custom

        loading = wasLoading

    End Sub

    Private Sub Report_Changed(sender As Object, e As EventArgs)
        If loading Then Return
        ApplyReportOptions()
        Generate()
    End Sub

    Private Sub Period_Changed(sender As Object, e As EventArgs)
        If loading Then Return
        ApplyReportOptions()
        If Convert.ToString(cboPeriod.SelectedItem) <> "Custom" Then Generate()
    End Sub

    Private Sub Status_Changed(sender As Object, e As EventArgs)
        If loading Then Return
        Generate()
    End Sub

    ' ============================================================
    ' GENERATE / SHOW
    ' ============================================================

    Private Sub Generate()

        If loading OrElse cboReport.SelectedIndex < 0 Then Return

        Dim status As String = "All Status"

        If cboStatus.SelectedItem IsNot Nothing Then status = cboStatus.SelectedItem.ToString()

        Dim period As String = "This Month"

        If cboPeriod.SelectedItem IsNot Nothing Then period = cboPeriod.SelectedItem.ToString()

        Cursor = Cursors.WaitCursor

        Try

            current = reportController.Build(SelectedKind(), period, dtFrom.Value, dtTo.Value, status)
            ShowData(current)

        Catch ex As Exception

            MessageBox.Show(
                "Unable to generate the report." & Environment.NewLine & Environment.NewLine & ex.Message,
                "Reports", MessageBoxButtons.OK, MessageBoxIcon.Error)

        Finally

            Cursor = Cursors.Default

        End Try

    End Sub

    Private Shared Function ScreenMoney(m As Decimal) As String
        Return Theme.FormatPeso(m)
    End Function

    Private Sub ShowData(d As ReportData)

        ' ---------- summary cards ----------
        Dim accents As Color() = {Theme.NeonCyan, Theme.NeonGreen, Theme.NeonPink, Theme.NeonYellow, Theme.NeonPurple}
        Dim n As Integer = d.Summary.Count

        statsTable.SuspendLayout()

        For i As Integer = statsTable.Controls.Count - 1 To 0 Step -1
            Dim old As Control = statsTable.Controls(i)
            statsTable.Controls.RemoveAt(i)
            old.Dispose()
        Next

        statsTable.ColumnStyles.Clear()
        statsTable.ColumnCount = Math.Max(1, n)

        For i As Integer = 0 To Math.Max(1, n) - 1
            statsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F / Math.Max(1, n)))
        Next

        For i As Integer = 0 To n - 1

            Dim item As ReportSummaryItem = d.Summary(i)
            Dim card As New StatCard()

            card.Caption = item.Label.ToUpper()
            card.Value = d.Format(item.Kind, item.Value, AddressOf ScreenMoney)
            card.Accent = accents(i Mod accents.Length)
            card.Dock = DockStyle.Fill
            card.Margin = New Padding(If(i = 0, 0, 6), 0, If(i = n - 1, 0, 6), 16)

            statsTable.Controls.Add(card, i, 0)

        Next

        statsTable.ResumeLayout(True)

        ' ---------- grid ----------
        grid.SuspendLayout()
        grid.Rows.Clear()
        grid.Columns.Clear()

        For Each col As ReportColumn In d.Columns

            Dim c As New DataGridViewTextBoxColumn()
            c.HeaderText = col.Header
            c.FillWeight = col.Weight * 10.0F
            c.SortMode = DataGridViewColumnSortMode.NotSortable
            c.Tag = col.Kind

            Dim al As DataGridViewContentAlignment =
                If(col.Align = ReportAlign.Right, DataGridViewContentAlignment.MiddleRight,
                   If(col.Align = ReportAlign.Center, DataGridViewContentAlignment.MiddleCenter,
                      DataGridViewContentAlignment.MiddleLeft))

            c.DefaultCellStyle.Alignment = al
            c.HeaderCell.Style.Alignment = al

            grid.Columns.Add(c)

        Next

        For Each row As Object() In d.Rows

            Dim cells(d.Columns.Count - 1) As Object

            For i As Integer = 0 To d.Columns.Count - 1
                cells(i) = d.Format(d.Columns(i).Kind, row(i), AddressOf ScreenMoney)
            Next

            grid.Rows.Add(cells)

        Next

        If d.Totals IsNot Nothing AndAlso d.Rows.Count > 0 Then

            Dim cells(d.Columns.Count - 1) As Object

            For i As Integer = 0 To d.Columns.Count - 1
                cells(i) = d.Format(d.Columns(i).Kind, d.Totals(i), AddressOf ScreenMoney)
            Next

            Dim idx As Integer = grid.Rows.Add(cells)

            grid.Rows(idx).DefaultCellStyle.Font = Theme.UiFont(9.5F, FontStyle.Bold)
            grid.Rows(idx).DefaultCellStyle.BackColor = Theme.Lerp(Theme.CardColor, Theme.NeonCyan, 0.12F)

        End If

        grid.ResumeLayout(True)

        lblCount.Text = If(d.Rows.Count = 0,
            "No records found for the selected filters.",
            d.Rows.Count.ToString("N0") & " record(s)   |   Generated " & d.GeneratedAt.ToString("h:mm tt"))

    End Sub

    Private Shared Function StatusInk(text As String) As Color

        Select Case text
            Case "Paid", "Completed", "In Stock" : Return Theme.NeonGreen
            Case "Delivered", "In Progress" : Return Theme.NeonCyan
            Case "Pending", "Partially Paid", "Low Stock" : Return Theme.NeonYellow
            Case "Waiting for Parts" : Return Color.Orange
            Case "Cancelled", "Unpaid", "Out of Stock" : Return Theme.NeonPink
            Case Else : Return Theme.TextWhite
        End Select

    End Function

    Private Sub Grid_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)

        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

        Dim tag As Object = grid.Columns(e.ColumnIndex).Tag

        If tag Is Nothing OrElse CType(tag, ColumnKind) <> ColumnKind.Status Then Return

        e.CellStyle.ForeColor = StatusInk(Convert.ToString(e.Value))
        e.CellStyle.Font = Theme.UiFont(9.5F, FontStyle.Bold)

    End Sub

    ' ============================================================
    ' EXPORT / PRINT
    ' ============================================================

    Private Function HasData() As Boolean

        If current Is Nothing Then
            MessageBox.Show("Generate a report first.", "Reports",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If

        If current.Rows.Count = 0 Then
            MessageBox.Show("There is no data to export for the selected filters.", "Reports",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If

        Return True

    End Function

    Private Shared Function SafeName(text As String) As String

        Dim result As String = text

        For Each ch As Char In Path.GetInvalidFileNameChars()
            result = result.Replace(ch, "_"c)
        Next

        Return result.Replace(" ", "_")

    End Function

    Private Sub AskOpen(path As String)

        If MessageBox.Show("Saved to:" & Environment.NewLine & path & Environment.NewLine & Environment.NewLine &
                           "Open the file now?", "Export complete",
                           MessageBoxButtons.YesNo, MessageBoxIcon.Information) <> DialogResult.Yes Then Return

        Try
            Process.Start(New ProcessStartInfo(path) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show("The file was saved but could not be opened automatically.",
                            "Reports", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Try

    End Sub

    Private Sub Export(filter As String, extension As String, doExport As Action(Of String))

        If Not HasData() Then Return

        Using dlg As New SaveFileDialog()

            dlg.Filter = filter
            dlg.FileName = SafeName(current.Title) & "_" & DateTime.Now.ToString("yyyyMMdd_HHmm") & extension

            If dlg.ShowDialog(Me.FindForm()) <> DialogResult.OK Then Return

            Try
                Cursor = Cursors.WaitCursor
                doExport(dlg.FileName)
                Cursor = Cursors.Default
                AskOpen(dlg.FileName)
            Catch ex As Exception
                Cursor = Cursors.Default
                MessageBox.Show("Export failed." & Environment.NewLine & Environment.NewLine & ex.Message,
                                "Reports", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try

        End Using

    End Sub

    Private Sub Excel_Click(sender As Object, e As EventArgs)
        Export("Excel Workbook (*.xlsx)|*.xlsx", ".xlsx",
               Sub(path As String) ExcelExporter.Export(current, New SettingsController(db).GetShop(), path))
    End Sub

    Private Sub Pdf_Click(sender As Object, e As EventArgs)
        Export("PDF Document (*.pdf)|*.pdf", ".pdf",
               Sub(path As String) ReportExporter.ExportPdf(current, New SettingsController(db).GetShop(), path))
    End Sub

    Private Sub Print_Click(sender As Object, e As EventArgs)

        If Not HasData() Then Return

        Try
            ReportExporter.PrintPreview(Me.FindForm(), current, New SettingsController(db).GetShop())
        Catch ex As Exception
            MessageBox.Show("Printing is not available." & Environment.NewLine & ex.Message &
                            Environment.NewLine & Environment.NewLine & "Use Export PDF instead.",
                            "Print", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Try

    End Sub

End Class