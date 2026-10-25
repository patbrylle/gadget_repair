Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Drawing.Text
Imports System.Reflection
Imports System.Windows.Forms
Imports gadget_repair.Data
Imports gadget_repair.Forms

Public Class MainLayout
    Inherits UserControl

    Private ReadOnly db As ApplicationDbContext

    Private sidebar As BufferedPanel
    Private header As BufferedPanel
    Private contentPanel As BufferedPanel
    Private footer As FooterBar

    Private titleLabel As Label
    Private breadcrumbLabel As Label
    Private clockLabel As Label

    Private notificationButton As HeaderIconButton
    Private settingsButton As HeaderIconButton
    Private profileButton As HeaderIconButton

    Private clockTimer As System.Windows.Forms.Timer

    Private ReadOnly navButtons As New Dictionary(Of String, NavButton)
    Private activeMenuName As String = "Dashboard"
    Private loadedMenuName As String = ""
    Private pendingRepairs As Integer = 0
        Private notifTimer As System.Windows.Forms.Timer
    Private notifBusy As Boolean = False

    ' raised after the admin confirms logout; the host form shows the login screen
    Public Event LogoutRequested As EventHandler

    ' page transition
    Private Const TransitionMs As Integer = 220
    Private fadeTimer As System.Windows.Forms.Timer
    Private fadeWatch As Stopwatch
    Private activeFade As FadeOverlay


    ' ==========================================================
    ' CONSTRUCTOR
    ' ==========================================================

       Public Sub New(database As ApplicationDbContext)

        db = database

        Try
            SchemaBootstrap.EnsureSupportTables(db)
        Catch ex As Exception
            Debug.WriteLine(ex)
        End Try

        ' technicians open on their repairs, not the dashboard
        If Not AppSession.IsAdmin Then
            activeMenuName = "Dashboard"
        End If

        InitializeLayout()

    End Sub

    Private Sub InitializeLayout()

        Me.SuspendLayout()

        Me.Dock = DockStyle.Fill
        Me.BackColor = BackgroundColor
        Me.MinimumSize = New Size(900, 600)
        Me.DoubleBuffered = True

        ' Docking order: the LAST added control docks first.
        ' Content (Fill) first, sidebar last -> the sidebar takes the full
        ' left edge, header/footer sit to its right, content fills the rest.
        CreateContent()
        CreateFooter()
        CreateHeader()
        CreateSidebar()

        UpdateHeader(activeMenuName)

        contentPanel.BringToFront()

        Me.ResumeLayout(True)

    End Sub
    Protected Overrides Sub OnLoad(e As EventArgs)

        MyBase.OnLoad(e)

        If contentPanel IsNot Nothing AndAlso contentPanel.Controls.Count = 0 Then
            NavigateToPage(activeMenuName)
        End If

        StartNotificationTimer()
        RefreshNotificationBadge()

    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)

        If disposing Then

            If clockTimer IsNot Nothing Then
                clockTimer.Stop()
                clockTimer.Dispose()
                clockTimer = Nothing
            End If

            If fadeTimer IsNot Nothing Then
                fadeTimer.Stop()
                fadeTimer.Dispose()
                fadeTimer = Nothing
            End If

            If notifTimer IsNot Nothing Then
                notifTimer.Stop()
                notifTimer.Dispose()
                notifTimer = Nothing
            End If

        End If

        MyBase.Dispose(disposing)

    End Sub


    ' ==========================================================
    ' MENU (single source of truth)
    ' Items starting with "#" are section headings.
    ' ==========================================================

    Private Function GetMenuItems() As String()

        If AppSession.IsAdmin Then

            Return New String() {
                "#MAIN", "Dashboard", "Repairs", "Customers",
                "#MANAGEMENT", "Technicians", "Inventory", "Payments", "Reports"
            }

        End If

