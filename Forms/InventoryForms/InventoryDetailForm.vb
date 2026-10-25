Imports System
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Models

Namespace Forms

    ' view + edit + add + delete in one modal
    Public Class InventoryDetailForm
        Inherits Form

        Private ReadOnly inventoryController As InventoryController
        Private ReadOnly inventoryId As Integer

        Private item As Inventory
        Private editing As Boolean = False
        Private loadFailed As Boolean = False
        Private _changed As Boolean = False

        ' True when an item was added, edited or deleted, so the list reloads
        Public ReadOnly Property Changed As Boolean
            Get
                Return _changed
            End Get
        End Property

        Private lblTitle As Label
        Private lblSubtitle As Label
        Private header As Panel
        Private btnClose As NeonCloseButton
        Private statusPill As NeonStatusPill

        Private txtName As TextBox
        Private cboCategory As NeonComboBox
        Private txtQuantity As TextBox
        Private txtMinimum As TextBox
        Private txtUnitCost As TextBox
        Private txtSellingPrice As TextBox
        Private lblUsed As Label
        Private lblMessage As Label

        Private footerPanel As Panel
        Private btnDelete As NeonButton
        Private btnCancel As NeonButton
        Private btnPrimary As NeonButton

        ' layout (form 500 x 400, inner width = 498, content = 450)
        Private Const PadX As Integer = 24
        Private Const ContentWidth As Integer = 450
        Private Const ColA As Integer = 24
        Private Const ColB As Integer = 259
        Private Const WidthHalf As Integer = 215


        Public Sub New(controller As InventoryController, id As Integer)

            inventoryController = controller
            inventoryId = id

            InitializeForm()
            BuildInterface()

            If inventoryId > 0 Then
                LoadItem()
            Else
                PrepareNew()
            End If

        End Sub

        Private Sub InitializeForm()

            Me.Text = "Inventory Item"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.None
            Me.Size = New Size(500, 400)
            Me.MinimizeBox = False
            Me.MaximizeBox = False
            Me.ShowInTaskbar = False
            Me.Font = Theme.UiFont(9.0F)
            Me.DoubleBuffered = True

            Me.BackColor = Theme.LineColor
            Me.Padding = New Padding(1)

        End Sub


        ' ============================================================
        ' INTERFACE
        ' ============================================================

        Private Sub BuildInterface()

            Dim accentStrip As New Panel()
            accentStrip.Dock = DockStyle.Top
            accentStrip.Height = 2
            accentStrip.BackColor = Theme.NeonPurple

            ' header
            header = New Panel()
            header.Dock = DockStyle.Top
            header.Height = 64
            header.BackColor = Theme.BackgroundColor

            lblTitle = New Label()
            lblTitle.Text = If(inventoryId = 0, "New Item", "Inventory Item")
            lblTitle.AutoSize = True
            lblTitle.Location = New Point(PadX, 12)
            lblTitle.Font = Theme.UiFont(14.0F, FontStyle.Bold)
            lblTitle.ForeColor = Theme.TextWhite

            lblSubtitle = New Label()
            lblSubtitle.AutoSize = True
            lblSubtitle.Location = New Point(PadX + 1, 40)
            lblSubtitle.Font = Theme.UiFont(8.5F)
            lblSubtitle.ForeColor = Theme.TextMuted

            btnClose = New NeonCloseButton()
            btnClose.BackColor = Theme.BackgroundColor
            btnClose.Location = New Point(454, 16)

            AddHandler btnClose.Click, AddressOf btnClose_Click

            statusPill = New NeonStatusPill()
            statusPill.BackColor = Theme.BackgroundColor
            statusPill.Top = (header.Height - statusPill.Height) \ 2

            Dim headerLine As New Panel()
            headerLine.Dock = DockStyle.Bottom
            headerLine.Height = 1
            headerLine.BackColor = Theme.LineColor

            header.Controls.Add(lblTitle)
            header.Controls.Add(lblSubtitle)
            header.Controls.Add(statusPill)
            header.Controls.Add(btnClose)
            header.Controls.Add(headerLine)

            AddHandler header.Resize,
                Sub(sender As Object, e As EventArgs) PositionHeader()

            MakeDraggable(Me, header, lblTitle, lblSubtitle)

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
            btnDelete.Size = New Size(110, 38)
            btnDelete.Location = New Point(PadX, 12)

            AddHandler btnDelete.Click, AddressOf btnDelete_Click

            btnCancel = New NeonButton()
            btnCancel.Filled = False
            btnCancel.Accent = Theme.TextMuted
            btnCancel.Size = New Size(100, 38)
            btnCancel.Top = 12

            AddHandler btnCancel.Click, AddressOf btnCancel_Click

            btnPrimary = New NeonButton()
            btnPrimary.Filled = True
            btnPrimary.Accent = Theme.NeonPurple
            btnPrimary.Size = New Size(150, 38)
            btnPrimary.Top = 12

            AddHandler btnPrimary.Click, AddressOf btnPrimary_Click

            footerPanel.Controls.Add(footerLine)
            footerPanel.Controls.Add(btnDelete)
            footerPanel.Controls.Add(btnCancel)
            footerPanel.Controls.Add(btnPrimary)

            AddHandler footerPanel.Resize,
                Sub(sender As Object, e As EventArgs) LayoutFooter()

            ' body
            Dim body As New Panel()
            body.Dock = DockStyle.Fill
            body.BackColor = Theme.BackgroundColor

            Me.Controls.Add(body)
            Me.Controls.Add(footerPanel)
            Me.Controls.Add(header)
            Me.Controls.Add(accentStrip)

            txtName = NewTextBox()
            AddField(body, "ITEM NAME", txtName, PadX, 16, ContentWidth)

            cboCategory = New NeonComboBox()
            cboCategory.Font = Theme.UiFont(9.5F)

            For Each c As String In InventoryController.CategoryList
                cboCategory.Items.Add(c)
            Next

            AddField(body, "CATEGORY", cboCategory, ColA, 68, WidthHalf)

            txtQuantity = NewTextBox()
            AddField(body, "QUANTITY IN STOCK", txtQuantity, ColB, 68, WidthHalf)

            txtUnitCost = NewTextBox()
            AddField(body, "UNIT COST (PHP)", txtUnitCost, ColA, 120, WidthHalf)

            txtSellingPrice = NewTextBox()
            AddField(body, "SELLING PRICE (PHP)", txtSellingPrice, ColB, 120, WidthHalf)

            txtMinimum = NewTextBox()
            AddField(body, "LOW STOCK ALERT AT", txtMinimum, ColA, 172, WidthHalf)

            lblUsed = AddValue(body, "USED IN REPAIRS", "-", ColB, 172, WidthHalf)

            lblMessage = New Label()
            lblMessage.AutoSize = False
            lblMessage.Size = New Size(ContentWidth, 20)
            lblMessage.Location = New Point(PadX, 232)
            lblMessage.Font = Theme.UiFont(8.5F)
            lblMessage.ForeColor = Theme.NeonPink
            lblMessage.TextAlign = ContentAlignment.MiddleLeft
            body.Controls.Add(lblMessage)

            AddHandler txtQuantity.TextChanged,
                Sub(sender As Object, e As EventArgs) RefreshPill()

            AddHandler txtMinimum.TextChanged,
                Sub(sender As Object, e As EventArgs) RefreshPill()

        End Sub

        Private Sub PositionHeader()

            If header Is Nothing OrElse statusPill Is Nothing Then Return

            btnClose.Left = header.ClientSize.Width - btnClose.Width - 14
            statusPill.Left = btnClose.Left - 10 - statusPill.Width

        End Sub

        Private Sub LayoutFooter()

            btnPrimary.Left = footerPanel.ClientSize.Width - PadX - btnPrimary.Width
            btnCancel.Left = btnPrimary.Left - 10 - btnCancel.Width

        End Sub

        ' stock status pill follows the quantity while typing
        Private Sub RefreshPill()

            Dim quantity As Integer = 0
            Dim minimum As Integer = 0

            Integer.TryParse(txtQuantity.Text.Trim(), quantity)
            Integer.TryParse(txtMinimum.Text.Trim(), minimum)

            Dim status As String = InventoryController.StockStatus(quantity, minimum)

            statusPill.SetStatus(
                status.ToUpperInvariant(),
                NeonGridPaint.StockStatusColor(status))

            PositionHeader()

        End Sub

        ' view mode locks the fields, edit mode unlocks them
        Private Sub SetEditing(enable As Boolean)

            editing = enable

            For Each box As TextBox In New TextBox() {
                txtName, txtQuantity, txtMinimum, txtUnitCost, txtSellingPrice}

                box.ReadOnly = Not enable
                box.ForeColor = If(enable, Theme.TextWhite, Theme.TextMuted)

            Next

            cboCategory.Locked = Not enable

            btnDelete.Visible = (inventoryId > 0 AndAlso Not enable)
            btnCancel.Text = If(enable, "CANCEL", "CLOSE")

            btnPrimary.Text =
                If(enable,
                   If(inventoryId = 0, "ADD ITEM", "SAVE CHANGES"),
                   "EDIT ITEM")

            lblSubtitle.Text =
                If(inventoryId = 0,
                   "Add a new part to your inventory",
                   If(enable, "Update the item details",
                      "Item #" & inventoryId.ToString()))

            lblMessage.Text = ""

            LayoutFooter()

        End Sub


        ' ============================================================
        ' VALUES
        ' ============================================================

        Private Sub PrepareNew()

            txtName.Text = ""
            cboCategory.SelectedIndex = -1
            txtQuantity.Text = "0"
            txtMinimum.Text = "5"
            txtUnitCost.Text = "0.00"
            txtSellingPrice.Text = "0.00"
            lblUsed.Text = "-"

            SetEditing(True)
            RefreshPill()

        End Sub

        Private Sub LoadItem()

            item = inventoryController.GetInventoryItem(inventoryId)

            If item Is Nothing Then

                loadFailed = True

                MessageBox.Show(
                    "The inventory item could not be found.",
                    "Inventory",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

                Return

            End If

            FillValues()
            SetEditing(False)

        End Sub

        Private Sub FillValues()

            txtName.Text = item.ItemName
            SelectOrAddCategory(item.Category)
            txtQuantity.Text = item.Quantity.ToString()
            txtMinimum.Text = item.MinimumStock.ToString()

            txtUnitCost.Text =
                item.UnitCost.ToString("0.00", CultureInfo.InvariantCulture)

            txtSellingPrice.Text =
                item.SellingPrice.ToString("0.00", CultureInfo.InvariantCulture)

            Dim used As Integer = 0

            inventoryController.GetUsageLookup().TryGetValue(item.InventoryId, used)

            lblUsed.Text = used.ToString() & " pcs"

            RefreshPill()

        End Sub

        ' an old category that is not in the fixed list is added so it still shows
        Private Sub SelectOrAddCategory(value As String)

            If String.IsNullOrWhiteSpace(value) Then
                cboCategory.SelectedIndex = -1
                Return
            End If

            Dim index As Integer = cboCategory.FindStringExact(value.Trim())

            If index < 0 Then
                index = cboCategory.Items.Add(value.Trim())
            End If

            cboCategory.SelectedIndex = index

        End Sub

        Private Sub ShowMessage(text As String, target As Control)

            lblMessage.Text = text

            If target IsNot Nothing Then target.Focus()

        End Sub


        ' ============================================================
        ' ACTIONS
        ' ============================================================

        Private Sub btnPrimary_Click(sender As Object, e As EventArgs)

            If editing Then
                SaveItem()
            Else
                SetEditing(True)
                txtName.Focus()
                txtName.SelectAll()
            End If

        End Sub

        Private Sub SaveItem()

            lblMessage.Text = ""

            Dim itemName As String = txtName.Text.Trim()

            If itemName = "" Then
                ShowMessage("Please enter the item name.", txtName)
                Return
            End If

            If cboCategory.SelectedIndex < 0 Then
                ShowMessage("Please choose a category.", cboCategory)
                Return
            End If

            Dim quantity As Integer
            Dim minimum As Integer
            Dim unitCost As Decimal
            Dim sellingPrice As Decimal

            If Not Integer.TryParse(
                txtQuantity.Text.Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, quantity) OrElse quantity < 0 Then

                ShowMessage("Quantity must be a whole number, 0 or more.", txtQuantity)
                Return

            End If

            If Not Integer.TryParse(
                txtMinimum.Text.Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, minimum) OrElse minimum < 0 Then

                ShowMessage("The alert level must be a whole number, 0 or more.", txtMinimum)
                Return

            End If

            Dim money As NumberStyles =
                NumberStyles.AllowDecimalPoint Or NumberStyles.AllowThousands

            If Not Decimal.TryParse(
                txtUnitCost.Text.Trim(), money,
                CultureInfo.InvariantCulture, unitCost) OrElse unitCost < 0D Then

                ShowMessage("Unit cost must be a valid amount.", txtUnitCost)
                Return

            End If

            If Not Decimal.TryParse(
                txtSellingPrice.Text.Trim(), money,
                CultureInfo.InvariantCulture, sellingPrice) OrElse sellingPrice < 0D Then

                ShowMessage("Selling price must be a valid amount.", txtSellingPrice)
                Return

            End If

            Try

                If inventoryId > 0 Then

                    If Not inventoryController.UpdateInventory(
                        inventoryId, itemName, cboCategory.Text,
                        quantity, unitCost, sellingPrice, minimum) Then

                        ShowMessage("The inventory item could not be found.", Nothing)
                        Return

                    End If

                Else

                    inventoryController.CreateInventory(
                        itemName, cboCategory.Text,
                        quantity, unitCost, sellingPrice, minimum)

                End If

                _changed = True

                MessageBox.Show(
                    "Inventory item saved successfully.",
                    "Inventory",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)

                If inventoryId = 0 Then

                    Me.DialogResult = DialogResult.OK
                    Me.Close()

                Else

                    item = inventoryController.GetInventoryItem(inventoryId)
                    FillValues()
                    SetEditing(False)

                End If

            Catch ex As Exception

                ShowMessage(ex.Message, Nothing)

            End Try

        End Sub

        Private Sub btnCancel_Click(sender As Object, e As EventArgs)

            If editing AndAlso inventoryId > 0 Then

                ' back to view mode with the saved values
                FillValues()
                SetEditing(False)

            Else

                CloseForm()

            End If

        End Sub

        Private Sub btnClose_Click(sender As Object, e As EventArgs)
            CloseForm()
        End Sub

        Private Sub CloseForm()

            Me.DialogResult = If(_changed, DialogResult.OK, DialogResult.Cancel)
            Me.Close()

        End Sub

        Private Sub btnDelete_Click(sender As Object, e As EventArgs)

            If item Is Nothing Then Return

            If MessageBox.Show(
                "Delete " & item.ItemName & "?" &
                Environment.NewLine & Environment.NewLine &
                "Repairs that already used this item keep its name and price." &
                Environment.NewLine &
                "This action cannot be undone.",
                "Delete Inventory Item",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) <> DialogResult.Yes Then

                Return

            End If

            Try

                If inventoryController.DeleteInventory(inventoryId) Then

                    _changed = True
                    Me.DialogResult = DialogResult.OK
                    Me.Close()

                Else

                    ShowMessage("The inventory item could not be found.", Nothing)

                End If

            Catch ex As Exception

                ShowMessage(ex.Message, Nothing)

            End Try

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

            If editing Then txtName.Focus()

        End Sub

        Protected Overrides Function ProcessCmdKey(
            ByRef msg As Message,
            keyData As Keys) As Boolean

            If keyData = Keys.Escape Then

                ' let an open dropdown list handle Esc first
                If cboCategory.DroppedDown Then
                    Return MyBase.ProcessCmdKey(msg, keyData)
                End If

                btnCancel_Click(Me, EventArgs.Empty)

                Return True

            End If

            If keyData = Keys.Enter AndAlso editing AndAlso
               Not cboCategory.DroppedDown Then

                SaveItem()

                Return True

            End If

            Return MyBase.ProcessCmdKey(msg, keyData)

        End Function

    End Class

End Namespace