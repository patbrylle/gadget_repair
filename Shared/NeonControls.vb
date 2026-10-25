Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.Runtime.InteropServices

' ================================================================
' CARD (rounded surface with optional title + gradient rule)
' ================================================================
Friend Class NeonCard
    Inherits Panel

    Private mTitle As String = ""

    <Browsable(True), DefaultValue("")>
    Public Property Title As String
        Get
            Return mTitle
        End Get
        Set(value As String)
            mTitle = value
            Invalidate()
        End Set
    End Property

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        BackColor = Theme.CardColor
    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        Dim back As Color = If(Parent IsNot Nothing, Parent.BackColor, Theme.BackgroundColor)
        e.Graphics.Clear(back)
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

        Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)
        Using path As GraphicsPath = Theme.RoundedRect(rect, 14)
            Using b As New SolidBrush(Theme.CardColor)
                e.Graphics.FillPath(b, path)
            End Using
            Using p As New Pen(Theme.LineColor, 1.0F)
                e.Graphics.DrawPath(p, path)
            End Using
        End Using
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        If String.IsNullOrWhiteSpace(mTitle) Then Return

        TextRenderer.DrawText(e.Graphics, mTitle, Theme.UiFont(9.5F, FontStyle.Bold),
                              New Point(20, 17), Theme.TextWhite)

        Dim lineRect As New Rectangle(20, 46, Math.Max(10, Width - 40), 2)
        Using lg As New LinearGradientBrush(lineRect, Theme.NeonCyan, Theme.NeonPurple,
                                            LinearGradientMode.Horizontal)
            e.Graphics.FillRectangle(lg, lineRect)
        End Using
    End Sub

End Class


' ================================================================
' INPUT HOST (rounded field shell around a TextBox / ComboBox / etc.)
' ================================================================
Friend Class NeonInput
    Inherits Panel

    Private isFocused As Boolean = False
    Private mSearchIcon As Boolean = False

    <Browsable(True), DefaultValue(False)>
    Public Property SearchIcon As Boolean
        Get
            Return mSearchIcon
        End Get
        Set(value As Boolean)
            mSearchIcon = value
            Invalidate()
        End Set
    End Property

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        BackColor = Theme.InputColor
    End Sub

    Protected Overrides Sub OnControlAdded(e As ControlEventArgs)
        MyBase.OnControlAdded(e)
        AddHandler e.Control.Enter, Sub(s, a) SetFocused(True)
        AddHandler e.Control.Leave, Sub(s, a) SetFocused(False)
        CenterChild()
    End Sub

    Private Sub SetFocused(value As Boolean)
        isFocused = value
        Invalidate()
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        CenterChild()
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        If Controls.Count > 0 Then Controls(0).Focus()
    End Sub

    Private Sub CenterChild()
        If Controls.Count = 0 Then Return
        Dim c As Control = Controls(0)
        If c.Dock = DockStyle.Fill Then Return
        c.Left = Padding.Left
        c.Width = Math.Max(10, Width - Padding.Horizontal)
        c.Top = Math.Max(0, (Height - c.Height) \ 2)
    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        Dim back As Color = If(Parent IsNot Nothing, Parent.BackColor, Theme.BackgroundColor)
        e.Graphics.Clear(back)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

        Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)

        Using path As GraphicsPath = Theme.RoundedRect(rect, 10)
            Using b As New SolidBrush(Theme.InputColor)
                e.Graphics.FillPath(b, path)
            End Using

            If isFocused Then
                Using glow As New Pen(Color.FromArgb(50, Theme.NeonCyan), 3.0F)
                    e.Graphics.DrawPath(glow, path)
                End Using
            End If

            Using p As New Pen(If(isFocused, Theme.NeonCyan, Theme.LineColor), 1.2F)
                e.Graphics.DrawPath(p, path)
            End Using
        End Using

        If mSearchIcon Then
            Dim cy As Integer = Height \ 2
            Using p As New Pen(Theme.TextMuted, 1.6F)
                p.EndCap = LineCap.Round
                e.Graphics.DrawEllipse(p, 14, cy - 7, 10, 10)
                e.Graphics.DrawLine(p, 22, cy + 2, 27, cy + 7)
            End Using
        End If
    End Sub

End Class


