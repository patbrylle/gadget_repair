Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' ================================================================
' SHOW / HIDE PASSWORD TOGGLE
' ================================================================
Friend Class NeonEyeToggle
    Inherits Control

    Private hovered As Boolean = False
    Private mRevealed As Boolean = False

    Public Property Revealed As Boolean
        Get
            Return mRevealed
        End Get
        Set(value As Boolean)
            mRevealed = value
            Invalidate()
        End Set
    End Property

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Size = New Size(30, 30)
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
        g.Clear(Theme.InputColor)
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim c As Color = If(mRevealed, Theme.NeonCyan, If(hovered, Theme.TextWhite, Theme.TextMuted))
        Dim cx As Single = Width / 2.0F
        Dim cy As Single = Height / 2.0F

        Using pen As New Pen(c, 1.6F)
            pen.StartCap = LineCap.Round
            pen.EndCap = LineCap.Round
            g.DrawEllipse(pen, cx - 9, cy - 5, 18, 10)
            g.DrawEllipse(pen, cx - 2.5F, cy - 2.5F, 5, 5)
            If Not mRevealed Then
                g.DrawLine(pen, cx - 8, cy + 8, cx + 8, cy - 8)
            End If
        End Using
    End Sub

End Class


' ================================================================
' STAFF LOGIN FORM (technician + admin)
' ================================================================
Public Class LoginForm
    Inherits Form

    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer,
                                        wParam As Integer, lParam As Integer) As Integer
    End Function

    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HTCAPTION As Integer = 2

    Private Const FormW As Integer = 900
    Private Const FormH As Integer = 560
    Private Const BrandW As Integer = 400
    Private Const PanelX As Integer = 460
    Private Const PanelW As Integer = 380
    Private Const InputH As Integer = 42

    ' One instance of each controller for the life of this form.
    ' The admin controller keeps its lockout counters in memory,
    ' so it must not be re-created on every click.
    Private ReadOnly auth As New AuthController()
    Private ReadOnly techAuth As New TechnicianAuthController()

    Private txtUser As TextBox
    Private txtPass As TextBox
    Private hostUser As NeonInput
    Private hostPass As NeonInput
    Private eye As NeonEyeToggle
    Private chkRemember As CheckBox
    Private lblError As Label
    Private btnLogin As NeonButton
    Private btnClose As NeonCloseButton
    Private shakeTimer As Timer
    Private shakeStep As Integer = 0
    Private shakeBaseLeft As Integer = 0
    Private busy As Boolean = False

    Public Sub New()
        InitializeUi()
    End Sub

    ' ==========================================================
    ' UI BUILD
    ' ==========================================================
    Private Sub InitializeUi()

        SuspendLayout()

        Text = "Staff Login"
        FormBorderStyle = FormBorderStyle.None
        StartPosition = FormStartPosition.CenterScreen
        ClientSize = New Size(FormW, FormH)
        BackColor = Theme.BackgroundColor
        ForeColor = Theme.TextWhite
        DoubleBuffered = True
        KeyPreview = True

        SetStyle(ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw, True)

        btnClose = New NeonCloseButton()
        btnClose.Location = New Point(FormW - 46, 14)
        AddHandler btnClose.Click, Sub(s, e) Close()
        Controls.Add(btnClose)

        Dim lblEyebrow As New Label()
        lblEyebrow.Text = "STAFF ACCESS"
        lblEyebrow.AutoSize = True
        lblEyebrow.Font = Theme.UiFont(8.0F, FontStyle.Bold)
        lblEyebrow.ForeColor = Theme.NeonCyan
        lblEyebrow.BackColor = Theme.BackgroundColor
        lblEyebrow.Location = New Point(PanelX, 84)
        Controls.Add(lblEyebrow)

        Dim lblTitle As New Label()
        lblTitle.Text = "Welcome back"
        lblTitle.AutoSize = True
        lblTitle.Font = Theme.UiFont(24.0F, FontStyle.Bold)
        lblTitle.ForeColor = Theme.TextWhite
        lblTitle.BackColor = Theme.BackgroundColor
        lblTitle.Location = New Point(PanelX - 2, 104)
        Controls.Add(lblTitle)

        Dim lblSub As New Label()
        lblSub.Text = "Sign in to your Repair Hub account."
        lblSub.AutoSize = True
        lblSub.Font = Theme.UiFont(9.0F)
        lblSub.ForeColor = Theme.TextMuted
        lblSub.BackColor = Theme.BackgroundColor
        lblSub.Location = New Point(PanelX, 156)
        Controls.Add(lblSub)

        ' ---- email ----
        NeonLayout.AddCaption(Me, "Email", PanelX, 200)

        hostUser = New NeonInput()
        hostUser.Location = New Point(PanelX, 200 + NeonLayout.CaptionGap)
        hostUser.Size = New Size(PanelW, InputH)
        hostUser.Padding = New Padding(14, 0, 14, 0)

        txtUser = New TextBox()
        txtUser.BorderStyle = BorderStyle.None
        txtUser.Font = Theme.UiFont(10.5F)
        txtUser.BackColor = Theme.InputColor
        txtUser.ForeColor = Theme.TextWhite
        txtUser.MaxLength = 255
        hostUser.Controls.Add(txtUser)
        Controls.Add(hostUser)

        ' ---- password ----
        NeonLayout.AddCaption(Me, "Password", PanelX, 276)

        hostPass = New NeonInput()
        hostPass.Location = New Point(PanelX, 276 + NeonLayout.CaptionGap)
        hostPass.Size = New Size(PanelW, InputH)
        hostPass.Padding = New Padding(14, 0, 46, 0)

        txtPass = New TextBox()
        txtPass.BorderStyle = BorderStyle.None
        txtPass.Font = Theme.UiFont(10.5F)
        txtPass.BackColor = Theme.InputColor
        txtPass.ForeColor = Theme.TextWhite
        txtPass.UseSystemPasswordChar = True
        txtPass.MaxLength = 128
        hostPass.Controls.Add(txtPass)

        eye = New NeonEyeToggle()
        eye.Location = New Point(PanelW - 38, (InputH - eye.Height) \ 2)
        AddHandler eye.Click, AddressOf OnToggleEye
        hostPass.Controls.Add(eye)
        eye.BringToFront()

        Controls.Add(hostPass)

        ' ---- remember me ----
        chkRemember = New CheckBox()
        chkRemember.Text = "Keep me signed in on this PC"
        chkRemember.AutoSize = True
        chkRemember.FlatStyle = FlatStyle.Flat
        chkRemember.FlatAppearance.BorderColor = Theme.LineColor
        chkRemember.FlatAppearance.CheckedBackColor = Theme.NeonCyan
        chkRemember.FlatAppearance.MouseOverBackColor = Theme.InputColor
        chkRemember.Font = Theme.UiFont(8.5F)
        chkRemember.ForeColor = Theme.TextMuted
        chkRemember.BackColor = Theme.BackgroundColor
        chkRemember.Cursor = Cursors.Hand
        chkRemember.Location = New Point(PanelX, 350)
        Controls.Add(chkRemember)

                ' ---- forgot password ----
        Dim lnkForgot As New Label()
        lnkForgot.Text = "Forgot password?"
        lnkForgot.AutoSize = False
        lnkForgot.Size = New Size(130, 20)
        lnkForgot.Location = New Point(PanelX + PanelW - 130, 352)
        lnkForgot.TextAlign = ContentAlignment.MiddleRight
        lnkForgot.Font = Theme.UiFont(8.5F, FontStyle.Bold)
        lnkForgot.ForeColor = Theme.NeonCyan
        lnkForgot.BackColor = Theme.BackgroundColor
        lnkForgot.Cursor = Cursors.Hand
        AddHandler lnkForgot.MouseEnter, Sub(s, e) lnkForgot.ForeColor = Theme.TextWhite
        AddHandler lnkForgot.MouseLeave, Sub(s, e) lnkForgot.ForeColor = Theme.NeonCyan
        AddHandler lnkForgot.Click, AddressOf OnForgotClick
        Controls.Add(lnkForgot)

        ' ---- error message ----
        lblError = New Label()
        lblError.AutoSize = False
        lblError.Size = New Size(PanelW, 20)
        lblError.Location = New Point(PanelX, 380)
        lblError.Font = Theme.UiFont(8.5F, FontStyle.Bold)
        lblError.ForeColor = Theme.NeonRed
        lblError.BackColor = Theme.BackgroundColor
        Controls.Add(lblError)

        ' ---- sign in ----
        btnLogin = New NeonButton()
        btnLogin.Text = "SIGN IN"
        btnLogin.Filled = True
        btnLogin.Font = Theme.UiFont(10.0F, FontStyle.Bold)
        btnLogin.Size = New Size(PanelW, 48)
        btnLogin.Location = New Point(PanelX, 408)
        AddHandler btnLogin.Click, AddressOf OnLoginClick
        Controls.Add(btnLogin)

        ' ---- footer ----
        Dim lblFoot As New Label()
        lblFoot.Text = "Authorized personnel only. Activity is logged."
        lblFoot.AutoSize = False
        lblFoot.Size = New Size(PanelW, 18)
        lblFoot.TextAlign = ContentAlignment.MiddleCenter
        lblFoot.Font = Theme.UiFont(8.0F)
        lblFoot.ForeColor = Theme.TextMuted
        lblFoot.BackColor = Theme.BackgroundColor
        lblFoot.Location = New Point(PanelX, 492)
        Controls.Add(lblFoot)

        shakeTimer = New Timer()
        shakeTimer.Interval = 16
        AddHandler shakeTimer.Tick, AddressOf OnShakeTick

        AddHandler KeyDown, AddressOf OnFormKeyDown

        ResumeLayout(False)

    End Sub

    ' ==========================================================
    ' PAINTING
    ' ==========================================================
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim brand As New Rectangle(0, 0, BrandW, FormH)

        Using lg As New LinearGradientBrush(brand,
                                            Color.FromArgb(16, 24, 56),
                                            Color.FromArgb(34, 12, 66), 60.0F)
            g.FillRectangle(lg, brand)
        End Using

        DrawGlow(g, 70, 90, 220, Theme.NeonCyan, 26)
        DrawGlow(g, 340, 470, 260, Theme.NeonPurple, 34)
        DrawGlow(g, 360, 120, 120, Theme.NeonPink, 18)

        Dim hexSpecs()() As Single = {
            New Single() {40, 440, 34, 40},
            New Single() {96, 470, 22, 30},
            New Single() {350, 60, 26, 36},
            New Single() {310, 400, 18, 26}
        }

        For Each h() As Single In hexSpecs
            Using p As New Pen(Color.FromArgb(CInt(h(3)), Theme.NeonCyan), 1.4F)
                p.LineJoin = LineJoin.Round
                g.DrawPolygon(p, Theme.HexPoints(h(0), h(1), h(2)))
            End Using
        Next

        Theme.DrawLogoMark(g, BrandW / 2.0F, 150.0F, 52.0F)

        Dim flagsC As TextFormatFlags = TextFormatFlags.HorizontalCenter Or
                                        TextFormatFlags.VerticalCenter Or
                                        TextFormatFlags.NoPadding

        TextRenderer.DrawText(g, "REPAIR HUB", Theme.UiFont(22.0F, FontStyle.Bold),
                              New Rectangle(0, 224, BrandW, 40), Theme.TextWhite, flagsC)

        TextRenderer.DrawText(g, "Gadget Repair Management System", Theme.UiFont(9.0F),
                              New Rectangle(0, 264, BrandW, 22), Theme.TextViolet, flagsC)

        Dim rule As New Rectangle((BrandW - 80) \ 2, 300, 80, 3)
        Using lg As New LinearGradientBrush(rule, Theme.NeonCyan, Theme.NeonPurple,
                                            LinearGradientMode.Horizontal)
            g.FillRectangle(lg, rule)
        End Using

        Dim features() As String = {
            "Track repairs from intake to release",
            "Manage customers and technicians",
            "Monitor inventory and payments"
        }
        Dim dots() As Color = {Theme.NeonCyan, Theme.NeonPurple, Theme.NeonGreen}

        For i As Integer = 0 To features.Length - 1
            Dim y As Integer = 340 + i * 34
            Using b As New SolidBrush(dots(i))
                g.FillEllipse(b, 78, y + 8, 7, 7)
            End Using
            TextRenderer.DrawText(g, features(i), Theme.UiFont(9.0F),
                                  New Rectangle(96, y, BrandW - 110, 24),
                                  Theme.TextWhite,
                                  TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
                                  TextFormatFlags.NoPadding)
        Next

        TextRenderer.DrawText(g, "v1.0", Theme.UiFont(8.0F),
                              New Rectangle(0, FormH - 36, BrandW, 20),
                              Theme.TextMuted, flagsC)

        Using p As New Pen(Theme.LineColor, 1.0F)
            g.DrawLine(p, BrandW, 0, BrandW, FormH)
        End Using

        Using p As New Pen(Theme.LineColor, 1.0F)
            g.SmoothingMode = SmoothingMode.None
            g.DrawRectangle(p, 0, 0, Width - 1, Height - 1)
        End Using
    End Sub

    Private Sub DrawGlow(g As Graphics, cx As Single, cy As Single,
                         radius As Single, c As Color, maxAlpha As Integer)

        Using path As New GraphicsPath()
            path.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2)

            Using pgb As New PathGradientBrush(path)
                pgb.CenterColor = Color.FromArgb(maxAlpha, c)
                pgb.SurroundColors = New Color() {Color.FromArgb(0, c)}

                Dim oldClip As Region = g.Clip
                g.SetClip(New Rectangle(0, 0, BrandW, FormH))
                g.FillEllipse(pgb, cx - radius, cy - radius, radius * 2, radius * 2)
                g.Clip = oldClip
            End Using
        End Using

    End Sub

    ' ==========================================================
    ' DRAG WINDOW
    ' ==========================================================
    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)

        If e.Button = MouseButtons.Left Then
            ReleaseCapture()
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0)
        End If
    End Sub

    ' ==========================================================
    ' EVENTS
    ' ==========================================================
    Private Sub OnToggleEye(sender As Object, e As EventArgs)
        eye.Revealed = Not eye.Revealed
        txtPass.UseSystemPasswordChar = Not eye.Revealed
        txtPass.Focus()
        txtPass.SelectionStart = txtPass.TextLength
    End Sub

    Private Sub OnFormKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            e.SuppressKeyPress = True
            DoLogin()
        ElseIf e.KeyCode = Keys.Escape Then
            e.Handled = True
            Close()
        End If
    End Sub
    Private Sub OnForgotClick(sender As Object, e As EventArgs)

        Using dlg As New ForgotPasswordForm(txtUser.Text.Trim())

            If dlg.ShowDialog(Me) = DialogResult.OK Then

                txtUser.Text = dlg.ResetEmail
                txtPass.Clear()

                lblError.ForeColor = Theme.NeonGreen
                lblError.Text = "Password updated. Sign in with your new password."

                txtPass.Focus()

            End If

        End Using

    End Sub

    Private Sub OnLoginClick(sender As Object, e As EventArgs)
        DoLogin()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        txtUser.Focus()
    End Sub

    ' ==========================================================
    ' LOGIN (technician first, then admin)
    ' ==========================================================
    Private Async Sub DoLogin()

        If busy Then Return

        lblError.ForeColor = Theme.NeonRed
        Dim email As String = txtUser.Text.Trim()
        Dim pass As String = txtPass.Text

        If email.Length = 0 Then
            lblError.Text = "Please enter your email."
            txtUser.Focus()
            Return
        End If

        If pass.Length = 0 Then
            lblError.Text = "Please enter your password."
            txtPass.Focus()
            Return
        End If

        busy = True
        lblError.Text = ""
        btnLogin.Text = "SIGNING IN..."
        btnLogin.Enabled = False

        ' run hashing and database work off the UI thread
        ' so the form stays responsive
        Dim outcome As (Success As Boolean, Message As String)

        Try
            outcome = Await System.Threading.Tasks.Task.Run(
                Function() SignIn(email, pass))
        Catch ex As Exception
            outcome = (False, "Unable to sign in: " & ex.Message)
        End Try

        busy = False
        btnLogin.Text = "SIGN IN"
        btnLogin.Enabled = True

        If outcome.Success Then
            DialogResult = DialogResult.OK
            Close()
        Else
            lblError.Text = outcome.Message
            txtPass.SelectAll()
            txtPass.Focus()
            StartShake()
        End If

    End Sub

    Private Function SignIn(email As String, pass As String) _
            As (Success As Boolean, Message As String)

        ' 1) Technician first: it keeps no lockout state,
        '    so it never touches the admin failure counters.
        Dim tech = techAuth.Login(email, pass)

        If tech.Status = LoginStatus.Success Then
            AuthController.Logout()          ' never leave an admin session behind
            Return (True, "")
        End If

        If tech.Status = LoginStatus.AccountDisabled Then
            Return (False, tech.Message)
        End If

        ' 2) Admin
        Dim admin = auth.Login(email, pass)

        If admin.Status = LoginStatus.Success Then
            TechnicianAuthController.Logout()
            Return (True, "")
        End If

        Return (False, admin.Message)

    End Function

    ' ==========================================================
    ' SHAKE ANIMATION
    ' ==========================================================
    Private Sub StartShake()
        If shakeTimer.Enabled Then Return
        shakeBaseLeft = Left
        shakeStep = 0
        shakeTimer.Start()
    End Sub

    Private Sub OnShakeTick(sender As Object, e As EventArgs)
        shakeStep += 1

        If shakeStep > 18 Then
            shakeTimer.Stop()
            Left = shakeBaseLeft
            Return
        End If

        Dim amplitude As Double = 10.0 * (1.0 - shakeStep / 18.0)
        Left = shakeBaseLeft + CInt(Math.Sin(shakeStep * 1.3) * amplitude)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso shakeTimer IsNot Nothing Then shakeTimer.Dispose()
        MyBase.Dispose(disposing)
    End Sub

End Class