Return New String() {"#MY WORK", "Dashboard", "Repairs", "Inventory"}

    End Function

    ' a menu can be opened only if it is in the current user's menu
    Private Function CanOpen(menuName As String) As Boolean

        For Each item As String In GetMenuItems()

            If Not item.StartsWith("#") AndAlso item = menuName Then
                Return True
            End If

        Next

        Return False

    End Function


    ' ==========================================================
    ' SIDEBAR
    ' ==========================================================

    Private Sub CreateSidebar()

        sidebar = New BufferedPanel()
        sidebar.Dock = DockStyle.Left
        sidebar.Width = 240
        sidebar.BackColor = SidebarColor

        ' --- navigation area (Fill, added first) ---
        Dim navPanel As New BufferedPanel()
        navPanel.Dock = DockStyle.Fill
        navPanel.BackColor = SidebarColor
        navPanel.Padding = New Padding(10, 4, 10, 10)
        sidebar.Controls.Add(navPanel)

        ' Dock=Top: last added sits on top, so add in reverse order.
        Dim items() As String = GetMenuItems()

        For i As Integer = items.Length - 1 To 0 Step -1

            If items(i).StartsWith("#") Then

                Dim section As New SectionLabel()
                section.Text = items(i).Substring(1)
                section.Dock = DockStyle.Top
                section.Height = 34
                navPanel.Controls.Add(section)

            Else

                AddNavButton(navPanel, items(i))

            End If

        Next

        ' --- logout button (bottom of the nav area) ---
        Dim logoutButton As New NavButton()
        logoutButton.MenuName = "Logout"
        logoutButton.Text = "Logout"
        logoutButton.Dock = DockStyle.Bottom
        logoutButton.Height = 46
        logoutButton.IsActive = False

        AddHandler logoutButton.Click,
            Sub(sender As Object, e As EventArgs)
                PerformLogout()
            End Sub

        navPanel.Controls.Add(logoutButton)

        ' --- status card (bottom) ---
        Dim statusCard As New BufferedPanel()
        statusCard.Dock = DockStyle.Bottom
        statusCard.Height = 66
        statusCard.BackColor = SidebarColor

        AddHandler statusCard.Paint,
            Sub(sender As Object, e As PaintEventArgs)

                Dim g As Graphics = e.Graphics
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit

                Dim r As New Rectangle(10, 6, statusCard.Width - 21, statusCard.Height - 14)

                If r.Width < 20 Then Return

                Using path As GraphicsPath = RoundedRect(r, 10)

                    Using br As New SolidBrush(CardColor)
                        g.FillPath(br, path)
                    End Using

                    Using p As New Pen(LineColor, 1.2F)
                        g.DrawPath(p, path)
                    End Using

                End Using

                Dim cy As Integer = statusCard.Height \ 2

                Using br As New SolidBrush(Color.FromArgb(35, NeonGreen))
                    g.FillEllipse(br, 18, cy - 11, 22, 22)
                End Using

                Using br As New SolidBrush(Color.FromArgb(70, NeonGreen))
                    g.FillEllipse(br, 22, cy - 7, 14, 14)
                End Using

                Using br As New SolidBrush(NeonGreen)
                    g.FillEllipse(br, 25, cy - 4, 8, 8)
                End Using

                Dim roleText As String =
                    If(AppSession.IsAdmin, "Administrator",
                       If(AppSession.IsTechnician, "Technician", "Guest"))

                Dim nameText As String =
                    If(String.IsNullOrWhiteSpace(AppSession.DisplayName),
                       "Session active",
                       AppSession.DisplayName)

                TextRenderer.DrawText(g, roleText, UiFont(9.0F, FontStyle.Bold),
                    New Point(46, cy - 16), TextWhite,
                    TextFormatFlags.Left Or TextFormatFlags.Top Or TextFormatFlags.NoPadding)

                TextRenderer.DrawText(g, nameText, UiFont(7.5F),
                    New Rectangle(46, cy + 2, statusCard.Width - 66, 14), TextMuted,
                    TextFormatFlags.Left Or TextFormatFlags.Top Or
                    TextFormatFlags.NoPadding Or TextFormatFlags.EndEllipsis)

            End Sub

        sidebar.Controls.Add(statusCard)

        ' --- gradient separator ---
        Dim separator As New BufferedPanel()
        separator.Dock = DockStyle.Top
        separator.Height = 2
        separator.BackColor = SidebarColor

        AddHandler separator.Paint,
            Sub(sender As Object, e As PaintEventArgs)

                If separator.Width < 2 Then Return

                Using lg As New LinearGradientBrush(
                    New Rectangle(0, 0, separator.Width, 2),
                    NeonCyan, NeonPurple, LinearGradientMode.Horizontal)

                    e.Graphics.FillRectangle(lg, 0, 0, separator.Width, 2)

                End Using

            End Sub

        sidebar.Controls.Add(separator)

        ' --- logo (top) ---
        Dim logo As New LogoControl()
        logo.Dock = DockStyle.Top
        logo.Height = 84
        sidebar.Controls.Add(logo)

        ' --- right edge line (added last -> docks first, full height) ---
        Dim edge As New BufferedPanel()
        edge.Dock = DockStyle.Right
        edge.Width = 1
        edge.BackColor = LineColor
        sidebar.Controls.Add(edge)

        Me.Controls.Add(sidebar)

    End Sub

    Private Sub AddNavButton(parent As Control, menuName As String)

        Dim btn As New NavButton()
        btn.MenuName = menuName
        btn.Text = menuName
        btn.Dock = DockStyle.Top
        btn.Height = 46
        btn.IsActive = (menuName = activeMenuName)

        AddHandler btn.Click,
            Sub(sender As Object, e As EventArgs)
                SelectMenu(menuName)
            End Sub

        navButtons(menuName) = btn
        parent.Controls.Add(btn)

    End Sub

    Private Sub SelectMenu(menuName As String)

        If Not CanOpen(menuName) Then Return

        ' Clicking the page you are already on should not rebuild it.
        If menuName = loadedMenuName Then
            Return
        End If

        activeMenuName = menuName

        For Each kvp As KeyValuePair(Of String, NavButton) In navButtons
            kvp.Value.IsActive = (kvp.Key = menuName)
        Next

        NavigateToPage(menuName)

    End Sub


    ' ==========================================================
    ' NAVIGATION
    ' ==========================================================
    Private Sub NavigateToPage(menuName As String)

        Try

            Select Case menuName

                Case "Dashboard"
                    If AppSession.IsAdmin Then
                        ShowDashboardPage()
                    Else
                        ShowTechnicianDashboard()
                    End If

                Case "Repairs"
                    ShowPage(New RepairsForm(db), "Repairs")

                Case "Customers"
                    ShowPage(New CustomersForm(db), "Customers")

                Case "Technicians"
                    ShowPage(New TechnicianForm(db), "Technicians")

                Case "Inventory"
                    ShowPage(New InventoryForm(db), "Inventory")

                Case "Payments"
                    ShowPage(New PaymentsForm(db), "Payments")

                Case "Reports"
                    ShowPage(New ReportsForm(db), "Reports")

                Case "Settings"
                    ShowPage(New SettingsForm(db), "Settings")

                Case Else
                    ShowPlaceholderPage(menuName, "This page does not exist yet.")

            End Select

            loadedMenuName = menuName

        Catch ex As Exception

            Debug.WriteLine(ex)

            ' allow the user to retry the same menu item
            loadedMenuName = ""

            ShowPlaceholderPage("Error", "This page could not be loaded: " & ex.Message)

        End Try

    End Sub

    Private Sub ShowDashboardPage()

        Dim page As New DashboardForm(db)

        ' Navigate after the click finishes: the dashboard (which owns the
        ' button that was clicked) is disposed during navigation.
        AddHandler page.NavigateRequested,
            Sub(target As String)

                If Me.IsDisposed OrElse Not Me.IsHandleCreated Then Return

                BeginInvoke(New System.Windows.Forms.MethodInvoker(Sub() SelectMenu(target)))

            End Sub

        AddHandler page.StatsLoaded, AddressOf OnDashboardStatsLoaded

        ShowPage(page, "Dashboard")

    End Sub

    Private Sub ShowTechnicianDashboard()

        Dim page As New TechnicianDashboardForm(db)

        AddHandler page.NavigateRequested,
            Sub(target As String)

                If Me.IsDisposed OrElse Not Me.IsHandleCreated Then Return

                BeginInvoke(New System.Windows.Forms.MethodInvoker(Sub() SelectMenu(target)))

            End Sub

        ShowPage(page, "Dashboard")

    End Sub
   ' polls for new alerts and refreshes the bell badge