' ================================================================
' BUTTON (filled = neon gradient, otherwise outlined accent)
' ================================================================
Friend Class NeonButton
    Inherits Control

    Private hovered As Boolean = False
    Private pressed As Boolean = False
    Private mAccent As Color = Theme.NeonPurple
    Private mFilled As Boolean = False

    Public Property Accent As Color
        Get
            Return mAccent
        End Get
        Set(value As Color)
            mAccent = value
            Invalidate()
        End Set
    End Property

    <DefaultValue(False)>
    Public Property Filled As Boolean
        Get
            Return mFilled
        End Get
        Set(value As Boolean)
            mFilled = value
            Invalidate()
        End Set
    End Property

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Cursor = Cursors.Hand
        Font = Theme.UiFont(9.0F, FontStyle.Bold)
        ForeColor = Theme.TextWhite
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        hovered = True : Invalidate() : MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        hovered = False : pressed = False : Invalidate() : MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            pressed = True
            Invalidate()
        End If
        MyBase.OnMouseDown(e)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        pressed = False : Invalidate() : MyBase.OnMouseUp(e)
    End Sub

    Protected Overrides Sub OnEnabledChanged(e As EventArgs)
        MyBase.OnEnabledChanged(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        Dim back As Color = If(Parent IsNot Nothing, Parent.BackColor, Theme.BackgroundColor)
        e.Graphics.Clear(back)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

        Dim rect As New Rectangle(1, 1, Width - 3, Height - 3)

        Using path As GraphicsPath = Theme.RoundedRect(rect, 10)

            If mFilled Then
                Dim c1 As Color = Theme.NeonPurple
                Dim c2 As Color = Theme.Lerp(Theme.NeonCyan, Theme.BackgroundColor, 0.2F)

                If pressed Then
                    c1 = Theme.Lerp(c1, Theme.BackgroundColor, 0.35F)
                    c2 = Theme.Lerp(c2, Theme.BackgroundColor, 0.35F)
                ElseIf hovered Then
                    c1 = Theme.Lerp(c1, Theme.TextWhite, 0.12F)
                    c2 = Theme.Lerp(c2, Theme.TextWhite, 0.12F)
                    Using glow As New Pen(Color.FromArgb(60, Theme.NeonCyan), 4.0F)
                        e.Graphics.DrawPath(glow, path)
                    End Using
                End If

                Using lg As New LinearGradientBrush(rect, c1, c2, LinearGradientMode.Horizontal)
                    e.Graphics.FillPath(lg, path)
                End Using
                      Else
                Dim edge As Color = If(hovered AndAlso Not mKeepAccent, Theme.NeonCyan, mAccent)

                Dim fillAlpha As Integer = If(pressed, 40, 18)

                If mKeepAccent Then
                    fillAlpha = If(pressed, 64, If(hovered, 44, 22))
                End If

                Using b As New SolidBrush(Color.FromArgb(fillAlpha, edge))
                    e.Graphics.FillPath(b, path)
                End Using
                Using p As New Pen(edge, 1.3F)
                    e.Graphics.DrawPath(p, path)
                End Using
            End If
        End Using

        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle,
                              If(Enabled, ForeColor, Theme.TextMuted),
                              TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or
                                  TextFormatFlags.SingleLine Or TextFormatFlags.NoPrefix)
    End Sub

    ' keeps its own color on hover (status and danger actions)
    Private mKeepAccent As Boolean = False

    <DefaultValue(False)>
    Public Property KeepAccentOnHover As Boolean
        Get
            Return mKeepAccent
        End Get
        Set(value As Boolean)
            mKeepAccent = value
            Invalidate()
        End Set
    End Property

End Class

