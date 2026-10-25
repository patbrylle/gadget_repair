Imports System
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Models

Public Class PaymentEditForm
    Inherits Form

    Private ReadOnly paymentController As PaymentController
    Private ReadOnly repairId As Integer
    Private ReadOnly canEdit As Boolean = AppSession.IsAdmin

    Private paymentId As Integer = 0
    Private ticket As Repair
    Private loadFailed As Boolean = False
        Private previousPaid As Decimal = 0D
    Private btnReceipt As NeonButton

    Private txtAmount As TextBox
    Private txtNotes As TextBox
    Private dtpDate As NeonDateTimeField
    Private lblBalance As Label
    Private payPill As NeonStatusPill
    Private lblMessage As Label

    Private footerPanel As Panel
    Private btnSave As NeonButton
    Private btnCancel As NeonButton
    Private btnDelete As NeonButton

    ' layout (form 500 x 480, inner width = 498, content = 450)
    Private Const PadX As Integer = 24
    Private Const ContentWidth As Integer = 450
    Private Const ColA As Integer = 24
    Private Const WidthA As Integer = 200
    Private Const ColB As Integer = 244
    Private Const WidthB As Integer = 230
    Private Const Col3A As Integer = 24
    Private Const Col3B As Integer = 180
    Private Const Col3C As Integer = 336
    Private Const Width3 As Integer = 140


    ' ============================================================
    ' CONSTRUCTOR
    ' ============================================================

    Public Sub New(controller As PaymentController, id As Integer)

        paymentController = controller
        repairId = id

        InitializeForm()

        ticket = paymentController.GetRepair(repairId)

        If ticket Is Nothing Then

            loadFailed = True

            MessageBox.Show(
                "Repair record not found.",
                "Payment",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)

            Return

        End If

        If Not PaymentController.IsPayable(ticket.Status) Then

            loadFailed = True

            MessageBox.Show(
                "Only completed or delivered repairs can be paid.",
                "Payment",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)

            Return

        End If

        Dim existing As Payment = paymentController.GetPaymentByRepair(repairId)

        If existing IsNot Nothing Then
            paymentId = existing.PaymentId
        End If

        BuildInterface()
        LoadValues(existing)

    End Sub

    Private Sub InitializeForm()

        Me.Text = "Payment"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(500, 480)
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.ShowInTaskbar = False
        Me.Font = Theme.UiFont(9.0F)
        Me.DoubleBuffered = True

        ' 1px border: form shows the line color, content sits inside
        Me.BackColor = Theme.LineColor
        Me.Padding = New Padding(1)

    End Sub


    ' ============================================================
    ' INTERFACE
    ' ============================================================

    Private Sub BuildInterface()

        ' accent strip
        Dim accentStrip As New Panel()
        accentStrip.Dock = DockStyle.Top
        accentStrip.Height = 2
        accentStrip.BackColor = Theme.NeonCyan


        ' header
        Dim header As New Panel()
        header.Dock = DockStyle.Top
        header.Height = 64
        header.BackColor = Theme.BackgroundColor

        Dim title As New Label()
        title.Text = "Payment"
        title.AutoSize = True
        title.Location = New Point(PadX, 12)
        title.Font = Theme.UiFont(14.0F, FontStyle.Bold)
        title.ForeColor = Theme.TextWhite

        Dim subtitle As New Label()
        subtitle.Text = "Ticket #" & repairId.ToString() &
            If(canEdit, " - record what the customer paid", " - payment details")
        subtitle.AutoSize = True
        subtitle.Location = New Point(PadX + 1, 40)
        subtitle.Font = Theme.UiFont(8.5F)
        subtitle.ForeColor = Theme.TextMuted

        Dim btnClose As New NeonCloseButton()
        btnClose.Location = New Point(454, 16)

        AddHandler btnClose.Click, AddressOf btnCancel_Click

        Dim headerLine As New Panel()
        headerLine.Dock = DockStyle.Bottom
        headerLine.Height = 1
        headerLine.BackColor = Theme.LineColor

        header.Controls.Add(title)
        header.Controls.Add(subtitle)
        header.Controls.Add(btnClose)
        header.Controls.Add(headerLine)

        AddHandler header.Resize,
            Sub(sender As Object, e As EventArgs)
                btnClose.Left = header.ClientSize.Width - btnClose.Width - 14
            End Sub

        MakeDraggable(Me, header, title, subtitle)


        ' footer
        footerPanel = New Panel()
        footerPanel.Dock = DockStyle.Bottom
        footerPanel.Height = 62
        footerPanel.BackColor = Theme.BackgroundColor

        Dim footerLine As New Panel()
        footerLine.Dock = DockStyle.Top
        footerLine.Height = 1
        footerLine.BackColor = Theme.LineColor

              btnDelete = New NeonButton()
        btnDelete.Text = "DELETE"
        btnDelete.Filled = False
        btnDelete.Accent = Theme.NeonPink
        btnDelete.ForeColor = Theme.NeonPink
        btnDelete.KeepAccentOnHover = True
        btnDelete.Size = New Size(90, 38)
        btnDelete.Location = New Point(PadX, 12)
        btnDelete.Visible = canEdit AndAlso paymentId > 0

        AddHandler btnDelete.Click, AddressOf btnDelete_Click

        btnReceipt = New NeonButton()
        btnReceipt.Text = "RECEIPT"
        btnReceipt.Filled = False
        btnReceipt.Accent = Theme.NeonCyan
        btnReceipt.Size = New Size(100, 38)
        btnReceipt.Visible = paymentId > 0

        AddHandler btnReceipt.Click, AddressOf btnReceipt_Click

        btnCancel = New NeonButton()
        btnCancel.Text = If(canEdit, "CANCEL", "CLOSE")
        btnCancel.Filled = False
        btnCancel.Accent = Theme.TextMuted
        btnCancel.Size = New Size(90, 38)
        btnCancel.Top = 12

        AddHandler btnCancel.Click, AddressOf btnCancel_Click

        btnSave = New NeonButton()
        btnSave.Text = If(paymentId = 0, "RECORD PAYMENT", "SAVE CHANGES")
        btnSave.Filled = True
        btnSave.Accent = Theme.NeonCyan
        btnSave.Size = New Size(150, 38)
        btnSave.Top = 12
        btnSave.Visible = canEdit

        AddHandler btnSave.Click, AddressOf btnSave_Click

        footerPanel.Controls.Add(footerLine)
        footerPanel.Controls.Add(btnDelete)
        footerPanel.Controls.Add(btnReceipt)
        footerPanel.Controls.Add(btnCancel)
        footerPanel.Controls.Add(btnSave)

        AddHandler footerPanel.Resize,
            Sub(sender As Object, e As EventArgs) LayoutFooter()

        LayoutFooter()


        ' body
        Dim body As New Panel()
        body.Dock = DockStyle.Fill
        body.BackColor = Theme.BackgroundColor

        Me.Controls.Add(body)
        Me.Controls.Add(footerPanel)
        Me.Controls.Add(header)
        Me.Controls.Add(accentStrip)

        BuildBody(body)

    End Sub

      Private Sub LayoutFooter()

    ' use the intended state, not .Visible (it reads False until the form is shown)
    Dim showSave As Boolean = canEdit
    Dim showDelete As Boolean = canEdit AndAlso paymentId > 0

    Dim right As Integer = footerPanel.ClientSize.Width - PadX

    If showSave Then
        btnSave.Left = right - btnSave.Width
        right = btnSave.Left - 10
    End If

    btnCancel.Left = right - btnCancel.Width

    btnDelete.Left = PadX

    btnReceipt.Left = If(showDelete, btnDelete.Right + 8, PadX)
    btnReceipt.Top = 12

