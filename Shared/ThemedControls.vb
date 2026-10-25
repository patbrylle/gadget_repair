Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.Windows.Forms

' Shared custom controls (flicker-free, no transparent back colors).
' Used by MainLayout and DashboardForm.

Public Class BufferedPanel
    Inherits Panel

    Public Sub New()
        DoubleBuffered = True
        SetStyle(ControlStyles.ResizeRedraw, True)
    End Sub

End Class


Public Class BufferedTable
    Inherits TableLayoutPanel

    Public Sub New()
        DoubleBuffered = True
    End Sub

End Class


Public Class SurfaceControl
    Inherits Control

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw, True)
        BackColor = BackgroundColor
    End Sub

End Class


Public Class AnimatedControl
    Inherits SurfaceControl

    Private ReadOnly animTimer As New System.Windows.Forms.Timer()
    Private hoverTarget As Single = 0.0F
    Private hoverCurrent As Single = 0.0F

    Protected ReadOnly Property Hover As Single
        Get
            Return hoverCurrent
        End Get
    End Property

    Public Sub New()
        Cursor = Cursors.Hand
        animTimer.Interval = 16
        AddHandler animTimer.Tick, AddressOf AnimTick
    End Sub

    Private Sub AnimTick(sender As Object, e As EventArgs)

        If hoverCurrent < hoverTarget Then
            hoverCurrent = Math.Min(hoverTarget, hoverCurrent + 0.12F)
        ElseIf hoverCurrent > hoverTarget Then
            hoverCurrent = Math.Max(hoverTarget, hoverCurrent - 0.12F)
        End If

        Invalidate()

        If hoverCurrent = hoverTarget Then
            animTimer.Stop()
        End If

    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        hoverTarget = 1.0F
        animTimer.Start()
        MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        hoverTarget = 0.0F
        animTimer.Start()
        MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            animTimer.Stop()
            animTimer.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class


' ---------------------- LOGO ----------------------

Public Class LogoControl
    Inherits SurfaceControl

    Public Sub New()
        BackColor = SidebarColor
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        Dim g As Graphics = e.Graphics
        g.Clear(SidebarColor)
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit

        Dim cy As Single = Height / 2.0F

        DrawLogoMark(g, 44.0F, cy, 23.0F)

        Using br As New SolidBrush(TextWhite)
            g.DrawString("GADGET", UiFont(15.0F, FontStyle.Bold), br, 78.0F, cy - 22.0F)
        End Using

        Using br As New SolidBrush(NeonCyan)
            g.DrawString("REPAIR  CONTROL", UiFont(7.0F, FontStyle.Bold), br, 80.0F, cy + 5.0F)
        End Using

    End Sub

End Class


' ---------------------- SIDEBAR SECTION HEADING ----------------------

Public Class SectionLabel
    Inherits SurfaceControl

    Public Sub New()
        BackColor = SidebarColor
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        Dim g As Graphics = e.Graphics
        g.Clear(SidebarColor)
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit

        Using sf As New StringFormat()
            sf.LineAlignment = StringAlignment.Far
            sf.Alignment = StringAlignment.Near
            sf.FormatFlags = StringFormatFlags.NoWrap

            Using br As New SolidBrush(TextMuted)
                g.DrawString(Text, UiFont(7.0F, FontStyle.Bold), br,
                             New RectangleF(16, 0, Math.Max(10, Width - 32), Height - 6), sf)
            End Using
        End Using

    End Sub

End Class


' ---------------------- SIDEBAR BUTTON ----------------------

Public Class NavButton
    Inherits AnimatedControl

    Private _isActive As Boolean = False
Private ReadOnly navFont As Font = MakeNavFont()

Private Shared Function MakeNavFont() As Font

    Try
        ' private copy, so disposing it never affects a shared theme font
        Return CType(UiFont(10.0F, FontStyle.Bold).Clone(), Font)
    Catch
        Return New Font(FontFamily.GenericSansSerif, 10.0F, FontStyle.Bold)
    End Try