' ================================================================
' STAT CARD (accent bar + big number + caption)
' ================================================================
Friend Class StatCard
    Inherits Control

    Private mCaption As String = ""
    Private mValue As String = "0"
    Private mDescription As String = ""
    Private mAccent As Color = Theme.NeonCyan

    Public Property Caption As String
        Get
            Return mCaption
        End Get
        Set(value As String)
            mCaption = If(value, "")
            Invalidate()
        End Set
    End Property

    Public Property Value As String
        Get
            Return mValue
        End Get
        Set(value As String)
            mValue = If(value, "0")
            Invalidate()
        End Set
    End Property

    Public Property Accent As Color
        Get
            Return mAccent
        End Get
        Set(value As Color)
            mAccent = value
            Invalidate()
        End Set
    End Property

    ' Dashboard compatibility
    Public Property Title As String
        Get
            Return mCaption
        End Get
        Set(value As String)
            mCaption = If(value, "")
            Invalidate()
        End Set
    End Property

    Public Property ValueText As String
        Get
            Return mValue
        End Get
        Set(value As String)
            mValue = If(value, "0")
            Invalidate()
        End Set
    End Property

    Public Property Description As String
        Get
            Return mDescription
        End Get
        Set(value As String)
            mDescription = If(value, "")
            Invalidate()
        End Set
    End Property

    Public Sub New()
        SetStyle(
            ControlStyles.UserPaint Or
            ControlStyles.AllPaintingInWmPaint Or
            ControlStyles.OptimizedDoubleBuffer Or
            ControlStyles.ResizeRedraw,
            True
        )

        BackColor = Theme.CardColor
        ForeColor = Theme.TextWhite
    End Sub

    Public Sub New(caption As String, value As String)
        Me.New()
        Me.Caption = caption
        Me.Value = value
    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        e.Graphics.Clear(BackColor)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        If Width <= 2 OrElse Height <= 2 Then Return

        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)

        Using path As Drawing2D.GraphicsPath = Theme.RoundedRect(rect, 14)
            Using brush As New SolidBrush(Theme.CardColor)
                e.Graphics.FillPath(brush, path)
            End Using

            Using pen As New Pen(Theme.LineColor, 1.0F)
                e.Graphics.DrawPath(pen, path)
            End Using
        End Using

        TextRenderer.DrawText(
            e.Graphics,
            mCaption,
            Theme.UiFont(9.0F, FontStyle.Bold),
            New Rectangle(18, 14, Width - 36, 24),
            Theme.TextMuted,
            TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis
        )

        TextRenderer.DrawText(
            e.Graphics,
            mValue,
            Theme.UiFont(22.0F, FontStyle.Bold),
            New Rectangle(18, 40, Width - 36, 42),
            mAccent,
            TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis
        )

        If Not String.IsNullOrWhiteSpace(mDescription) Then
            TextRenderer.DrawText(
                e.Graphics,
                mDescription,
                Theme.UiFont(8.0F),
                New Rectangle(18, 84, Width - 36, Height - 94),
                Theme.TextMuted,
                TextFormatFlags.Left Or TextFormatFlags.Top Or TextFormatFlags.EndEllipsis
            )
        End If
    End Sub
End Class
    ' ================================================================
