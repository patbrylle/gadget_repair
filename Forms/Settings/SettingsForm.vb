Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports gadget_repair.Controllers
Imports gadget_repair.Data

Public Class SettingsForm
    Inherits UserControl

    Private ReadOnly db As ApplicationDbContext
    Private ReadOnly settings As SettingsController

    Private tabButtons As New Dictionary(Of String, NeonButton)()
    Private pages As New Dictionary(Of String, Panel)()

    Private txtShopName As TextBox
    Private txtShopEmail As TextBox
    Private txtShopAddress As TextBox
    Private txtShopContact As TextBox
    Private cboPeriod As NeonComboBox
    Private lblGeneralStatus As Label

    Private chkRepairs As CheckBox
    Private chkStock As CheckBox
    Private chkPayments As CheckBox
    Private lblNotifStatus As Label

    Private lblDbStatus As Label

    Public Sub New(database As ApplicationDbContext)

        db = database
        settings = New SettingsController(db)

        Me.Dock = DockStyle.Fill
        Me.BackColor = Theme.BackgroundColor
        Me.Padding = New Padding(30, 28, 30, 26)
        Me.Font = Theme.UiFont(9.0F)
        Me.DoubleBuffered = True

        Dim content As New Panel()
        content.Dock = DockStyle.Fill
        content.BackColor = Theme.BackgroundColor

        Dim tabs As New Panel()
        tabs.Dock = DockStyle.Top
        tabs.Height = 58
        tabs.BackColor = Theme.BackgroundColor

        Dim header As New Panel()
        header.Dock = DockStyle.Top
        header.Height = 84
        header.BackColor = Theme.BackgroundColor

        Dim lblHeading As New Label()
        lblHeading.Text = "Settings"
        lblHeading.AutoSize = True
        lblHeading.Location = New Point(0, 0)
        lblHeading.Font = Theme.UiFont(23.0F, FontStyle.Bold)
        lblHeading.ForeColor = Theme.TextWhite

        Dim lblSub As New Label()
        lblSub.Text = "Shop details, notification preferences, help and support"
        lblSub.AutoSize = True
        lblSub.Location = New Point(2, 42)
        lblSub.Font = Theme.UiFont(9.5F)
        lblSub.ForeColor = Theme.TextMuted

        header.Controls.Add(lblHeading)
        header.Controls.Add(lblSub)

        Dim x As Integer = 0

        If AppSession.IsAdmin Then
            AddTab(tabs, "General", x) : x += 148
            AddTab(tabs, "Notifications", x) : x += 148
        End If

        AddTab(tabs, "Help & Support", x)

        If AppSession.IsAdmin Then
            pages("General") = BuildGeneralPage()
            pages("Notifications") = BuildNotificationsPage()
        End If

        pages("Help & Support") = BuildHelpPage()

        For Each p As Panel In pages.Values
            p.Dock = DockStyle.Fill
            content.Controls.Add(p)
        Next

        Me.Controls.Add(content)
        Me.Controls.Add(tabs)
        Me.Controls.Add(header)

        LoadValues()

        SwitchTab(If(AppSession.IsAdmin, "General", "Help & Support"))

    End Sub

    ' ============================================================
    ' TABS
    ' ============================================================

    Private Sub AddTab(parent As Panel, name As String, x As Integer)

        Dim b As New NeonButton()
        b.Text = name
        b.Size = New Size(140, 38)
        b.Location = New Point(x, 8)

        AddHandler b.Click, Sub(s As Object, e As EventArgs) SwitchTab(name)

        tabButtons(name) = b
        parent.Controls.Add(b)

    End Sub

    Private Sub SwitchTab(name As String)

        For Each kvp As KeyValuePair(Of String, Panel) In pages
            kvp.Value.Visible = (kvp.Key = name)
        Next

        For Each kvp As KeyValuePair(Of String, NeonButton) In tabButtons
            kvp.Value.Filled = (kvp.Key = name)
            kvp.Value.Accent = If(kvp.Key = name, Theme.NeonPurple, Theme.NeonCyan)
        Next

    End Sub

    Private Shared Function NewPage() As Panel
        Dim p As New Panel()
        p.BackColor = Theme.BackgroundColor
        p.AutoScroll = True
        Return p
    End Function

    Private Shared Function NewStatusLabel(x As Integer, y As Integer) As Label
        Dim l As New Label()
        l.AutoSize = True
        l.Location = New Point(x, y)
        l.Font = Theme.UiFont(9.0F)
        l.ForeColor = Theme.TextMuted
        l.BackColor = Theme.CardColor
        Return l
    End Function

    ' ============================================================
    ' GENERAL
    ' ============================================================

    Private Function BuildGeneralPage() As Panel

        Dim page As Panel = NewPage()

        Dim card As New NeonCard()
        card.Title = "SHOP DETAILS"
        card.Location = New Point(0, 0)
        card.Size = New Size(760, 372)

        txtShopName = NeonLayout.NewTextBox()
        NeonLayout.AddField(card, "SHOP NAME", txtShopName, 24, 64, 340)

        txtShopEmail = NeonLayout.NewTextBox()
        NeonLayout.AddField(card, "SHOP EMAIL", txtShopEmail, 388, 64, 340)

        txtShopAddress = NeonLayout.NewTextBox()
        NeonLayout.AddField(card, "ADDRESS", txtShopAddress, 24, 129, 704)

        txtShopContact = NeonLayout.NewTextBox()
        NeonLayout.AddField(card, "CONTACT NUMBER", txtShopContact, 24, 194, 340)

        cboPeriod = New NeonComboBox()
        cboPeriod.Font = Theme.UiFont(9.5F)

        For Each p As String In ReportController.PeriodList
            If p <> "Custom" Then cboPeriod.Items.Add(p)
        Next

        NeonLayout.AddField(card, "DEFAULT REPORT PERIOD", cboPeriod, 388, 194, 340)

        Dim hint As New Label()
        hint.Text = "The shop details appear in the header of printed and exported reports."
        hint.AutoSize = True
        hint.Location = New Point(24, 262)
        hint.Font = Theme.UiFont(8.5F)
        hint.ForeColor = Theme.TextMuted
        hint.BackColor = Theme.CardColor
        card.Controls.Add(hint)

        Dim btn As New NeonButton()
        btn.Text = "Save Settings"
        btn.Filled = True
        btn.Size = New Size(160, 40)
        btn.Location = New Point(24, 300)
        AddHandler btn.Click, AddressOf SaveGeneral_Click
        card.Controls.Add(btn)

        lblGeneralStatus = NewStatusLabel(196, 312)
        card.Controls.Add(lblGeneralStatus)

        page.Controls.Add(card)

        Return page

    End Function

    Private Sub SaveGeneral_Click(sender As Object, e As EventArgs)

        Try

            If String.IsNullOrWhiteSpace(txtShopName.Text) Then
                Throw New ArgumentException("Please enter the shop name.")
            End If

            Dim values As New Dictionary(Of String, String)()
            values(SettingsController.KeyShopName) = txtShopName.Text
            values(SettingsController.KeyShopEmail) = txtShopEmail.Text
            values(SettingsController.KeyShopAddress) = txtShopAddress.Text
            values(SettingsController.KeyShopContact) = txtShopContact.Text
            values(SettingsController.KeyDefaultPeriod) = Convert.ToString(cboPeriod.SelectedItem)

            settings.SetValues(values)

            lblGeneralStatus.ForeColor = Theme.NeonGreen
            lblGeneralStatus.Text = "Settings saved."

        Catch ex As Exception

            lblGeneralStatus.ForeColor = Theme.NeonRed
            lblGeneralStatus.Text = ex.Message

        End Try

    End Sub

    ' ============================================================
    ' NOTIFICATIONS
    ' ============================================================

    Private Function NewToggle(card As NeonCard, text As String, description As String, y As Integer) As CheckBox

        Dim chk As New CheckBox()
        chk.Text = text
        chk.AutoSize = True
        chk.Location = New Point(24, y)
        chk.Font = Theme.UiFont(10.0F, FontStyle.Bold)
        chk.ForeColor = Theme.TextWhite
        chk.BackColor = Theme.CardColor

        Dim d As New Label()
        d.Text = description
        d.AutoSize = True
        d.Location = New Point(44, y + 24)
        d.Font = Theme.UiFont(8.5F)
        d.ForeColor = Theme.TextMuted
        d.BackColor = Theme.CardColor

        card.Controls.Add(chk)
        card.Controls.Add(d)

        Return chk

    End Function

    Private Function BuildNotificationsPage() As Panel

        Dim page As Panel = NewPage()

        Dim card As New NeonCard()
        card.Title = "NOTIFICATION PREFERENCES"
        card.Location = New Point(0, 0)
        card.Size = New Size(760, 330)

        chkRepairs = NewToggle(card, "Repairs",
            "Pending repairs, repairs without a technician and repairs waiting for parts.", 70)
        chkStock = NewToggle(card, "Inventory",
            "Items that are low on stock or out of stock.", 130)
        chkPayments = NewToggle(card, "Payments",
            "Completed or delivered repairs that still have a balance.", 190)

        Dim btn As New NeonButton()
        btn.Text = "Save Preferences"
        btn.Filled = True
        btn.Size = New Size(170, 40)
        btn.Location = New Point(24, 258)
        AddHandler btn.Click, AddressOf SaveNotifications_Click
        card.Controls.Add(btn)

        lblNotifStatus = NewStatusLabel(208, 270)
        card.Controls.Add(lblNotifStatus)

        page.Controls.Add(card)

        Return page

    End Function

    Private Sub SaveNotifications_Click(sender As Object, e As EventArgs)

        Try

            Dim values As New Dictionary(Of String, String)()
            values(SettingsController.KeyNotifyRepairs) = If(chkRepairs.Checked, "1", "0")
            values(SettingsController.KeyNotifyStock) = If(chkStock.Checked, "1", "0")
            values(SettingsController.KeyNotifyPayments) = If(chkPayments.Checked, "1", "0")

            settings.SetValues(values)

            lblNotifStatus.ForeColor = Theme.NeonGreen
            lblNotifStatus.Text = "Preferences saved. They apply at the next refresh."

        Catch ex As Exception

            lblNotifStatus.ForeColor = Theme.NeonRed
            lblNotifStatus.Text = ex.Message

        End Try

    End Sub

    Private Sub LoadValues()

        If Not AppSession.IsAdmin Then Return

        Try

            Dim shop As ShopInfo = settings.GetShop()

            txtShopName.Text = shop.Name
            txtShopEmail.Text = shop.Email
            txtShopAddress.Text = shop.Address
            txtShopContact.Text = shop.Contact

            Dim idx As Integer =
                cboPeriod.Items.IndexOf(settings.GetValue(SettingsController.KeyDefaultPeriod, "This Month"))

            cboPeriod.SelectedIndex = If(idx >= 0, idx, 2)

            chkRepairs.Checked = settings.GetBool(SettingsController.KeyNotifyRepairs, True)
            chkStock.Checked = settings.GetBool(SettingsController.KeyNotifyStock, True)
            chkPayments.Checked = settings.GetBool(SettingsController.KeyNotifyPayments, True)

        Catch ex As Exception

            Debug.WriteLine(ex)

        End Try

    End Sub

    ' ============================================================
    ' HELP & SUPPORT
    ' ============================================================

    Private Function BuildHelpPage() As Panel

        Dim page As New Panel()
        page.BackColor = Theme.BackgroundColor

        Dim faq As New FlowLayoutPanel()
        faq.Dock = DockStyle.Fill
        faq.FlowDirection = FlowDirection.TopDown
        faq.WrapContents = False
        faq.AutoScroll = True
        faq.BackColor = Theme.BackgroundColor
        faq.Padding = New Padding(0, 14, 0, 0)

        For Each article As String() In HelpArticles()
            faq.Controls.Add(New HelpItem(article(0), article(1)))
        Next

        AddHandler faq.Resize,
            Sub(s As Object, e As EventArgs)
                For Each c As Control In faq.Controls
                    c.Width = Math.Max(200, faq.ClientSize.Width - 24)
                Next
            End Sub

        ' ---------- support card ----------
        Dim card As New NeonCard()
        card.Title = "SUPPORT & SYSTEM INFO"
        card.Dock = DockStyle.Top
        card.Height = 232

        Dim version As Version = Assembly.GetExecutingAssembly().GetName().Version
        Dim versionText As String = If(version Is Nothing, "1.0", version.Major & "." & version.Minor)

        NeonLayout.AddValue(card, "SIGNED IN AS", AppSession.DisplayName, 24, 62, 250)
        NeonLayout.AddValue(card, "ROLE", If(AppSession.IsAdmin, "Administrator", "Technician"), 290, 62, 160)
        NeonLayout.AddValue(card, "APP VERSION", versionText, 470, 62, 140)

        Dim shop As ShopInfo = settings.GetShop()
        Dim contact As String = (shop.Name & "  " & shop.Contact & "  " & shop.Email).Trim()

        NeonLayout.AddValue(card, "SHOP CONTACT", contact, 24, 112, 586)

        Dim btnTest As New NeonButton()
        btnTest.Text = "Test database connection"
        btnTest.Accent = Theme.NeonCyan
        btnTest.KeepAccentOnHover = True
        btnTest.Size = New Size(210, 38)
        btnTest.Location = New Point(24, 164)
        AddHandler btnTest.Click, AddressOf TestDb_Click

        Dim btnCopy As New NeonButton()
        btnCopy.Text = "Copy system info"
        btnCopy.Accent = Theme.NeonPurple
        btnCopy.KeepAccentOnHover = True
        btnCopy.Size = New Size(170, 38)
        btnCopy.Location = New Point(246, 164)
        AddHandler btnCopy.Click, AddressOf CopyInfo_Click

        lblDbStatus = NewStatusLabel(432, 174)

        card.Controls.Add(btnTest)
        card.Controls.Add(btnCopy)
        card.Controls.Add(lblDbStatus)

        page.Controls.Add(faq)
        page.Controls.Add(card)

        Return page

    End Function

    Private Function HelpArticles() As List(Of String())

        Dim list As New List(Of String())()

        list.Add(New String() {"Getting started",
            "Use the sidebar to move between pages. Administrators see the Dashboard, Repairs, Customers, Technicians, Inventory, Payments and Reports. Technicians only see the repairs assigned to them. The header shows notifications, settings and your profile."})

        If AppSession.IsAdmin Then

            list.Add(New String() {"Creating and tracking repairs",
                "Open Repairs, add the customer, device and problem, then assign a technician. The technician sets the final cost and updates the status. When a repair is completed, you can mark it as delivered or cancel it. Delivered and cancelled repairs are locked."})
            list.Add(New String() {"Recording payments",
                "Completed and delivered repairs appear on the Payments page. Open a record, enter the total amount paid so far and save. The status changes between Unpaid, Partially Paid and Paid automatically."})
            list.Add(New String() {"Inventory and low stock",
                "Set a minimum stock for every item. When the quantity reaches the minimum, the item is marked Low Stock and a notification is created. Items with zero quantity are marked Out of Stock."})
            list.Add(New String() {"Reports, export and print",
                "Open Reports, choose a report, pick a period and press Generate. Use Export Excel or Export PDF to save the report, or Print to preview and print it. The shop details from Settings appear at the top of every report."})

        Else

            list.Add(New String() {"Updating your repairs",
                "Open Repairs to see the jobs assigned to you. Open a repair to set the final cost, describe the work done, add the parts used and move the status to In Progress or Completed. You must set the final cost before completing a repair."})

        End If

        list.Add(New String() {"Notifications",
            "The bell in the header shows how many alerts are unread. Open it to read them, click one to jump to the related page, use the x to dismiss it, or mark everything as read."})
        list.Add(New String() {"Changing your name or password",
            "Click the profile icon in the header. The Profile tab edits your details and the Password tab changes your password. If you forget your password, contact the shop administrator."})
        list.Add(New String() {"Something is not loading",
            "Make sure the database server is running and this computer can reach it, then press Test database connection above. If the problem continues, press Copy system info and send it to whoever maintains the system."})

        Return list

    End Function

    Private Sub TestDb_Click(sender As Object, e As EventArgs)

        Cursor = Cursors.WaitCursor

        Try

            If db.Database.CanConnect() Then
                lblDbStatus.ForeColor = Theme.NeonGreen
                lblDbStatus.Text = "Database connected"
            Else
                lblDbStatus.ForeColor = Theme.NeonRed
                lblDbStatus.Text = "Cannot reach the database"
            End If

        Catch ex As Exception

            lblDbStatus.ForeColor = Theme.NeonRed
            lblDbStatus.Text = "Connection failed"

        Finally

            Cursor = Cursors.Default

        End Try

    End Sub

    Private Sub CopyInfo_Click(sender As Object, e As EventArgs)

        Try

            Dim version As Version = Assembly.GetExecutingAssembly().GetName().Version

            Dim text As String =
                "Gadget Repair" & Environment.NewLine &
                "Version: " & If(version Is Nothing, "n/a", version.ToString()) & Environment.NewLine &
                "User: " & AppSession.DisplayName & " (" & If(AppSession.IsAdmin, "Administrator", "Technician") & ")" & Environment.NewLine &
                "OS: " & RuntimeInformation.OSDescription & Environment.NewLine &
                "Runtime: " & RuntimeInformation.FrameworkDescription & Environment.NewLine &
                "Time: " & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")

            Clipboard.SetText(text)

            lblDbStatus.ForeColor = Theme.NeonGreen
            lblDbStatus.Text = "System info copied"

        Catch ex As Exception

            lblDbStatus.ForeColor = Theme.NeonRed
            lblDbStatus.Text = "Could not copy"

        End Try

    End Sub

    ' ============================================================
    ' EXPANDABLE HELP ITEM
    ' ============================================================
    Private Class HelpItem
        Inherits Panel

        Private ReadOnly head As Label
        Private ReadOnly body As Label
        Private expanded As Boolean = False

        Public Sub New(title As String, text As String)

            BackColor = Theme.CardColor
            Height = 48
            Width = 600
            Margin = New Padding(0, 0, 0, 8)

            head = New Label()
            head.Text = title
            head.AutoSize = False
            head.Location = New Point(18, 0)
            head.Height = 48
            head.Width = 500
            head.TextAlign = ContentAlignment.MiddleLeft
            head.Font = Theme.UiFont(10.0F, FontStyle.Bold)
            head.ForeColor = Theme.TextWhite
            head.BackColor = Theme.CardColor
            head.Cursor = Cursors.Hand

            body = New Label()
            body.Text = text
            body.AutoSize = True
            body.Location = New Point(18, 48)
            body.Font = Theme.UiFont(9.5F)
            body.ForeColor = Theme.TextMuted
            body.BackColor = Theme.CardColor
            body.Visible = False

            Controls.Add(head)
            Controls.Add(body)

            AddHandler head.Click, AddressOf Toggle
            AddHandler Me.Click, AddressOf Toggle

            Relayout()

        End Sub

        Private Sub Toggle(sender As Object, e As EventArgs)
            expanded = Not expanded
            Relayout()
        End Sub

        Private Sub Relayout()

            head.Width = Math.Max(100, Width - 60)
            body.MaximumSize = New Size(Math.Max(100, Width - 40), 0)
            body.Visible = expanded

            If expanded Then
                Height = 48 + body.PreferredHeight + 18
            Else
                Height = 48
            End If

            Invalidate()

        End Sub

        Protected Overrides Sub OnResize(eventargs As EventArgs)
            MyBase.OnResize(eventargs)
            If head IsNot Nothing Then
                head.Width = Math.Max(100, Width - 60)
                body.MaximumSize = New Size(Math.Max(100, Width - 40), 0)
            End If
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)

            MyBase.OnPaint(e)

            Using p As New Pen(Theme.LineColor, 1.0F)
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1)
            End Using

            e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            Dim cx As Single = Width - 26.0F
            Dim cy As Single = 24.0F

            Using p As New Pen(Theme.NeonCyan, 1.8F)
                p.StartCap = Drawing2D.LineCap.Round
                p.EndCap = Drawing2D.LineCap.Round
                If expanded Then
                    e.Graphics.DrawLines(p, New PointF() {New PointF(cx - 5, cy + 2), New PointF(cx, cy - 3), New PointF(cx + 5, cy + 2)})
                Else
                    e.Graphics.DrawLines(p, New PointF() {New PointF(cx - 5, cy - 2), New PointF(cx, cy + 3), New PointF(cx + 5, cy - 2)})
                End If
            End Using

        End Sub

    End Class

End Class