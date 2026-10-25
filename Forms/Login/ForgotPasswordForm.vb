Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports gadget_repair.Controllers

Public Class ForgotPasswordForm
    Inherits Form

    Private ReadOnly resetController As New PasswordResetController()

    Private txtEmail As TextBox
    Private txtVerify As TextBox
    Private txtNew As TextBox
    Private txtConfirm As TextBox
    Private chkShow As CheckBox
    Private lblMessage As Label
    Private btnSave As NeonButton

    Private busy As Boolean = False

    ' filled after a successful reset so the login form can prefill it
    Public Property ResetEmail As String = ""


    Public Sub New(prefillEmail As String)

        InitializeForm()
        BuildInterface()

        txtEmail.Text = If(prefillEmail, "")

    End Sub


    Private Sub InitializeForm()

        Me.Text = "Reset Password"
        Me.ClientSize = New Size(520, 530)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.None
        Me.ShowInTaskbar = False
        Me.BackColor = Theme.CardColor
        Me.Padding = New Padding(1)
        Me.Font = Theme.UiFont(9.0F)
        Me.DoubleBuffered = True

    End Sub


    Private Sub BuildInterface()

        ' ---------------- accent line ----------------
        Dim accentLine As New Panel()
        accentLine.Dock = DockStyle.Top
        accentLine.Height = 2

        AddHandler accentLine.Paint,
            Sub(s, e)
                Using lg As New LinearGradientBrush(accentLine.ClientRectangle,
                                                    Theme.NeonCyan, Theme.NeonPurple,
                                                    LinearGradientMode.Horizontal)
                    e.Graphics.FillRectangle(lg, accentLine.ClientRectangle)
                End Using
            End Sub

        ' ---------------- header (draggable) ----------------
        Dim header As New Panel()
        header.Dock = DockStyle.Top
        header.Height = 84
        header.BackColor = Theme.CardColor

        Dim lblTitle As New Label()
        lblTitle.Text = "RESET PASSWORD"
        lblTitle.Font = Theme.UiFont(15.0F, FontStyle.Bold)
        lblTitle.ForeColor = Theme.TextWhite
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(28, 18)

        Dim lblSubtitle As New Label()
        lblSubtitle.Text = "Confirm your details, then choose a new password."
        lblSubtitle.Font = Theme.UiFont(9.0F)
        lblSubtitle.ForeColor = Theme.TextMuted
        lblSubtitle.AutoSize = True
        lblSubtitle.Location = New Point(30, 50)

        Dim btnClose As New NeonCloseButton()
        btnClose.Location = New Point(462, 16)
        btnClose.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        AddHandler btnClose.Click, AddressOf CancelReset

        header.Controls.Add(btnClose)
        header.Controls.Add(lblTitle)
        header.Controls.Add(lblSubtitle)

        NeonLayout.MakeDraggable(Me, header, lblTitle, lblSubtitle)

        Me.Controls.Add(header)
        Me.Controls.Add(accentLine)

        ' ---------------- fields ----------------
        txtEmail = NeonLayout.NewTextBox()
        txtEmail.MaxLength = 255
        txtEmail.PlaceholderText = "Your account email"
        NeonLayout.AddField(Me, "EMAIL", txtEmail, 28, 108, 464, 42)

        txtVerify = NeonLayout.NewTextBox()
        txtVerify.MaxLength = 100
        txtVerify.PlaceholderText = "Technician: contact number  |  Admin: username"
        NeonLayout.AddField(Me, "CONTACT NUMBER (TECHNICIAN) OR USERNAME (ADMIN)", txtVerify, 28, 184, 464, 42)

        txtNew = NeonLayout.NewTextBox()
        txtNew.MaxLength = 128
        txtNew.UseSystemPasswordChar = True
        txtNew.PlaceholderText = "At least 8 characters, with a letter and a number"
        NeonLayout.AddField(Me, "NEW PASSWORD", txtNew, 28, 260, 464, 42)

        txtConfirm = NeonLayout.NewTextBox()
        txtConfirm.MaxLength = 128
        txtConfirm.UseSystemPasswordChar = True
        txtConfirm.PlaceholderText = "Re-enter the new password"
        NeonLayout.AddField(Me, "CONFIRM PASSWORD", txtConfirm, 28, 336, 464, 42)

        ' ---------------- show password ----------------
        chkShow = New CheckBox()
        chkShow.Text = "Show passwords"
        chkShow.AutoSize = True
        chkShow.FlatStyle = FlatStyle.Flat
        chkShow.FlatAppearance.BorderColor = Theme.LineColor
        chkShow.FlatAppearance.CheckedBackColor = Theme.NeonCyan
        chkShow.FlatAppearance.MouseOverBackColor = Theme.InputColor
        chkShow.Font = Theme.UiFont(8.5F)
        chkShow.ForeColor = Theme.TextMuted
        chkShow.BackColor = Theme.CardColor
        chkShow.Cursor = Cursors.Hand
        chkShow.Location = New Point(30, 404)
        AddHandler chkShow.CheckedChanged,
            Sub(s, e)
                txtNew.UseSystemPasswordChar = Not chkShow.Checked
                txtConfirm.UseSystemPasswordChar = Not chkShow.Checked
            End Sub
        Me.Controls.Add(chkShow)

        ' ---------------- message ----------------
        lblMessage = New Label()
        lblMessage.AutoSize = False
        lblMessage.Size = New Size(464, 22)
        lblMessage.Location = New Point(30, 432)
        lblMessage.Font = Theme.UiFont(8.5F)
        lblMessage.ForeColor = Theme.NeonPink
        lblMessage.TextAlign = ContentAlignment.MiddleLeft
        Me.Controls.Add(lblMessage)

        ' ---------------- buttons ----------------
        Dim btnCancel As New NeonButton()
        btnCancel.Text = "CANCEL"
        btnCancel.Accent = Theme.NeonCyan
        btnCancel.Size = New Size(140, 46)
        btnCancel.Location = New Point(180, 466)
        AddHandler btnCancel.Click, AddressOf CancelReset
        Me.Controls.Add(btnCancel)

        btnSave = New NeonButton()
        btnSave.Text = "RESET PASSWORD"
        btnSave.Filled = True
        btnSave.Size = New Size(172, 46)
        btnSave.Location = New Point(332, 466)
        AddHandler btnSave.Click, AddressOf SaveClick
        Me.Controls.Add(btnSave)

    End Sub


    ' ==========================================================
    ' WINDOW BEHAVIOR
    ' ==========================================================

    Protected Overrides Sub OnShown(e As EventArgs)

        MyBase.OnShown(e)

        If txtEmail.Text.Length > 0 Then
            txtVerify.Focus()
        Else
            txtEmail.Focus()
        End If

    End Sub


    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        MyBase.OnPaint(e)

        Using p As New Pen(Theme.Lerp(Theme.LineColor, Theme.NeonCyan, 0.35F), 1.0F)
            e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1)
        End Using

    End Sub


    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean

        If keyData = Keys.Escape Then
            CancelReset(Me, EventArgs.Empty)
            Return True
        End If

        If keyData = Keys.Enter Then
            SubmitReset()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)

    End Function


    ' ==========================================================
    ' SAVE / CANCEL
    ' ==========================================================

    Private Sub SaveClick(sender As Object, e As EventArgs)
        SubmitReset()
    End Sub


    Private Async Sub SubmitReset()

        If busy Then Return

        busy = True
        lblMessage.Text = ""
        btnSave.Text = "SAVING..."
        btnSave.Enabled = False

        Dim email As String = txtEmail.Text.Trim()
        Dim verifier As String = txtVerify.Text.Trim()
        Dim pw As String = txtNew.Text
        Dim confirm As String = txtConfirm.Text

        Dim result As PasswordResetResult

        Try
            ' hashing is slow on purpose, so keep it off the UI thread
            result = Await System.Threading.Tasks.Task.Run(
                Function() resetController.ResetPassword(email, verifier, pw, confirm))
        Catch ex As Exception
            result = New PasswordResetResult With {
                .Success = False,
                .Message = "Unable to reset password: " & ex.Message
            }
        End Try

        If Me.IsDisposed Then Return

        busy = False
        btnSave.Text = "RESET PASSWORD"
        btnSave.Enabled = True

        If result.Success Then
            ResetEmail = email
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Else
            lblMessage.Text = result.Message
        End If

    End Sub


    Private Sub CancelReset(sender As Object, e As EventArgs)

        Me.DialogResult = DialogResult.Cancel
        Me.Close()

    End Sub

End Class