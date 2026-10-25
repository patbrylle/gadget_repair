Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports gadget_repair.Controllers

Public Class ProfileForm
    Inherits Form

    Private ReadOnly controller As ProfileController
    Private info As ProfileInfo
    Private changed As Boolean = False

    Private header As Panel
    Private lblName As Label
    Private pill As NeonStatusPill
    Private avatar As Panel
    Private btnTabProfile As NeonButton
    Private btnTabPassword As NeonButton
    Private pageProfile As Panel
    Private pagePassword As Panel
    Private lblStatus As Label

    Private txtName As TextBox
    Private txtContact As TextBox
    Private txtEmail As TextBox
    Private cboSpec As NeonComboBox

    Private txtCurrent As TextBox
    Private txtNew As TextBox
    Private txtConfirm As TextBox
    Private chkShow As CheckBox

    Public Sub New(profileController As ProfileController)

        controller = profileController
        info = controller.GetProfile()

        FormBorderStyle = FormBorderStyle.None
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        BackColor = Theme.LineColor
        Padding = New Padding(1)
        ClientSize = New Size(540, 600)
        KeyPreview = True
        Font = Theme.UiFont(9.0F)

        BuildUi()

    End Sub

    Public ReadOnly Property ProfileChanged As Boolean
        Get
            Return changed
        End Get
    End Property

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean

        If keyData = Keys.Escape Then
            Close()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)

    End Function

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        DialogResult = If(changed, DialogResult.OK, DialogResult.Cancel)
    End Sub

    ' ============================================================
    ' UI
    ' ============================================================

    Private Sub BuildUi()

        Dim root As New Panel()
        root.Dock = DockStyle.Fill
        root.BackColor = Theme.CardColor

        ' ---------- header ----------
        header = New Panel()
        header.Dock = DockStyle.Top
        header.Height = 112
        header.BackColor = Theme.CardColor

        AddHandler header.Paint,
            Sub(s As Object, e As PaintEventArgs)
                Using lg As New LinearGradientBrush(
                    New Rectangle(0, header.Height - 2, Math.Max(2, header.Width), 2),
                    Theme.NeonCyan, Theme.NeonPurple, LinearGradientMode.Horizontal)
                    e.Graphics.FillRectangle(lg, 0, header.Height - 2, header.Width, 2)
                End Using
            End Sub

        avatar = New Panel()
        avatar.Size = New Size(68, 68)
        avatar.Location = New Point(28, 22)
        avatar.BackColor = Theme.CardColor

        AddHandler avatar.Paint,
            Sub(s As Object, e As PaintEventArgs)
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                Using lg As New LinearGradientBrush(New Rectangle(0, 0, 68, 68),
                                                    Theme.NeonCyan, Theme.NeonPurple, 45.0F)
                    e.Graphics.FillEllipse(lg, 1, 1, 65, 65)
                End Using
                TextRenderer.DrawText(e.Graphics, Initials(info.FullName), Theme.UiFont(18.0F, FontStyle.Bold),
                                      New Rectangle(0, 0, 68, 68), Color.White,
                                      TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or
                                      TextFormatFlags.NoPadding)
            End Sub

        lblName = New Label()
        lblName.AutoSize = False
        lblName.Location = New Point(114, 26)
        lblName.Size = New Size(340, 30)
        lblName.Font = Theme.UiFont(15.0F, FontStyle.Bold)
        lblName.ForeColor = Theme.TextWhite
        lblName.BackColor = Theme.CardColor
        lblName.AutoEllipsis = True

        pill = New NeonStatusPill()
        pill.Location = New Point(116, 64)
        pill.SetStatus(info.RoleText.ToUpper(), Theme.NeonCyan)

        Dim btnClose As New NeonCloseButton()
        btnClose.Location = New Point(540 - 2 - 32 - 14, 14)
        AddHandler btnClose.Click, Sub(s As Object, e As EventArgs) Close()

        header.Controls.Add(avatar)
        header.Controls.Add(lblName)
        header.Controls.Add(pill)
        header.Controls.Add(btnClose)

        NeonLayout.MakeDraggable(Me, header, lblName, avatar)

        ' ---------- tabs ----------
        btnTabProfile = New NeonButton()
        btnTabProfile.Text = "Profile"
        btnTabProfile.Size = New Size(120, 36)
        btnTabProfile.Location = New Point(28, 126)

        btnTabPassword = New NeonButton()
        btnTabPassword.Text = "Password"
        btnTabPassword.Size = New Size(120, 36)
        btnTabPassword.Location = New Point(156, 126)

        AddHandler btnTabProfile.Click, Sub(s As Object, e As EventArgs) SwitchTab(True)
        AddHandler btnTabPassword.Click, Sub(s As Object, e As EventArgs) SwitchTab(False)

        ' ---------- pages ----------
        pageProfile = New Panel()
        pageProfile.Location = New Point(0, 178)
        pageProfile.Size = New Size(538, 380)
        pageProfile.BackColor = Theme.CardColor

        pagePassword = New Panel()
        pagePassword.Location = New Point(0, 178)
        pagePassword.Size = New Size(538, 380)
        pagePassword.BackColor = Theme.CardColor

        BuildProfilePage()
        BuildPasswordPage()

        lblStatus = New Label()
        lblStatus.Dock = DockStyle.Bottom
        lblStatus.Height = 34
        lblStatus.Padding = New Padding(28, 0, 28, 0)
        lblStatus.TextAlign = ContentAlignment.MiddleLeft
        lblStatus.Font = Theme.UiFont(9.0F)
        lblStatus.ForeColor = Theme.TextMuted
        lblStatus.BackColor = Theme.CardColor

        root.Controls.Add(pageProfile)
        root.Controls.Add(pagePassword)
        root.Controls.Add(btnTabProfile)
        root.Controls.Add(btnTabPassword)
        root.Controls.Add(lblStatus)
        root.Controls.Add(header)

        Controls.Add(root)

        RefreshHeader()
        SwitchTab(True)

    End Sub

    Private Sub BuildProfilePage()

        txtName = NeonLayout.NewTextBox()
        txtName.Text = info.FullName
        NeonLayout.AddField(pageProfile, "FULL NAME", txtName, 28, 0, 482)

        If info.IsAdmin Then

            NeonLayout.AddValue(pageProfile, "USERNAME", info.Username, 28, 70, 482)

            Dim hint As New Label()
            hint.Text = "Your username is used to sign in and cannot be changed here."
            hint.AutoSize = True
            hint.Location = New Point(28, 120)
            hint.Font = Theme.UiFont(8.5F)
            hint.ForeColor = Theme.TextMuted
            pageProfile.Controls.Add(hint)

            AddSaveButton(170)

        Else

            cboSpec = New NeonComboBox()
            cboSpec.Font = Theme.UiFont(9.5F)

            For Each s As String In TechnicianController.SpecializationList
                cboSpec.Items.Add(s)
            Next

            Dim idx As Integer = cboSpec.Items.IndexOf(info.Specialization)

            If idx < 0 AndAlso info.Specialization <> "" Then
                cboSpec.Items.Add(info.Specialization)
                idx = cboSpec.Items.Count - 1
            End If

            If idx >= 0 Then cboSpec.SelectedIndex = idx

            NeonLayout.AddField(pageProfile, "SPECIALIZATION", cboSpec, 28, 65, 482)

            txtContact = NeonLayout.NewTextBox()
            txtContact.Text = info.ContactNumber
            NeonLayout.AddField(pageProfile, "MOBILE NUMBER", txtContact, 28, 130, 230)

            txtEmail = NeonLayout.NewTextBox()
            txtEmail.Text = info.Email
            NeonLayout.AddField(pageProfile, "EMAIL", txtEmail, 280, 130, 230)

            AddSaveButton(210)

        End If

    End Sub

    Private Sub AddSaveButton(y As Integer)

        Dim btn As New NeonButton()
        btn.Text = "Save Changes"
        btn.Filled = True
        btn.Size = New Size(160, 40)
        btn.Location = New Point(28, y)

        AddHandler btn.Click, AddressOf SaveProfile_Click

        pageProfile.Controls.Add(btn)

    End Sub

    Private Sub BuildPasswordPage()

        txtCurrent = NewPasswordBox()
        NeonLayout.AddField(pagePassword, "CURRENT PASSWORD", txtCurrent, 28, 0, 482)

        txtNew = NewPasswordBox()
        NeonLayout.AddField(pagePassword, "NEW PASSWORD", txtNew, 28, 65, 482)

        txtConfirm = NewPasswordBox()
        NeonLayout.AddField(pagePassword, "CONFIRM NEW PASSWORD", txtConfirm, 28, 130, 482)

        chkShow = New CheckBox()
        chkShow.Text = "Show passwords"
        chkShow.AutoSize = True
        chkShow.Location = New Point(28, 198)
        chkShow.ForeColor = Theme.TextMuted
        chkShow.BackColor = Theme.CardColor
        chkShow.Font = Theme.UiFont(9.0F)

        AddHandler chkShow.CheckedChanged,
            Sub(s As Object, e As EventArgs)
                txtCurrent.UseSystemPasswordChar = Not chkShow.Checked
                txtNew.UseSystemPasswordChar = Not chkShow.Checked
                txtConfirm.UseSystemPasswordChar = Not chkShow.Checked
            End Sub

        Dim hint As New Label()
        hint.Text = "Use at least 8 characters with a letter and a number."
        hint.AutoSize = True
        hint.Location = New Point(28, 226)
        hint.Font = Theme.UiFont(8.5F)
        hint.ForeColor = Theme.TextMuted

        Dim btn As New NeonButton()
        btn.Text = "Update Password"
        btn.Filled = True
        btn.Size = New Size(170, 40)
        btn.Location = New Point(28, 256)

        AddHandler btn.Click, AddressOf ChangePassword_Click

        pagePassword.Controls.Add(chkShow)
        pagePassword.Controls.Add(hint)
        pagePassword.Controls.Add(btn)

    End Sub

    Private Shared Function NewPasswordBox() As TextBox
        Dim box As TextBox = NeonLayout.NewTextBox()
        box.UseSystemPasswordChar = True
        box.MaxLength = 128
        Return box
    End Function

    Private Shared Function Initials(name As String) As String

        Dim parts() As String = If(name, "").Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)

        If parts.Length = 0 Then Return "?"
        If parts.Length = 1 Then Return parts(0).Substring(0, 1).ToUpper()

        Return (parts(0).Substring(0, 1) & parts(parts.Length - 1).Substring(0, 1)).ToUpper()

    End Function

    Private Sub RefreshHeader()
        lblName.Text = info.FullName
        avatar.Invalidate()
    End Sub

    Private Sub SwitchTab(showProfile As Boolean)

        pageProfile.Visible = showProfile
        pagePassword.Visible = Not showProfile

        btnTabProfile.Filled = showProfile
        btnTabPassword.Filled = Not showProfile
        btnTabProfile.Accent = If(showProfile, Theme.NeonPurple, Theme.NeonCyan)
        btnTabPassword.Accent = If(showProfile, Theme.NeonCyan, Theme.NeonPurple)

        ShowStatus("", True)

    End Sub

    Private Sub ShowStatus(message As String, ok As Boolean)
        lblStatus.Text = message
        lblStatus.ForeColor = If(ok, Theme.NeonGreen, Theme.NeonRed)
    End Sub

    ' ============================================================
    ' ACTIONS
    ' ============================================================

    Private Sub SaveProfile_Click(sender As Object, e As EventArgs)

        Try

            controller.UpdateProfile(
                txtName.Text,
                If(cboSpec IsNot Nothing, Convert.ToString(cboSpec.SelectedItem), ""),
                If(txtContact IsNot Nothing, txtContact.Text, ""),
                If(txtEmail IsNot Nothing, txtEmail.Text, ""))

            info = controller.GetProfile()
            changed = True

            RefreshHeader()
            ShowStatus("Profile updated.", True)

        Catch ex As Exception

            ShowStatus(ex.Message, False)

        End Try

    End Sub

    Private Sub ChangePassword_Click(sender As Object, e As EventArgs)

        Try

            controller.ChangePassword(txtCurrent.Text, txtNew.Text, txtConfirm.Text)

            txtCurrent.Clear()
            txtNew.Clear()
            txtConfirm.Clear()

            ShowStatus("Password updated. Use it the next time you sign in.", True)

        Catch ex As Exception

            ShowStatus(ex.Message, False)

        End Try

    End Sub

End Class