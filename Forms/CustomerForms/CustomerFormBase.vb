Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Controllers

' Shared shell for Add / Edit customer: header, fields, validation, buttons.
Public MustInherit Class CustomerFormBase
    Inherits Form

    Protected ReadOnly db As ApplicationDbContext
    Protected ReadOnly customerController As CustomerController

    Protected txtFullName As TextBox
    Protected txtContactNumber As TextBox
    Protected txtAddress As TextBox

    Private lblMessage As Label

    Private dragging As Boolean = False
    Private dragOrigin As Point


    Protected Sub New(database As ApplicationDbContext,
                      headingText As String,
                      hintText As String,
                      saveCaption As String)

        db = database
        customerController = New CustomerController(db)

        InitializeForm(headingText)
        BuildInterface(headingText, hintText, saveCaption)

    End Sub


    ' Called only after validation passed. Values are already cleaned.
    Protected MustOverride Sub SaveToDatabase(fullName As String,
                                              contactNumber As String,
                                              address As String)


    Private Sub InitializeForm(headingText As String)

        Me.Text = headingText
        Me.ClientSize = New Size(520, 530)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.None
        Me.ShowInTaskbar = False
        Me.BackColor = Theme.CardColor
        Me.Padding = New Padding(1)
        Me.Font = Theme.UiFont(9.0F)
        Me.DoubleBuffered = True

    End Sub


    Private Sub BuildInterface(headingText As String, hintText As String, saveCaption As String)

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
        lblTitle.Text = headingText
        lblTitle.Font = Theme.UiFont(15.0F, FontStyle.Bold)
        lblTitle.ForeColor = Theme.TextWhite
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(28, 18)

        Dim lblSubtitle As New Label()
        lblSubtitle.Text = hintText
        lblSubtitle.Font = Theme.UiFont(9.0F)
        lblSubtitle.ForeColor = Theme.TextMuted
        lblSubtitle.AutoSize = True
        lblSubtitle.Location = New Point(30, 50)

        Dim btnClose As New NeonButton()
        btnClose.Text = "×"
        btnClose.Accent = Theme.NeonPink
        btnClose.Font = Theme.UiFont(11.0F, FontStyle.Bold)
        btnClose.Size = New Size(36, 30)
        btnClose.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnClose.Location = New Point(462, 16)
        AddHandler btnClose.Click, AddressOf CancelCustomer

        header.Controls.Add(btnClose)
        header.Controls.Add(lblTitle)
        header.Controls.Add(lblSubtitle)

        For Each c As Control In New Control() {header, lblTitle, lblSubtitle}
            AddHandler c.MouseDown, AddressOf Header_MouseDown
            AddHandler c.MouseMove, AddressOf Header_MouseMove
            AddHandler c.MouseUp, AddressOf Header_MouseUp
        Next

        Me.Controls.Add(header)
        Me.Controls.Add(accentLine)

        ' ---------------- fields ----------------
        txtFullName = AddField("FULL NAME", 118, 42, False, "Enter customer's full name", 100)
        txtContactNumber = AddField("CONTACT NUMBER", 198, 42, False, "e.g. 09171234567", 20)
        txtAddress = AddField("ADDRESS", 278, 96, True, "Enter customer's address", 200)

        ' ---------------- message ----------------
        lblMessage = New Label()
        lblMessage.AutoSize = False
        lblMessage.Size = New Size(464, 22)
        lblMessage.Location = New Point(30, 396)
        lblMessage.Font = Theme.UiFont(8.5F)
        lblMessage.ForeColor = Theme.NeonPink
        lblMessage.TextAlign = ContentAlignment.MiddleLeft
        Me.Controls.Add(lblMessage)

        ' ---------------- buttons ----------------
        Dim btnCancel As New NeonButton()
        btnCancel.Text = "CANCEL"
        btnCancel.Accent = Theme.NeonCyan
        btnCancel.Size = New Size(140, 46)
        btnCancel.Location = New Point(180, 440)
        AddHandler btnCancel.Click, AddressOf CancelCustomer
        Me.Controls.Add(btnCancel)

        Dim btnSave As New NeonButton()
        btnSave.Text = saveCaption
        btnSave.Filled = True
        btnSave.Size = New Size(172, 46)
        btnSave.Location = New Point(332, 440)
        AddHandler btnSave.Click, AddressOf SaveCustomer
        Me.Controls.Add(btnSave)

    End Sub


    Private Function AddField(caption As String, y As Integer, height As Integer,
                              multiline As Boolean, placeholder As String,
                              maxLength As Integer) As TextBox

        Dim lbl As New Label()
        lbl.Text = caption
        lbl.AutoSize = True
        lbl.Font = Theme.UiFont(8.0F, FontStyle.Bold)
        lbl.ForeColor = Theme.TextMuted
        lbl.Location = New Point(30, y)

        Dim box As New TextBox()
        box.BackColor = Theme.InputColor
        box.ForeColor = Theme.TextWhite
        box.BorderStyle = BorderStyle.None
        box.Font = Theme.UiFont(10.0F)
        box.PlaceholderText = placeholder
        box.MaxLength = maxLength

        Dim host As New NeonInput()
        host.Location = New Point(28, y + 22)
        host.Size = New Size(464, height)

        If multiline Then
            box.Multiline = True
            box.ScrollBars = ScrollBars.Vertical
            box.Dock = DockStyle.Fill
            host.Padding = New Padding(12, 8, 12, 8)
        Else
            box.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            host.Padding = New Padding(12, 0, 12, 0)
        End If

        host.Controls.Add(box)

        Me.Controls.Add(lbl)
        Me.Controls.Add(host)

        Return box

    End Function


    ' ==========================================================
    ' WINDOW BEHAVIOR
    ' ==========================================================

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        txtFullName.Focus()
        txtFullName.SelectAll()
    End Sub


    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        MyBase.OnPaint(e)

        Using p As New Pen(Theme.Lerp(Theme.LineColor, Theme.NeonCyan, 0.35F), 1.0F)
            e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1)
        End Using

    End Sub


    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean

        If keyData = Keys.Escape Then
            CancelCustomer(Me, EventArgs.Empty)
            Return True
        End If

        ' Enter saves, except inside the address box (Ctrl+Enter saves from there).
        If keyData = (Keys.Control Or Keys.Enter) OrElse
           (keyData = Keys.Enter AndAlso
            TypeOf ActiveControl Is TextBox AndAlso
            Not Object.ReferenceEquals(ActiveControl, txtAddress)) Then

            SaveCustomer(Me, EventArgs.Empty)
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)

    End Function


    Private Sub Header_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            dragging = True
            dragOrigin = Control.MousePosition
        End If
    End Sub


    Private Sub Header_MouseMove(sender As Object, e As MouseEventArgs)

        If Not dragging Then Return

        Dim p As Point = Control.MousePosition

        Me.Location = New Point(Me.Left + p.X - dragOrigin.X,
                                Me.Top + p.Y - dragOrigin.Y)

        dragOrigin = p

    End Sub


    Private Sub Header_MouseUp(sender As Object, e As MouseEventArgs)
        dragging = False
    End Sub


    ' ==========================================================
    ' SAVE / CANCEL
    ' ==========================================================

    Private Sub ShowError(field As String, message As String)

        lblMessage.Text = message

        Select Case field
            Case CustomerValidator.NameField : txtFullName.Focus()
            Case CustomerValidator.ContactField : txtContactNumber.Focus()
            Case CustomerValidator.AddressField : txtAddress.Focus()
        End Select

    End Sub


    Private Sub SaveCustomer(sender As Object, e As EventArgs)

        lblMessage.Text = ""

        Dim fullName As String = CustomerValidator.NormalizeName(txtFullName.Text)
        Dim contactNumber As String = txtContactNumber.Text.Trim()
        Dim address As String = txtAddress.Text.Trim()

        Dim result As CustomerValidationResult =
            CustomerValidator.Validate(fullName, contactNumber, address)

        If Not result.IsValid Then
            ShowError(result.Field, result.Message)
            Return
        End If

        contactNumber = CustomerValidator.NormalizeContact(contactNumber)

        Try

            SaveToDatabase(fullName, contactNumber, address)

            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As CustomerValidationException

            ShowError(ex.Field, ex.Message)

        Catch ex As DbUpdateException

            lblMessage.Text = "Unable to save customer: " & If(ex.InnerException?.Message, ex.Message)

        Catch ex As Exception

            lblMessage.Text = "Unable to save customer: " & ex.Message

        End Try

    End Sub


    Private Sub CancelCustomer(sender As Object, e As EventArgs)

        Me.DialogResult = DialogResult.Cancel
        Me.Close()

    End Sub

End Class