End Sub

    Private Sub BuildBody(body As Panel)

        ' read-only ticket info
        AddValue(body, "CUSTOMER",
                 paymentController.GetCustomerName(ticket.CustomerId),
                 ColA, 16, WidthA)

        AddValue(body, "DEVICE", DeviceText(ticket), ColB, 16, WidthB)

        AddValue(body, "TECHNICIAN",
                 PaymentController.TechnicianName(ticket),
                 ColA, 68, WidthA)

        Dim completed As String = "-"

        If ticket.CompletionDate.HasValue Then
            completed = ticket.CompletionDate.Value.ToString("MMM dd, yyyy")
        End If

        AddValue(body, "COMPLETED", completed, ColB, 68, WidthB)

        AddValue(body, "FINAL COST", Money.Peso(ticket.Cost), Col3A, 120, Width3)

        lblBalance = AddValue(body, "BALANCE", "", Col3B, 120, Width3)

        AddCaption(body, "PAYMENT STATUS", Col3C, 120)

        payPill = New NeonStatusPill()
        payPill.BackColor = Theme.BackgroundColor
        payPill.Location = New Point(Col3C, 120 + CaptionGap)
        body.Controls.Add(payPill)

        AddDivider(body, 172, PadX, ContentWidth)

        ' inputs
        txtAmount = NewTextBox()
        AddField(body, "TOTAL AMOUNT PAID (PHP)", txtAmount, ColA, 186, WidthA)

        AddCaption(body, "PAYMENT DATE", ColB, 186)

        dtpDate = New NeonDateTimeField()
        dtpDate.Location = New Point(ColB, 186 + CaptionGap)
        dtpDate.Size = New Size(WidthB, HostHeight)
        body.Controls.Add(dtpDate)

        txtNotes = NewTextBox(True)
        AddField(body, "NOTES", txtNotes, ColA, 248, ContentWidth, 56)

        lblMessage = New Label()
        lblMessage.AutoSize = False
        lblMessage.Size = New Size(ContentWidth, 20)
        lblMessage.Location = New Point(PadX, 327)
        lblMessage.Font = Theme.UiFont(8.5F)
        lblMessage.ForeColor = Theme.NeonPink
        lblMessage.TextAlign = ContentAlignment.MiddleLeft
        body.Controls.Add(lblMessage)

        AddHandler txtAmount.TextChanged,
            Sub(sender As Object, e As EventArgs) RefreshSummary()

        If Not canEdit Then

            txtAmount.ReadOnly = True
            txtNotes.ReadOnly = True
            dtpDate.Locked = True

            txtAmount.ForeColor = Theme.TextMuted
            txtNotes.ForeColor = Theme.TextMuted

        End If

    End Sub

    Private Function DeviceText(repair As Repair) As String

        Dim item As String = paymentController.GetItemUsed(repair)
        Dim typeName As String = If(repair.DeviceType, "").Trim()

        If item = "-" Then Return "Not provided"

        If typeName = "" OrElse item = typeName Then Return item

        Return item & " (" & typeName & ")"

    End Function


    ' ============================================================
    ' VALUES
    ' ============================================================

    Private Sub LoadValues(existing As Payment)

        If existing IsNot Nothing Then

            previousPaid = existing.AmountPaid
            txtAmount.Text =
                existing.AmountPaid.ToString("0.00", CultureInfo.InvariantCulture)

            dtpDate.Value = existing.PaymentDate
            txtNotes.Text = If(existing.Notes, "")

        Else

            txtAmount.Text = "0.00"
            dtpDate.Value = DateTime.Now

        End If

        RefreshSummary()

    End Sub

    Private Shared Function TryParseAmount(
        text As String,
        ByRef amount As Decimal) As Boolean

        If Not Decimal.TryParse(
            If(text, "").Trim(),
            NumberStyles.AllowDecimalPoint Or NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            amount) Then

            Return False

        End If

        Return amount >= 0D

    End Function

    ' balance + status follow the amount while typing
    Private Sub RefreshSummary()

        Dim paid As Decimal = 0D

        If Not TryParseAmount(txtAmount.Text, paid) Then
            paid = 0D
        End If

        Dim balance As Decimal = Math.Max(0D, ticket.Cost - paid)

        lblBalance.Text = Money.Peso(balance)
        lblBalance.ForeColor = If(balance > 0D, Theme.NeonYellow, Theme.NeonGreen)

        Dim status As String = PaymentController.ComputeStatus(ticket.Cost, paid)

        payPill.SetStatus(status, PayStatusColor(status))

    End Sub

    Private Sub ShowMessage(text As String, target As Control)

        lblMessage.Text = text

        If target IsNot Nothing Then target.Focus()

    End Sub


    ' ============================================================
    ' SAVE / DELETE / CANCEL
    ' ============================================================

       Private Sub btnSave_Click(sender As Object, e As EventArgs)

        If Not canEdit Then Return

        lblMessage.Text = ""

        Dim amount As Decimal = 0D

        If Not TryParseAmount(txtAmount.Text, amount) Then
            ShowMessage("Enter a valid amount, for example 1500.00.", txtAmount)
            Return
        End If

        If amount > ticket.Cost Then
            ShowMessage("Payment cannot be greater than the repair cost.", txtAmount)
            Return
        End If

        If paymentId = 0 AndAlso amount <= 0D Then
            ShowMessage("Enter an amount above 0.", txtAmount)
            Return
        End If

        Try

            Dim saved As Payment

            If paymentId = 0 Then

                saved = paymentController.CreatePayment(
                    repairId, amount, dtpDate.Value, txtNotes.Text.Trim())

            Else

                If Not paymentController.UpdatePayment(
                    paymentId, amount, dtpDate.Value, txtNotes.Text.Trim()) Then

                    ShowMessage("The payment record could not be found.", Nothing)
                    Return

                End If

                saved = paymentController.GetPayment(paymentId)

            End If

            Dim received As Decimal = amount - previousPaid

            If received > 0D AndAlso saved IsNot Nothing Then

                ' money was received: success message + receipt
                Using receipt As New PaymentReceiptForm(
                    paymentController, ticket, saved, received)

                    receipt.ShowDialog(Me)

                End Using

            Else

                MessageBox.Show(
                    "Payment updated successfully.",
                    "Payment",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)

            End If

            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception

            ShowMessage(ex.Message, Nothing)

        End Try

    End Sub

    ' reprint the receipt of a saved payment
    Private Sub btnReceipt_Click(sender As Object, e As EventArgs)

        If paymentId <= 0 Then Return

        Dim saved As Payment = paymentController.GetPayment(paymentId)

        If saved Is Nothing Then Return

        Using receipt As New PaymentReceiptForm(
            paymentController, ticket, saved, 0D)

            receipt.ShowDialog(Me)

        End Using

    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs)

        If paymentId <= 0 Then Return

        If MessageBox.Show(
            "Delete the payment for ticket #" & repairId.ToString() & "?" &
            Environment.NewLine & Environment.NewLine &
            "This action cannot be undone.",
            "Delete Payment",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) <> DialogResult.Yes Then

            Return

        End If

        Try

            If paymentController.DeletePayment(paymentId) Then

                Me.DialogResult = DialogResult.OK
                Me.Close()

            Else

                ShowMessage("The payment record could not be found.", Nothing)

            End If

        Catch ex As Exception

            ShowMessage(ex.Message, Nothing)

        End Try

    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)

        Me.DialogResult = DialogResult.Cancel
        Me.Close()

    End Sub


    ' ============================================================
    ' FORM BEHAVIOR
    ' ============================================================

    Protected Overrides Sub OnShown(e As EventArgs)

        MyBase.OnShown(e)

        If loadFailed Then
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
            Return
        End If

        If canEdit Then
            txtAmount.Focus()
            txtAmount.SelectAll()
        End If

    End Sub

    Protected Overrides Function ProcessCmdKey(
        ByRef msg As Message,
        keyData As Keys) As Boolean

        If keyData = Keys.Escape Then

            Me.DialogResult = DialogResult.Cancel
            Me.Close()

            Return True

        End If

        If keyData = Keys.Enter AndAlso canEdit AndAlso
           Not Object.ReferenceEquals(ActiveControl, txtNotes) Then

            btnSave_Click(Me, EventArgs.Empty)

            Return True

        End If

        Return MyBase.ProcessCmdKey(msg, keyData)

    End Function

End Class