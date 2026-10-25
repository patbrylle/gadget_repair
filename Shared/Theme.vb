Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
   Imports System.Globalization

Public Module Theme

    ' ==========================================================
    ' THEME COLORS
    ' ==========================================================
    Public ReadOnly InputColor As Color = Color.FromArgb(11, 17, 34)
Public ReadOnly NeonAmber As Color = Color.FromArgb(255, 200, 80)
Public ReadOnly NeonYellow As Color = Color.FromArgb(255, 230, 80)

    Public ReadOnly BackgroundColor As Color = Color.FromArgb(8, 12, 24)
    Public ReadOnly SidebarColor As Color = Color.FromArgb(12, 18, 35)
    Public ReadOnly HeaderColor As Color = Color.FromArgb(14, 20, 38)
    Public ReadOnly FooterColor As Color = Color.FromArgb(10, 15, 28)
    Public ReadOnly CardColor As Color = Color.FromArgb(18, 27, 48)
    Public ReadOnly LineColor As Color = Color.FromArgb(32, 46, 76)

    Public ReadOnly NeonCyan As Color = Color.FromArgb(0, 229, 255)
    Public ReadOnly NeonPurple As Color = Color.FromArgb(174, 0, 255)
    Public ReadOnly NeonPink As Color = Color.FromArgb(255, 0, 153)
    Public ReadOnly NeonGreen As Color = Color.FromArgb(0, 255, 170)
    Public ReadOnly NeonRed As Color = Color.FromArgb(255, 70, 90)

    Public ReadOnly TextWhite As Color = Color.FromArgb(235, 245, 255)
    Public ReadOnly TextMuted As Color = Color.FromArgb(135, 153, 180)
    Public ReadOnly TextViolet As Color = Color.FromArgb(235, 215, 255)


    ' ==========================================================
    ' FONT HELPER (falls back when Segoe UI is missing, e.g. Wine)
    ' ==========================================================

    Private ReadOnly fontCache As New Dictionary(Of String, Font)
    Private fontFamilyName As String = Nothing

    Private Function ResolveFontFamily() As String

        If fontFamilyName IsNot Nothing Then
            Return fontFamilyName
        End If

        Dim installed As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        For Each family As FontFamily In FontFamily.Families
            installed.Add(family.Name)
        Next

        Dim candidates() As String = {
            "Segoe UI", "Tahoma", "DejaVu Sans",
            "Liberation Sans", "Arial", "Microsoft Sans Serif"
        }

        For Each name As String In candidates
            If installed.Contains(name) Then
                fontFamilyName = name
                Return name
            End If
        Next

        fontFamilyName = SystemFonts.DefaultFont.FontFamily.Name
        Return fontFamilyName

    End Function

    Public Function UiFont(size As Single, Optional style As FontStyle = FontStyle.Regular) As Font

        Dim key As String = size.ToString() & "|" & CInt(style).ToString()
        Dim cached As Font = Nothing

        If fontCache.TryGetValue(key, cached) Then
            Return cached
        End If

        Dim created As Font

        Try
            created = New Font(ResolveFontFamily(), size, style, GraphicsUnit.Point)
        Catch
            created = New Font(ResolveFontFamily(), size, FontStyle.Regular, GraphicsUnit.Point)
        End Try

        fontCache(key) = created
        Return created

    End Function


    ' ==========================================================
    ' DRAWING HELPERS
    ' ==========================================================

    Public Function RoundedRect(r As Rectangle, radius As Integer) As GraphicsPath

        Dim path As New GraphicsPath()
        Dim d As Integer = Math.Min(radius * 2, Math.Min(r.Width, r.Height))

        If d <= 0 Then
            path.AddRectangle(r)
            Return path
        End If

        path.AddArc(r.X, r.Y, d, d, 180, 90)
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90)
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
        path.CloseFigure()

        Return path

    End Function

    Public Function Lerp(a As Color, b As Color, t As Single) As Color

        t = Math.Max(0.0F, Math.Min(1.0F, t))

        Return Color.FromArgb(
            CInt(CInt(a.R) + (CInt(b.R) - CInt(a.R)) * t),
            CInt(CInt(a.G) + (CInt(b.G) - CInt(a.G)) * t),
            CInt(CInt(a.B) + (CInt(b.B) - CInt(a.B)) * t))

    End Function

    Public Function HexPoints(cx As Single, cy As Single, r As Single) As PointF()

        Dim pts(5) As PointF

        For i As Integer = 0 To 5
            Dim angle As Double = (-90 + 60 * i) * Math.PI / 180.0
            pts(i) = New PointF(
                CSng(cx + r * Math.Cos(angle)),
                CSng(cy + r * Math.Sin(angle)))
        Next

        Return pts

    End Function

    ' Pure GDI+ vector logo: no image files, no resources.
    Public Sub DrawLogoMark(g As Graphics, cx As Single, cy As Single, r As Single)

        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim pts() As PointF = HexPoints(cx, cy, r)

        Using glow As New Pen(Color.FromArgb(45, NeonCyan), 8.0F)
            glow.LineJoin = LineJoin.Round
            g.DrawPolygon(glow, pts)
        End Using

        Using fill As New SolidBrush(Color.FromArgb(10, 16, 32))
            g.FillPolygon(fill, pts)
        End Using

        Using lg As New LinearGradientBrush(
            New RectangleF(cx - r, cy - r, r * 2, r * 2),
            NeonCyan, NeonPurple, 45.0F)

            Using edge As New Pen(lg, 2.6F)
                edge.LineJoin = LineJoin.Round
                g.DrawPolygon(edge, pts)
            End Using
        End Using

        Dim phone As New Rectangle(CInt(cx - 7), CInt(cy - 12), 14, 24)

        Using path As GraphicsPath = RoundedRect(phone, 3)
            Using p As New Pen(Color.FromArgb(200, NeonCyan), 1.6F)
                g.DrawPath(p, path)
            End Using
        End Using

        Using p As New Pen(Color.FromArgb(160, NeonCyan), 1.4F)
            p.StartCap = LineCap.Round
            p.EndCap = LineCap.Round
            g.DrawLine(p, cx - 2, cy - 9, cx + 2, cy - 9)
        End Using

        Dim bolt() As PointF = {
            New PointF(cx + 2.5F, cy - 8.0F),
            New PointF(cx - 4.0F, cy + 1.0F),
            New PointF(cx - 0.5F, cy + 1.0F),
            New PointF(cx - 2.5F, cy + 8.5F),
            New PointF(cx + 4.0F, cy - 2.0F),
            New PointF(cx + 0.5F, cy - 2.0F)
        }

        Using br As New SolidBrush(NeonCyan)
            g.FillPolygon(br, bolt)
        End Using

        Using br As New SolidBrush(NeonPink)
            g.FillEllipse(br, pts(0).X - 2.5F, pts(0).Y - 2.5F, 5.0F, 5.0F)
        End Using

    End Sub

    Public Sub DrawNavIcon(g As Graphics, iconName As String, pen As Pen)

        Select Case iconName

            Case "Dashboard"
                g.DrawRectangle(pen, 3, 3, 7, 7)
                g.DrawRectangle(pen, 14, 3, 7, 7)
                g.DrawRectangle(pen, 3, 14, 7, 7)
                g.DrawRectangle(pen, 14, 14, 7, 7)

            Case "Repairs"
                g.DrawArc(pen, 3, 3, 12, 12, 75, 270)
                g.DrawLine(pen, 12, 12, 21, 21)

            Case "Customers"
                g.DrawEllipse(pen, 8, 3, 8, 8)
                g.DrawArc(pen, 4, 13, 16, 14, 180, 180)

            Case "Technicians"
                g.DrawArc(pen, 6, 1, 12, 9, 180, 180)
                g.DrawLine(pen, 5, 5, 19, 5)
                g.DrawEllipse(pen, 8, 7, 8, 7)
                g.DrawArc(pen, 4, 16, 16, 12, 180, 180)

            Case "Inventory"
                g.DrawRectangle(pen, 3, 9, 18, 12)
                g.DrawLine(pen, 3, 9, 7, 3)
                g.DrawLine(pen, 7, 3, 17, 3)
                g.DrawLine(pen, 17, 3, 21, 9)
                g.DrawLine(pen, 10, 13, 14, 13)

            Case "Payments"
                g.DrawRectangle(pen, 2, 5, 20, 14)
                g.DrawLine(pen, 2, 10, 22, 10)
                g.DrawLine(pen, 5, 15, 10, 15)

            Case "Reports"
                g.DrawLine(pen, 3, 3, 3, 21)
                g.DrawLine(pen, 3, 21, 21, 21)
                g.DrawLine(pen, 8, 17, 8, 13)
                g.DrawLine(pen, 12, 17, 12, 9)
                g.DrawLine(pen, 16, 17, 16, 11)
                g.DrawLine(pen, 20, 17, 20, 6)
                            Case "Logout"
                g.DrawLine(pen, 11, 3, 4, 3)
                g.DrawLine(pen, 4, 3, 4, 21)
                g.DrawLine(pen, 4, 21, 11, 21)
                g.DrawLine(pen, 9, 12, 21, 12)
                g.DrawLine(pen, 17, 8, 21, 12)
                g.DrawLine(pen, 17, 16, 21, 12)

        End Select

    End Sub

    Public Sub DrawHeaderGlyph(g As Graphics, iconName As String, pen As Pen)

        Select Case iconName

            Case "Notification"
                g.DrawArc(pen, 6, 3, 12, 12, 180, 180)
                g.DrawLine(pen, 6, 9, 6, 16)
                g.DrawLine(pen, 18, 9, 18, 16)
                g.DrawLine(pen, 4, 17, 20, 17)
                g.DrawArc(pen, 10, 18, 4, 4, 0, 180)

            Case "Settings"
                g.DrawEllipse(pen, 6, 6, 12, 12)
                g.DrawEllipse(pen, 10, 10, 4, 4)

                For i As Integer = 0 To 7
                    Dim a As Double = i * Math.PI / 4.0
                    g.DrawLine(pen,
                        CSng(12 + Math.Cos(a) * 7.5),
                        CSng(12 + Math.Sin(a) * 7.5),
                        CSng(12 + Math.Cos(a) * 10.5),
                        CSng(12 + Math.Sin(a) * 10.5))
                Next

            Case "Profile"
                g.DrawEllipse(pen, 8, 3, 8, 8)
                g.DrawArc(pen, 4, 13, 16, 14, 180, 180)

        End Select

    End Sub

    ' Draws an icon with a soft neon glow. strength 0..1 controls the glow.
    Public Sub DrawGlowGlyph(
        g As Graphics,
        isNav As Boolean,
        iconName As String,
        baseColor As Color,
        strength As Single,
        offsetX As Single,
        offsetY As Single)

        strength = Math.Max(0.0F, Math.Min(1.0F, strength))

        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TranslateTransform(offsetX, offsetY)

        If strength > 0.05F Then

            Dim widths() As Single = {9.0F, 5.5F}
            Dim alphas() As Single = {30.0F, 60.0F}

            For i As Integer = 0 To 1

                Using gp As New Pen(Color.FromArgb(CInt(Math.Min(255.0F, alphas(i) * strength)), baseColor), widths(i))
                    gp.StartCap = LineCap.Round
                    gp.EndCap = LineCap.Round
                    gp.LineJoin = LineJoin.Round

                    If isNav Then
                        DrawNavIcon(g, iconName, gp)
                    Else
                        DrawHeaderGlyph(g, iconName, gp)
                    End If
                End Using

            Next

        End If

        Using ip As New Pen(Color.FromArgb(CInt(150 + 105 * strength), baseColor), 2.0F)
            ip.StartCap = LineCap.Round
            ip.EndCap = LineCap.Round
            ip.LineJoin = LineJoin.Round

            If isNav Then
                DrawNavIcon(g, iconName, ip)
            Else
                DrawHeaderGlyph(g, iconName, ip)
            End If
        End Using

        g.ResetTransform()

    End Sub
  Public Sub DrawPeso(
    g As Graphics,
    x As Single,
    y As Single,
    size As Single,
    color As Color
)

    Using pen As New Pen(color, Math.Max(1.0F, size / 7.0F))

        pen.StartCap = LineCap.Round
        pen.EndCap = LineCap.Round

        ' Main P shape
        Dim left As Single = x
        Dim top As Single = y
        Dim height As Single = size
        Dim width As Single = size * 0.58F

        g.DrawLine(pen, left, top, left, top + height)

        g.DrawArc(
            pen,
            left,
            top,
            width,
            height * 0.55F,
            270,
            180
        )

        ' Peso double horizontal lines
        Dim lineY1 As Single = top + height * 0.48F
        Dim lineY2 As Single = top + height * 0.62F

        g.DrawLine(
            pen,
            left - size * 0.08F,
            lineY1,
            left + width * 0.85F,
            lineY1
        )

        g.DrawLine(
            pen,
            left - size * 0.08F,
            lineY2,
            left + width * 0.85F,
            lineY2
        )

    End Using

