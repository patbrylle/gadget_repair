Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.Windows.Forms

Namespace Forms

    ' ============================================================
    ' KPI CARD (compact layout for the 3 x 2 grid)
    ' ============================================================

    Friend Class DashKpiCard
        Inherits Control

        Private Shared ReadOnly ValueSizes As Single() =
            {20.0F, 18.0F, 16.0F, 14.0F, 12.0F, 11.0F, 10.0F}

        Private _title As String = ""
        Private _valueText As String = ""
        Private _description As String = ""
        Private _accent As Color = Color.White
        Private _glyph As DashGlyph = DashGlyph.Customers

        Public Sub New()

            MyBase.New()

            SetStyle(ControlStyles.UserPaint Or
                     ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or
                     ControlStyles.ResizeRedraw, True)

            BackColor = CardColor
            Size = New Size(200, 93)

        End Sub


        Public Property Title As String
            Get
                Return _title
            End Get
            Set(value As String)
                _title = value
                Invalidate()
            End Set
        End Property


        Public Property ValueText As String
            Get
                Return _valueText
            End Get
            Set(value As String)
                _valueText = value
                Invalidate()
            End Set
        End Property


        Public Property Description As String
            Get
                Return _description
            End Get
            Set(value As String)
                _description = value
                Invalidate()
            End Set
        End Property


        Public Property Accent As Color
            Get
                Return _accent
            End Get
            Set(value As Color)
                _accent = value
                Invalidate()
            End Set
        End Property


        Public Property Glyph As DashGlyph
            Get
                Return _glyph
            End Get
            Set(value As DashGlyph)
                _glyph = value
                Invalidate()
            End Set
        End Property


        Protected Overrides Sub OnPaint(e As PaintEventArgs)

            MyBase.OnPaint(e)

            Dim g As Graphics = e.Graphics

            g.Clear(DashGfx.ParentBack(Me))

            If Width < 60 OrElse Height < 60 Then
                Return
            End If

            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

            Dim rect As New RectangleF(0.5F, 0.5F, Width - 1.0F, Height - 1.0F)

            ' ---------- card body ----------
            Using path As GraphicsPath = DashGfx.RoundedPath(rect, 14.0F)

                Using fill As New SolidBrush(BackColor)
                    g.FillPath(fill, path)
                End Using

                g.SetClip(path)

                Using glow As New LinearGradientBrush(
                    rect,
                    Color.FromArgb(38, _accent),
                    Color.FromArgb(0, _accent),
                    LinearGradientMode.ForwardDiagonal)

                    g.FillRectangle(glow, rect)

                End Using

                Using bar As New SolidBrush(Color.FromArgb(220, _accent))
                    g.FillRectangle(bar, rect.X, rect.Y, rect.Width, 2.5F)
                End Using

                g.ResetClip()

                Using border As New Pen(Color.FromArgb(34, 255, 255, 255), 1.0F)
                    g.DrawPath(border, path)
                End Using

            End Using

            ' ---------- icon badge ----------
            Dim badgeSize As Single = 28.0F

            Dim badgeRect As New RectangleF(Width - 14.0F - badgeSize, 10.0F, badgeSize, badgeSize)

            DashGfx.FillRounded(g, Color.FromArgb(40, _accent), badgeRect, 9.0F)

            DashGfx.DrawGlyph(g, _glyph,
                              New RectangleF(badgeRect.X + 6.0F, badgeRect.Y + 6.0F, 16.0F, 16.0F),
                              _accent)

            ' ---------- title ----------
            DashGfx.DrawText(g, _title, DashGfx.Ui(8.5F), TextMuted,
                             New RectangleF(16.0F, 10.0F, Math.Max(10.0F, badgeRect.X - 24.0F), 20.0F))

            ' ---------- value (auto-fit) ----------
            Dim valueRect As New RectangleF(16.0F, 30.0F, Width - 32.0F, Math.Max(20.0F, Height - 30.0F - 29.0F))

            Dim valueFont As Font = DashGfx.Ui(ValueSizes(ValueSizes.Length - 1), FontStyle.Bold)

            For Each size As Single In ValueSizes

                Dim candidate As Font = DashGfx.Ui(size, FontStyle.Bold)

                If DashGfx.TextWidth(g, _valueText, candidate) <= valueRect.Width Then
                    valueFont = candidate
                    Exit For
                End If

            Next

            DashGfx.DrawText(g, _valueText, valueFont, TextWhite, valueRect)

            ' ---------- description ----------
            DashGfx.DrawText(g, _description, DashGfx.Ui(7.5F), TextMuted,
                             New RectangleF(16.0F, Height - 28.0F, Width - 32.0F, 16.0F))

        End Sub

    End Class


    ' ============================================================
    ' TREND CARD (DashCard + "% vs previous month" pill)
    ' ============================================================

    Friend Class DashTrendCard
        Inherits DashCard

        Private _badge As String = ""
        Private _badgeColor As Color = Color.White
        Private _badgeDirection As Integer


        Public Property Badge As String
            Get
                Return _badge
            End Get
            Set(value As String)
                _badge = value
                Invalidate()
            End Set
        End Property


        Public Property BadgeColor As Color
            Get
                Return _badgeColor
            End Get
            Set(value As Color)
                _badgeColor = value
                Invalidate()
            End Set
        End Property


        ' 1 = up, -1 = down, 0 = flat
        Public Property BadgeDirection As Integer
            Get
                Return _badgeDirection
            End Get
            Set(value As Integer)
                _badgeDirection = value
                Invalidate()
            End Set
        End Property


        Protected Overrides Sub OnPaint(e As PaintEventArgs)

            MyBase.OnPaint(e)

            If String.IsNullOrEmpty(_badge) OrElse Width < 260 Then
                Return
            End If

            Dim g As Graphics = e.Graphics

            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

            Dim badgeFont As Font = DashGfx.Ui(8.5F, FontStyle.Bold)

            Dim textW As Single = DashGfx.TextWidth(g, _badge, badgeFont)
            Dim pillW As Single = textW + 38.0F

            Dim pillRect As New RectangleF(Width - 18.0F - pillW, 16.0F, pillW, 26.0F)

            DashGfx.FillRounded(g, Color.FromArgb(34, _badgeColor), pillRect, 13.0F)

            Dim cx As Single = pillRect.X + 15.0F
            Dim cy As Single = pillRect.Y + 13.0F

            Using arrowBrush As New SolidBrush(_badgeColor)

                If _badgeDirection > 0 Then

                    g.FillPolygon(arrowBrush, New PointF() {
                        New PointF(cx - 4.5F, cy + 3.0F),
                        New PointF(cx + 4.5F, cy + 3.0F),
                        New PointF(cx, cy - 4.0F)})

                ElseIf _badgeDirection < 0 Then

                    g.FillPolygon(arrowBrush, New PointF() {
                        New PointF(cx - 4.5F, cy - 3.0F),
                        New PointF(cx + 4.5F, cy - 3.0F),
                        New PointF(cx, cy + 4.0F)})

                Else

                    g.FillRectangle(arrowBrush, cx - 4.0F, cy - 1.0F, 8.0F, 2.0F)

                End If

            End Using

            DashGfx.DrawText(g, _badge, badgeFont, _badgeColor,
                             New RectangleF(pillRect.X + 26.0F, pillRect.Y, textW + 12.0F, pillRect.Height))

        End Sub

    End Class

End Namespace