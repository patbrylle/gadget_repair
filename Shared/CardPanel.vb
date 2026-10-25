Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Public Class CardPanel
    Inherits Panel

    Private _title As String = ""

    Public Property Title As String
        Get
            Return _title
        End Get
        Set(value As String)
            _title = value
            Invalidate()
        End Set
    End Property

    Public Sub New()

        DoubleBuffered = True
        BackColor = Theme.CardColor
        ForeColor = Theme.TextWhite

        SetStyle(
            ControlStyles.UserPaint Or
            ControlStyles.AllPaintingInWmPaint Or
            ControlStyles.OptimizedDoubleBuffer Or
            ControlStyles.ResizeRedraw,
            True)

        Padding = New Padding(20, 58, 20, 20)

    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)

        e.Graphics.Clear(
            If(Parent IsNot Nothing,
               Parent.BackColor,
               Theme.BackgroundColor))

    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        MyBase.OnPaint(e)

        If Width <= 2 OrElse Height <= 2 Then Return

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

        Dim rect As New Rectangle(
            0,
            0,
            Width - 1,
            Height - 1)

        Using path As GraphicsPath = Theme.RoundedRect(rect, 14)

            Using brush As New SolidBrush(Theme.CardColor)
                e.Graphics.FillPath(brush, path)
            End Using

            Using pen As New Pen(Theme.LineColor, 1.0F)
                e.Graphics.DrawPath(pen, path)
            End Using

        End Using

        If Not String.IsNullOrWhiteSpace(_title) Then

            TextRenderer.DrawText(
                e.Graphics,
                _title,
                Theme.UiFont(9.0F, FontStyle.Bold),
                New Rectangle(20, 16, Width - 40, 28),
                Theme.TextWhite,
                TextFormatFlags.Left Or
                TextFormatFlags.VerticalCenter Or
                TextFormatFlags.EndEllipsis)

        End If

    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)

        MyBase.OnResize(e)
        Invalidate()

    End Sub

End Class