End Function

    Public Property MenuName As String = ""

    Public Property IsActive As Boolean
        Get
            Return _isActive
        End Get
        Set(value As Boolean)
            _isActive = value
            Invalidate()
        End Set
    End Property

    Public Sub New()
        BackColor = SidebarColor
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        Dim g As Graphics = e.Graphics
        g.Clear(SidebarColor)

        ' too small to draw safely
        If Width < 30 OrElse Height < 24 Then Return

        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit

        Dim v As Single = Math.Min(1.0F, Math.Max(Hover, If(_isActive, 0.85F, 0.0F)))
        Dim rect As New Rectangle(6, 3, Width - 13, Height - 7)

        If v > 0.01F AndAlso rect.Width > 20 AndAlso rect.Height > 20 Then

            Using path As GraphicsPath = RoundedRect(rect, 10)

                Using br As New SolidBrush(Color.FromArgb(CInt(10 + v * 45), 120, 55, 210))
                    g.FillPath(br, path)
                End Using

                Using p As New Pen(Color.FromArgb(CInt(15 + v * 110), NeonPurple), 1.2F)
                    g.DrawPath(p, path)
                End Using

            End Using

        End If

        If _isActive AndAlso Height > 30 Then

            Using glow As New Pen(Color.FromArgb(80, NeonPurple), 7.0F)
                g.DrawLine(glow, 3, 12, 3, Height - 12)
            End Using

            Using bar As New Pen(NeonPurple, 3.0F)
                bar.StartCap = LineCap.Round
                bar.EndCap = LineCap.Round
                g.DrawLine(bar, 3, 12, 3, Height - 12)
            End Using

        End If

        ' glowing icon: cyan when active, purple otherwise
        Dim iconColor As Color = If(_isActive, NeonCyan, Lerp(NeonPurple, NeonCyan, Hover * 0.5F))

        DrawGlowGlyph(g, True, MenuName, iconColor, v, 18.0F, (Height - 24) / 2.0F)

                ' text
        Dim textRect As New Rectangle(54, 0, Math.Max(10, Width - 60), Height)

        TextRenderer.DrawText(g, Text, navFont, textRect, Lerp(TextMuted, TextViolet, v),
            TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
            TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding Or
            TextFormatFlags.SingleLine)
    End Sub

     Protected Overrides Sub Dispose(disposing As Boolean)
        MyBase.Dispose(disposing)
    End Sub

End Class


' ---------------------- HEADER ICON BUTTON ----------------------

Public Class HeaderIconButton
    Inherits AnimatedControl

    Private _hasBadge As Boolean = False

    Public Property IconName As String = ""

    Public Property HasBadge As Boolean
        Get
            Return _hasBadge
        End Get
        Set(value As Boolean)
            _hasBadge = value
            Invalidate()
        End Set
    End Property

    Public Sub New()
        BackColor = HeaderColor
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        Dim g As Graphics = e.Graphics
        g.Clear(HeaderColor)
        g.SmoothingMode = SmoothingMode.AntiAlias

        If Width < 12 OrElse Height < 12 Then Return

        Dim rect As New Rectangle(1, 1, Width - 3, Height - 3)

        Using path As GraphicsPath = RoundedRect(rect, 10)

            Using br As New SolidBrush(Lerp(Color.FromArgb(18, 28, 48), Color.FromArgb(34, 30, 68), Hover))
                g.FillPath(br, path)
            End Using

            If Hover > 0.02F Then
                Using glow As New Pen(Color.FromArgb(CInt(Hover * 50), NeonPurple), 4.0F)
                    g.DrawPath(glow, path)
                End Using
            End If

            Using p As New Pen(Color.FromArgb(CInt(45 + Hover * 150), NeonPurple), 1.2F)
                g.DrawPath(p, path)
            End Using

        End Using

        ' glowing icon: purple, shifting to cyan on hover
        DrawGlowGlyph(g, False, IconName, Lerp(NeonPurple, NeonCyan, Hover),
                      0.35F + Hover * 0.65F, (Width - 24) / 2.0F, (Height - 24) / 2.0F)

        ' notification badge
        If _hasBadge AndAlso IconName = "Notification" Then

            Using br As New SolidBrush(Color.FromArgb(60, NeonPink))
                g.FillEllipse(br, Width - 18, 2, 15, 15)
            End Using
            Using br As New SolidBrush(NeonPink)
                g.FillEllipse(br, Width - 15, 5, 9, 9)
            End Using

        End If

    End Sub

End Class


' ---------------------- FOOTER ----------------------

