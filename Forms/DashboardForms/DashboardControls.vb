Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.Windows.Forms

Namespace Forms

    ' ============================================================
    ' GLYPH KINDS (icons drawn with GDI+, no icon font needed)
    ' ============================================================

    Friend Enum DashGlyph
        Customers
        Repairs
        Active
        Completed
        Warning
        Coin
        Inventory
        Payments
        Reports
        Refresh
    End Enum


    ' ============================================================
    ' DRAWING HELPERS
    ' ============================================================

    Friend NotInheritable Class DashGfx

        Private Sub New()
        End Sub

        Private Shared ReadOnly fontCache As New Dictionary(Of String, Font)()


        ' Cached fonts: created once, reused everywhere (no GDI leaks)
        Public Shared Function Ui(size As Single, Optional style As FontStyle = FontStyle.Regular) As Font

            Dim key As String =
                size.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) &
                "|" & CInt(style).ToString()

            Dim cached As Font = Nothing

            If Not fontCache.TryGetValue(key, cached) Then
                cached = UiFont(size, style)
                fontCache(key) = cached
            End If

            Return cached

        End Function


        Public Shared Function ParentBack(c As Control) As Color

            If c.Parent IsNot Nothing Then
                Return c.Parent.BackColor
            End If

            Return c.BackColor

        End Function


        Public Shared Function RoundedPath(rect As RectangleF, radius As Single) As GraphicsPath

            Dim path As New GraphicsPath()

            Dim d As Single =
                Math.Min(radius * 2.0F, Math.Min(rect.Width, rect.Height))

            If d < 1.0F Then
                path.AddRectangle(rect)
                Return path
            End If

            path.AddArc(rect.X, rect.Y, d, d, 180.0F, 90.0F)
            path.AddArc(rect.Right - d, rect.Y, d, d, 270.0F, 90.0F)
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0.0F, 90.0F)
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90.0F, 90.0F)
            path.CloseFigure()

            Return path

        End Function


        Public Shared Sub FillRounded(g As Graphics, brush As Brush, rect As RectangleF, radius As Single)

            Using path As GraphicsPath = RoundedPath(rect, radius)
                g.FillPath(brush, path)
            End Using

        End Sub


        Public Shared Sub FillRounded(g As Graphics, color As Color, rect As RectangleF, radius As Single)

            Using brush As New SolidBrush(color)
                FillRounded(g, brush, rect, radius)
            End Using

        End Sub


        Public Shared Sub StrokeRounded(g As Graphics, color As Color, width As Single, rect As RectangleF, radius As Single)

            Using path As GraphicsPath = RoundedPath(rect, radius)
                Using pen As New Pen(color, width)
                    g.DrawPath(pen, path)
                End Using
            End Using

        End Sub


              Public Shared Function TextWidth(g As Graphics, text As String, font As Font) As Single

            If String.IsNullOrEmpty(text) OrElse font Is Nothing Then
                Return 0.0F
            End If

            Return TextRenderer.MeasureText(
                g, text, font,
                New Size(10000, 10000),
                TextFormatFlags.NoPadding Or TextFormatFlags.SingleLine).Width

        End Function

                Public Shared Sub DrawText(
            g As Graphics,
            text As String,
            font As Font,
            color As Color,
            rect As RectangleF,
            Optional horizontal As StringAlignment = StringAlignment.Near,
            Optional vertical As StringAlignment = StringAlignment.Center)

            If g Is Nothing OrElse font Is Nothing Then
                Return
            End If

            If String.IsNullOrEmpty(text) Then
                Return
            End If

            If Single.IsNaN(rect.Width) OrElse Single.IsNaN(rect.Height) Then
                Return
            End If

            If rect.Width < 2.0F OrElse rect.Height < 2.0F Then
                Return
            End If

            Dim flags As TextFormatFlags =
                TextFormatFlags.NoPadding Or
                TextFormatFlags.SingleLine Or
                TextFormatFlags.EndEllipsis

            Select Case horizontal
                Case StringAlignment.Near
                    flags = flags Or TextFormatFlags.Left
                Case StringAlignment.Center
                    flags = flags Or TextFormatFlags.HorizontalCenter
                Case Else
                    flags = flags Or TextFormatFlags.Right
            End Select

            Select Case vertical
                Case StringAlignment.Near
                    flags = flags Or TextFormatFlags.Top
                Case StringAlignment.Center
                    flags = flags Or TextFormatFlags.VerticalCenter
                Case Else
                    flags = flags Or TextFormatFlags.Bottom
            End Select

            TextRenderer.DrawText(g, text, font, Rectangle.Round(rect), color, flags)

        End Sub


        Public Shared Sub DrawGlyph(g As Graphics, kind As DashGlyph, bounds As RectangleF, color As Color)

            Dim w As Single = bounds.Width
            Dim h As Single = bounds.Height
            Dim ox As Single = bounds.X
            Dim oy As Single = bounds.Y

            Dim P As Func(Of Single, Single, PointF) =
                Function(u As Single, v As Single) New PointF(ox + u * w, oy + v * h)

            Dim previousMode As SmoothingMode = g.SmoothingMode
            g.SmoothingMode = SmoothingMode.AntiAlias

            Using pen As New Pen(color, Math.Max(1.4F, w / 10.0F))

                pen.StartCap = LineCap.Round
                pen.EndCap = LineCap.Round
                pen.LineJoin = LineJoin.Round

                Using brush As New SolidBrush(color)

                    Select Case kind

                        Case DashGlyph.Customers

                            g.DrawEllipse(pen, ox + 0.32F * w, oy + 0.06F * h, 0.36F * w, 0.36F * h)
                            g.DrawArc(pen, ox + 0.12F * w, oy + 0.56F * h, 0.76F * w, 0.8F * h, 180.0F, 180.0F)

                        Case DashGlyph.Repairs

                            Using phone As GraphicsPath =
                                RoundedPath(New RectangleF(ox + 0.27F * w, oy + 0.04F * h, 0.46F * w, 0.92F * h), 0.1F * w)

                                g.DrawPath(pen, phone)

                            End Using

                            g.DrawLine(pen, P(0.43F, 0.84F), P(0.57F, 0.84F))

                        Case DashGlyph.Active

                            g.DrawLines(pen, New PointF() {
                                P(0.04F, 0.52F), P(0.28F, 0.52F), P(0.4F, 0.2F),
                                P(0.58F, 0.84F), P(0.7F, 0.52F), P(0.96F, 0.52F)})

                        Case DashGlyph.Completed

                            g.DrawEllipse(pen, ox + 0.08F * w, oy + 0.08F * h, 0.84F * w, 0.84F * h)
                            g.DrawLines(pen, New PointF() {P(0.3F, 0.52F), P(0.45F, 0.67F), P(0.72F, 0.37F)})

                        Case DashGlyph.Warning

                            g.DrawPolygon(pen, New PointF() {P(0.5F, 0.1F), P(0.94F, 0.88F), P(0.06F, 0.88F)})
                            g.DrawLine(pen, P(0.5F, 0.4F), P(0.5F, 0.62F))
                            g.FillEllipse(brush, ox + 0.45F * w, oy + 0.7F * h, 0.1F * w, 0.1F * h)

                                                Case DashGlyph.Coin

                            g.DrawEllipse(pen, ox + 0.06F * w, oy + 0.06F * h, 0.88F * w, 0.88F * h)

                            Dim capH As Single = h * 0.42F
                            Dim pw As Single = capH * 0.75F
                            Dim px As Single = ox + (w - pw) / 2.0F
                            Dim py As Single = oy + (h - capH) / 2.0F
                            Dim bowlH As Single = capH * 0.6F
                            Dim stemX As Single = px + pw * 0.12F
                            Dim rightX As Single = px + pw

                            Using thin As New Pen(color, Math.Max(1.0F, w / 14.0F))
                                g.DrawLine(thin, stemX, py, stemX, py + capH)
                                g.DrawLine(thin, stemX, py, rightX - bowlH / 2.0F, py)
                                g.DrawArc(thin, rightX - bowlH, py, bowlH, bowlH, -90.0F, 180.0F)
                                g.DrawLine(thin, stemX, py + bowlH, rightX - bowlH / 2.0F, py + bowlH)
                                g.DrawLine(thin, px, py + capH * 0.27F, rightX + 1.0F, py + capH * 0.27F)
                                g.DrawLine(thin, px, py + capH * 0.45F, rightX + 1.0F, py + capH * 0.45F)
                            End Using

                        Case DashGlyph.Inventory

                            g.DrawRectangle(pen, ox + 0.06F * w, oy + 0.12F * h, 0.88F * w, 0.2F * h)

                            g.DrawLines(pen, New PointF() {
                                P(0.12F, 0.32F), P(0.12F, 0.9F), P(0.88F, 0.9F), P(0.88F, 0.32F)})

                            g.DrawLine(pen, P(0.4F, 0.54F), P(0.6F, 0.54F))

                        Case DashGlyph.Payments

                            Using card As GraphicsPath =
                                RoundedPath(New RectangleF(ox + 0.04F * w, oy + 0.2F * h, 0.92F * w, 0.6F * h), 0.12F * w)

                                g.DrawPath(pen, card)

                            End Using

                            g.DrawLine(pen, P(0.04F, 0.4F), P(0.96F, 0.4F))
                            g.DrawLine(pen, P(0.2F, 0.64F), P(0.4F, 0.64F))

                        Case DashGlyph.Reports

                            g.FillRectangle(brush, ox + 0.12F * w, oy + 0.52F * h, 0.2F * w, 0.38F * h)
                            g.FillRectangle(brush, ox + 0.4F * w, oy + 0.2F * h, 0.2F * w, 0.7F * h)
                            g.FillRectangle(brush, ox + 0.68F * w, oy + 0.38F * h, 0.2F * w, 0.52F * h)

                        Case DashGlyph.Refresh

                            Dim arcRect As New RectangleF(ox + 0.14F * w, oy + 0.14F * h, 0.72F * w, 0.72F * h)

                            g.DrawArc(pen, arcRect, 50.0F, 280.0F)

                            Dim radius As Single = arcRect.Width / 2.0F
                            Dim centerX As Single = arcRect.X + radius
                            Dim centerY As Single = arcRect.Y + arcRect.Height / 2.0F
                            Dim endAngle As Double = 330.0 * Math.PI / 180.0

                            Dim tip As New PointF(
                                centerX + radius * CSng(Math.Cos(endAngle)),
                                centerY + radius * CSng(Math.Sin(endAngle)))

                            Dim tx As Single = -CSng(Math.Sin(endAngle))
                            Dim ty As Single = CSng(Math.Cos(endAngle))
                            Dim nx As Single = -ty
                            Dim ny As Single = tx
                            Dim s As Single = 0.3F * w

                            Dim pa As New PointF(tip.X + tx * s * 0.7F, tip.Y + ty * s * 0.7F)

                            Dim pb As New PointF(
                                tip.X - tx * s * 0.15F + nx * s * 0.7F,
                                tip.Y - ty * s * 0.15F + ny * s * 0.7F)

                            Dim pc As New PointF(
                                tip.X - tx * s * 0.15F - nx * s * 0.7F,
                                tip.Y - ty * s * 0.15F - ny * s * 0.7F)

                            g.FillPolygon(brush, New PointF() {pa, pb, pc})

                    End Select

                End Using

            End Using

            g.SmoothingMode = previousMode

        End Sub

    End Class


    ' ============================================================
    ' DOUBLE-BUFFERED TABLE (no flicker while resizing)
    ' ============================================================

    Friend Class DashTable
        Inherits TableLayoutPanel

        Public Sub New()
            MyBase.New()
            DoubleBuffered = True
        End Sub

    End Class


    ' ============================================================
    ' CHART SURFACE (double-buffered panel the charts paint on)
    ' ============================================================

    Friend Class DashChartSurface
        Inherits Panel

        Public Sub New()
            MyBase.New()
            DoubleBuffered = True
            ResizeRedraw = True
            BackColor = CardColor
        End Sub

    End Class


    ' ============================================================
    ' CARD (rounded container with accent title + subtitle)
    ' ============================================================

    Friend Class DashCard
        Inherits Panel

        Private Const CornerRadius As Single = 14.0F

        Private _title As String = ""
        Private _subtitle As String = ""
        Private _accent As Color = Color.Empty

        Public Sub New()

            MyBase.New()

            SetStyle(ControlStyles.UserPaint Or
                     ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or
                     ControlStyles.ResizeRedraw, True)

            BackColor = CardColor

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


        Public Property Subtitle As String
            Get
                Return _subtitle
            End Get
            Set(value As String)
                _subtitle = value
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


        Protected Overrides Sub OnPaint(e As PaintEventArgs)

            MyBase.OnPaint(e)

            Dim g As Graphics = e.Graphics

            g.Clear(DashGfx.ParentBack(Me))

            If Width < 8 OrElse Height < 8 Then
                Return
            End If

            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

            Dim rect As New RectangleF(0.5F, 0.5F, Width - 1.0F, Height - 1.0F)

            DashGfx.FillRounded(g, BackColor, rect, CornerRadius)
            DashGfx.StrokeRounded(g, Color.FromArgb(34, 255, 255, 255), 1.0F, rect, CornerRadius)

            Dim titleX As Single = 20.0F

            If Not _accent.IsEmpty Then
                DashGfx.FillRounded(g, _accent, New RectangleF(20.0F, 17.0F, 4.0F, 30.0F), 2.0F)
                titleX = 32.0F
            End If

            Dim textWidth As Single = Math.Max(10.0F, Width - titleX - 16.0F)

            DashGfx.DrawText(g, _title, DashGfx.Ui(10.5F, FontStyle.Bold), TextWhite,
                             New RectangleF(titleX, 14.0F, textWidth, 20.0F))

            DashGfx.DrawText(g, _subtitle, DashGfx.Ui(8.0F), TextMuted,
                             New RectangleF(titleX, 34.0F, textWidth, 16.0F))

        End Sub

    End Class


    ' ============================================================
    ' STAT CARD
    ' ============================================================

    Friend Class DashStatCard
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
            Size = New Size(160, 110)

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

            If Width < 60 OrElse Height < 70 Then
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
            Dim badgeSize As Single = 30.0F

            Dim badgeRect As New RectangleF(Width - 14.0F - badgeSize, 14.0F, badgeSize, badgeSize)

            DashGfx.FillRounded(g, Color.FromArgb(40, _accent), badgeRect, 9.0F)

            DashGfx.DrawGlyph(g, _glyph,
                              New RectangleF(badgeRect.X + 7.0F, badgeRect.Y + 7.0F, 16.0F, 16.0F),
                              _accent)

            ' ---------- title ----------
            DashGfx.DrawText(g, _title, DashGfx.Ui(8.5F), TextMuted,
                             New RectangleF(16.0F, 14.0F, Math.Max(10.0F, badgeRect.X - 24.0F), badgeSize))

            ' ---------- value (auto-fit) ----------
          ' ---------- value (auto-fit) ----------