' COMBOBOX (no system arrow button, chevron drawn inside the field)
' ================================================================
Friend Class NeonComboBox
    Inherits ComboBox

    Private Const WM_PAINT As Integer = &HF
    Private Const WM_ERASEBKGND As Integer = &H14

    <DllImport("user32.dll")>
    Private Shared Function ValidateRect(hWnd As IntPtr, lpRect As IntPtr) As Boolean
    End Function

    Private hovered As Boolean = False

    ' draws a 1px border (use when the combo is NOT inside a NeonInput)
    Public Bordered As Boolean = False

        Private mLocked As Boolean = False

    <DefaultValue(False)>
    Public Property Locked As Boolean
        Get
            Return mLocked
        End Get
        Set(value As Boolean)
            mLocked = value
            Cursor = If(value, Cursors.Default, Cursors.Hand)
            Invalidate()
        End Set
    End Property

    Public Sub New()
        DropDownStyle = ComboBoxStyle.DropDownList
        DrawMode = DrawMode.OwnerDrawFixed
        ItemHeight = 26
        BackColor = Theme.InputColor
        ForeColor = Theme.TextWhite
        Cursor = Cursors.Hand
    End Sub

           Protected Overrides Sub WndProc(ByRef m As Message)
        Select Case m.Msg
            Case WM_ERASEBKGND
                m.Result = New IntPtr(1)
                Return

            Case &H20A ' WM_MOUSEWHEEL: only scroll when the list is open
                If mLocked OrElse Not DroppedDown Then Return

            Case &H201, &H203, &H100, &H104 ' click, double-click, keys
                If mLocked Then Return

            Case WM_PAINT
                PaintFace()
                ValidateRect(Handle, IntPtr.Zero)
                m.Result = IntPtr.Zero
                Return
        End Select

        MyBase.WndProc(m)
    End Sub

    Private Sub PaintFace()
        If Width <= 0 OrElse Height <= 0 OrElse Not IsHandleCreated Then Return

        Using bmp As New Bitmap(Width, Height)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.Clear(Theme.InputColor)
                g.SmoothingMode = SmoothingMode.AntiAlias

                Dim padL As Integer = If(Bordered, 8, 0)
                Dim shown As String = ""

                If SelectedIndex >= 0 Then shown = GetItemText(SelectedItem)

                               TextRenderer.DrawText(g, shown, Font,
                                      New Rectangle(padL, 0, Width - padL - 26, Height),
                                      If(mLocked, Theme.TextMuted, Theme.TextWhite),
                                      TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
                                      TextFormatFlags.NoPadding Or TextFormatFlags.EndEllipsis)

                ' chevron
                Dim cx As Single = Width - 14.0F
                Dim cy As Single = Height / 2.0F
                Dim chevron As Color = If(hovered OrElse DroppedDown, Theme.NeonCyan, Theme.TextMuted)

                Using pen As New Pen(chevron, 1.6F)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round
                    pen.LineJoin = LineJoin.Round

                    g.DrawLines(pen, New PointF() {
                        New PointF(cx - 4.0F, cy - 2.0F),
                        New PointF(cx, cy + 2.0F),
                        New PointF(cx + 4.0F, cy - 2.0F)})
                End Using

                If Bordered Then
                    Using p As New Pen(Theme.LineColor, 1.0F)
                        g.SmoothingMode = SmoothingMode.None
                        g.DrawRectangle(p, 0, 0, Width - 1, Height - 1)
                    End Using
                End If
            End Using

            Using wg As Graphics = Graphics.FromHwnd(Handle)
                wg.DrawImageUnscaled(bmp, 0, 0)
            End Using
        End Using
    End Sub

    ' ---- dropdown list items ----
    Protected Overrides Sub OnDrawItem(e As DrawItemEventArgs)
        Dim g As Graphics = e.Graphics

        Using b1 As New SolidBrush(Theme.InputColor)
            g.FillRectangle(b1, e.Bounds)
        End Using

        If e.Index < 0 Then Return

        Dim isHot As Boolean = (e.State And DrawItemState.Selected) = DrawItemState.Selected

        If isHot Then
            Using b2 As New SolidBrush(Color.FromArgb(50, Theme.NeonCyan))
                g.FillRectangle(b2, e.Bounds)
            End Using
            Using b3 As New SolidBrush(Theme.NeonCyan)
                g.FillRectangle(b3, e.Bounds.X, e.Bounds.Y + 4, 3, e.Bounds.Height - 8)
            End Using
        End If

        TextRenderer.DrawText(g, GetItemText(Items(e.Index)), Font,
                              New Rectangle(e.Bounds.X + 12, e.Bounds.Y,
                                            e.Bounds.Width - 16, e.Bounds.Height),
                              Theme.TextWhite,
                              TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
                              TextFormatFlags.NoPadding Or TextFormatFlags.EndEllipsis)
    End Sub

    ' ---- repaint triggers ----
    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        hovered = True : Invalidate() : MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        hovered = False : Invalidate() : MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnDropDown(e As EventArgs)
        MyBase.OnDropDown(e) : Invalidate()
    End Sub

    Protected Overrides Sub OnDropDownClosed(e As EventArgs)
        MyBase.OnDropDownClosed(e) : Invalidate()
    End Sub

    Protected Overrides Sub OnSelectedIndexChanged(e As EventArgs)
        MyBase.OnSelectedIndexChanged(e) : Invalidate()
    End Sub

End Class


