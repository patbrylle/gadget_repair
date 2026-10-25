Imports System.Drawing
Imports System.Windows.Forms

Namespace Forms

    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class DashboardForm

        Inherits Form


        <System.Diagnostics.DebuggerNonUserCode()>
        Protected Overrides Sub Dispose(
            disposing As Boolean
        )

            Try

                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                End If

            Finally

                MyBase.Dispose(disposing)

            End Try

        End Sub


        Private components As System.ComponentModel.IContainer

        Private Const Gap As Single = 14.0F
        Private Const TileGap As Single = 12.0F


        ' ============================================================
        ' ROOT + HEADER
        ' ============================================================

        Friend rootTable As DashTable

        Friend headingPanel As Panel

        Friend welcomeLabel As Label

        Friend subtitleLabel As Label

        Friend dateLabel As Label

        Friend btnRefresh As DashPillButton


        ' ============================================================
        ' BODY: LEFT (KPIs + TREND) AND RIGHT (SIDEBAR)
        ' ============================================================

        Friend bodyTable As DashTable

        Friend leftTable As DashTable

        Friend rightTable As DashTable


        ' ============================================================
        ' KPI GRID
        ' ============================================================

        Friend kpiTable As DashTable

        Friend statCustomers As DashKpiCard

        Friend statRepairs As DashKpiCard

        Friend statActive As DashKpiCard

        Friend statCompleted As DashKpiCard

        Friend statLowStock As DashKpiCard

        Friend statOutstanding As DashKpiCard


        ' ============================================================
        ' CHART CARDS
        ' ============================================================

        Friend monthlyRepairsCard As DashTrendCard

        Friend repairStatusCard As DashCard

        Friend paymentCard As DashCard

        Friend monthlyRepairsChart As DashChartSurface

        Friend repairStatusChart As DashChartSurface

        Friend paymentChart As DashChartSurface


        ' ============================================================
        ' QUICK ACTIONS
        ' ============================================================

        Friend quickCard As DashCard

        Friend actionsTable As DashTable

        Friend btnCustomers As DashActionTile

        Friend btnRepairs As DashActionTile

        Friend btnInventory As DashActionTile

        Friend btnPayments As DashActionTile

        Friend btnReports As DashActionTile


        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()

            Me.components = New System.ComponentModel.Container()

            Me.rootTable = New DashTable()
            Me.headingPanel = New Panel()
            Me.welcomeLabel = New Label()
            Me.subtitleLabel = New Label()
            Me.dateLabel = New Label()
            Me.btnRefresh = New DashPillButton()

            Me.bodyTable = New DashTable()
            Me.leftTable = New DashTable()
            Me.rightTable = New DashTable()

            Me.kpiTable = New DashTable()
            Me.statCustomers = New DashKpiCard()
            Me.statRepairs = New DashKpiCard()
            Me.statActive = New DashKpiCard()
            Me.statCompleted = New DashKpiCard()
            Me.statLowStock = New DashKpiCard()
            Me.statOutstanding = New DashKpiCard()

            Me.monthlyRepairsCard = New DashTrendCard()
            Me.repairStatusCard = New DashCard()
            Me.paymentCard = New DashCard()
            Me.monthlyRepairsChart = New DashChartSurface()
            Me.repairStatusChart = New DashChartSurface()
            Me.paymentChart = New DashChartSurface()

            Me.quickCard = New DashCard()
            Me.actionsTable = New DashTable()
            Me.btnCustomers = New DashActionTile()
            Me.btnRepairs = New DashActionTile()
            Me.btnInventory = New DashActionTile()
            Me.btnPayments = New DashActionTile()
            Me.btnReports = New DashActionTile()

            Me.SuspendLayout()


            ' ========================================================
            ' HEADER
            ' ========================================================

            Me.headingPanel.Dock = DockStyle.Fill
            Me.headingPanel.BackColor = BackgroundColor
            Me.headingPanel.Margin = Padding.Empty

            Me.welcomeLabel.Text = "Dashboard"
            Me.welcomeLabel.Font = UiFont(22.0F, FontStyle.Bold)
            Me.welcomeLabel.ForeColor = TextWhite
            Me.welcomeLabel.BackColor = BackgroundColor
            Me.welcomeLabel.AutoSize = True
            Me.welcomeLabel.Location = New Point(0, 2)

            Me.subtitleLabel.Text = "Loading dashboard information..."
            Me.subtitleLabel.Font = UiFont(9.5F)
            Me.subtitleLabel.ForeColor = TextMuted
            Me.subtitleLabel.BackColor = BackgroundColor
            Me.subtitleLabel.AutoSize = True
            Me.subtitleLabel.Location = New Point(2, 44)

            Me.dateLabel.Text = ""
            Me.dateLabel.Font = UiFont(9.0F)
            Me.dateLabel.ForeColor = TextMuted
            Me.dateLabel.BackColor = BackgroundColor
            Me.dateLabel.AutoSize = True
            Me.dateLabel.Location = New Point(0, 0)

            Me.btnRefresh.Caption = "Refresh"
            Me.btnRefresh.Accent = NeonCyan
            Me.btnRefresh.Glyph = DashGlyph.Refresh

            Me.headingPanel.Controls.Add(Me.welcomeLabel)
            Me.headingPanel.Controls.Add(Me.subtitleLabel)
            Me.headingPanel.Controls.Add(Me.dateLabel)
            Me.headingPanel.Controls.Add(Me.btnRefresh)


            ' ========================================================
            ' KPI GRID (3 columns x 2 rows)
            ' ========================================================

            ConfigureKpiCard(Me.statCustomers, "Customers", "Registered clients", NeonCyan, DashGlyph.Customers)

            ConfigureKpiCard(Me.statRepairs, "Total repairs", "All repair jobs", NeonPurple, DashGlyph.Repairs)

            ConfigureKpiCard(Me.statActive, "Active repairs", "Currently being worked on", NeonYellow, DashGlyph.Active)

            ConfigureKpiCard(Me.statCompleted, "Completed", "Completed and delivered", NeonGreen, DashGlyph.Completed)

            ConfigureKpiCard(Me.statLowStock, "Low stock", "Items needing attention", Color.Orange, DashGlyph.Warning)

            ConfigureKpiCard(Me.statOutstanding, "Outstanding", "Unpaid repair balance", NeonPink, DashGlyph.Coin)

            BuildGrid(Me.kpiTable, 3, Gap,
                      Me.statCustomers, Me.statRepairs, Me.statActive,
                      Me.statCompleted, Me.statLowStock, Me.statOutstanding)