End Sub

'   ' ==========================================================
   ' CURRENCY HELPER
   ' ==========================================================

   Private Declare Function SelectObject Lib "gdi32.dll" (
       hdc As IntPtr, hObject As IntPtr) As IntPtr

   Private Declare Function DeleteObject Lib "gdi32.dll" (
       hObject As IntPtr) As Boolean

   Private Declare Unicode Function GetGlyphIndicesW Lib "gdi32.dll" (
       hdc As IntPtr, text As String, count As Integer,
       indices As UShort(), flags As UInteger) As UInteger

   Private pesoChecked As Boolean = False
   Private pesoSupported As Boolean = False

   Private Function FontHasPesoGlyph() As Boolean

       Try
           Using bmp As New Bitmap(1, 1)
               Using g As Graphics = Graphics.FromImage(bmp)

                   Dim hdc As IntPtr = g.GetHdc()
                   Dim hFont As IntPtr = UiFont(9.0F).ToHfont()
                   Dim oldFont As IntPtr = SelectObject(hdc, hFont)

                   Try
                       Dim idx(0) As UShort

                       Dim r As UInteger = GetGlyphIndicesW(
                           hdc, ChrW(&H20B1).ToString(), 1, idx, 1UI)

                       Return r <> UInteger.MaxValue AndAlso
                              CInt(idx(0)) <> 65535

                   Finally
                       SelectObject(hdc, oldFont)
                       DeleteObject(hFont)
                       g.ReleaseHdc(hdc)
                   End Try

               End Using
           End Using

       Catch
           Return False
       End Try

   End Function

   Public Function FormatPeso(amount As Decimal) As String

       If Not pesoChecked Then
           pesoChecked = True
           pesoSupported = FontHasPesoGlyph()
       End If

       Dim text As String =
           amount.ToString("#,##0.00", CultureInfo.InvariantCulture)

       If pesoSupported Then
           Return ChrW(&H20B1).ToString() & " " & text
       End If

       Return "PHP " & text

   End Function

    ' ==========================================================
    ' REPAIR STATUS COLORS (one place for every form)
    ' ==========================================================

    Public Function StatusColor(status As String) As Color

        Select Case If(status, "").Trim().ToLowerInvariant()

            Case "pending"
                Return Color.Gold

            Case "in progress"
                Return NeonCyan

            Case "waiting for parts"
                Return Color.Orange

            Case "completed"
                Return NeonGreen

            Case "delivered"
                Return NeonCyan

            Case "cancelled", "canceled"
                Return NeonRed

            Case Else
                Return TextMuted

        End Select

    End Function
    
End Module