Public Class FooterBar
    Inherits SurfaceControl

    Public Property VersionText As String = "v1.0"

    Public Sub New()
        BackColor = FooterColor
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        Dim g As Graphics = e.Graphics
        g.Clear(FooterColor)
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit

        Using p As New Pen(LineColor)
            g.DrawLine(p, 0, 0, Width, 0)
        End Using

        Dim cy As Integer = Height \ 2

        ' glowing online dot
        Using br As New SolidBrush(Color.FromArgb(35, NeonGreen))
            g.FillEllipse(br, 14, cy - 7, 14, 14)
        End Using
        Using br As New SolidBrush(Color.FromArgb(80, NeonGreen))
            g.FillEllipse(br, 17, cy - 4, 8, 8)
        End Using
        Using br As New SolidBrush(NeonGreen)
            g.FillEllipse(br, 19, cy - 2, 4, 4)
        End Using

        Using sf As New StringFormat()
            sf.LineAlignment = StringAlignment.Center
            sf.FormatFlags = StringFormatFlags.NoWrap
            sf.Trimming = StringTrimming.EllipsisCharacter

            sf.Alignment = StringAlignment.Near

            Using br As New SolidBrush(NeonGreen)
                g.DrawString("SYSTEM ONLINE", UiFont(7.0F, FontStyle.Bold), br,
                             New RectangleF(36, 0, 110, Height), sf)
            End Using

            Using br As New SolidBrush(TextMuted)
                g.DrawString("GADGET REPAIR   |   MANAGEMENT CONSOLE", UiFont(7.0F, FontStyle.Bold), br,
                             New RectangleF(160, 0, Math.Max(10, Width - 260), Height), sf)
            End Using

            sf.Alignment = StringAlignment.Far

            Using br As New SolidBrush(NeonPurple)
                g.DrawString(VersionText, UiFont(7.0F, FontStyle.Bold), br,
                             New RectangleF(0, 0, Math.Max(10, Width - 20), Height), sf)
            End Using
        End Using

    End Sub

End Class


' ---------------------- ACTION BUTTON ----------------------

Public Class ActionButton
    Inherits AnimatedControl

    Public Property Caption As String = ""
    Public Property Accent As Color = NeonCyan

    Public Sub New()
        BackColor = CardColor
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        Dim g As Graphics = e.Graphics
        g.Clear(CardColor)
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit

        If Width < 40 OrElse Height < 16 Then Return

        Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)

        Using path As GraphicsPath = RoundedRect(rect, 9)

            Using br As New SolidBrush(Color.FromArgb(CInt(14 + Hover * 40), Accent))
                g.FillPath(br, path)
            End Using

            Using p As New Pen(Color.FromArgb(CInt(70 + Hover * 150), Accent), 1.2F)
                g.DrawPath(p, path)
            End Using

        End Using

        Dim cy As Integer = Height \ 2

        Using br As New SolidBrush(Color.FromArgb(CInt(30 + Hover * 60), Accent))
            g.FillEllipse(br, 11, cy - 6, 12, 12)
        End Using
        Using br As New SolidBrush(Accent)
            g.FillEllipse(br, 14, cy - 3, 6, 6)
        End Using

        Using sf As New StringFormat()
            sf.LineAlignment = StringAlignment.Center
            sf.FormatFlags = StringFormatFlags.NoWrap
            sf.Trimming = StringTrimming.EllipsisCharacter

            Using br As New SolidBrush(Lerp(TextMuted, TextWhite, 0.4F + Hover * 0.6F))
                g.DrawString(Caption, UiFont(9.5F, FontStyle.Bold), br,
                             New RectangleF(30, 0, Math.Max(10, Width - 60), Height), sf)
            End Using
        End Using

        Using p As New Pen(Color.FromArgb(CInt(120 + Hover * 135), Accent), 2.0F)
            p.StartCap = LineCap.Round
            p.EndCap = LineCap.Round
            g.DrawLine(p, Width - 24, cy - 5, Width - 18, cy)
            g.DrawLine(p, Width - 18, cy, Width - 24, cy + 5)
        End Using

    End Sub

End Class


' ---------------------- REPAIR OVERVIEW ----------------------