' ================================================================
' DATE / TIME FIELD (dark field + calendar and time popup)
' ================================================================
Friend Class NeonDateTimeField
    Inherits Control

    Private mValue As DateTime = DateTime.Now
    Private hovered As Boolean = False
    Private isOpen As Boolean = False
    Private closedAt As DateTime = DateTime.MinValue
    Private dropDown As Form

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Font = Theme.UiFont(9.5F)
        Cursor = Cursors.Hand
        TabStop = True
    End Sub

    Public Property Value As DateTime
        Get
            Return mValue
        End Get
        Set(v As DateTime)
            mValue = v
            Invalidate()
        End Set
    End Property
        Private mLocked As Boolean = False

    Public Property Locked As Boolean
        Get
            Return mLocked
        End Get
        Set(v As Boolean)
            mLocked = v
            Cursor = If(v, Cursors.Default, Cursors.Hand)
            Invalidate()
        End Set
    End Property

    ' ---- paint (same look as NeonInput) ----
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim back As Color = If(Parent IsNot Nothing, Parent.BackColor, Theme.BackgroundColor)

        g.Clear(back)
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim active As Boolean = isOpen OrElse Focused
        Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)

        Using path As GraphicsPath = Theme.RoundedRect(rect, 10)
            Using b As New SolidBrush(Theme.InputColor)
                g.FillPath(b, path)
            End Using

            If active Then
                Using glow As New Pen(Color.FromArgb(50, Theme.NeonCyan), 3.0F)
                    g.DrawPath(glow, path)
                End Using
            End If

            Dim edge As Color = Theme.LineColor
            If active Then
                edge = Theme.NeonCyan
            ElseIf hovered AndAlso Not mLocked Then
                                edge = Theme.TextMuted
            End If

            Using p As New Pen(edge, 1.2F)
                g.DrawPath(p, path)
            End Using
        End Using

        TextRenderer.DrawText(g, mValue.ToString("MMM dd, yyyy  hh:mm tt"), Font,
                              New Rectangle(12, 0, Width - 44, Height),
                              If(mLocked, Theme.TextMuted, Theme.TextWhite),
                                                            TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
                              TextFormatFlags.NoPadding Or TextFormatFlags.EndEllipsis)

        ' calendar glyph
        Dim gx As Integer = Width - 28
        Dim gy As Integer = (Height - 14) \ 2

        Using pen As New Pen(Theme.TextMuted, 1.4F)
            g.DrawRectangle(pen, gx, gy + 2, 14, 12)
            g.DrawLine(pen, gx, gy + 6, gx + 14, gy + 6)
            g.DrawLine(pen, gx + 4, gy, gx + 4, gy + 4)
            g.DrawLine(pen, gx + 10, gy, gx + 10, gy + 4)
        End Using
    End Sub

    ' ---- mouse / keyboard ----
    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        hovered = True : Invalidate() : MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        hovered = False : Invalidate() : MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnGotFocus(e As EventArgs)
        Invalidate() : MyBase.OnGotFocus(e)
    End Sub

    Protected Overrides Sub OnLostFocus(e As EventArgs)
        Invalidate() : MyBase.OnLostFocus(e)
    End Sub

    Protected Overrides Sub OnClick(e As EventArgs)
        MyBase.OnClick(e)
                If mLocked Then Return
        ShowPopup()
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
                If mLocked Then Return

        If e.KeyCode = Keys.Space OrElse e.KeyCode = Keys.F4 OrElse
           (e.Alt AndAlso e.KeyCode = Keys.Down) Then

            e.Handled = True
            ShowPopup()
        End If
    End Sub

    ' ---- popup ----
    Private Sub ShowPopup()
        ' clicking the field again closes the popup instead of reopening it
        If (DateTime.Now - closedAt).TotalMilliseconds < 250 Then Return

        If dropDown IsNot Nothing Then
            dropDown.Dispose()
            dropDown = Nothing
        End If

        Dim cal As New MonthCalendar()
        cal.MaxSelectionCount = 1
        cal.SetDate(mValue.Date)
        cal.Location = New Point(8, 8)

        Dim timeY As Integer = cal.Bottom + 10

        Dim lblTime As New Label()
        lblTime.Text = "TIME"
        lblTime.AutoSize = True
        lblTime.Font = Theme.UiFont(7.0F, FontStyle.Bold)
        lblTime.ForeColor = Theme.TextMuted
        lblTime.Location = New Point(8, timeY + 6)

        Dim hour12 As Integer = mValue.Hour Mod 12
        If hour12 = 0 Then hour12 = 12

        Dim nudHour As NumericUpDown = CreateSpinner(1, 12)
        nudHour.Location = New Point(48, timeY)
        nudHour.Value = hour12

        Dim lblColon As New Label()
        lblColon.Text = ":"
        lblColon.AutoSize = True
        lblColon.Font = Theme.UiFont(10.0F, FontStyle.Bold)
        lblColon.ForeColor = Theme.TextWhite
        lblColon.Location = New Point(102, timeY + 2)

        Dim nudMinute As NumericUpDown = CreateSpinner(0, 59)
        nudMinute.Location = New Point(114, timeY)
        nudMinute.Value = mValue.Minute

        Dim cboAmPm As New NeonComboBox()
        cboAmPm.Bordered = True
        cboAmPm.Font = Theme.UiFont(9.5F)
        cboAmPm.ItemHeight = 18
        cboAmPm.Items.Add("AM")
        cboAmPm.Items.Add("PM")
        cboAmPm.SelectedIndex = If(mValue.Hour >= 12, 1, 0)
        cboAmPm.Width = 58
        cboAmPm.Location = New Point(172, timeY)

        ' apply changes live
        Dim apply As Action =
            Sub()
                Dim hour24 As Integer = CInt(nudHour.Value) Mod 12
                If cboAmPm.SelectedIndex = 1 Then hour24 += 12

                Dim d As DateTime = cal.SelectionStart.Date

                mValue = New DateTime(d.Year, d.Month, d.Day,
                                      hour24, CInt(nudMinute.Value), 0)
                Invalidate()
            End Sub

        AddHandler cal.DateChanged, Sub(s As Object, a As DateRangeEventArgs) apply()
        AddHandler nudHour.ValueChanged, Sub(s As Object, a As EventArgs) apply()
        AddHandler nudMinute.ValueChanged, Sub(s As Object, a As EventArgs) apply()
        AddHandler cboAmPm.SelectedIndexChanged, Sub(s As Object, a As EventArgs) apply()

        Dim popup As New Panel()
        popup.BackColor = Theme.BackgroundColor
        popup.Size = New Size(cal.Width + 16, timeY + 28 + 10)

        popup.Controls.Add(cal)
        popup.Controls.Add(lblTime)
        popup.Controls.Add(nudHour)
        popup.Controls.Add(lblColon)
        popup.Controls.Add(nudMinute)
        popup.Controls.Add(cboAmPm)
        popup.Dock = DockStyle.Fill

        dropDown = New Form()
        dropDown.FormBorderStyle = FormBorderStyle.None
        dropDown.ShowInTaskbar = False
        dropDown.StartPosition = FormStartPosition.Manual
        dropDown.BackColor = Theme.LineColor
        dropDown.Padding = New Padding(1)
        dropDown.ClientSize = New Size(popup.Width + 2, popup.Height + 2)
        dropDown.Controls.Add(popup)

        AddHandler dropDown.Deactivate,
            Sub(s As Object, a As EventArgs)
                If dropDown IsNot Nothing AndAlso Not dropDown.IsDisposed Then
                    dropDown.Close()
                End If
            End Sub

        AddHandler dropDown.FormClosed,
            Sub(s As Object, a As FormClosedEventArgs)
                isOpen = False
                closedAt = DateTime.Now
                Invalidate()
            End Sub

        dropDown.Location = PointToScreen(New Point(0, Height + 4))

        isOpen = True
        Invalidate()

        dropDown.Show(FindForm())
    End Sub

    Private Shared Function CreateSpinner(min As Integer, max As Integer) As NumericUpDown
        Dim nud As New NumericUpDown()

        nud.Minimum = min
        nud.Maximum = max
        nud.Width = 52
        nud.BackColor = Theme.InputColor
        nud.ForeColor = Theme.TextWhite
        nud.BorderStyle = BorderStyle.FixedSingle
        nud.Font = Theme.UiFont(9.5F)
        nud.TextAlign = HorizontalAlignment.Center

        Return nud
    End Function

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso dropDown IsNot Nothing Then dropDown.Dispose()
        MyBase.Dispose(disposing)
    End Sub