Me.kpiTable.Margin = New Padding(0, 0, 0, CInt(Gap))

            ' ========================================================
            ' CHART CARDS
            ' ========================================================

            ConfigureChartCard(Me.monthlyRepairsCard, Me.monthlyRepairsChart,
                               "Repair Trend", "Repair tickets per month", NeonPurple)

            ConfigureChartCard(Me.repairStatusCard, Me.repairStatusChart,
                               "Repair Status", "Tickets by current stage", NeonCyan)

            ConfigureChartCard(Me.paymentCard, Me.paymentChart,
                               "Payment Overview", "Payment records by status", NeonGreen)


            ' ========================================================
            ' LEFT COLUMN: KPI grid on top, trend chart fills the rest
            ' ========================================================

            Me.leftTable.Dock = DockStyle.Fill
            Me.leftTable.BackColor = BackgroundColor
            Me.leftTable.Margin = Padding.Empty
            Me.leftTable.ColumnCount = 1
            Me.leftTable.RowCount = 2

            Me.leftTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

            Me.leftTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 214.0F))
            Me.leftTable.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

            Me.leftTable.Controls.Add(Me.kpiTable, 0, 0)
            Me.leftTable.Controls.Add(Me.monthlyRepairsCard, 0, 1)


            ' ========================================================
            ' RIGHT COLUMN: sidebar with two stacked cards
            ' ========================================================

            BuildStack(Me.rightTable, Gap, Me.repairStatusCard, Me.paymentCard)


            ' ========================================================
            ' BODY: left (66%) + right (34%)
            ' ========================================================

            BuildRow(Me.bodyTable, Gap, New Single() {66.0F, 34.0F}, Me.leftTable, Me.rightTable)


            ' ========================================================
            ' QUICK ACTIONS (full-width bar, no title)
            ' ========================================================

            ConfigureActionTile(Me.btnCustomers, "Customers", "Manage clients", NeonCyan, DashGlyph.Customers)

            ConfigureActionTile(Me.btnRepairs, "Repairs", "Track tickets", NeonPink, DashGlyph.Repairs)

            ConfigureActionTile(Me.btnInventory, "Inventory", "Parts and stock", NeonPurple, DashGlyph.Inventory)

            ConfigureActionTile(Me.btnPayments, "Payments", "Billing and balances", NeonGreen, DashGlyph.Payments)

            ConfigureActionTile(Me.btnReports, "Reports", "Summaries", NeonYellow, DashGlyph.Reports)

            BuildRow(Me.actionsTable, TileGap, Nothing,
                     Me.btnCustomers, Me.btnRepairs, Me.btnInventory,
                     Me.btnPayments, Me.btnReports)

            Me.actionsTable.BackColor = CardColor

            Me.quickCard.Dock = DockStyle.Fill