Dim valueRect As New RectangleF(
    16.0F,
    46.0F,
    Width - 32.0F,
    Math.Max(20.0F, Height - 28.0F - 46.0F - 2.0F)
)

Dim displayText As String = _valueText
Dim isPeso As Boolean = False

' Detect Philippine Peso symbol
If Not String.IsNullOrEmpty(displayText) AndAlso
   displayText.StartsWith(ChrW(&H20B1).ToString()) Then

    isPeso = True
    displayText = displayText.Substring(1)

End If

Dim valueFont As Font =
    DashGfx.Ui(ValueSizes(ValueSizes.Length - 1), FontStyle.Bold)

For Each size As Single In ValueSizes

    Dim candidate As Font =
        DashGfx.Ui(size, FontStyle.Bold)

    Dim estimatedPesoWidth As Single =
        If(isPeso, size * 0.58F + size * 0.20F, 0.0F)

    If DashGfx.TextWidth(g, displayText, candidate) +
       estimatedPesoWidth <= valueRect.Width Then

        valueFont = candidate
        Exit For

    End If

Next

If isPeso Then

    ' Custom-draw the peso symbol so it does not depend on font support
    Dim pesoSize As Single = valueFont.Size * 0.82F
    Dim pesoWidth As Single = pesoSize * 0.58F

    Dim pesoY As Single =
        valueRect.Y +
        (valueRect.Height - pesoSize) / 2.0F

    DrawPeso(
        g,
        valueRect.X,
        pesoY,
        pesoSize,
        TextWhite
    )

    ' Draw the numeric amount beside the custom peso
    Dim numberRect As New RectangleF(
        valueRect.X + pesoWidth + valueFont.Size * 0.20F,
        valueRect.Y,
        valueRect.Width - pesoWidth - valueFont.Size * 0.20F,
        valueRect.Height
    )

    DashGfx.DrawText(
        g,
        displayText,
        valueFont,
        TextWhite,
        numberRect
    )