End Class


' ================================================================
' CLOSE (X) BUTTON
' ================================================================
Friend Class NeonCloseButton
    Inherits Control

    Private hovered As Boolean = False

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Size = New Size(32, 32)
        Cursor = Cursors.Hand
        TabStop = False
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        hovered = True : Invalidate() : MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        hovered = False : Invalidate() : MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics

        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Theme.BackgroundColor))
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim hoverColor As Color = Color.FromArgb(255, 92, 108)
        Dim lineColor As Color = If(hovered, hoverColor, Theme.TextMuted)

        If hovered Then
            Using b As New SolidBrush(Color.FromArgb(36, hoverColor))
                g.FillEllipse(b, 0, 0, Width - 1, Height - 1)
            End Using
        End If

        Dim cx As Single = Width / 2.0F
        Dim cy As Single = Height / 2.0F
        Dim s As Single = 5.0F

        Using pen As New Pen(lineColor, 1.6F)
            pen.StartCap = LineCap.Round
            pen.EndCap = LineCap.Round

            g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s)
            g.DrawLine(pen, cx - s, cy + s, cx + s, cy - s)
        End Using
    End Sub

End Class


' ================================================================
' STATUS PILL (dot + label, tinted with the accent color)
' ================================================================
Friend Class NeonStatusPill
    Inherits Control

    Private accent As Color = Color.Gray

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Font = Theme.UiFont(8.0F, FontStyle.Bold)
        Height = 24
        TabStop = False
    End Sub

    Public Sub SetStatus(value As String, color As Color)
        Text = value
        accent = color

        Dim textWidth As Integer =
            TextRenderer.MeasureText(value, Font,
                                     New Size(Integer.MaxValue, Integer.MaxValue),
                                     TextFormatFlags.NoPadding).Width

        Width = textWidth + 34
        Invalidate()
    End Sub

    Private Shared Function PillPath(r As Rectangle) As GraphicsPath
        Dim d As Integer = r.Height
        Dim path As New GraphicsPath()

        path.AddArc(r.X, r.Y, d, d, 90, 180)
        path.AddArc(r.Right - d, r.Y, d, d, 270, 180)
        path.CloseFigure()

        Return path
    End Function

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics

        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Theme.BackgroundColor))
        g.SmoothingMode = SmoothingMode.AntiAlias

        Using path As GraphicsPath = PillPath(New Rectangle(0, 0, Width - 1, Height - 1))
            Using b As New SolidBrush(Color.FromArgb(38, accent))
                g.FillPath(b, path)
            End Using
            Using p As New Pen(Color.FromArgb(110, accent), 1.0F)
                g.DrawPath(p, path)
            End Using
        End Using

        Using b As New SolidBrush(accent)
            g.FillEllipse(b, 11, (Height - 6) \ 2, 6, 6)
        End Using

        TextRenderer.DrawText(g, Text, Font,
                              New Rectangle(22, 0, Width - 22, Height),
                              accent,
                              TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
                              TextFormatFlags.NoPadding)
    End Sub

