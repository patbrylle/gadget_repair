Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.Linq
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Data

Namespace Forms

    Public Partial Class DashboardForm

        ' ============================================================
        ' SETTINGS
        ' ============================================================

        ' Set to False once the database is working to stop the
        ' error pop-up (the error is still written to the Output window).
        Private Const ShowDebugErrors As Boolean = True

        Private Shared ReadOnly ChartGridColor As Color = Color.FromArgb(30, 255, 255, 255)
        Private Shared ReadOnly ChartTrackColor As Color = Color.FromArgb(28, 255, 255, 255)


        ' ============================================================
        ' STATE
        ' ============================================================

        Private ReadOnly controller As DashboardController

        Private currentStats As DashboardStats

        Private isLoading As Boolean

        Private loadFailed As Boolean


        ' ============================================================
        ' EVENTS (unchanged - MainLayout keeps working)
        ' ============================================================

        Public Event NavigateRequested(menuName As String)

        Public Event StatsLoaded(pendingCount As Integer)


        ' ============================================================
        ' CONSTRUCTOR
        ' ============================================================

        Public Sub New(database As ApplicationDbContext)

            InitializeComponent()

            controller = New DashboardController(database)

            dateLabel.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy")

            ' Header layout
            AddHandler headingPanel.Resize, AddressOf LayoutHeader
            AddHandler btnRefresh.Click, Sub(s, e) LoadStats()

            ' Quick actions
            AddHandler btnCustomers.Click, Sub(s, e) Navigate("Customers")
            AddHandler btnRepairs.Click, Sub(s, e) Navigate("Repairs")
            AddHandler btnInventory.Click, Sub(s, e) Navigate("Inventory")
            AddHandler btnPayments.Click, Sub(s, e) Navigate("Payments")
            AddHandler btnReports.Click, Sub(s, e) Navigate("Reports")

            ' Charts
            AddHandler repairStatusChart.Paint, AddressOf DrawRepairStatusChart
            AddHandler monthlyRepairsChart.Paint, AddressOf DrawMonthlyRepairsChart
            AddHandler paymentChart.Paint, AddressOf DrawPaymentChart

        End Sub


        Private Sub Navigate(menuName As String)
            RaiseEvent NavigateRequested(menuName)
        End Sub


        ' ============================================================
        ' FORM LOAD
        ' ============================================================

        Private Sub DashboardForm_Load(
            sender As Object,
            e As EventArgs
        ) Handles MyBase.Load

            LayoutHeader(Nothing, EventArgs.Empty)

            BeginInvoke(New MethodInvoker(AddressOf LoadStats))

        End Sub


        ' ============================================================
        ' HEADER LAYOUT (date + refresh stay on the right)
        ' ============================================================

        Private Sub LayoutHeader(sender As Object, e As EventArgs)

            Dim panelWidth As Integer = headingPanel.ClientSize.Width

            btnRefresh.Location =
                New Point(Math.Max(0, panelWidth - btnRefresh.Width), 8)

            dateLabel.Location =
                New Point(
                    Math.Max(welcomeLabel.Right + 12, btnRefresh.Left - dateLabel.Width - 16),
                    btnRefresh.Top + (btnRefresh.Height - dateLabel.Height) \ 2)

        End Sub


        ' ============================================================
        ' GREETING
        ' ============================================================

        Private Shared Function GreetingText() As String

            Dim hour As Integer = DateTime.Now.Hour

            If hour < 12 Then
                Return "Good morning"
            End If

            If hour < 18 Then
                Return "Good afternoon"
            End If

            Return "Good evening"

        End Function


        ' ============================================================
        ' LOAD DASHBOARD DATA
        ' ============================================================

        Private Sub LoadStats()

            If IsDisposed OrElse Disposing Then
                Return
            End If

            If isLoading Then
                Return
            End If

            isLoading = True

            Try

                currentStats = controller.GetStats()
                loadFailed = False

                ' ---------- KPI cards ----------
                statCustomers.ValueText = currentStats.CustomerCount.ToString("N0")
                statRepairs.ValueText = currentStats.TotalRepairs.ToString("N0")
                statActive.ValueText = currentStats.ActiveRepairs.ToString("N0")
                statCompleted.ValueText = currentStats.CompletedCount.ToString("N0")
                statLowStock.ValueText = currentStats.LowStockCount.ToString("N0")
                statOutstanding.ValueText = FormatPeso(currentStats.TotalOutstanding)

                ' ---------- header ----------
                dateLabel.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy")

                subtitleLabel.Text =
                    GreetingText() & ", here's your repair shop at a glance  " &
                    ChrW(&HB7) & "  Updated " & DateTime.Now.ToString("h:mm tt")

                subtitleLabel.ForeColor = TextMuted

                LayoutHeader(Nothing, EventArgs.Empty)

                ' ---------- trend badge + charts ----------
                UpdateTrendBadge()

                RefreshCharts()

                ' ---------- notify MainLayout ----------
                RaiseEvent StatsLoaded(currentStats.PendingRepairs)

            Catch ex As Exception

                Debug.WriteLine(ex.ToString())

                currentStats = Nothing
                loadFailed = True

                If ShowDebugErrors Then

                    MessageBox.Show(
                        ex.ToString(),
                        "Dashboard Database Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)

                End If

                statCustomers.ValueText = "N/A"
                statRepairs.ValueText = "N/A"
                statActive.ValueText = "N/A"
                statCompleted.ValueText = "N/A"
                statLowStock.ValueText = "N/A"
                statOutstanding.ValueText = "N/A"

                subtitleLabel.Text = "Unable to load dashboard data"
                subtitleLabel.ForeColor = NeonPink

                monthlyRepairsCard.Badge = ""

                RefreshCharts()

            Finally

                isLoading = False

            End Try

        End Sub


        Private Sub RefreshCharts()

            repairStatusChart.Invalidate()
            monthlyRepairsChart.Invalidate()
            paymentChart.Invalidate()

        End Sub


        ' ============================================================
        ' TREND BADGE ("12% vs previous month")
        ' Assumes MonthlyRepairs is in date order, newest month last.
        ' ============================================================

        Private Sub UpdateTrendBadge()

            monthlyRepairsCard.Badge = ""

            If currentStats Is Nothing OrElse currentStats.MonthlyRepairs Is Nothing Then
                Return
            End If

            Dim data = currentStats.MonthlyRepairs

            If data.Count < 2 Then
                Return
            End If

            Dim current As Integer = CInt(data(data.Count - 1).RepairCount)
            Dim previous As Integer = CInt(data(data.Count - 2).RepairCount)

            If previous <= 0 AndAlso current <= 0 Then

                monthlyRepairsCard.BadgeDirection = 0
                monthlyRepairsCard.BadgeColor = TextMuted
                monthlyRepairsCard.Badge = "No change vs previous month"
                Return

            End If

            If previous <= 0 Then

                monthlyRepairsCard.BadgeDirection = 1
                monthlyRepairsCard.BadgeColor = NeonGreen
                monthlyRepairsCard.Badge = "+" & current.ToString() & " vs previous month"
                Return

            End If

            Dim change As Integer = CInt(Math.Round((current - previous) * 100.0 / previous))

            If change > 0 Then

                monthlyRepairsCard.BadgeDirection = 1
                monthlyRepairsCard.BadgeColor = NeonGreen
                monthlyRepairsCard.Badge = change.ToString() & "% vs previous month"

            ElseIf change < 0 Then

                monthlyRepairsCard.BadgeDirection = -1
                monthlyRepairsCard.BadgeColor = NeonPink
                monthlyRepairsCard.Badge = Math.Abs(change).ToString() & "% vs previous month"

            Else

                monthlyRepairsCard.BadgeDirection = 0
                monthlyRepairsCard.BadgeColor = TextMuted
                monthlyRepairsCard.Badge = "No change vs previous month"

            End If

        End Sub

        ' ============================================================
        ' SHARED CHART HELPERS
        ' ============================================================

        Private Shared Sub PrepareGraphics(g As Graphics)

            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

        End Sub


        ' Picks a "nice" y-axis: integer ticks, up to 4 intervals
        Private Shared Sub ComputeAxis(
            maxValue As Integer,
            ByRef intervals As Integer,
            ByRef axisMax As Integer)

            maxValue = Math.Max(1, maxValue)

            intervals = If(maxValue < 4, maxValue, 4)

            Dim raw As Double = maxValue / CDbl(intervals)
            Dim magnitude As Double = Math.Pow(10.0, Math.Floor(Math.Log10(raw)))
            Dim factors As Double() = {1.0, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0, 6.0, 8.0, 10.0}

            Dim chosen As Double = 10.0

            For Each factor As Double In factors

                If raw <= factor * magnitude + 0.000001 Then
                    chosen = factor
                    Exit For
                End If

            Next

            Dim stepValue As Integer = CInt(Math.Ceiling(chosen * magnitude))

            axisMax = Math.Max(1, stepValue * intervals)

        End Sub


        ' ============================================================
        ' PLACEHOLDER TEXT
        ' ============================================================

        Private Sub DrawLoadingText(g As Graphics, bounds As Rectangle)

            Dim message As String = If(loadFailed, "Data unavailable", "Loading...")

            DrawCenteredText(g, bounds, message)

        End Sub


        Private Sub DrawCenteredText(g As Graphics, bounds As Rectangle, text As String)

            DashGfx.DrawText(
                g, text, DashGfx.Ui(9.0F), TextMuted,
                New RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height),
                StringAlignment.Center, StringAlignment.Center)

        End Sub


        ' ============================================================
        ' CHART 1 - REPAIR STATUS (horizontal progress bars)
        ' ============================================================

        Private Sub DrawRepairStatusChart(sender As Object, e As PaintEventArgs)

            Dim surface As Control = DirectCast(sender, Control)
            Dim g As Graphics = e.Graphics

            Dim width As Integer = surface.ClientSize.Width
            Dim height As Integer = surface.ClientSize.Height

            If width <= 0 OrElse height <= 0 Then
                Return
            End If

            PrepareGraphics(g)

            If currentStats Is Nothing Then
                DrawLoadingText(g, surface.ClientRectangle)
                Return
            End If

            Dim values As Integer() = {
                currentStats.PendingRepairs,
                currentStats.InProgressRepairs,
                currentStats.WaitingForPartsRepairs,
                currentStats.CompletedCount,
                currentStats.CancelledRepairs
            }

            Dim labels As String() = {
                "Pending",
                "In Progress",
                "Waiting",
                "Completed",
                "Cancelled"
            }

            Dim accents As Color() = {
                NeonYellow,
                NeonCyan,
                Color.Orange,
                NeonGreen,
                NeonPink
            }

            Dim maxValue As Integer = Math.Max(1, values.Max())

            Dim labelFont As Font = DashGfx.Ui(8.5F)
            Dim valueFont As Font = DashGfx.Ui(9.0F, FontStyle.Bold)

            ' ---------- column sizes ----------
            Dim labelWidth As Single = 0.0F

            For Each label As String In labels
                labelWidth = Math.Max(labelWidth, DashGfx.TextWidth(g, label, labelFont))
            Next

            Dim valueWidth As Single =
                Math.Max(24.0F, DashGfx.TextWidth(g, maxValue.ToString(), valueFont) + 6.0F)

            Dim trackLeft As Single = 16.0F + labelWidth + 12.0F
            Dim trackWidth As Single = Math.Max(20.0F, width - valueWidth - 8.0F - trackLeft)

            Dim rowHeight As Single = height / CSng(values.Length)
            Dim barHeight As Single = 8.0F

            ' ---------- rows ----------
            For i As Integer = 0 To values.Length - 1

                Dim rowTop As Single = rowHeight * i
                Dim centerY As Single = rowTop + rowHeight / 2.0F

                ' accent dot
                Using dotBrush As New SolidBrush(accents(i))
                    g.FillEllipse(dotBrush, 1.0F, centerY - 3.5F, 7.0F, 7.0F)
                End Using

                ' label
                DashGfx.DrawText(g, labels(i), labelFont, TextMuted,
                                 New RectangleF(16.0F, rowTop, labelWidth + 4.0F, rowHeight))

                ' track
                Dim trackRect As New RectangleF(trackLeft, centerY - barHeight / 2.0F, trackWidth, barHeight)

                DashGfx.FillRounded(g, ChartTrackColor, trackRect, barHeight / 2.0F)

                ' fill
                If values(i) > 0 Then

                    Dim fillWidth As Single =
                        Math.Max(barHeight, trackWidth * values(i) / maxValue)

                    Dim fillRect As New RectangleF(trackRect.X, trackRect.Y, fillWidth, barHeight)

                    Using fillBrush As New LinearGradientBrush(
                        fillRect,
                        Color.FromArgb(120, accents(i)),
                        accents(i),
                        LinearGradientMode.Horizontal)

                        DashGfx.FillRounded(g, fillBrush, fillRect, barHeight / 2.0F)

                    End Using

                End If

                ' value
                DashGfx.DrawText(g, values(i).ToString(), valueFont, TextWhite,
                                 New RectangleF(width - valueWidth, rowTop, valueWidth, rowHeight),
                                 StringAlignment.Far, StringAlignment.Center)

            Next

        End Sub


        ' ============================================================
        ' CHART 2 - MONTHLY REPAIR TREND (smooth area line)
        ' ============================================================

        Private Sub DrawMonthlyRepairsChart(sender As Object, e As PaintEventArgs)

            Dim surface As Control = DirectCast(sender, Control)
            Dim g As Graphics = e.Graphics

            Dim width As Integer = surface.ClientSize.Width
            Dim height As Integer = surface.ClientSize.Height

            If width <= 0 OrElse height <= 0 Then
                Return
            End If

            PrepareGraphics(g)

            If currentStats Is Nothing OrElse currentStats.MonthlyRepairs Is Nothing Then
                DrawLoadingText(g, surface.ClientRectangle)
                Return
            End If

            Dim data = currentStats.MonthlyRepairs
            Dim n As Integer = data.Count

            If n = 0 Then
                DrawCenteredText(g, surface.ClientRectangle, "No repair data")
                Return
            End If

            ' ---------- axis ----------
            Dim dataMax As Integer = CInt(data.Max(Function(x) x.RepairCount))

            Dim intervals As Integer
            Dim axisMax As Integer

            ComputeAxis(dataMax, intervals, axisMax)

            Dim tickStep As Integer = axisMax \ intervals

            Dim axisFont As Font = DashGfx.Ui(7.5F)
            Dim tickLabelWidth As Single = DashGfx.TextWidth(g, axisMax.ToString(), axisFont)

            Dim plotLeft As Single = Math.Max(22.0F, tickLabelWidth + 12.0F)
            Dim plotRight As Single = width - 10.0F
            Dim plotTop As Single = 24.0F
            Dim plotBottom As Single = height - 26.0F

            If plotRight - plotLeft < 40.0F OrElse plotBottom - plotTop < 30.0F Then
                Return
            End If

            Dim plotWidth As Single = plotRight - plotLeft
            Dim plotHeight As Single = plotBottom - plotTop

            ' ---------- grid + y labels ----------
            Using gridPen As New Pen(ChartGridColor, 1.0F)

                For i As Integer = 0 To intervals

                    Dim y As Single = plotTop + plotHeight * i / intervals

                    If i = intervals Then
                        gridPen.DashStyle = DashStyle.Solid
                    Else
                        gridPen.DashPattern = New Single() {2.0F, 3.0F}
                    End If

                    g.DrawLine(gridPen, plotLeft, y, plotRight, y)

                    DashGfx.DrawText(g, (axisMax - i * tickStep).ToString(), axisFont, TextMuted,
                                     New RectangleF(0.0F, y - 8.0F, plotLeft - 8.0F, 16.0F),
                                     StringAlignment.Far, StringAlignment.Center)

                Next

            End Using

            ' ---------- points ----------
            Dim pad As Single = 18.0F

            Dim points(n - 1) As PointF

            For i As Integer = 0 To n - 1

                Dim x As Single

                If n = 1 Then
                    x = plotLeft + plotWidth / 2.0F
                Else
                    x = plotLeft + pad + (plotWidth - 2.0F * pad) * i / (n - 1)
                End If

                Dim y As Single = plotBottom - plotHeight * CSng(data(i).RepairCount) / axisMax

                points(i) = New PointF(x, y)

            Next

            ' ---------- area + line ----------
            If n > 1 Then

                g.SetClip(New RectangleF(plotLeft, plotTop - 6.0F, plotWidth, plotHeight + 8.0F))

                Using areaPath As New GraphicsPath()

                    areaPath.AddCurve(points, 0.35F)
                    areaPath.AddLine(points(n - 1), New PointF(points(n - 1).X, plotBottom))
                    areaPath.AddLine(New PointF(points(n - 1).X, plotBottom), New PointF(points(0).X, plotBottom))
                    areaPath.CloseFigure()

                    Using areaBrush As New LinearGradientBrush(
                        New RectangleF(plotLeft, plotTop, plotWidth, plotHeight),
                        Color.FromArgb(90, NeonCyan),
                        Color.FromArgb(0, NeonCyan),
                        LinearGradientMode.Vertical)

                        g.FillPath(areaBrush, areaPath)

                    End Using

                End Using

                Using linePen As New Pen(NeonCyan, 2.5F)

                    linePen.StartCap = LineCap.Round
                    linePen.EndCap = LineCap.Round
                    linePen.LineJoin = LineJoin.Round

                    g.DrawCurve(linePen, points, 0.35F)

                End Using

                g.ResetClip()

            End If

            ' ---------- markers + labels ----------
            Dim valueFont As Font = DashGfx.Ui(8.0F, FontStyle.Bold)
            Dim monthFont As Font = DashGfx.Ui(7.5F)

            For i As Integer = 0 To n - 1

                Dim p As PointF = points(i)
                Dim isLast As Boolean = (i = n - 1)

                ' glow
                Using glowBrush As New SolidBrush(Color.FromArgb(If(isLast, 70, 40), NeonCyan))
                    g.FillEllipse(glowBrush, p.X - 8.0F, p.Y - 8.0F, 16.0F, 16.0F)
                End Using

                ' ring + hole
                Using holeBrush As New SolidBrush(CardColor)
                    g.FillEllipse(holeBrush, p.X - 4.5F, p.Y - 4.5F, 9.0F, 9.0F)
                End Using

                Using ringPen As New Pen(NeonCyan, 2.0F)
                    g.DrawEllipse(ringPen, p.X - 4.5F, p.Y - 4.5F, 9.0F, 9.0F)
                End Using

                ' value above point
                DashGfx.DrawText(g, data(i).RepairCount.ToString(), valueFont,
                                 If(isLast, NeonCyan, TextWhite),
                                 New RectangleF(p.X - 24.0F, p.Y - 24.0F, 48.0F, 16.0F),
                                 StringAlignment.Center, StringAlignment.Far)

                ' month label
                DashGfx.DrawText(g, data(i).MonthLabel, monthFont, TextMuted,
                                 New RectangleF(p.X - 28.0F, plotBottom + 7.0F, 56.0F, 14.0F),
                                 StringAlignment.Center, StringAlignment.Center)

            Next

        End Sub


        ' ============================================================
        ' CHART 3 - PAYMENT OVERVIEW (ring + legend)
        ' ============================================================

        Private Sub DrawPaymentChart(sender As Object, e As PaintEventArgs)

            Dim surface As Control = DirectCast(sender, Control)
            Dim g As Graphics = e.Graphics

            Dim width As Integer = surface.ClientSize.Width
            Dim height As Integer = surface.ClientSize.Height

            If width <= 0 OrElse height <= 0 Then
                Return
            End If

            PrepareGraphics(g)

            If currentStats Is Nothing Then
                DrawLoadingText(g, surface.ClientRectangle)
                Return
            End If

            Dim values As Integer() = {
                currentStats.PaidPayments,
                currentStats.PartialPayments,
                currentStats.UnpaidPayments
            }

            Dim labels As String() = {"Paid", "Partial", "Unpaid"}

            Dim accents As Color() = {NeonGreen, NeonYellow, NeonPink}

            Dim total As Integer = values.Sum()

            If total <= 0 Then
                DrawCenteredText(g, surface.ClientRectangle, "No payment records")
                Return
            End If

            ' ---------- ring geometry ----------
            Dim ringSize As Integer = Math.Min(height - 8, CInt(width * 0.46F))

            ringSize = Math.Max(60, ringSize)

            If ringSize > height - 4 Then
                ringSize = Math.Max(40, height - 4)
            End If

            Dim thickness As Single = Math.Max(10.0F, ringSize * 0.13F)

            Dim ringX As Single = 4.0F
            Dim ringY As Single = (height - ringSize) / 2.0F

            Dim ringRect As New RectangleF(
                ringX + thickness / 2.0F,
                ringY + thickness / 2.0F,
                ringSize - thickness,
                ringSize - thickness)

            ' ---------- track ----------
            Using trackPen As New Pen(ChartTrackColor, thickness)
                g.DrawEllipse(trackPen, ringRect)
            End Using

            ' ---------- segments ----------
            Dim nonZero As Integer = values.Count(Function(v) v > 0)

            Dim gapAngle As Single = If(nonZero > 1, 4.0F, 0.0F)

            Dim startAngle As Single = -90.0F + gapAngle / 2.0F

            For i As Integer = 0 To values.Length - 1

                If values(i) <= 0 Then
                    Continue For
                End If

                Dim sweep As Single = 360.0F * values(i) / total

                Dim drawSweep As Single = Math.Max(1.0F, sweep - gapAngle)

                Using segmentPen As New Pen(accents(i), thickness)
                    g.DrawArc(segmentPen, ringRect, startAngle, drawSweep)
                End Using

                startAngle += sweep

            Next

            ' ---------- center total ----------
            Dim centerX As Single = ringX + ringSize / 2.0F
            Dim centerY As Single = ringY + ringSize / 2.0F

            DashGfx.DrawText(g, total.ToString("N0"), DashGfx.Ui(16.0F, FontStyle.Bold), TextWhite,
                             New RectangleF(centerX - 40.0F, centerY - 22.0F, 80.0F, 24.0F),
                             StringAlignment.Center, StringAlignment.Far)

            DashGfx.DrawText(g, "payments", DashGfx.Ui(7.5F), TextMuted,
                             New RectangleF(centerX - 40.0F, centerY + 2.0F, 80.0F, 14.0F),
                             StringAlignment.Center, StringAlignment.Near)

            ' ---------- legend ----------
            Dim legendX As Single = ringX + ringSize + 22.0F
            Dim legendWidth As Single = width - legendX - 8.0F

            If legendWidth < 50.0F Then
                Return
            End If

            Dim rowH As Single = Math.Min(46.0F, height / 3.0F)
            Dim legendTop As Single = Math.Max(0.0F, (height - rowH * 3.0F) / 2.0F)

            Dim legendLabelFont As Font = DashGfx.Ui(8.5F)
            Dim legendValueFont As Font = DashGfx.Ui(11.0F, FontStyle.Bold)
            Dim legendPercentFont As Font = DashGfx.Ui(8.0F)

            For i As Integer = 0 To 2

                Dim rowY As Single = legendTop + rowH * i

                Dim percent As Integer = CInt(Math.Round(values(i) * 100.0 / total))

                ' dot
                Using dotBrush As New SolidBrush(accents(i))
                    g.FillEllipse(dotBrush, legendX, rowY + 5.0F, 9.0F, 9.0F)
                End Using

                ' label
                DashGfx.DrawText(g, labels(i), legendLabelFont, TextMuted,
                                 New RectangleF(legendX + 16.0F, rowY, legendWidth - 16.0F, 18.0F))

                ' value + percent
                Dim valueText As String = values(i).ToString("N0")

                DashGfx.DrawText(g, valueText, legendValueFont, TextWhite,
                                 New RectangleF(legendX + 16.0F, rowY + 17.0F, legendWidth - 16.0F, 20.0F))

                Dim valueTextWidth As Single = DashGfx.TextWidth(g, valueText, legendValueFont)

                DashGfx.DrawText(g, percent.ToString() & "%", legendPercentFont, TextMuted,
                                 New RectangleF(legendX + 16.0F + valueTextWidth + 8.0F, rowY + 17.0F,
                                                Math.Max(10.0F, legendWidth - 16.0F - valueTextWidth - 8.0F), 20.0F))

            Next

        End Sub

    End Class

End Namespace