Else

    ' Normal KPI values
    DashGfx.DrawText(
        g,
        displayText,
        valueFont,
        TextWhite,
        valueRect
    )

End If

            ' ---------- description ----------
            DashGfx.DrawText(g, _description, DashGfx.Ui(7.5F), TextMuted,
                             New RectangleF(16.0F, Height - 28.0F, Width - 32.0F, 16.0F))

        End Sub

    End Class


    ' ============================================================
    ' QUICK ACTION TILE
    ' ============================================================

    Friend Class DashActionTile
        Inherits Control

        Private _caption As String = ""
        Private _hint As String = ""
        Private _accent As Color = Color.White
        Private _glyph As DashGlyph = DashGlyph.Customers
        Private _hover As Boolean
        Private _pressed As Boolean

        Public Sub New()

            MyBase.New()

            SetStyle(ControlStyles.UserPaint Or
                     ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or
                     ControlStyles.ResizeRedraw Or
                     ControlStyles.Selectable, True)

            TabStop = True
            Cursor = Cursors.Hand
            BackColor = CardColor
            Size = New Size(160, 50)

        End Sub


        Public Property Caption As String
            Get
                Return _caption
            End Get
            Set(value As String)
                _caption = value
                Invalidate()
            End Set
        End Property


        Public Property Hint As String
            Get
                Return _hint
            End Get
            Set(value As String)
                _hint = value
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


        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            MyBase.OnMouseEnter(e)
            _hover = True
            Invalidate()
        End Sub


        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)
            _hover = False
            _pressed = False
            Invalidate()
        End Sub


        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)
            If e.Button = MouseButtons.Left Then
                _pressed = True
                Invalidate()
            End If
        End Sub


        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)
            _pressed = False
            Invalidate()
        End Sub


        Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
            MyBase.OnKeyDown(e)
            If e.KeyCode = Keys.Enter OrElse e.KeyCode = Keys.Space Then
                OnClick(EventArgs.Empty)
            End If
        End Sub


        Protected Overrides Sub OnGotFocus(e As EventArgs)
            MyBase.OnGotFocus(e)
            Invalidate()
        End Sub


        Protected Overrides Sub OnLostFocus(e As EventArgs)
            MyBase.OnLostFocus(e)
            Invalidate()
        End Sub


        Protected Overrides Sub OnPaint(e As PaintEventArgs)

            MyBase.OnPaint(e)

            Dim g As Graphics = e.Graphics

            g.Clear(DashGfx.ParentBack(Me))

            If Width < 60 OrElse Height < 28 Then
                Return
            End If

            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

            Dim rect As New RectangleF(0.5F, 0.5F, Width - 1.0F, Height - 1.0F)

            Dim fillColor As Color
            Dim borderColor As Color

            If _pressed Then
                fillColor = Color.FromArgb(56, _accent)
                borderColor = Color.FromArgb(190, _accent)
            ElseIf _hover Then
                fillColor = Color.FromArgb(34, _accent)
                borderColor = Color.FromArgb(150, _accent)
            Else
                fillColor = Color.FromArgb(14, 255, 255, 255)
                borderColor = Color.FromArgb(30, 255, 255, 255)
            End If

            If Focused AndAlso ShowFocusCues Then
                borderColor = _accent
            End If

            DashGfx.FillRounded(g, fillColor, rect, 12.0F)
            DashGfx.StrokeRounded(g, borderColor, 1.0F, rect, 12.0F)

            ' ---------- icon badge ----------
            Dim badgeSize As Single = Math.Min(34.0F, Height - 16.0F)

            Dim badgeRect As New RectangleF(12.0F, (Height - badgeSize) / 2.0F, badgeSize, badgeSize)

            DashGfx.FillRounded(g, Color.FromArgb(If(_hover OrElse _pressed, 70, 40), _accent), badgeRect, 10.0F)

            Dim glyphSize As Single = badgeSize * 0.5F

            DashGfx.DrawGlyph(g, _glyph,
                              New RectangleF(badgeRect.X + (badgeSize - glyphSize) / 2.0F,
                                             badgeRect.Y + (badgeSize - glyphSize) / 2.0F,
                                             glyphSize, glyphSize),
                              _accent)

            ' ---------- text ----------
            Dim textX As Single = badgeRect.Right + 10.0F
            Dim textW As Single = Width - textX - 10.0F

            If textW < 12.0F Then
                Return
            End If

            DashGfx.DrawText(g, _caption, DashGfx.Ui(9.5F, FontStyle.Bold), TextWhite,
                             New RectangleF(textX, Height / 2.0F - 19.0F, textW, 18.0F),
                             StringAlignment.Near, StringAlignment.Far)

            DashGfx.DrawText(g, _hint, DashGfx.Ui(7.5F), TextMuted,
                             New RectangleF(textX, Height / 2.0F + 1.0F, textW, 16.0F),
                             StringAlignment.Near, StringAlignment.Near)

        End Sub

    End Class


    ' ============================================================
    ' PILL BUTTON (Refresh)
    ' ============================================================

    Friend Class DashPillButton
        Inherits Control

        Private _caption As String = ""
        Private _accent As Color = Color.White
        Private _glyph As DashGlyph = DashGlyph.Refresh
        Private _hover As Boolean
        Private _pressed As Boolean

        Public Sub New()

            MyBase.New()

            SetStyle(ControlStyles.UserPaint Or
                     ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or
                     ControlStyles.ResizeRedraw Or
                     ControlStyles.Selectable, True)

            TabStop = True
            Cursor = Cursors.Hand
            Size = New Size(112, 34)

        End Sub


        Public Property Caption As String
            Get
                Return _caption
            End Get
            Set(value As String)
                _caption = value
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


        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            MyBase.OnMouseEnter(e)
            _hover = True
            Invalidate()
        End Sub


        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)
            _hover = False
            _pressed = False
            Invalidate()
        End Sub


        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)
            If e.Button = MouseButtons.Left Then
                _pressed = True
                Invalidate()
            End If
        End Sub


        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)
            _pressed = False
            Invalidate()
        End Sub


        Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
            MyBase.OnKeyDown(e)
            If e.KeyCode = Keys.Enter OrElse e.KeyCode = Keys.Space Then
                OnClick(EventArgs.Empty)
            End If
        End Sub


        Protected Overrides Sub OnPaint(e As PaintEventArgs)

            MyBase.OnPaint(e)

            Dim g As Graphics = e.Graphics

            g.Clear(DashGfx.ParentBack(Me))

            If Width < 50 OrElse Height < 20 Then
                Return
            End If

            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

            Dim rect As New RectangleF(0.5F, 0.5F, Width - 1.0F, Height - 1.0F)
            Dim radius As Single = rect.Height / 2.0F

            Dim fillAlpha As Integer = If(_pressed, 64, If(_hover, 46, 22))
            Dim borderAlpha As Integer = If(_hover OrElse _pressed, 170, 110)

            DashGfx.FillRounded(g, Color.FromArgb(fillAlpha, _accent), rect, radius)
            DashGfx.StrokeRounded(g, Color.FromArgb(borderAlpha, _accent), 1.0F, rect, radius)

            DashGfx.DrawGlyph(g, _glyph,
                              New RectangleF(14.0F, (Height - 14.0F) / 2.0F, 14.0F, 14.0F),
                              _accent)

            DashGfx.DrawText(g, _caption, DashGfx.Ui(9.0F, FontStyle.Bold), TextWhite,
                             New RectangleF(34.0F, 0.0F, Width - 34.0F - 12.0F, CSng(Height)))

        End Sub

    End Class

End Namespace