End Class


' ================================================================
' LAYOUT HELPERS (caption + NeonInput field + divider)
' ================================================================
Friend Module NeonLayout

    Public Const HostHeight As Integer = 34
    Public Const CaptionGap As Integer = 17

    Public Sub AddCaption(parent As Control, text As String, x As Integer, y As Integer)
        Dim label As New Label()

        label.Text = text
        label.AutoSize = True
        label.Location = New Point(x, y)
        label.Font = Theme.UiFont(7.0F, FontStyle.Bold)
        label.ForeColor = Theme.TextMuted

        parent.Controls.Add(label)
    End Sub

    Public Sub AddField(parent As Control, caption As String, child As Control,
                        x As Integer, y As Integer, width As Integer,
                        Optional height As Integer = HostHeight)

        AddCaption(parent, caption, x, y)

        Dim host As New NeonInput()
        host.Location = New Point(x, y + CaptionGap)
        host.Size = New Size(width, height)

        child.BackColor = Theme.InputColor
        child.ForeColor = Theme.TextWhite

        Dim tb = TryCast(child, TextBox)

        If tb IsNot Nothing AndAlso tb.Multiline Then
            host.Padding = New Padding(12, 9, 12, 9)
            child.Dock = DockStyle.Fill
        ElseIf TypeOf child Is ComboBox Then
            host.Padding = New Padding(12, 0, 8, 0)
        Else
            host.Padding = New Padding(12, 0, 12, 0)
        End If

        host.Controls.Add(child)
        parent.Controls.Add(host)
    End Sub

    Public Sub AddDivider(parent As Control, y As Integer,
                          Optional x As Integer = 28,
                          Optional width As Integer = 582)

        Dim line As New Panel()

        line.BackColor = Theme.LineColor
        line.Location = New Point(x, y)
        line.Size = New Size(width, 1)

        parent.Controls.Add(line)
    End Sub
        ' caption + read-only value (returns the label so it can be updated)
    Public Function AddValue(parent As Control, caption As String, value As String,
                             x As Integer, y As Integer, width As Integer) As Label

        AddCaption(parent, caption, x, y)

        Dim lbl As New Label()

        lbl.Text = value
        lbl.AutoEllipsis = True
        lbl.Location = New Point(x, y + CaptionGap)
        lbl.Size = New Size(width, 24)
        lbl.Font = Theme.UiFont(10.0F, FontStyle.Bold)
        lbl.ForeColor = Theme.TextWhite

        parent.Controls.Add(lbl)

        Return lbl

    End Function

    Public Function NewTextBox(Optional multiline As Boolean = False) As TextBox

        Dim box As New TextBox()

        box.BorderStyle = BorderStyle.None
        box.BackColor = Theme.InputColor
        box.ForeColor = Theme.TextWhite
        box.Font = Theme.UiFont(9.5F)

        If multiline Then
            box.Multiline = True
            box.ScrollBars = ScrollBars.Vertical
        End If

        Return box

    End Function

    ' drag a borderless form by any of the given controls
    Public Sub MakeDraggable(form As Form, ParamArray grips As Control())
        Dim dragging As Boolean = False
        Dim offset As Point

        For Each c As Control In grips
            
            AddHandler c.MouseDown,
                Sub(s As Object, e As MouseEventArgs)
                    If e.Button <> MouseButtons.Left Then Return
                    dragging = True
                    offset = New Point(
                        System.Windows.Forms.Cursor.Position.X - form.Left,
                        System.Windows.Forms.Cursor.Position.Y - form.Top)
                End Sub

            AddHandler c.MouseMove,
                Sub(s As Object, e As MouseEventArgs)
                    If Not dragging Then Return
                    form.Location = New Point(
                        System.Windows.Forms.Cursor.Position.X - offset.X,
                        System.Windows.Forms.Cursor.Position.Y - offset.Y)
                End Sub

            AddHandler c.MouseUp,
                Sub(s As Object, e As MouseEventArgs)
                    dragging = False
                End Sub

        Next

    End Sub