Private Sub StartNotificationTimer()

    If notifTimer Is Nothing Then

        notifTimer = New System.Windows.Forms.Timer()
        notifTimer.Interval = 60000   ' every 60 seconds

        AddHandler notifTimer.Tick,
            Sub(sender As Object, e As EventArgs)
                RefreshNotificationBadge()
            End Sub

    End If

    notifTimer.Start()

End Sub

    ' syncs alerts from live data and updates the bell badge
    Private Sub RefreshNotificationBadge()

        If Me.IsDisposed OrElse notifBusy Then Return

        notifBusy = True

        Try

            Dim controller As New gadget_repair.Controllers.NotificationController()

            controller.Sync()

            pendingRepairs = controller.GetUnreadCount()

            If notificationButton IsNot Nothing AndAlso Not notificationButton.IsDisposed Then
                notificationButton.HasBadge = (pendingRepairs > 0)
            End If

        Catch ex As Exception

            Debug.WriteLine(ex)

        Finally

            notifBusy = False

        End Try

    End Sub
    Private Sub OnDashboardStatsLoaded(pendingCount As Integer)

        If Me.IsDisposed Then Return

        pendingRepairs = pendingCount

        If notificationButton IsNot Nothing AndAlso Not notificationButton.IsDisposed Then
            notificationButton.HasBadge = (pendingCount > 0)
        End If

    End Sub


    ' ==========================================================
    ' HEADER
    ' ==========================================================

    Private Sub CreateHeader()

        header = New BufferedPanel()
        header.Dock = DockStyle.Top
        header.Height = 72
        header.BackColor = HeaderColor

        AddHandler header.Paint,
            Sub(sender As Object, e As PaintEventArgs)

                Dim g As Graphics = e.Graphics

                Using p As New Pen(LineColor)
                    g.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1)
                End Using

                Dim w As Integer = header.Width \ 2

                If w > 4 Then

                    Using lg As New LinearGradientBrush(
                        New Rectangle(0, header.Height - 2, w, 2),
                        Color.FromArgb(200, NeonCyan),
                        Color.FromArgb(0, NeonPurple),
                        LinearGradientMode.Horizontal)

                        g.FillRectangle(lg, 0, header.Height - 2, w, 2)

                    End Using

                End If

            End Sub

        titleLabel = New Label()
        titleLabel.Text = "Dashboard"
        titleLabel.Font = UiFont(17.0F, FontStyle.Bold)
        titleLabel.ForeColor = NeonCyan
        titleLabel.BackColor = HeaderColor
        titleLabel.AutoSize = True
        titleLabel.Location = New Point(28, 10)
        header.Controls.Add(titleLabel)

        breadcrumbLabel = New Label()
        breadcrumbLabel.Text = "GADGET REPAIR  /  DASHBOARD"
        breadcrumbLabel.Font = UiFont(7.5F, FontStyle.Bold)
        breadcrumbLabel.ForeColor = NeonPurple
        breadcrumbLabel.BackColor = HeaderColor
        breadcrumbLabel.AutoSize = True
        breadcrumbLabel.Location = New Point(30, 45)
        header.Controls.Add(breadcrumbLabel)

        clockLabel = New Label()
        clockLabel.Font = UiFont(9.0F)
        clockLabel.ForeColor = TextMuted
        clockLabel.BackColor = HeaderColor
        clockLabel.AutoSize = False
        clockLabel.Size = New Size(230, 20)
        clockLabel.TextAlign = ContentAlignment.MiddleRight
        clockLabel.Top = 26
        header.Controls.Add(clockLabel)

        notificationButton = CreateHeaderButton("Notification", AddressOf NotificationClicked)
        settingsButton = CreateHeaderButton("Settings", AddressOf SettingsClicked)
        profileButton = CreateHeaderButton("Profile", AddressOf ProfileClicked)

        AddHandler header.Resize,
            Sub(sender As Object, e As EventArgs)
                PositionHeaderItems()
            End Sub

        clockTimer = New System.Windows.Forms.Timer()
        clockTimer.Interval = 1000

        AddHandler clockTimer.Tick,
            Sub(sender As Object, e As EventArgs)
                UpdateClock()
            End Sub

        UpdateClock()
        clockTimer.Start()

        PositionHeaderItems()

        Me.Controls.Add(header)

    End Sub

    Private Function CreateHeaderButton(iconName As String, handler As EventHandler) As HeaderIconButton

        Dim btn As New HeaderIconButton()
        btn.IconName = iconName
        btn.Size = New Size(42, 42)
        btn.Top = 15

        AddHandler btn.Click, handler

        header.Controls.Add(btn)

        Return btn

    End Function

    Private Sub UpdateClock()

        If clockLabel IsNot Nothing Then
            clockLabel.Text = DateTime.Now.ToString("ddd, dd MMM yyyy  |  HH:mm:ss")
        End If

    End Sub

    Private Sub PositionHeaderItems()

        If header Is Nothing OrElse profileButton Is Nothing Then
            Return
        End If

        Dim rightEdge As Integer = header.ClientSize.Width - 24

        profileButton.Left = rightEdge - profileButton.Width
        settingsButton.Left = profileButton.Left - settingsButton.Width - 10
        notificationButton.Left = settingsButton.Left - notificationButton.Width - 10

        clockLabel.Visible = header.ClientSize.Width > 760
        clockLabel.Left = notificationButton.Left - clockLabel.Width - 24

    End Sub

    Private Sub UpdateHeader(pageTitle As String)

        titleLabel.Text = pageTitle
        breadcrumbLabel.Text = "GADGET REPAIR  /  " & pageTitle.ToUpper()

    End Sub


    ' ==========================================================
    ' CONTENT / FOOTER
    ' ==========================================================

    Private Sub CreateContent()

        contentPanel = New BufferedPanel()
        contentPanel.Dock = DockStyle.Fill
        contentPanel.BackColor = BackgroundColor

        Me.Controls.Add(contentPanel)

    End Sub

    Private Sub CreateFooter()

        footer = New FooterBar()
        footer.Dock = DockStyle.Bottom
        footer.Height = 30

        Dim appVersion As Version = Assembly.GetExecutingAssembly().GetName().Version

        footer.VersionText = If(appVersion Is Nothing,
                                "v1.0",
                                "v" & appVersion.Major & "." & appVersion.Minor)

        Me.Controls.Add(footer)

    End Sub


    ' ==========================================================
    ' SHOW PAGE (with crossfade)
    ' ==========================================================

    Public Sub ShowPage(page As Control, pageTitle As String)

        ' end any fade that is still running, so snapshots are clean
        FinishTransition()

        ' snapshot of the page we are leaving (none on first load)
        Dim oldSnapshot As Bitmap = Nothing

        If contentPanel.Controls.Count > 0 Then
            oldSnapshot = TakeSnapshot(contentPanel)
        End If

        contentPanel.SuspendLayout()

        ' dispose the previous page
        For i As Integer = contentPanel.Controls.Count - 1 To 0 Step -1
            Dim old As Control = contentPanel.Controls(i)
            contentPanel.Controls.RemoveAt(i)
            old.Dispose()
        Next

        ' a Form can only be hosted as a child when it is not top-level
        If TypeOf page Is Form Then
            Dim f As Form = DirectCast(page, Form)
            f.TopLevel = False
            f.FormBorderStyle = FormBorderStyle.None
        End If

        page.Dock = DockStyle.Fill
        contentPanel.Controls.Add(page)
        page.Visible = True

        UpdateHeader(pageTitle)

        contentPanel.ResumeLayout(True)

        If oldSnapshot IsNot Nothing Then
            StartTransition(oldSnapshot)
        End If

    End Sub

    Private Function TakeSnapshot(target As Control) As Bitmap

        If target Is Nothing OrElse Not target.IsHandleCreated Then Return Nothing
        If target.Width < 2 OrElse target.Height < 2 Then Return Nothing

        Try

            Dim bmp As New Bitmap(target.Width, target.Height)
            target.DrawToBitmap(bmp, New Rectangle(0, 0, target.Width, target.Height))
            Return bmp

        Catch ex As Exception

            Debug.WriteLine(ex)
            Return Nothing

        End Try

    End Function

    Private Sub StartTransition(oldSnapshot As Bitmap)

        contentPanel.PerformLayout()

        Dim newSnapshot As Bitmap = TakeSnapshot(contentPanel)

        If newSnapshot Is Nothing Then
            oldSnapshot.Dispose()
            Return
        End If

        activeFade = New FadeOverlay(oldSnapshot, newSnapshot)
        activeFade.Dock = DockStyle.Fill

        contentPanel.Controls.Add(activeFade)
        activeFade.BringToFront()

        If fadeTimer Is Nothing Then
            fadeTimer = New System.Windows.Forms.Timer()
            fadeTimer.Interval = 15
            AddHandler fadeTimer.Tick, AddressOf FadeTick
        End If

        fadeWatch = Stopwatch.StartNew()
        fadeTimer.Start()

    End Sub

    Private Sub FadeTick(sender As Object, e As EventArgs)

        If activeFade Is Nothing OrElse fadeWatch Is Nothing Then
            FinishTransition()
            Return
        End If

        Dim t As Single = CSng(fadeWatch.ElapsedMilliseconds) / TransitionMs

        If t >= 1.0F Then
            FinishTransition()
            Return
        End If

        ' ease-out cubic
        Dim eased As Single = 1.0F - CSng(Math.Pow(1.0F - t, 3.0))

        activeFade.Progress = eased

    End Sub

    Private Sub FinishTransition()

        If fadeTimer IsNot Nothing Then
            fadeTimer.Stop()
        End If

        If fadeWatch IsNot Nothing Then
            fadeWatch.Stop()
            fadeWatch = Nothing
        End If

        If activeFade IsNot Nothing Then

            Dim overlay As FadeOverlay = activeFade
            activeFade = Nothing

            If contentPanel IsNot Nothing AndAlso Not contentPanel.IsDisposed Then
                contentPanel.Controls.Remove(overlay)
            End If

            overlay.Dispose()

        End If

    End Sub


    ' ==========================================================
    ' PLACEHOLDER PAGE
    ' ==========================================================

    Private Sub ShowPlaceholderPage(pageTitle As String, message As String)

        Dim page As New BufferedPanel()
        page.BackColor = BackgroundColor

        Dim card As New CardPanel()
        card.Title = pageTitle.ToUpper()
        card.Size = New Size(520, 170)
        card.Location = New Point(28, 28)

        Dim lbl As New Label()
        lbl.Text = message
        lbl.Font = UiFont(10.0F)
        lbl.ForeColor = TextMuted
        lbl.BackColor = CardColor
        lbl.AutoSize = True
        lbl.MaximumSize = New Size(480, 0)
        lbl.Location = New Point(20, 66)
        card.Controls.Add(lbl)

        page.Controls.Add(card)

        ShowPage(page, pageTitle)

    End Sub


    ' ==========================================================
    ' LOGOUT
    ' ==========================================================
    Private Sub PerformLogout()

        Dim answer As DialogResult = MessageBox.Show(
            "Are you sure you want to log out?",
            "Logout",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If answer <> DialogResult.Yes Then Return

        ' stop background work so nothing fires after the page is gone
        If clockTimer IsNot Nothing Then clockTimer.Stop()
        If notifTimer IsNot Nothing Then notifTimer.Stop()
        FinishTransition()

        ' clear the signed-in user
        AppSession.SignOut()

        If Me.IsDisposed OrElse Not Me.IsHandleCreated Then Return

        ' the host swaps to the login form after this click finishes
        BeginInvoke(New System.Windows.Forms.MethodInvoker(
            Sub()
                RaiseEvent LogoutRequested(Me, EventArgs.Empty)
            End Sub))

    End Sub


    ' ==========================================================
    ' HEADER BUTTON EVENTS
    ' ==========================================================

    Private Sub NotificationClicked(sender As Object, e As EventArgs)

        Dim target As String = ""

        Try

            Using dlg As New NotificationsForm()
                dlg.ShowDialog(Me.FindForm())
                target = dlg.NavigateTarget
            End Using

        Catch ex As Exception

            Debug.WriteLine(ex)

        End Try

        RefreshNotificationBadge()

        If target <> "" AndAlso CanOpen(target) Then
            BeginInvoke(New System.Windows.Forms.MethodInvoker(Sub() SelectMenu(target)))
        End If

    End Sub

    Private Sub SettingsClicked(sender As Object, e As EventArgs)

        If loadedMenuName = "Settings" Then Return

        activeMenuName = "Settings"

        For Each kvp As KeyValuePair(Of String, NavButton) In navButtons
            kvp.Value.IsActive = False
        Next

        NavigateToPage("Settings")

    End Sub

    Private Sub ProfileClicked(sender As Object, e As EventArgs)

        Try

            Dim controller As New gadget_repair.Controllers.ProfileController(db)

            Using dlg As New ProfileForm(controller)

                If dlg.ShowDialog(Me.FindForm()) = DialogResult.OK Then
                    ' the sidebar status card shows the signed-in name
                    sidebar.Invalidate(True)
                End If

            End Using

        Catch ex As Exception

            MessageBox.Show("Could not open your profile." & Environment.NewLine & ex.Message,
                            "Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning)

        End Try

    End Sub

    ' ==========================================================
    ' FADE OVERLAY (opaque: draws new page, old page fading out)
    ' ==========================================================

    Private Class FadeOverlay
        Inherits Control

        Private oldImage As Bitmap
        Private newImage As Bitmap
        Private _progress As Single = 0.0F

        Public Sub New(oldSnapshot As Bitmap, newSnapshot As Bitmap)

            SetStyle(ControlStyles.UserPaint Or
                     ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or
                     ControlStyles.Opaque, True)

            oldImage = oldSnapshot
            newImage = newSnapshot

        End Sub

        Public Property Progress As Single
            Get
                Return _progress
            End Get
            Set(value As Single)
                _progress = Math.Max(0.0F, Math.Min(1.0F, value))
                Invalidate()
            End Set
        End Property

        Protected Overrides Sub OnPaint(e As PaintEventArgs)

            Dim g As Graphics = e.Graphics

            g.Clear(BackgroundColor)

            If newImage IsNot Nothing Then
                g.DrawImageUnscaled(newImage, 0, 0)
            End If

            Dim alpha As Single = 1.0F - _progress

            If oldImage IsNot Nothing AndAlso alpha > 0.01F Then

                Using attrs As New ImageAttributes()

                    Dim matrix As New ColorMatrix()
                    matrix.Matrix33 = alpha
                    attrs.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap)

                    g.DrawImage(oldImage,
                                New Rectangle(0, 0, oldImage.Width, oldImage.Height),
                                0, 0, oldImage.Width, oldImage.Height,
                                GraphicsUnit.Pixel, attrs)

                End Using

            End If

        End Sub

        Protected Overrides Sub Dispose(disposing As Boolean)

            If disposing Then

                If oldImage IsNot Nothing Then
                    oldImage.Dispose()
                    oldImage = Nothing
                End If

                If newImage IsNot Nothing Then
                    newImage.Dispose()
                    newImage = Nothing
                End If

            End If

            MyBase.Dispose(disposing)

        End Sub

    End Class

End Class