Public Class OverviewPanel
    Inherits SurfaceControl

    Public Property Pending As Integer = 0
    Public Property Completed As Integer = 0
    Public Property Total As Integer = 0

    Public Sub SetCounts(pendingCount As Integer, completedCount As Integer, totalCount As Integer)
        Pending = pendingCount
        Completed = completedCount
        Total = totalCount
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        Dim g As Graphics = e.Graphics
        g.Clear(BackgroundColor)
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit

        If Width < 200 OrElse Height < 170 Then Return

        Using path As GraphicsPath = RoundedRect(New Rectangle(0, 0, Width - 1, Height - 1), 12)

            Using br As New SolidBrush(CardColor)
                g.FillPath(br, path)
            End Using

            Using p As New Pen(LineColor, 1.2F)
                g.DrawPath(p, path)
            End Using

        End Using

        Using br As New SolidBrush(TextMuted)
            g.DrawString("REPAIR OVERVIEW", UiFont(9.0F, FontStyle.Bold), br, 20.0F, 16.0F)
        End Using

        Using p As New Pen(NeonPurple, 2.0F)
            p.StartCap = LineCap.Round
            p.EndCap = LineCap.Round
            g.DrawLine(p, 21, 40, 46, 40)
        End Using

        ' ---------- completion ring ----------
        Dim ringSize As Integer = Math.Max(70, Math.Min(Height - 84, 170))
        Dim ring As New Rectangle(38, 68, ringSize - 16, ringSize - 16)

        Dim rate As Single = 0.0F

        If Total > 0 Then
            rate = Math.Min(1.0F, CSng(Completed) / CSng(Total))
        End If

        Using track As New Pen(Color.FromArgb(40, 55, 85), 12.0F)
            g.DrawEllipse(track, ring)
        End Using

        If rate > 0.0F Then

            Dim sweep As Single = Math.Min(359.9F, 360.0F * rate)

            Using glow As New Pen(Color.FromArgb(45, NeonGreen), 18.0F)
                glow.StartCap = LineCap.Round
                glow.EndCap = LineCap.Round
                g.DrawArc(glow, ring, -90.0F, sweep)
            End Using

            Using arc As New Pen(NeonGreen, 12.0F)
                arc.StartCap = LineCap.Round
                arc.EndCap = LineCap.Round
                g.DrawArc(arc, ring, -90.0F, sweep)
            End Using

        End If

        Using sf As New StringFormat()
            sf.Alignment = StringAlignment.Center
            sf.LineAlignment = StringAlignment.Center

            Using br As New SolidBrush(TextWhite)
                g.DrawString(CInt(Math.Round(rate * 100)).ToString() & "%",
                             UiFont(16.0F, FontStyle.Bold), br,
                             New RectangleF(ring.X, ring.Y - 8, ring.Width, ring.Height), sf)
            End Using

            Using br As New SolidBrush(TextMuted)
                g.DrawString("COMPLETED", UiFont(6.5F, FontStyle.Bold), br,
                             New RectangleF(ring.X, ring.Y + 14, ring.Width, ring.Height), sf)
            End Using
        End Using

        ' ---------- status bars ----------
        Dim x0 As Integer = 38 + ringSize + 30
        Dim x1 As Integer = Width - 28

        If x1 - x0 < 90 Then Return

        Dim other As Integer = Math.Max(0, Total - Pending - Completed)
        Dim names() As String = {"Pending", "Completed", "Other"}
        Dim counts() As Integer = {Pending, Completed, other}
        Dim colors() As Color = {NeonPink, NeonGreen, NeonCyan}

        Dim rowH As Integer = Math.Max(34, Math.Min(46, (Height - 72) \ 3))
        Dim y As Integer = 62

        For i As Integer = 0 To 2

            Using br As New SolidBrush(TextWhite)
                g.DrawString(names(i), UiFont(9.0F, FontStyle.Bold), br, x0, y)
            End Using

            Using sf As New StringFormat()
                sf.Alignment = StringAlignment.Far

                Using br As New SolidBrush(colors(i))
                    g.DrawString(counts(i).ToString(), UiFont(9.0F, FontStyle.Bold), br,
                                 New RectangleF(x0, y, x1 - x0, 20), sf)
                End Using
            End Using

            Dim trackRect As New Rectangle(x0, y + 22, x1 - x0, 8)

            Using tp As GraphicsPath = RoundedRect(trackRect, 4)
                Using br As New SolidBrush(Color.FromArgb(32, 44, 70))
                    g.FillPath(br, tp)
                End Using
            End Using

            If Total > 0 AndAlso counts(i) > 0 Then

                Dim fillW As Integer = Math.Max(8, CInt(trackRect.Width * CSng(counts(i)) / CSng(Total)))
                Dim fillRect As New Rectangle(x0, trackRect.Y, Math.Min(fillW, trackRect.Width), trackRect.Height)

                Using fp As GraphicsPath = RoundedRect(fillRect, 4)
                    Using br As New SolidBrush(colors(i))
                        g.FillPath(br, fp)
                    End Using
                End Using

            End If

            y += rowH

        Next

    End Sub

End Class