End Module
' ================================================================
' POPUP MENU (borderless form, no ToolStrip, works under Wine)
' ================================================================
Friend Class NeonPopupMenu
    Inherits Form

    Private Const RowHeight As Integer = 34
    Private isClosing As Boolean = False
    Public Sub New(
        statuses As String(),
        labelOf As Func(Of String, String),
        onPick As Action(Of String),
        menuWidth As Integer)

        FormBorderStyle = FormBorderStyle.None
        ShowInTaskbar = False
        StartPosition = FormStartPosition.Manual
        BackColor = Theme.LineColor
        Padding = New Padding(1)
        Size = New Size(menuWidth, statuses.Length * RowHeight + 2)

        ' docked Top: add in reverse so the first status ends up on top
        For i As Integer = statuses.Length - 1 To 0 Step -1

            Dim chosen As String = statuses(i)
            Dim accent As Color = Theme.StatusColor(chosen)

            Dim row As New Label()
            row.Dock = DockStyle.Top
            row.Height = RowHeight
            row.Text = labelOf(chosen)
            row.TextAlign = ContentAlignment.MiddleLeft
            row.Padding = New Padding(14, 0, 0, 0)
            row.Font = Theme.UiFont(9.5F, FontStyle.Bold)
            row.ForeColor = accent
            row.BackColor = Theme.CardColor
            row.Cursor = Cursors.Hand

            AddHandler row.MouseEnter,
                Sub(s As Object, e As EventArgs)
                    row.BackColor = Theme.Lerp(Theme.CardColor, accent, 0.2F)
                End Sub

            AddHandler row.MouseLeave,
                Sub(s As Object, e As EventArgs)
                    row.BackColor = Theme.CardColor
                End Sub

            AddHandler row.Click,
                Sub(s As Object, e As EventArgs)
                    CloseMenu()
                    onPick(chosen)
                End Sub

            Controls.Add(row)

        Next

    End Sub
    Private Sub CloseMenu()
        If isClosing Then Return
        isClosing = True
        Close()
    End Sub

    Protected Overrides Sub OnDeactivate(e As EventArgs)
        MyBase.OnDeactivate(e)
        CloseMenu()
    End Sub

    Protected Overrides Function ProcessCmdKey(
        ByRef msg As Message, keyData As Keys) As Boolean

        If keyData = Keys.Escape Then
            CloseMenu()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)

    End Function

    ' shows the menu above (or below) a control, left edges aligned
    Public Sub ShowNear(anchor As Control, above As Boolean)

        Dim y As Integer = If(above, -Height, anchor.Height)

        Location = anchor.PointToScreen(New Point(0, y))

        Show(anchor.FindForm())

    End Sub

End Class


