Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

' shared grid cell painting (used by the Payments page; Repairs can switch to it later)
Friend Module NeonGridPaint

    Public Function PayStatusColor(status As String) As Color

        Select Case status

            Case "Paid"
                Return Theme.NeonGreen

            Case "Partially Paid"
                Return Theme.NeonYellow

            Case "Unpaid"
                Return Theme.NeonPink

            Case Else
                Return Theme.TextMuted

        End Select

    End Function
        Public Function StockStatusColor(status As String) As Color

        Select Case status

            Case "In Stock"
                Return Theme.NeonGreen

            Case "Low Stock"
                Return Color.Orange

            Case "Out of Stock"
                Return Theme.NeonPink

            Case Else
                Return Theme.TextMuted

        End Select

    End Function

    Public Function ActionButtonRect(cell As Rectangle) As Rectangle

        Const padding As Integer = 8
        Const buttonHeight As Integer = 32

        Return New Rectangle(
            cell.X + padding,
            cell.Y + (cell.Height - buttonHeight) \ 2,
            cell.Width - padding * 2,
            buttonHeight)

    End Function

    ' row background (with hover tint) + bottom line
    Public Sub PaintBase(e As DataGridViewCellPaintingEventArgs, hot As Boolean)

        e.Handled = True

        Dim rowColor As Color = Theme.CardColor

        If hot Then
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

    End Sub

    ' bold name on top, small caps line below
    Public Sub PaintTwoLine(
        e As DataGridViewCellPaintingEventArgs,
        hot As Boolean,
        secondLineColor As Color)

        PaintBase(e, hot)

        Dim raw As String = If(e.Value Is Nothing, "", e.Value.ToString())
        Dim parts() As String = raw.Split(ChrW(10))

        Dim firstLine As String = parts(0)
        Dim secondLine As String = If(parts.Length > 1, parts(1), "")

        Dim x As Integer = e.CellBounds.X + 12
        Dim w As Integer = Math.Max(10, e.CellBounds.Width - 20)

        Dim flags As TextFormatFlags =
            TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
            TextFormatFlags.NoPadding Or TextFormatFlags.EndEllipsis

        If secondLine = "" Then

            TextRenderer.DrawText(
                e.Graphics, firstLine, Theme.UiFont(9.5F, FontStyle.Bold),
                New Rectangle(x, e.CellBounds.Y, w, e.CellBounds.Height - 1),
                Theme.TextWhite, flags)

        Else

            TextRenderer.DrawText(
                e.Graphics, firstLine, Theme.UiFont(9.5F, FontStyle.Bold),
                New Rectangle(x, e.CellBounds.Y + 12, w, 22),
                Theme.TextWhite, flags)

            TextRenderer.DrawText(
                e.Graphics, secondLine.ToUpperInvariant(),
                Theme.UiFont(7.5F, FontStyle.Bold),
                New Rectangle(x, e.CellBounds.Y + 35, w, 18),
                secondLineColor, flags)

        End If

    End Sub

    ' rounded status pill, colored by colorOf(status)
    Public Sub PaintPill(
        e As DataGridViewCellPaintingEventArgs,
        hot As Boolean,
        colorOf As Func(Of String, Color))

        PaintBase(e, hot)

        Dim status As String = If(e.Value Is Nothing, "", e.Value.ToString())

        If String.IsNullOrWhiteSpace(status) Then Return

        Dim statusColor As Color = colorOf(status)

        Dim pillWidth As Integer =
            Math.Min(
                e.CellBounds.Width - 20,
                Math.Max(82, status.Length * 7 + 28))

        Dim pillHeight As Integer = 28

        Dim pillRect As New Rectangle(
            e.CellBounds.X + (e.CellBounds.Width - pillWidth) \ 2,
            e.CellBounds.Y + (e.CellBounds.Height - pillHeight) \ 2,
            pillWidth,
            pillHeight)

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

        Using path As GraphicsPath = Theme.RoundedRect(pillRect, 10)

            Using brush As New SolidBrush(Color.FromArgb(35, statusColor))
                e.Graphics.FillPath(brush, path)
            End Using

            Using pen As New Pen(Color.FromArgb(90, statusColor), 1.0F)
                e.Graphics.DrawPath(pen, path)
            End Using

        End Using

        TextRenderer.DrawText(
            e.Graphics,
            status,
            Theme.UiFont(8.0F, FontStyle.Bold),
            pillRect,
            statusColor,
            TextFormatFlags.HorizontalCenter Or
            TextFormatFlags.VerticalCenter Or
            TextFormatFlags.NoPadding)

    End Sub

    Public Sub DrawActionButton(
        g As Graphics,
        rect As Rectangle,
        caption As String,
        accent As Color,
        hot As Boolean)

        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim alpha As Integer = If(hot, 80, 24)
        Dim borderAlpha As Integer = If(hot, 255, 150)

        Dim drawRect As Rectangle = rect

        If hot Then
            drawRect = New Rectangle(
                rect.X - 1, rect.Y - 1,
                rect.Width + 2, rect.Height + 2)
        End If

        Using path As GraphicsPath = Theme.RoundedRect(drawRect, 8)

            Using brush As New SolidBrush(Color.FromArgb(alpha, accent))
                g.FillPath(brush, path)
            End Using

            Using pen As New Pen(Color.FromArgb(borderAlpha, accent), 1.2F)
                g.DrawPath(pen, path)
            End Using

        End Using

        TextRenderer.DrawText(
            g,
            caption,
            Theme.UiFont(7.5F, FontStyle.Bold),
            rect,
            accent,
            TextFormatFlags.HorizontalCenter Or
            TextFormatFlags.VerticalCenter Or
            TextFormatFlags.NoPadding)

    End Sub

End Module