Imports System
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Models

Namespace Forms

    ' items used on one repair: the technician adds / removes, the admin views
    Public Class RepairPartsForm
        Inherits Form

        Private ReadOnly partController As RepairPartController
        Private ReadOnly ticket As Repair
        Private ReadOnly canEdit As Boolean

        Private _changed As Boolean = False

        ' True when items were added or removed, so the caller reloads
        Public ReadOnly Property Changed As Boolean
            Get
                Return _changed
            End Get
        End Property

        Private cboItem As NeonComboBox
        Private txtQty As TextBox
        Private btnAdd As NeonButton
        Private btnRemove As NeonButton
        Private btnClose As NeonButton
        Private lblTotal As Label
        Private lblMessage As Label
        Private grid As SmoothDataGridView
        Private footerPanel As Panel

        Private Const PadX As Integer = 24

        Private Class ItemChoice

            Public ReadOnly Property Id As Integer
            Public ReadOnly Property Text As String

            Public Sub New(itemId As Integer, itemText As String)
                Id = itemId
                Text = itemText
            End Sub

            Public Overrides Function ToString() As String
                Return Text
            End Function

        End Class


        Public Sub New(controller As RepairPartController, repair As Repair)

            partController = controller
            ticket = repair
            canEdit = RepairPartController.CanEditItems(repair)

            InitializeForm()
            BuildInterface()
            LoadItems()
            LoadParts()

        End Sub

        Private Sub InitializeForm()

            Me.Text = "Items Used"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.None
            Me.Size = New Size(520, 500)
            Me.MinimizeBox = False
            Me.MaximizeBox = False
            Me.ShowInTaskbar = False
            Me.Font = Theme.UiFont(9.0F)
            Me.DoubleBuffered = True

            Me.BackColor = Theme.LineColor
            Me.Padding = New Padding(1)

        End Sub

        Private Sub BuildInterface()

            Dim accentStrip As New Panel()
            accentStrip.Dock = DockStyle.Top
            accentStrip.Height = 2
            accentStrip.BackColor = Theme.NeonGreen

            ' header
            Dim header As New Panel()
            header.Dock = DockStyle.Top
            header.Height = 64
            header.BackColor = Theme.BackgroundColor

            Dim title As New Label()
            title.Text = "Items Used"
            title.AutoSize = True
            title.Location = New Point(PadX, 12)
            title.Font = Theme.UiFont(14.0F, FontStyle.Bold)
            title.ForeColor = Theme.TextWhite

            Dim subtitle As New Label()
            subtitle.Text = "Ticket #" & ticket.RepairId.ToString() &
                If(canEdit, " - choose the parts used in this repair",
                            " - parts used in this repair")
            subtitle.AutoSize = True
            subtitle.Location = New Point(PadX + 1, 40)
            subtitle.Font = Theme.UiFont(8.5F)
            subtitle.ForeColor = Theme.TextMuted

            Dim btnX As New NeonCloseButton()
            btnX.BackColor = Theme.BackgroundColor
            btnX.Location = New Point(474, 16)

            AddHandler btnX.Click, AddressOf btnClose_Click

            Dim headerLine As New Panel()
            headerLine.Dock = DockStyle.Bottom
            headerLine.Height = 1
            headerLine.BackColor = Theme.LineColor

            header.Controls.Add(title)
            header.Controls.Add(subtitle)
            header.Controls.Add(btnX)
            header.Controls.Add(headerLine)

            AddHandler header.Resize,
                Sub(sender As Object, e As EventArgs)
                    btnX.Left = header.ClientSize.Width - btnX.Width - 14
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

            lblTotal = New Label()
            lblTotal.AutoSize = False
            lblTotal.Location = New Point(PadX, 1)
            lblTotal.Size = New Size(205, 60)
            lblTotal.TextAlign = ContentAlignment.MiddleLeft
            lblTotal.Font = Theme.UiFont(9.0F, FontStyle.Bold)
            lblTotal.ForeColor = Theme.NeonGreen

            btnClose = New NeonButton()
            btnClose.Text = "CLOSE"
            btnClose.Filled = True
            btnClose.Accent = Theme.NeonGreen
            btnClose.Size = New Size(100, 38)
            btnClose.Top = 12

            AddHandler btnClose.Click, AddressOf btnClose_Click

            btnRemove = New NeonButton()
            btnRemove.Text = "REMOVE ITEM"
            btnRemove.Filled = False
            btnRemove.Accent = Theme.NeonPink
            btnRemove.ForeColor = Theme.NeonPink
            btnRemove.KeepAccentOnHover = True
            btnRemove.Size = New Size(140, 38)
            btnRemove.Top = 12
            btnRemove.Visible = canEdit

            AddHandler btnRemove.Click, AddressOf btnRemove_Click

            footerPanel.Controls.Add(footerLine)
            footerPanel.Controls.Add(lblTotal)
            footerPanel.Controls.Add(btnRemove)
            footerPanel.Controls.Add(btnClose)

            AddHandler footerPanel.Resize,
                Sub(sender As Object, e As EventArgs)
                    btnClose.Left = footerPanel.ClientSize.Width - PadX - btnClose.Width
                    btnRemove.Left = btnClose.Left - 10 - btnRemove.Width
                End Sub

            ' body
            Dim body As New Panel()
            body.Dock = DockStyle.Fill
            body.BackColor = Theme.BackgroundColor

            Me.Controls.Add(body)
            Me.Controls.Add(footerPanel)
            Me.Controls.Add(header)
            Me.Controls.Add(accentStrip)

            lblMessage = New Label()
            lblMessage.AutoSize = False
            lblMessage.Size = New Size(470, 20)
            lblMessage.Font = Theme.UiFont(8.5F)
            lblMessage.TextAlign = ContentAlignment.MiddleLeft

            Dim gridTop As Integer

            If canEdit Then

                cboItem = New NeonComboBox()
                cboItem.Font = Theme.UiFont(9.5F)
                AddField(body, "ITEM", cboItem, PadX, 14, 300)

                txtQty = NewTextBox()
                AddField(body, "QTY", txtQty, 336, 14, 70)

                btnAdd = New NeonButton()
                btnAdd.Text = "ADD"
                btnAdd.Filled = True
                btnAdd.Accent = Theme.NeonGreen
                btnAdd.Size = New Size(78, HostHeight)
                btnAdd.Location = New Point(418, 14 + CaptionGap)

                AddHandler btnAdd.Click, AddressOf btnAdd_Click

                body.Controls.Add(btnAdd)

                lblMessage.Location = New Point(PadX, 69)
                lblMessage.ForeColor = Theme.NeonPink

                gridTop = 96

            Else

                lblMessage.Text = "Only the assigned technician can change the items."
                lblMessage.Location = New Point(PadX, 12)
                lblMessage.ForeColor = Theme.TextMuted

                gridTop = 44

            End If

            body.Controls.Add(lblMessage)

            BuildGrid(body, gridTop)

        End Sub

        Private Sub BuildGrid(body As Panel, top As Integer)

            grid = New SmoothDataGridView()

            With grid

                .Location = New Point(PadX, top)
                .Size = New Size(470, 358 - top)
                .AutoGenerateColumns = False
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

                .ReadOnly = True
                .AllowUserToAddRows = False
                .AllowUserToDeleteRows = False
                .AllowUserToResizeRows = False
                .AllowUserToResizeColumns = False
                .AllowUserToOrderColumns = False
                .MultiSelect = False
                .RowHeadersVisible = False
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect

                .BackgroundColor = Theme.CardColor
                .GridColor = Theme.LineColor
                .BorderStyle = BorderStyle.None
                .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
                .ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
                .ColumnHeadersHeightSizeMode =
                    DataGridViewColumnHeadersHeightSizeMode.DisableResizing
                .EnableHeadersVisualStyles = False
                .ColumnHeadersHeight = 38
                .RowTemplate.Height = 38
                .DefaultCellStyle.NullValue = ""

            End With

            With grid.ColumnHeadersDefaultCellStyle

                .BackColor = Theme.CardColor
                .ForeColor = Theme.TextMuted
                .SelectionBackColor = Theme.CardColor
                .SelectionForeColor = Theme.TextMuted
                .Font = Theme.UiFont(8.0F, FontStyle.Bold)
                .Padding = New Padding(12, 0, 0, 0)

            End With

            With grid.DefaultCellStyle

                .BackColor = Theme.CardColor
                .ForeColor = Theme.TextWhite
                .SelectionBackColor = Theme.CardColor
                .SelectionForeColor = Theme.TextWhite
                .Font = Theme.UiFont(9.5F)
                .Padding = New Padding(12, 0, 0, 0)

            End With

            Dim colItem As New DataGridViewTextBoxColumn()
            colItem.Name = "colItem"
            colItem.HeaderText = "ITEM"
            colItem.FillWeight = 42.0F

            Dim colQty As New DataGridViewTextBoxColumn()
            colQty.Name = "colQty"
            colQty.HeaderText = "QTY"
            colQty.FillWeight = 12.0F

            Dim colPrice As New DataGridViewTextBoxColumn()
            colPrice.Name = "colPrice"
            colPrice.HeaderText = "PRICE"
            colPrice.FillWeight = 23.0F
            colPrice.DefaultCellStyle.ForeColor = Theme.TextMuted

            Dim colTotal As New DataGridViewTextBoxColumn()
            colTotal.Name = "colTotal"
            colTotal.HeaderText = "TOTAL"
            colTotal.FillWeight = 23.0F
            colTotal.DefaultCellStyle.ForeColor = Theme.NeonGreen

            grid.Columns.AddRange(
                New DataGridViewColumn() {colItem, colQty, colPrice, colTotal})

            ' the selected row is tinted, like the hovered row on the other pages
            AddHandler grid.CellPainting,
                Sub(sender As Object, e As DataGridViewCellPaintingEventArgs)

                    If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

                    NeonGridPaint.PaintBase(e, grid.Rows(e.RowIndex).Selected)

                    e.Paint(e.CellBounds, DataGridViewPaintParts.ContentForeground)

                End Sub

            body.Controls.Add(grid)

        End Sub


        ' ============================================================
        ' DATA
        ' ============================================================

        Private Sub LoadItems()

            If cboItem Is Nothing Then Return

            cboItem.Items.Clear()

            For Each i As Inventory In partController.GetAvailableItems()

                cboItem.Items.Add(
                    New ItemChoice(
                        i.InventoryId,
                        i.ItemName & "  (" & i.Quantity.ToString() & " in stock)"))

            Next

            If cboItem.Items.Count > 0 Then cboItem.SelectedIndex = 0

        End Sub

        Private Sub LoadParts()

            grid.Rows.Clear()

            Dim parts = partController.GetParts(ticket.RepairId)

            For Each p As RepairPart In parts

                Dim rowIndex As Integer =
                    grid.Rows.Add(
                        p.ItemName,
                        p.Quantity.ToString(),
                        Money.Peso(p.UnitPrice),
                        Money.Peso(p.Quantity * p.UnitPrice))

                grid.Rows(rowIndex).Tag = p.RepairPartId

            Next

            lblTotal.Text = "PARTS TOTAL  " & Money.Peso(RepairPartController.PartsTotal(parts))

            btnRemove.Enabled = (parts.Count > 0)

            grid.ClearSelection()

        End Sub

        Private Sub ShowMessage(text As String)
            lblMessage.Text = text
        End Sub


        ' ============================================================
        ' ACTIONS
        ' ============================================================

        Private Sub btnAdd_Click(sender As Object, e As EventArgs)

            ShowMessage("")

            Dim choice As ItemChoice = TryCast(cboItem.SelectedItem, ItemChoice)

            If choice Is Nothing Then
                ShowMessage("Choose an item. Only items with stock are listed.")
                Return
            End If

            Dim quantity As Integer

            If Not Integer.TryParse(
                txtQty.Text.Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, quantity) OrElse quantity <= 0 Then

                ShowMessage("Enter a quantity above 0.")
                txtQty.Focus()
                Return

            End If

            Try

                partController.AddPart(ticket.RepairId, choice.Id, quantity)

                _changed = True

                txtQty.Text = "1"

                LoadItems()
                LoadParts()

            Catch ex As Exception

                ShowMessage(ex.Message)

            End Try

        End Sub

        Private Sub btnRemove_Click(sender As Object, e As EventArgs)

            ShowMessage("")

            If grid.SelectedRows.Count = 0 Then
                ShowMessage("Select an item in the list first.")
                Return
            End If

            Dim row As DataGridViewRow = grid.SelectedRows(0)

            If MessageBox.Show(
                "Remove " & Convert.ToString(row.Cells("colItem").Value) &
                " from this repair?" & Environment.NewLine & Environment.NewLine &
                "The quantity is returned to stock.",
                "Remove Item",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) <> DialogResult.Yes Then

                Return

            End If

            Try

                partController.RemovePart(Convert.ToInt32(row.Tag))

                _changed = True

                LoadItems()
                LoadParts()

            Catch ex As Exception

                ShowMessage(ex.Message)

            End Try

        End Sub

        Private Sub btnClose_Click(sender As Object, e As EventArgs)

            Me.DialogResult = If(_changed, DialogResult.OK, DialogResult.Cancel)
            Me.Close()

        End Sub

        Protected Overrides Sub OnShown(e As EventArgs)

            MyBase.OnShown(e)

            If canEdit Then
                txtQty.Text = "1"
                txtQty.Focus()
                txtQty.SelectAll()
            End If

        End Sub

        Protected Overrides Function ProcessCmdKey(
            ByRef msg As Message,
            keyData As Keys) As Boolean

            If keyData = Keys.Escape Then

                ' let an open dropdown list handle Esc first
                If cboItem IsNot Nothing AndAlso cboItem.DroppedDown Then
                    Return MyBase.ProcessCmdKey(msg, keyData)
                End If

                btnClose_Click(Me, EventArgs.Empty)

                Return True

            End If

            If keyData = Keys.Enter AndAlso canEdit AndAlso
               Object.ReferenceEquals(ActiveControl, txtQty) Then

                btnAdd_Click(Me, EventArgs.Empty)

                Return True

            End If

            Return MyBase.ProcessCmdKey(msg, keyData)

        End Function

    End Class

End Namespace