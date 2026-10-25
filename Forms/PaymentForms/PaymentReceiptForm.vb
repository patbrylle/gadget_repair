Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Models

Public Class PaymentReceiptForm
    Inherits Form

    Private Class Line
        Public Label As String = ""
        Public Value As String = ""
        Public Strong As Boolean = False
        Public Rule As Boolean = False
        Public ValueColor As Color = Color.Empty
    End Class

    Private ReadOnly lines As New List(Of Line)
    Private ReadOnly receiptNo As String
    Private ReadOnly justPaid As Boolean

    Private pic As PictureBox
    Private footerPanel As Panel
    Private btnPrint As NeonButton
    Private btnSave As NeonButton
    Private btnDone As NeonButton

    Private Const PadX As Integer = 20
    Private Const FormWidth As Integer = 440
    Private Const PaperWidth As Integer = 396


    ' ============================================================
    ' CONSTRUCTOR
    ' amountNow = money received in this transaction (0 = reprint)
    ' ============================================================

    Public Sub New(
        controller As PaymentController,
        repair As Repair,
        payment As Payment,
        amountNow As Decimal)

        justPaid = amountNow > 0D
        receiptNo = "RCPT-" & payment.PaymentId.ToString("00000")

        BuildLines(controller, repair, payment, amountNow)

        Me.Text = "Receipt"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.None
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.ShowInTaskbar = False
        Me.Font = Theme.UiFont(9.0F)
        Me.DoubleBuffered = True

        ' 1px border: form shows the line color, content sits inside
        Me.BackColor = Theme.LineColor
        Me.Padding = New Padding(1)

        BuildInterface()

    End Sub


    ' ============================================================
    ' RECEIPT CONTENT (one list drives screen, print and text file)
    ' ============================================================

    Private Sub AddRow(
        label As String,
        value As String,
        Optional strong As Boolean = False,
        Optional valueColor As Color = Nothing)

        Dim ln As New Line()

        ln.Label = label
        ln.Value = If(value, "")
        ln.Strong = strong
        ln.ValueColor = valueColor

        lines.Add(ln)

    End Sub

    Private Sub AddRule()

        Dim ln As New Line()
        ln.Rule = True

        lines.Add(ln)

    End Sub

    Private Sub BuildLines(
        controller As PaymentController,
        repair As Repair,
        payment As Payment,
        amountNow As Decimal)

        Dim total As Decimal = payment.AmountPaid
        Dim balance As Decimal = Math.Max(0D, repair.Cost - total)
        Dim status As String = PaymentController.ComputeStatus(repair.Cost, total)

        Dim device As String = controller.GetItemUsed(repair)
        Dim typeName As String = If(repair.DeviceType, "").Trim()

        If device = "-" Then
            device = "Not provided"
        ElseIf typeName <> "" AndAlso device <> typeName Then
            device = device & " (" & typeName & ")"
        End If

        AddRow("Receipt No.", receiptNo)
        AddRow("Date", payment.PaymentDate.ToString("MMM dd, yyyy  hh:mm tt"))
        AddRow("Ticket", "#" & repair.RepairId.ToString())
        AddRow("Customer", controller.GetCustomerName(repair.CustomerId))
        AddRow("Device", device)
        AddRow("Technician", PaymentController.TechnicianName(repair))

        AddRule()

        AddRow("Final cost", Money.Peso(repair.Cost))

        If justPaid AndAlso total - amountNow > 0D Then
            AddRow("Previously paid", Money.Peso(total - amountNow))
        End If

        If justPaid Then
            AddRow("This payment", Money.Peso(amountNow), True, Theme.NeonGreen)
        End If

        AddRow("Total paid", Money.Peso(total), Not justPaid, Theme.NeonGreen)

        AddRow("Balance", Money.Peso(balance), True,
               If(balance > 0D, Theme.NeonYellow, Theme.NeonGreen))

        AddRow("Status", status, False, NeonGridPaint.PayStatusColor(status))

        If Not String.IsNullOrWhiteSpace(payment.Notes) Then
            AddRule()
            AddRow("Notes", payment.Notes.Trim())
        End If

        AddRule()

        AddRow("Received by", AppSession.DisplayName)

    End Sub


    ' ============================================================
    ' DRAW RECEIPT (screen = dark, print = black on white)
    ' returns the height used
    ' ============================================================

    Private Shared Sub DrawText(
        g As Graphics,
        text As String,
        font As Font,
        color As Color,
        rect As RectangleF,
        format As StringFormat)

        Using brush As New SolidBrush(color)
            g.DrawString(text, font, brush, rect, format)
        End Using

    End Sub

    Private Function DrawReceipt(
        g As Graphics,
        width As Integer,
        print As Boolean) As Integer

        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim fore As Color = If(print, Color.Black, Theme.TextWhite)
        Dim muted As Color = If(print, Color.DimGray, Theme.TextMuted)
        Dim ruleColor As Color = If(print, Color.Gray, Theme.LineColor)
        Dim brand As Color = If(print, Color.Black, Theme.NeonCyan)

        Dim fmtCenter As New StringFormat()
        fmtCenter.Alignment = StringAlignment.Center

        Dim fmtLeft As New StringFormat()
        fmtLeft.LineAlignment = StringAlignment.Center
        fmtLeft.Trimming = StringTrimming.EllipsisCharacter
        fmtLeft.FormatFlags = StringFormatFlags.NoWrap

        Dim fmtRight As New StringFormat()
        fmtRight.Alignment = StringAlignment.Far
        fmtRight.LineAlignment = StringAlignment.Center
        fmtRight.Trimming = StringTrimming.EllipsisCharacter
        fmtRight.FormatFlags = StringFormatFlags.NoWrap

        Dim y As Integer = 18

        DrawText(g, "GADGET REPAIR CONTROL",
                 Theme.UiFont(12.0F, FontStyle.Bold), brand,
                 New RectangleF(0, y, width, 24), fmtCenter)
        y += 26

        DrawText(g, "PAYMENT RECEIPT",
                 Theme.UiFont(8.0F, FontStyle.Bold), muted,
                 New RectangleF(0, y, width, 16), fmtCenter)
        y += 24

        DrawRule(g, ruleColor, width, y)
        y += 14

        For Each ln As Line In lines

            If ln.Rule Then

                DrawRule(g, ruleColor, width, y + 6)
                y += 14

                Continue For

            End If

            Dim h As Integer = If(ln.Strong, 30, 24)

            DrawText(g, ln.Label, Theme.UiFont(8.5F), muted,
                     New RectangleF(PadX, y, 110, h), fmtLeft)

            Dim valueColor As Color =
                If(ln.ValueColor.IsEmpty OrElse print, fore, ln.ValueColor)

            DrawText(g, ln.Value,
                     Theme.UiFont(If(ln.Strong, 11.0F, 9.5F), FontStyle.Bold),
                     valueColor,
                     New RectangleF(PadX + 118, y, width - PadX * 2 - 118, h),
                     fmtRight)

            y += h

        Next

        DrawRule(g, ruleColor, width, y + 6)
        y += 14

        DrawText(g, "Thank you for your payment!",
                 Theme.UiFont(8.5F), muted,
                 New RectangleF(0, y + 4, width, 18), fmtCenter)

        fmtCenter.Dispose()
        fmtLeft.Dispose()
        fmtRight.Dispose()

        Return y + 36

    End Function

    Private Shared Sub DrawRule(
        g As Graphics,
        color As Color,
        width As Integer,
        y As Integer)

        Using pen As New Pen(color, 1.0F)
            pen.DashStyle = DashStyle.Dash
            g.DrawLine(pen, PadX, y, width - PadX, y)
        End Using

    End Sub


    ' ============================================================
    ' INTERFACE
    ' ============================================================

    Private Sub BuildInterface()

        Dim accent As Color = If(justPaid, Theme.NeonGreen, Theme.NeonCyan)

        ' accent strip
        Dim accentStrip As New Panel()
        accentStrip.Dock = DockStyle.Top
        accentStrip.Height = 2
        accentStrip.BackColor = accent


        ' header
        Dim header As New Panel()
        header.Dock = DockStyle.Top
        header.Height = 64
        header.BackColor = Theme.BackgroundColor

        Dim icon As New Panel()
        icon.Size = New Size(36, 36)
        icon.Location = New Point(PadX, 14)
        icon.BackColor = Theme.BackgroundColor

        AddHandler icon.Paint,
            Sub(sender As Object, e As PaintEventArgs)

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

                Using b As New SolidBrush(Color.FromArgb(40, accent))
                    e.Graphics.FillEllipse(b, 0, 0, 34, 34)
                End Using

                Using p As New Pen(accent, 1.4F)
                    e.Graphics.DrawEllipse(p, 0, 0, 34, 34)
                End Using

                Using p As New Pen(accent, 2.6F)
                    p.StartCap = LineCap.Round
                    p.EndCap = LineCap.Round
                    p.LineJoin = LineJoin.Round

                    e.Graphics.DrawLines(p, New Point() {
                        New Point(10, 18),
                        New Point(15, 23),
                        New Point(24, 12)})
                End Using

            End Sub

        Dim title As New Label()
        title.Text = If(justPaid, "Payment Successful", "Payment Receipt")
        title.AutoSize = True
        title.Location = New Point(PadX + 48, 12)
        title.Font = Theme.UiFont(14.0F, FontStyle.Bold)
        title.ForeColor = If(justPaid, Theme.NeonGreen, Theme.TextWhite)

        Dim subtitle As New Label()
        subtitle.Text = "Receipt " & receiptNo
        subtitle.AutoSize = True
        subtitle.Location = New Point(PadX + 49, 40)
        subtitle.Font = Theme.UiFont(8.5F)
        subtitle.ForeColor = Theme.TextMuted

        Dim btnClose As New NeonCloseButton()
        btnClose.Location = New Point(FormWidth - 54, 16)

        AddHandler btnClose.Click, AddressOf btnDone_Click

        Dim headerLine As New Panel()
        headerLine.Dock = DockStyle.Bottom
        headerLine.Height = 1
        headerLine.BackColor = Theme.LineColor

        header.Controls.Add(icon)
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

        btnPrint = New NeonButton()
        btnPrint.Text = "PRINT"
        btnPrint.Filled = False
        btnPrint.Accent = Theme.NeonCyan
        btnPrint.Size = New Size(90, 38)
        btnPrint.Top = 12

        AddHandler btnPrint.Click, AddressOf btnPrint_Click

        btnSave = New NeonButton()
        btnSave.Text = "SAVE"
        btnSave.Filled = False
        btnSave.Accent = Theme.TextMuted
        btnSave.Size = New Size(90, 38)
        btnSave.Top = 12

        AddHandler btnSave.Click, AddressOf btnSave_Click

        btnDone = New NeonButton()
        btnDone.Text = "DONE"
        btnDone.Filled = True
        btnDone.Accent = accent
        btnDone.Size = New Size(110, 38)
        btnDone.Top = 12

        AddHandler btnDone.Click, AddressOf btnDone_Click

        footerPanel.Controls.Add(footerLine)
        footerPanel.Controls.Add(btnPrint)
        footerPanel.Controls.Add(btnSave)
        footerPanel.Controls.Add(btnDone)

        AddHandler footerPanel.Resize,
            Sub(sender As Object, e As EventArgs) LayoutFooter()

        LayoutFooter()


        ' body: the receipt is drawn once into a picture
        Dim body As New Panel()
        body.Dock = DockStyle.Fill
        body.BackColor = Theme.BackgroundColor
        body.AutoScroll = True

        Dim paperHeight As Integer
        Dim paper As Bitmap = RenderPaper(paperHeight)

        pic = New PictureBox()
        pic.SizeMode = PictureBoxSizeMode.AutoSize
        pic.Image = paper
        pic.Location = New Point(21, 20)

        body.Controls.Add(pic)

        Me.Controls.Add(body)
        Me.Controls.Add(footerPanel)
        Me.Controls.Add(header)
        Me.Controls.Add(accentStrip)

        ' the window fits the receipt (scrolls when it is very long)
        Dim wanted As Integer = 2 + 64 + 62 + paperHeight + 40 + 2

        Me.Size = New Size(FormWidth, Math.Min(wanted, 700))

    End Sub

    Private Sub LayoutFooter()

        btnDone.Left = footerPanel.ClientSize.Width - 24 - btnDone.Width
        btnSave.Left = btnDone.Left - 10 - btnSave.Width
        btnPrint.Left = btnSave.Left - 10 - btnPrint.Width

    End Sub

    Private Function RenderPaper(ByRef height As Integer) As Bitmap

        ' measure first, then draw
        Using probe As New Bitmap(1, 1)
            Using g As Graphics = Graphics.FromImage(probe)
                height = DrawReceipt(g, PaperWidth, False)
            End Using
        End Using

        Dim bmp As New Bitmap(PaperWidth, height)

        Using g As Graphics = Graphics.FromImage(bmp)

            g.Clear(Theme.CardColor)

            DrawReceipt(g, PaperWidth, False)

            Using pen As New Pen(Theme.LineColor, 1.0F)
                g.DrawRectangle(pen, 0, 0, PaperWidth - 1, height - 1)
            End Using

        End Using

        Return bmp

    End Function


    ' ============================================================
    ' PRINT / SAVE / DONE
    ' ============================================================

    Private Sub btnPrint_Click(sender As Object, e As EventArgs)

        Try

            Using doc As New PrintDocument()

                doc.DocumentName = receiptNo

                AddHandler doc.PrintPage,
                    Sub(s As Object, a As PrintPageEventArgs)
                        DrawReceipt(a.Graphics, 330, True)
                        a.HasMorePages = False
                    End Sub

                doc.Print()

            End Using

        Catch ex As Exception

            MessageBox.Show(
                "Unable to print the receipt." &
                Environment.NewLine & Environment.NewLine &
                ex.Message & Environment.NewLine & Environment.NewLine &
                "You can use SAVE to keep a copy instead.",
                "Print",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)

        End Try

    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs)

        Try

            Using dlg As New SaveFileDialog()

                dlg.Filter = "Text file (*.txt)|*.txt"
                dlg.FileName = receiptNo & ".txt"

                If dlg.ShowDialog(Me) = DialogResult.OK Then

                    File.WriteAllText(dlg.FileName, BuildText(), Encoding.UTF8)

                    MessageBox.Show(
                        "Receipt saved.",
                        "Receipt",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)

                End If

            End Using

        Catch ex As Exception

            MessageBox.Show(
                "Unable to save the receipt." &
                Environment.NewLine & Environment.NewLine & ex.Message,
                "Receipt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)

        End Try

    End Sub

    Private Function BuildText() As String

        Dim sb As New StringBuilder()

        sb.AppendLine("GADGET REPAIR CONTROL")
        sb.AppendLine("PAYMENT RECEIPT")
        sb.AppendLine(New String("="c, 44))

        For Each ln As Line In lines

            If ln.Rule Then
                sb.AppendLine(New String("-"c, 44))
            Else
                sb.AppendLine(ln.Label.PadRight(18) & ln.Value)
            End If

        Next

        sb.AppendLine(New String("="c, 44))
        sb.AppendLine("Thank you for your payment!")

        Return sb.ToString()

    End Function

    Private Sub btnDone_Click(sender As Object, e As EventArgs)

        Me.DialogResult = DialogResult.OK
        Me.Close()

    End Sub

    Protected Overrides Function ProcessCmdKey(
        ByRef msg As Message,
        keyData As Keys) As Boolean

        If keyData = Keys.Escape OrElse keyData = Keys.Enter Then

            btnDone_Click(Me, EventArgs.Empty)

            Return True

        End If

        Return MyBase.ProcessCmdKey(msg, keyData)

    End Function

    Protected Overrides Sub Dispose(disposing As Boolean)

        If disposing AndAlso pic IsNot Nothing AndAlso pic.Image IsNot Nothing Then
            pic.Image.Dispose()
        End If

        MyBase.Dispose(disposing)

    End Sub

End Class