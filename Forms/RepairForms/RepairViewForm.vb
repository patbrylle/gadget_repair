Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Models

Namespace Forms

    Public Class RepairViewForm
        Inherits Form


        ' ============================================================
        ' DATA
        ' ============================================================

        Private ReadOnly repairController As RepairController

        Private ReadOnly repairId As Integer

        Private loadFailed As Boolean = False

        Private _changed As Boolean = False

        ' True when this modal edited, re-statused or deleted the repair,
        ' so the caller knows it has to reload its list.
        Public ReadOnly Property Changed As Boolean
            Get
                Return _changed
            End Get
        End Property


        ' ============================================================
        ' UI REFERENCES
        ' ============================================================

        Private bodyPanel As Panel

        Private subtitleLabel As Label

        Private actionBar As Panel

        Private actionNote As Label

        Private footerPanel As Panel

        Private btnEdit As NeonButton

        Private btnDelete As NeonButton
                Private btnItems As NeonButton

        Private ReadOnly partController As RepairPartController
                Private btnSetStatus As NeonButton

        Private currentActions As String() = New String() {}

        Private ReadOnly tips As New ToolTip()

        Private dragging As Boolean = False

        Private dragOffset As Point


        ' ============================================================
        ' COLORS
        ' ============================================================

        Private ReadOnly BgColor As Color =
            Theme.BackgroundColor

        Private ReadOnly MutedColor As Color =
            Theme.TextMuted

        Private ReadOnly WhiteColor As Color =
            Theme.TextWhite


        ' ============================================================
        ' LAYOUT (inner width = 538, content width = 482)
        ' ============================================================

        Private Const PadX As Integer = 28
        Private Const ContentWidth As Integer = 482

        Private Const Col2A As Integer = 28
        Private Const Col2B As Integer = 279
        Private Const Width2 As Integer = 231

        Private Const Col3A As Integer = 28
        Private Const Col3B As Integer = 195
        Private Const Col3C As Integer = 362
        Private Const Width3 As Integer = 148

        Private Const FooterHeight As Integer = 62
        Private Const ActionBarHeight As Integer = 66
                Private Const ItemsExtra As Integer = 74


        ' ============================================================
        ' CONSTRUCTOR
        ' ============================================================

        Public Sub New(
            controller As RepairController,
            id As Integer)

            repairController = controller

            repairId = id
                        partController = repairController.PartsController()

            BuildInterface()

            LoadRepair()

        End Sub


        ' ============================================================
        ' BUILD INTERFACE
        ' ============================================================

        Private Sub BuildInterface()

            Dim isAdmin As Boolean = AppSession.IsAdmin

            Me.Text = "View Repair"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.None
            Dim formHeight As Integer = If(isAdmin, 512 + ActionBarHeight, 512) + ItemsExtra
            Me.Size = New Size(540, formHeight)
                        Me.MaximizeBox = False
            Me.ShowInTaskbar = False
            Me.DoubleBuffered = True

            ' 1px border: form shows the line color, content sits inside
            Me.BackColor = Theme.LineColor
            Me.Padding = New Padding(1)


            ' ========================================================
            ' ACCENT STRIP
            ' ========================================================

            Dim accentStrip As New Panel()
            accentStrip.Dock = DockStyle.Top
            accentStrip.Height = 2
            accentStrip.BackColor = Theme.NeonPurple


            ' ========================================================
            ' HEADER
            ' ========================================================

            Dim header As New Panel()
            header.Dock = DockStyle.Top
            header.Height = 64
            header.BackColor = BgColor


            Dim title As New Label()
            title.Text = "Repair Details"
            title.AutoSize = True
            title.Location = New Point(PadX, 12)
            title.Font = Theme.UiFont(14.0F, FontStyle.Bold)
            title.ForeColor = WhiteColor


            subtitleLabel = New Label()
            subtitleLabel.Text = "Ticket #" & repairId.ToString()
            subtitleLabel.AutoSize = True
            subtitleLabel.Location = New Point(PadX + 1, 40)
            subtitleLabel.Font = Theme.UiFont(8.5F)
            subtitleLabel.ForeColor = MutedColor


            Dim btnClose As New NeonCloseButton()
            btnClose.Location = New Point(492, 16)

            AddHandler btnClose.Click,
                Sub(sender As Object, e As EventArgs) Me.Close()


            Dim headerLine As New Panel()
            headerLine.Dock = DockStyle.Bottom
            headerLine.Height = 1
            headerLine.BackColor = Theme.LineColor


            header.Controls.Add(title)
            header.Controls.Add(subtitleLabel)
            header.Controls.Add(btnClose)
            header.Controls.Add(headerLine)


            AddHandler header.Resize,
                Sub(sender As Object, e As EventArgs)

                    btnClose.Left =
                        header.ClientSize.Width -
                        btnClose.Width -
                        14

                End Sub


            ' drag the form by the header
            For Each ctl As Control In
                New Control() {header, title, subtitleLabel}

                AddHandler ctl.MouseDown, AddressOf Header_MouseDown
                AddHandler ctl.MouseMove, AddressOf Header_MouseMove
                AddHandler ctl.MouseUp, AddressOf Header_MouseUp

            Next


            ' ========================================================
            ' BODY
            ' ========================================================

            bodyPanel = New Panel()
            bodyPanel.Dock = DockStyle.Fill
            bodyPanel.BackColor = BgColor
            bodyPanel.AutoScroll = True


            ' ========================================================
            ' ADMIN ACTION BAR (tinted so it stands out)
            ' ========================================================

            actionBar = New Panel()
            actionBar.Dock = DockStyle.Bottom
            actionBar.Height = ActionBarHeight
            actionBar.BackColor = Theme.Lerp(BgColor, Theme.NeonPurple, 0.1F)
            actionBar.Visible = isAdmin


            Dim barLine As New Panel()
            barLine.Dock = DockStyle.Top
            barLine.Height = 1
            barLine.BackColor = Theme.Lerp(Theme.LineColor, Theme.NeonPurple, 0.5F)


            Dim barCaption As New Label()
            barCaption.Text = "ADMIN ACTIONS  -  SET STATUS"
            barCaption.AutoSize = True
            barCaption.Location = New Point(PadX, 9)
            barCaption.Font = Theme.UiFont(7.0F, FontStyle.Bold)
            barCaption.ForeColor = Theme.TextViolet


            actionNote = New Label()
            actionNote.AutoSize = True
            actionNote.Location = New Point(PadX, 32)
            actionNote.Font = Theme.UiFont(9.0F)
            actionNote.ForeColor = MutedColor
            actionNote.Visible = False


                       btnSetStatus = New NeonButton()
            btnSetStatus.Text = "SET STATUS"
            btnSetStatus.Filled = False
            btnSetStatus.Accent = Theme.NeonPurple
            btnSetStatus.ForeColor = Theme.NeonPurple
            btnSetStatus.KeepAccentOnHover = True
            btnSetStatus.Size = New Size(160, 32)
            btnSetStatus.Location = New Point(PadX, 26)

            AddHandler btnSetStatus.Click,
                Sub(sender As Object, e As EventArgs) ShowStatusMenu()

            actionBar.Controls.Add(barLine)
            actionBar.Controls.Add(barCaption)
            actionBar.Controls.Add(actionNote)
            actionBar.Controls.Add(btnSetStatus)

            ' ========================================================
            ' FOOTER (delete on the left, edit on the right)
            ' ========================================================

            footerPanel = New Panel()
            footerPanel.Dock = DockStyle.Bottom
            footerPanel.Height = FooterHeight
            footerPanel.BackColor = BgColor


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

            AddHandler btnDelete.Click,
                Sub(sender As Object, e As EventArgs) DeleteThisRepair()


            btnEdit = New NeonButton()
            btnEdit.Text = "EDIT REPAIR"
            btnEdit.Filled = True
            btnEdit.Accent = Theme.NeonPurple
            btnEdit.Size = New Size(150, 38)
            btnEdit.Location = New Point(538 - PadX - 150, 12)

            AddHandler btnEdit.Click,
                Sub(sender As Object, e As EventArgs) OpenEdit()

            btnItems = New NeonButton()
            btnItems.Text = "ITEMS USED"
            btnItems.Filled = False
            btnItems.Accent = Theme.NeonGreen
            btnItems.ForeColor = Theme.NeonGreen
            btnItems.KeepAccentOnHover = True
            btnItems.Size = New Size(130, 38)
            btnItems.Top = 12

            AddHandler btnItems.Click,
                Sub(sender As Object, e As EventArgs) OpenItems()

            footerPanel.Controls.Add(footerLine)
            footerPanel.Controls.Add(btnDelete)
            footerPanel.Controls.Add(btnEdit)
            footerPanel.Controls.Add(btnItems)


                      AddHandler footerPanel.Resize,
                Sub(sender As Object, e As EventArgs) LayoutFooter()


            ' fill first, then edge-docked controls (last added = outermost)
            Me.Controls.Add(bodyPanel)
            Me.Controls.Add(actionBar)
            Me.Controls.Add(footerPanel)
            Me.Controls.Add(header)
            Me.Controls.Add(accentStrip)

        End Sub


        ' ============================================================
        ' LOAD REPAIR (also used to refresh after edit / status change)
        ' ============================================================

        Private Sub LoadRepair()

            Try

                Dim repair As Repair =
                    repairController.GetRepair(
                        repairId)


                If repair Is Nothing Then

                    loadFailed = True

                    MessageBox.Show(
                        "Repair record was not found.",
                        "View Repair",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)

                    If Me.Visible Then Me.Close()

                    Return

                End If


                ClearBody()


                ' ====================================================
                ' CUSTOMER
                ' ====================================================

                Dim customerName As String = "Unknown Customer"

                Dim customer =
                    repairController.GetCustomers().
                    FirstOrDefault(
                        Function(c) c.CustomerId = repair.CustomerId)

                If customer IsNot Nothing Then
                    customerName = customer.CustomerName
                End If


                ' ====================================================
                ' DEVICE
                ' ====================================================

                Dim deviceName As String =
                    String.Join(
                        " ",
                        New String() {
                            repair.DeviceType,
                            repair.Brand,
                            repair.Model}.
                        Where(Function(s) Not String.IsNullOrWhiteSpace(s)))

                If deviceName = "" Then
                    deviceName = "Not provided"
                End If


                ' ====================================================
                ' VALUES
                ' ====================================================

                Dim technicianName As String =
                    GetTechnicianName(repair)

                Dim serialNumber As String =
                    If(String.IsNullOrWhiteSpace(repair.SerialNumber),
                       "Not provided",
                       repair.SerialNumber)

                Dim status As String =
                    If(String.IsNullOrWhiteSpace(repair.Status),
                       "Unknown",
                       repair.Status)

                Dim repairDate As String =
                    repair.RepairDate.ToString("MMM dd, yyyy")

                Dim cost As String =
                    If(repair.Cost > 0D, Money.Peso(repair.Cost), "Not set yet")


                ' ====================================================
                ' OVERVIEW GRID
                ' ====================================================

                AddField(bodyPanel, "CUSTOMER", customerName,
                         Col2A, 16, Width2)

                AddField(bodyPanel, "DEVICE", deviceName,
                         Col2B, 16, Width2)

                AddField(bodyPanel, "SERIAL NUMBER", serialNumber,
                         Col2A, 68, Width2)

                AddField(bodyPanel, "ASSIGNED TECHNICIAN", technicianName,
                         Col2B, 68, Width2)

                AddField(bodyPanel, "DATE", repairDate,
                         Col3A, 120, Width3)

                AddField(bodyPanel, "COST", cost,
                         Col3B, 120, Width3)

                AddStatusField(bodyPanel, status,
                               Col3C, 120)


                ' ====================================================
                ' DIVIDER
                ' ====================================================

                Dim divider As New Panel()
                divider.BackColor = Theme.LineColor
                divider.Location = New Point(PadX, 172)
                divider.Size = New Size(ContentWidth, 1)

                bodyPanel.Controls.Add(divider)


                ' ====================================================
                ' NOTES
                ' ====================================================

                AddNote(bodyPanel,
                        "PROBLEM DESCRIPTION",
                        repair.ProblemDescription,
                        "No problem description.",
                        PadX, 188, ContentWidth, 64,
                        Theme.NeonPurple)

                AddNote(bodyPanel,
                        "REPAIR DESCRIPTION",
                        repair.RepairDescription,
                        "No repair description.",
                        PadX, 282, ContentWidth, 64,
                        Theme.NeonCyan)
                Dim parts = partController.GetParts(repair.RepairId)

                AddNote(bodyPanel,
                        "ITEMS USED  -  " &
                        Money.Peso(RepairPartController.PartsTotal(parts)),
                        RepairPartController.Summary(parts),
                        "No items used yet.",
                        PadX, 376, ContentWidth, 52,
                        Theme.NeonGreen)

                ' ====================================================
                ' BUTTONS (depend on role + current status)
                ' ====================================================

                RefreshActions(repair)


            Catch ex As Exception

                loadFailed = True

                MessageBox.Show(
                    "Unable to load repair information." &
                    Environment.NewLine &
                    Environment.NewLine &
                    ex.Message,
                    "View Repair",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

                If Me.Visible Then Me.Close()

            End Try

        End Sub


        Private Sub ClearBody()

            For i As Integer = bodyPanel.Controls.Count - 1 To 0 Step -1

                Dim c As Control = bodyPanel.Controls(i)

                bodyPanel.Controls.RemoveAt(i)

                c.Dispose()

            Next

        End Sub


        ' ============================================================
        ' ACTIONS (footer buttons + admin status buttons)
        ' ============================================================

                Private Sub RefreshActions(repair As Repair)

            Dim isAdmin As Boolean = AppSession.IsAdmin
            Dim status As String = If(repair.Status, "")


            ' technicians can update progress until the repair is final
            Dim canEdit As Boolean =
                isAdmin OrElse
                (AppSession.IsTechnician AndAlso
                 Not RepairController.IsFinal(status))
            btnEdit.Text = If(isAdmin, "EDIT REPAIR", "UPDATE PROGRESS")
            btnEdit.Visible = canEdit
            btnDelete.Visible = isAdmin

            btnItems.Text =
                If(RepairPartController.CanEditItems(repair), "MANAGE ITEMS", "ITEMS USED")

            footerPanel.Visible = True

            LayoutFooter()

                      If Not isAdmin Then Return


            Dim actions As String() =
                RepairController.AdminActionsFor(status)

            ' rebuild the menu for the new status
                       currentActions = actions

            btnSetStatus.Visible = (actions.Length > 0)
            actionNote.Visible = (actions.Length = 0)

            If actions.Length = 0 Then
                actionNote.Text =
                    "No further status changes: this repair is " &
                    status.ToLower() & "."
            End If

        End Sub

        ' opens the status list above the SET STATUS button
                Private Sub ShowStatusMenu()

            If currentActions.Length = 0 Then Return

            Dim menu As New NeonPopupMenu(
                currentActions,
                Function(s As String) RepairController.ActionLabel(s),
                Sub(s As String) ChangeStatus(s),
                btnSetStatus.Width)

            menu.ShowNear(btnSetStatus, True)

        End Sub



        ' ============================================================
        ' CHANGE STATUS (admin)
        ' ============================================================
        Private Sub ChangeStatus(newStatus As String)

            If MessageBox.Show(
                RepairController.ConfirmText(repairId, newStatus),
                "Change Status",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) <> DialogResult.Yes Then

                Return

            End If

            Try

                repairController.SetStatusAsAdmin(repairId, newStatus)

                _changed = True

                LoadRepair()

            Catch ex As Exception

                ShowFailure(ex, "change the status")

            End Try

        End Sub
        Private Sub LayoutFooter()

            Dim right As Integer = footerPanel.ClientSize.Width - PadX

            If btnEdit.Visible Then
                btnEdit.Left = right - btnEdit.Width
                right = btnEdit.Left - 10
            End If

            btnItems.Left = right - btnItems.Width

        End Sub

        Private Sub OpenItems()

            Dim repair As Repair = repairController.GetRepair(repairId)

            If repair Is Nothing Then Return

            Using modal As New RepairPartsForm(partController, repair)

                modal.ShowDialog(Me)

                If modal.Changed Then

                    _changed = True

                    LoadRepair()

                End If

            End Using

        End Sub

        ' ============================================================
        ' EDIT (opens the edit form, then refreshes this view)
        ' ============================================================

        Private Sub OpenEdit()

            Using modal As New RepairEditForm(
                repairController,
                repairId)

                If modal.ShowDialog(Me) = DialogResult.OK Then

                    _changed = True

                    LoadRepair()

                End If

            End Using

        End Sub


        ' ============================================================
        ' DELETE (admin)
        ' ============================================================

        Private Sub DeleteThisRepair()

            If MessageBox.Show(
                "Delete repair ticket #" & repairId.ToString() & "?" &
                Environment.NewLine &
                Environment.NewLine &
                "This action cannot be undone.",
                "Delete Repair",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) <> DialogResult.Yes Then

                Return

            End If

            Try

                If Not repairController.DeleteRepair(repairId) Then

                    MessageBox.Show(
                        "Repair could not be found.",
                        "Delete Repair",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)

                End If

                _changed = True

                Me.DialogResult = DialogResult.OK
                Me.Close()

            Catch ex As Exception

                ShowFailure(ex, "delete the repair")

            End Try

        End Sub


        Private Sub ShowFailure(ex As Exception, action As String)

            Dim isRule As Boolean =
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException

            If isRule Then

                MessageBox.Show(
                    ex.Message,
                    "Not Allowed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

            Else

                MessageBox.Show(
                    "Unable to " & action & "." &
                    Environment.NewLine &
                    Environment.NewLine &
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)

            End If

        End Sub


        ' ============================================================
        ' TECHNICIAN
        ' ============================================================

        Private Function GetTechnicianName(repair As Repair) As String

            If repair Is Nothing OrElse repair.Technician Is Nothing Then
                Return "Unassigned"
            End If

            If String.IsNullOrWhiteSpace(repair.Technician.TechnicianName) Then
                Return "Unassigned"
            End If

            Return repair.Technician.TechnicianName

        End Function


        ' ============================================================
        ' FIELD (caption + value)
        ' ============================================================

        Private Sub AddField(
            parent As Panel,
            caption As String,
            value As String,
            x As Integer,
            y As Integer,
            width As Integer)

            Dim captionLabel As New Label()
            captionLabel.Text = caption
            captionLabel.AutoSize = True
            captionLabel.Location = New Point(x, y)
            captionLabel.Font = Theme.UiFont(7.0F, FontStyle.Bold)
            captionLabel.ForeColor = MutedColor


            Dim valueLabel As New Label()
            valueLabel.Text = value
            valueLabel.AutoEllipsis = True
            valueLabel.Location = New Point(x, y + 17)
            valueLabel.Size = New Size(width, 24)
            valueLabel.Font = Theme.UiFont(10.0F, FontStyle.Bold)
            valueLabel.ForeColor = WhiteColor

            tips.SetToolTip(valueLabel, value)


            parent.Controls.Add(captionLabel)
            parent.Controls.Add(valueLabel)

        End Sub


        ' ============================================================
        ' STATUS FIELD (pill badge)
        ' ============================================================

        Private Sub AddStatusField(
            parent As Panel,
            status As String,
            x As Integer,
            y As Integer)

            Dim captionLabel As New Label()
            captionLabel.Text = "STATUS"
            captionLabel.AutoSize = True
            captionLabel.Location = New Point(x, y)
            captionLabel.Font = Theme.UiFont(7.0F, FontStyle.Bold)
            captionLabel.ForeColor = MutedColor


            Dim pill As New NeonStatusPill()
            pill.BackColor = BgColor
            pill.SetStatus(status, Theme.StatusColor(status))
            pill.Location = New Point(x, y + 17)


            parent.Controls.Add(captionLabel)
            parent.Controls.Add(pill)

        End Sub


        ' ============================================================
        ' NOTE (caption + accent bar + read-only text)
        ' ============================================================

        Private Sub AddNote(
            parent As Panel,
            caption As String,
            value As String,
            placeholder As String,
            x As Integer,
            y As Integer,
            width As Integer,
            height As Integer,
            accent As Color)

            Dim hasText As Boolean =
                Not String.IsNullOrWhiteSpace(value)


            Dim captionLabel As New Label()
            captionLabel.Text = caption
            captionLabel.AutoSize = True
            captionLabel.Location = New Point(x, y)
            captionLabel.Font = Theme.UiFont(7.0F, FontStyle.Bold)
            captionLabel.ForeColor = MutedColor


            Dim bar As New Panel()
            bar.BackColor = accent
            bar.Location = New Point(x, y + 20)
            bar.Size = New Size(3, height)


            Dim box As New TextBox()
            box.Font = Theme.UiFont(9.0F)
            box.Multiline = True
            box.ReadOnly = True
            box.TabStop = False
            box.BorderStyle = BorderStyle.None
            box.BackColor = BgColor
            box.ForeColor = If(hasText, WhiteColor, MutedColor)
            box.Location = New Point(x + 14, y + 20)
            box.Size = New Size(width - 14, height)
            box.Text = If(hasText, value, placeholder)


            ' show the scrollbar only when the text really overflows
            Dim textHeight As Integer =
                TextRenderer.MeasureText(
                    box.Text,
                    box.Font,
                    New Size(box.Width - 6, Integer.MaxValue),
                    TextFormatFlags.WordBreak).Height

            box.ScrollBars =
                If(textHeight > box.Height,
                   ScrollBars.Vertical,
                   ScrollBars.None)


            parent.Controls.Add(captionLabel)
            parent.Controls.Add(bar)
            parent.Controls.Add(box)

        End Sub


        ' ============================================================
        ' DRAG HEADER
        ' ============================================================

        Private Sub Header_MouseDown(
            sender As Object,
            e As MouseEventArgs)

            If e.Button <> MouseButtons.Left Then
                Return
            End If

            dragging = True

            dragOffset =
                New Point(
                    System.Windows.Forms.Cursor.Position.X - Me.Left,
                    System.Windows.Forms.Cursor.Position.Y - Me.Top)

        End Sub


        Private Sub Header_MouseMove(
            sender As Object,
            e As MouseEventArgs)

            If Not dragging Then
                Return
            End If

            Me.Location =
                New Point(
                    System.Windows.Forms.Cursor.Position.X - dragOffset.X,
                    System.Windows.Forms.Cursor.Position.Y - dragOffset.Y)

        End Sub


        Private Sub Header_MouseUp(
            sender As Object,
            e As MouseEventArgs)

            dragging = False

        End Sub


        ' ============================================================
        ' FORM BEHAVIOR
        ' ============================================================

        Protected Overrides Sub OnShown(e As EventArgs)

            MyBase.OnShown(e)

            ' close if the repair could not be loaded
            If loadFailed Then
                Me.Close()
            End If

        End Sub


        Protected Overrides Function ProcessCmdKey(
            ByRef msg As Message,
            keyData As Keys) As Boolean

            If keyData = Keys.Escape Then

                Me.Close()

                Return True

            End If

            Return MyBase.ProcessCmdKey(msg, keyData)

        End Function


              Protected Overrides Sub Dispose(disposing As Boolean)

            If disposing Then
                tips.Dispose()
            End If

            MyBase.Dispose(disposing)

        End Sub

    End Class

End Namespace