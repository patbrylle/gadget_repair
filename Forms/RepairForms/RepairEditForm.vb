Imports System
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Models

Namespace Forms

    Public Class RepairEditForm
        Inherits Form

        Private ReadOnly repairController As RepairController
        Private ReadOnly repairId As Integer
        Private loadFailed As Boolean = False

        ' follows the status combo; "Pending" for new tickets
        Private currentStatus As String = "Pending"

        Private lblTitle As Label
        Private lblSubtitle As Label
        Private headerPanel As Panel
        Private btnClose As NeonCloseButton
        Private headerStatus As NeonStatusPill

        Private cboCustomer As ComboBox
        Private cboTechnician As ComboBox
        Private cboDeviceType As ComboBox
        Private cboBrand As ComboBox
        Private txtModel As TextBox
        Private txtSerialNumber As TextBox
        Private dtpRepairDate As NeonDateTimeField
        Private txtProblem As TextBox
        Private txtRepairDescription As TextBox
        Private cboStatus As ComboBox
        Private txtCost As TextBox

        Private btnSave As NeonButton
        Private btnCancel As NeonButton
        Private bodyPanel As Panel

        Private dragging As Boolean = False
        Private dragOffset As Point

        ' layout (form 580 x 500, inner width = 578, content = 530)
        Private Const PadX As Integer = 24
        Private Const ContentWidth As Integer = 530
        Private Const HeaderHeight As Integer = 60
        Private Const FooterHeight As Integer = 62
        Private Const DescHeight As Integer = 72

        Private Const Col2A As Integer = 24
        Private Const Col2B As Integer = 295
        Private Const Width2 As Integer = 259

        Private Const Col3A As Integer = 24
        Private Const Col3B As Integer = 205
        Private Const Col3C As Integer = 386
        Private Const Width3 As Integer = 168

        ' ============================================================
        ' CONSTRUCTOR
        ' ============================================================

        Public Sub New(controller As RepairController, id As Integer)

            repairController = controller
            repairId = id

            InitializeForm()
            BuildInterface()
            ApplyRoleRules()

            If loadFailed Then Return

            If repairId > 0 Then
                LoadRepair()
            Else
                PrepareNewRepair()
            End If

        End Sub

        Private Sub InitializeForm()

            Me.Text = If(repairId = 0, "New Repair", "Edit Repair")
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.None
            Me.Size = New Size(580, If(repairId = 0, 440, 500))
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
        ' MAIN INTERFACE
        ' ============================================================

        Private Sub BuildInterface()

            ' accent strip
            Dim accentStrip As New Panel()
            accentStrip.Dock = DockStyle.Top
            accentStrip.Height = 2
            accentStrip.BackColor = Theme.NeonPurple


            ' header
            headerPanel = New Panel()
            headerPanel.Dock = DockStyle.Top
            headerPanel.Height = HeaderHeight
            headerPanel.BackColor = Theme.BackgroundColor

            lblTitle = New Label()
            lblTitle.AutoSize = True
            lblTitle.Location = New Point(PadX, 10)
            lblTitle.Font = Theme.UiFont(13.5F, FontStyle.Bold)
            lblTitle.ForeColor = Theme.TextWhite
            lblTitle.Text = If(repairId = 0, "New Repair", "Edit Repair")

            lblSubtitle = New Label()
            lblSubtitle.AutoSize = True
            lblSubtitle.Location = New Point(PadX + 1, 36)
            lblSubtitle.Font = Theme.UiFont(8.5F)
            lblSubtitle.ForeColor = Theme.TextMuted
            lblSubtitle.Text = If(repairId = 0,
                "Create a new repair ticket",
                "Update repair ticket #" & repairId.ToString())

            btnClose = New NeonCloseButton()
            btnClose.BackColor = Theme.BackgroundColor
            btnClose.Location = New Point(534, 14)

            AddHandler btnClose.Click, AddressOf btnCancel_Click

            ' status pill (follows the status field)
            headerStatus = New NeonStatusPill()
            headerStatus.BackColor = Theme.BackgroundColor
            headerStatus.SetStatus(currentStatus.ToUpperInvariant(), Theme.StatusColor(currentStatus))
            headerStatus.Top = (HeaderHeight - headerStatus.Height) \ 2

            Dim headerLine As New Panel()
            headerLine.Dock = DockStyle.Bottom
            headerLine.Height = 1
            headerLine.BackColor = Theme.LineColor

            headerPanel.Controls.Add(lblTitle)
            headerPanel.Controls.Add(lblSubtitle)
            headerPanel.Controls.Add(headerStatus)
            headerPanel.Controls.Add(btnClose)
            headerPanel.Controls.Add(headerLine)

            PositionHeaderControls()

            AddHandler headerPanel.Resize,
                Sub(sender As Object, e As EventArgs) PositionHeaderControls()

            ' drag the form by the header
            For Each ctl As Control In New Control() {headerPanel, lblTitle, lblSubtitle}
                AddHandler ctl.MouseDown, AddressOf Header_MouseDown
                AddHandler ctl.MouseMove, AddressOf Header_MouseMove
                AddHandler ctl.MouseUp, AddressOf Header_MouseUp
            Next


            ' footer
            Dim footer As New Panel()
            footer.Dock = DockStyle.Bottom
            footer.Height = FooterHeight
            footer.BackColor = Theme.BackgroundColor

            Dim footerLine As New Panel()
            footerLine.Dock = DockStyle.Top
            footerLine.Height = 1
            footerLine.BackColor = Theme.LineColor

            btnCancel = New NeonButton()
            btnCancel.Text = "CANCEL"
            btnCancel.Filled = False
            btnCancel.Accent = Theme.TextMuted
            btnCancel.Size = New Size(100, 38)
            btnCancel.Top = 12

            AddHandler btnCancel.Click, AddressOf btnCancel_Click

            btnSave = New NeonButton()
            btnSave.Text = If(repairId = 0, "CREATE REPAIR", "SAVE CHANGES")
            btnSave.Filled = True
            btnSave.Accent = Theme.NeonPurple
            btnSave.Size = New Size(160, 38)
            btnSave.Top = 12

            AddHandler btnSave.Click, AddressOf btnSave_Click

            footer.Controls.Add(footerLine)
            footer.Controls.Add(btnCancel)
            footer.Controls.Add(btnSave)

            AddHandler footer.Resize,
                Sub(sender As Object, e As EventArgs)

                    btnSave.Left = footer.ClientSize.Width - PadX - btnSave.Width
                    btnCancel.Left = btnSave.Left - 10 - btnCancel.Width

                End Sub


            ' body
            bodyPanel = New Panel()
            bodyPanel.Dock = DockStyle.Fill
            bodyPanel.BackColor = Theme.BackgroundColor
            bodyPanel.AutoScroll = True

            ' fill first, then edge-docked controls (last added = topmost)
            Me.Controls.Add(bodyPanel)
            Me.Controls.Add(footer)
            Me.Controls.Add(headerPanel)
            Me.Controls.Add(accentStrip)


            ' row 1: customer / technician
            cboCustomer = CreateComboBox()
            AddField(bodyPanel, "CUSTOMER", cboCustomer, Col2A, 18, Width2)

            cboTechnician = CreateComboBox()
            AddField(bodyPanel, "ASSIGNED TECHNICIAN", cboTechnician, Col2B, 18, Width2)


            ' row 2: device type / brand / model
            cboDeviceType = CreateComboBox()

            For Each t As String In New String() {
                "Laptop", "Desktop", "Smartphone", "Tablet",
                "Printer", "Monitor", "Other"}
                cboDeviceType.Items.Add(t)
            Next

            AddField(bodyPanel, "DEVICE TYPE", cboDeviceType, Col3A, 80, Width3)

            cboBrand = CreateComboBox()

            For Each b As String In New String() {
                "Acer", "Asus", "Dell", "HP", "Lenovo", "MSI", "Apple",
                "Samsung", "Xiaomi", "Huawei", "Oppo", "Vivo", "Realme",
                "Canon", "Epson", "Brother", "LG", "Sony", "Other"}
                cboBrand.Items.Add(b)
            Next

            AddField(bodyPanel, "BRAND", cboBrand, Col3B, 80, Width3)

            txtModel = CreateTextBox()
            AddField(bodyPanel, "MODEL", txtModel, Col3C, 80, Width3)


            ' row 3: serial number / repair date
            txtSerialNumber = CreateTextBox()
            AddField(bodyPanel, "SERIAL NUMBER", txtSerialNumber, Col2A, 142, Width2)

            AddCaption(bodyPanel, "REPAIR DATE", Col2B, 142)

            dtpRepairDate = New NeonDateTimeField()
            dtpRepairDate.Location = New Point(Col2B, 142 + CaptionGap)
            dtpRepairDate.Size = New Size(Width2, HostHeight)

            bodyPanel.Controls.Add(dtpRepairDate)


            ' row 4: problem (+ repair description when editing)
            txtProblem = CreateMultiLineTextBox()

            AddField(bodyPanel, "PROBLEM DESCRIPTION", txtProblem,
                     Col2A, 204,
                     If(repairId = 0, ContentWidth, Width2),
                     DescHeight)

            txtRepairDescription = CreateMultiLineTextBox()

            cboStatus = CreateComboBox()

            ' technicians only pick these; the current status is added when loaded
            For Each s As String In RepairController.TechnicianStatuses
                cboStatus.Items.Add(s)
            Next

            AddHandler cboStatus.SelectedIndexChanged, AddressOf Status_Changed

            txtCost = CreateTextBox()

            If repairId > 0 Then

                AddField(bodyPanel, "REPAIR DESCRIPTION", txtRepairDescription,
                         Col2B, 204, Width2, DescHeight)

                AddField(bodyPanel, "STATUS", cboStatus, Col2A, 298, Width2)

                AddField(bodyPanel, "FINAL COST (PHP)", txtCost, Col2B, 298, Width2)

            End If

            LoadCustomers()
            LoadTechnicians()

        End Sub

        ' ============================================================
        ' HEADER (X button + status pill, right aligned)
        ' ============================================================

        Private Sub PositionHeaderControls()

            If headerPanel Is Nothing Then Return

            btnClose.Left = headerPanel.ClientSize.Width - btnClose.Width - 14
            headerStatus.Left = btnClose.Left - 10 - headerStatus.Width

        End Sub

        Private Sub RefreshHeaderStatus()

            headerStatus.SetStatus(
                currentStatus.ToUpperInvariant(),
                Theme.StatusColor(currentStatus))

            PositionHeaderControls()

        End Sub

        ' ============================================================
        ' CONTROL HELPERS (AddField / AddCaption come from NeonLayout)
        ' ============================================================

        Private Function CreateTextBox() As TextBox

            Dim box As New TextBox()

            box.BorderStyle = BorderStyle.None
            box.BackColor = Theme.InputColor
            box.ForeColor = Theme.TextWhite
            box.Font = Theme.UiFont(9.5F)

            Return box

        End Function

        Private Function CreateMultiLineTextBox() As TextBox

            Dim box = CreateTextBox()

            box.Multiline = True
            box.ScrollBars = ScrollBars.Vertical

            Return box

        End Function

        Private Function CreateComboBox() As ComboBox

            Dim combo As New NeonComboBox()

            combo.Font = Theme.UiFont(9.5F)

            Return combo

        End Function

        ' select a value (adds it if the list does not have it yet)
        Private Sub SelectOrAdd(combo As ComboBox, value As String)

            If String.IsNullOrWhiteSpace(value) Then
                combo.SelectedIndex = -1
                Return
            End If

            Dim index As Integer = combo.FindStringExact(value.Trim())

            If index < 0 Then
                index = combo.Items.Add(value.Trim())
            End If

            combo.SelectedIndex = index

        End Sub

        ' ============================================================
        ' LOAD DROPDOWNS
        ' ============================================================

        Private Sub LoadCustomers()

            cboCustomer.Items.Clear()

            For Each customer As Customer In repairController.GetCustomers()
                cboCustomer.Items.Add(
                    New CustomerItem(customer.CustomerId, customer.CustomerName))
            Next

        End Sub

        Private Sub LoadTechnicians()

            cboTechnician.Items.Clear()

            cboTechnician.Items.Add(New TechnicianItem(Nothing, "Unassigned"))

            For Each technician As Technician In repairController.GetTechnicians()
                cboTechnician.Items.Add(
                    New TechnicianItem(technician.TechnicianId, technician.TechnicianName))
            Next

            cboTechnician.SelectedIndex = 0

        End Sub

        ' ============================================================
        ' ROLE RULES
        '   Admin      : customer, technician, device, brand, model,
        '                serial, date, problem
        '   Technician : status, final cost, repair description
        ' ============================================================

        Private Sub ApplyRoleRules()

            Dim isAdmin As Boolean = AppSession.IsAdmin
            Dim isTech As Boolean = AppSession.IsTechnician

            If repairId = 0 AndAlso Not isAdmin Then

                loadFailed = True

                MessageBox.Show(
                    "Only an administrator can create repair tickets.",
                    "New Repair",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

                Return

            End If

            If Not isAdmin Then
                LockField(cboCustomer)
                LockField(cboTechnician)
                LockField(cboDeviceType)
                LockField(cboBrand)
                LockField(txtModel)
                LockField(txtSerialNumber)
                LockField(dtpRepairDate)
                LockField(txtProblem)
            End If

            If Not isTech Then
                LockField(cboStatus)
                LockField(txtCost)
                LockField(txtRepairDescription)
            End If

            If isAdmin Then

                lblSubtitle.Text = If(repairId = 0,
                    "Record the device and the problem, then assign a technician",
                    "Ticket #" & repairId.ToString() &
                    " - device details and technician assignment")

            ElseIf isTech Then

                lblSubtitle.Text =
                    "Ticket #" & repairId.ToString() &
                    " - update the status, final cost and repair notes"

            End If

        End Sub

        Private Sub LockField(c As Control)

            c.ForeColor = Theme.TextMuted

            Dim tb = TryCast(c, TextBox)
            If tb IsNot Nothing Then tb.ReadOnly = True

            Dim cb = TryCast(c, NeonComboBox)
            If cb IsNot Nothing Then cb.Locked = True

            Dim dt = TryCast(c, NeonDateTimeField)
            If dt IsNot Nothing Then dt.Locked = True

        End Sub

        Private Sub Status_Changed(sender As Object, e As EventArgs)

            If cboStatus.SelectedItem Is Nothing Then Return

            currentStatus = cboStatus.SelectedItem.ToString()

            RefreshHeaderStatus()

        End Sub

        Private Sub ShowWarning(message As String, target As Control)

            MessageBox.Show(
                message,
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)

            If target IsNot Nothing Then target.Focus()

        End Sub

        ' ============================================================
        ' NEW REPAIR
        ' ============================================================

        Private Sub PrepareNewRepair()

            currentStatus = "Pending"

            SelectOrAdd(cboStatus, currentStatus)
            txtCost.Text = "0.00"

            RefreshHeaderStatus()

            dtpRepairDate.Value = DateTime.Now

        End Sub

        ' ============================================================
        ' LOAD EXISTING REPAIR
        ' ============================================================

        Private Sub LoadRepair()

            Dim repair As Repair = repairController.GetRepair(repairId)

            If repair Is Nothing Then

                loadFailed = True

                MessageBox.Show(
                    "Repair record not found.",
                    "Edit Repair",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

                Return

            End If

            ' customer
            For Each item As CustomerItem In cboCustomer.Items
                If item.Id.HasValue AndAlso item.Id.Value = repair.CustomerId Then
                    cboCustomer.SelectedItem = item
                    Exit For
                End If
            Next

            ' technician
            For Each item As TechnicianItem In cboTechnician.Items
                If item.Id.HasValue AndAlso
                   repair.TechnicianId.HasValue AndAlso
                   item.Id.Value = repair.TechnicianId.Value Then
                    cboTechnician.SelectedItem = item
                    Exit For
                End If
            Next

            ' device
            SelectOrAdd(cboDeviceType, repair.DeviceType)
            SelectOrAdd(cboBrand, repair.Brand)

            txtModel.Text = If(repair.Model, "")
            txtSerialNumber.Text = If(repair.SerialNumber, "")

            ' descriptions
            txtProblem.Text = If(repair.ProblemDescription, "")
            txtRepairDescription.Text = If(repair.RepairDescription, "")

            ' status and cost
            If Not String.IsNullOrWhiteSpace(repair.Status) Then
                currentStatus = repair.Status
            End If

            SelectOrAdd(cboStatus, currentStatus)
            RefreshHeaderStatus()

            txtCost.Text = repair.Cost.ToString("0.00", CultureInfo.InvariantCulture)

            ' date
            dtpRepairDate.Value = repair.RepairDate

        End Sub

        ' ============================================================
        ' SAVE
        ' ============================================================

        Private Sub btnSave_Click(sender As Object, e As EventArgs)

            Dim customerItem = TryCast(cboCustomer.SelectedItem, CustomerItem)

            If customerItem Is Nothing OrElse Not customerItem.Id.HasValue Then
                ShowWarning("Please select a customer.", cboCustomer)
                Return
            End If

            Dim technicianId As Integer? = Nothing

            Dim technicianItem = TryCast(cboTechnician.SelectedItem, TechnicianItem)

            If technicianItem IsNot Nothing AndAlso technicianItem.Id.HasValue Then
                technicianId = technicianItem.Id.Value
            End If

            Dim costValue As Decimal? = Nothing

            If AppSession.IsAdmin Then

                If String.IsNullOrWhiteSpace(cboDeviceType.Text) Then
                    ShowWarning("Please select the device type.", cboDeviceType)
                    Return
                End If

                If String.IsNullOrWhiteSpace(txtProblem.Text) Then
                    ShowWarning("Please describe the problem.", txtProblem)
                    Return
                End If

            ElseIf AppSession.IsTechnician Then

                Dim parsed As Decimal

                If Not Decimal.TryParse(
                        txtCost.Text.Trim(),
                        NumberStyles.AllowDecimalPoint Or NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture,
                        parsed) OrElse parsed < 0D Then

                    ShowWarning(
                        "Please enter a valid final cost, for example 1500.00.",
                        txtCost)

                    Return

                End If

                If (currentStatus = "Completed" OrElse currentStatus = "Delivered") AndAlso
                   parsed <= 0D Then

                    ShowWarning(
                        "Enter the final cost before marking the repair as " &
                        currentStatus.ToLower() & ".",
                        txtCost)

                    Return

                End If

                costValue = parsed

            End If

            Try

                If repairId = 0 Then

                    ' new tickets always start as Pending with no cost
                    repairController.CreateRepair(
                        customerItem.Id.Value,
                        technicianId,
                        cboDeviceType.Text.Trim(),
                        cboBrand.Text.Trim(),
                        txtModel.Text.Trim(),
                        txtSerialNumber.Text.Trim(),
                        txtProblem.Text.Trim(),
                        txtRepairDescription.Text.Trim(),
                        "Pending",
                        dtpRepairDate.Value)

                Else

                    ' the controller applies only what this role may change
                    repairController.UpdateRepair(
                        repairId,
                        customerItem.Id.Value,
                        technicianId,
                        cboDeviceType.Text.Trim(),
                        cboBrand.Text.Trim(),
                        txtModel.Text.Trim(),
                        txtSerialNumber.Text.Trim(),
                        txtProblem.Text.Trim(),
                        txtRepairDescription.Text.Trim(),
                        currentStatus,
                        cost:=costValue,
                        repairDate:=dtpRepairDate.Value)

                End If

                MessageBox.Show(
                    If(repairId = 0,
                       "Repair ticket created successfully.",
                       "Repair ticket updated successfully."),
                    "Repair",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)

                Me.DialogResult = DialogResult.OK
                Me.Close()

            Catch ex As UnauthorizedAccessException

                MessageBox.Show(ex.Message, "Not Allowed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning)

            Catch ex As ArgumentException

                MessageBox.Show(ex.Message, "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning)

            Catch ex As InvalidOperationException

                MessageBox.Show(ex.Message, "Not Allowed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning)

            Catch ex As Exception

                MessageBox.Show(
                    "Unable to save repair." &
                    Environment.NewLine & Environment.NewLine & ex.Message,
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

            End Try

        End Sub

        Private Sub btnCancel_Click(sender As Object, e As EventArgs)

            Me.DialogResult = DialogResult.Cancel
            Me.Close()

        End Sub

        ' ============================================================
        ' DRAG HEADER
        ' ============================================================

        Private Sub Header_MouseDown(sender As Object, e As MouseEventArgs)

            If e.Button <> MouseButtons.Left Then Return

            dragging = True

            dragOffset = New Point(
                System.Windows.Forms.Cursor.Position.X - Me.Left,
                System.Windows.Forms.Cursor.Position.Y - Me.Top)

        End Sub

        Private Sub Header_MouseMove(sender As Object, e As MouseEventArgs)

            If Not dragging Then Return

            Me.Location = New Point(
                System.Windows.Forms.Cursor.Position.X - dragOffset.X,
                System.Windows.Forms.Cursor.Position.Y - dragOffset.Y)

        End Sub

        Private Sub Header_MouseUp(sender As Object, e As MouseEventArgs)

            dragging = False

        End Sub

        ' ============================================================
        ' FORM BEHAVIOR
        ' ============================================================

        Protected Overrides Sub OnShown(e As EventArgs)

            MyBase.OnShown(e)

            ' close if the repair could not be loaded
            If loadFailed Then
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
            End If

        End Sub

        Protected Overrides Function ProcessCmdKey(
            ByRef msg As Message,
            keyData As Keys) As Boolean

            If keyData = Keys.Escape Then

                ' let an open dropdown list handle Esc first
                Dim combo = TryCast(Me.ActiveControl, ComboBox)

                If combo IsNot Nothing AndAlso combo.DroppedDown Then
                    Return MyBase.ProcessCmdKey(msg, keyData)
                End If

                Me.DialogResult = DialogResult.Cancel
                Me.Close()

                Return True

            End If

            Return MyBase.ProcessCmdKey(msg, keyData)

        End Function

        ' ============================================================
        ' COMBO ITEMS
        ' ============================================================

        Private Class CustomerItem

            Public ReadOnly Property Id As Integer?
            Public ReadOnly Property Name As String

            Public Sub New(id As Integer, name As String)
                Me.Id = id
                Me.Name = name
            End Sub

            Public Overrides Function ToString() As String
                Return Name
            End Function

        End Class

        Private Class TechnicianItem

            Public ReadOnly Property Id As Integer?
            Public ReadOnly Property Name As String

            Public Sub New(id As Integer?, name As String)
                Me.Id = id
                Me.Name = name
            End Sub

            Public Overrides Function ToString() As String
                Return Name
            End Function

        End Class

    End Class

End Namespace