Me.kpiTable.Margin = New Padding(0, 0, 0, CInt(Gap))    
        Me.quickCard.Padding = New Padding(12, 12, 12, 12)

            Me.quickCard.Controls.Add(Me.actionsTable)


            ' ========================================================
            ' ROOT TABLE: header / body / quick actions
            ' ========================================================

            Me.rootTable.Dock = DockStyle.Fill
            Me.rootTable.BackColor = BackgroundColor
            Me.rootTable.Margin = Padding.Empty
            Me.rootTable.ColumnCount = 1
            Me.rootTable.RowCount = 3

            Me.rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

            Me.rootTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 72.0F))
            Me.rootTable.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
            Me.rootTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 90.0F))

            Me.rootTable.Controls.Add(Me.headingPanel, 0, 0)
            Me.rootTable.Controls.Add(Me.bodyTable, 0, 1)
            Me.rootTable.Controls.Add(Me.quickCard, 0, 2)


            ' ========================================================
            ' FORM
            ' ========================================================

            Me.AutoScaleMode = AutoScaleMode.None
            Me.AutoScroll = True
            Me.AutoScrollMinSize = New Size(860, 640)
            Me.BackColor = BackgroundColor
            Me.Padding = New Padding(28, 22, 28, 22)
            Me.FormBorderStyle = FormBorderStyle.None
            Me.ShowIcon = False
            Me.ShowInTaskbar = False
            Me.Text = "Dashboard"
            Me.DoubleBuffered = True

            Me.Controls.Add(Me.rootTable)

            Me.ResumeLayout(False)

        End Sub


        ' ============================================================
        ' ONE-ROW TABLE WITH EQUAL (OR WEIGHTED) CELLS + GAP COLUMNS
        ' ============================================================

        Private Shared Sub BuildRow(
            table As TableLayoutPanel,
            gapWidth As Single,
            weights As Single(),
            ParamArray cells As Control())

            table.Dock = DockStyle.Fill
            table.BackColor = BackgroundColor
            table.Margin = Padding.Empty

            table.RowStyles.Clear()
            table.ColumnStyles.Clear()

            table.RowCount = 1
            table.ColumnCount = cells.Length * 2 - 1

            table.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

            For i As Integer = 0 To cells.Length - 1

                If i > 0 Then
                    table.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, gapWidth))
                End If

                Dim weight As Single = 100.0F / cells.Length

                If weights IsNot Nothing AndAlso i < weights.Length Then
                    weight = weights(i)
                End If

                table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, weight))

                cells(i).Dock = DockStyle.Fill
                cells(i).Margin = Padding.Empty

                table.Controls.Add(cells(i), i * 2, 0)

            Next

        End Sub


        ' ============================================================
        ' ONE-COLUMN TABLE WITH EQUAL CELLS + GAP ROWS
        ' ============================================================

        Private Shared Sub BuildStack(
            table As TableLayoutPanel,
            gapHeight As Single,
            ParamArray cells As Control())

            table.Dock = DockStyle.Fill
            table.BackColor = BackgroundColor
            table.Margin = Padding.Empty

            table.RowStyles.Clear()
            table.ColumnStyles.Clear()

            table.ColumnCount = 1
            table.RowCount = cells.Length * 2 - 1

            table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

            For i As Integer = 0 To cells.Length - 1

                If i > 0 Then
                    table.RowStyles.Add(New RowStyle(SizeType.Absolute, gapHeight))
                End If

                table.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F / cells.Length))

                cells(i).Dock = DockStyle.Fill
                cells(i).Margin = Padding.Empty

                table.Controls.Add(cells(i), 0, i * 2)

            Next

        End Sub


        ' ============================================================
        ' GRID: N columns, equal cells, gap rows/columns (fills row by row)
        ' ============================================================

        Private Shared Sub BuildGrid(
            table As TableLayoutPanel,
            columns As Integer,
            gapSize As Single,
            ParamArray cells As Control())

            Dim rows As Integer = (cells.Length + columns - 1) \ columns

            table.Dock = DockStyle.Fill
            table.BackColor = BackgroundColor
            table.Margin = Padding.Empty

            table.RowStyles.Clear()
            table.ColumnStyles.Clear()

            table.ColumnCount = columns * 2 - 1
            table.RowCount = rows * 2 - 1

            For c As Integer = 0 To columns - 1

                If c > 0 Then
                    table.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, gapSize))
                End If

                table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F / columns))

            Next

            For r As Integer = 0 To rows - 1

                If r > 0 Then
                    table.RowStyles.Add(New RowStyle(SizeType.Absolute, gapSize))
                End If

                table.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F / rows))

            Next

            For i As Integer = 0 To cells.Length - 1

                cells(i).Dock = DockStyle.Fill
                cells(i).Margin = Padding.Empty

                table.Controls.Add(cells(i), (i Mod columns) * 2, (i \ columns) * 2)

            Next

        End Sub


        ' ============================================================
        ' KPI CARD CONFIGURATION
        ' ============================================================

        Private Shared Sub ConfigureKpiCard(
            card As DashKpiCard,
            title As String,
            description As String,
            accent As Color,
            glyph As DashGlyph)

            card.Title = title
            card.ValueText = "..."
            card.Description = description
            card.Accent = accent
            card.Glyph = glyph

        End Sub


        ' ============================================================
        ' CHART CARD CONFIGURATION
        ' ============================================================

        Private Shared Sub ConfigureChartCard(
            card As DashCard,
            chart As DashChartSurface,
            title As String,
            subtitle As String,
            accent As Color)

            card.Title = title
            card.Subtitle = subtitle
            card.Accent = accent
            card.Dock = DockStyle.Fill
            card.Margin = Padding.Empty
            card.Padding = New Padding(18, 58, 18, 14)

            chart.Dock = DockStyle.Fill
            chart.BackColor = CardColor

            card.Controls.Add(chart)

        End Sub


        ' ============================================================
        ' QUICK ACTION TILE CONFIGURATION
        ' ============================================================

        Private Shared Sub ConfigureActionTile(
            tile As DashActionTile,
            caption As String,
            hint As String,
            accent As Color,
            glyph As DashGlyph)

            tile.Caption = caption
            tile.Hint = hint
            tile.Accent = accent
            tile.Glyph = glyph

        End Sub

    End Class

End Namespace