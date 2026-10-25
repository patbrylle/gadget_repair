Imports System.Drawing
Imports System.Windows.Forms
Imports gadget_repair.Data

Public Class MainWindow
    Inherits Form

    Private db As ApplicationDbContext
    Private mainLayout As MainLayout

    Public Sub New(database As ApplicationDbContext)

        db = database

        Text = "Gadget Repair"
        StartPosition = FormStartPosition.CenterScreen
        ClientSize = New Size(1280, 800)
        MinimumSize = New Size(900, 600)

        ShowLayout()

    End Sub

    Private Sub DisposeLayout()

        If mainLayout Is Nothing Then Return

        RemoveHandler mainLayout.LogoutRequested, AddressOf OnLogoutRequested

        Controls.Remove(mainLayout)
        mainLayout.Dispose()
        mainLayout = Nothing

    End Sub

    Private Sub ShowLayout()

        Try

            DisposeLayout()

            mainLayout = New MainLayout(db)
            mainLayout.Dock = DockStyle.Fill

            AddHandler mainLayout.LogoutRequested, AddressOf OnLogoutRequested

            Controls.Add(mainLayout)

        Catch ex As Exception

            MessageBox.Show(
                ex.ToString(),
                "MainWindow Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

            Throw

        End Try

    End Sub

    Private Sub OnLogoutRequested(sender As Object, e As EventArgs)

        ' remove the signed-in screen first so nothing keeps using the old session
        DisposeLayout()
        Hide()

        Dim signedIn As Boolean = False

        Try

            Using login As New LoginForm()
                signedIn = (login.ShowDialog() = DialogResult.OK)
            End Using

        Catch ex As Exception

            MessageBox.Show(
                "The login screen could not be opened." & Environment.NewLine & Environment.NewLine & ex.Message,
                "Logout",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        End Try

        If Not signedIn Then
            Close()
            Return
        End If

        Try

            ' new session = new database context
            Dim fresh As ApplicationDbContext = DbFactory.Create()
            Dim old As ApplicationDbContext = db

            db = fresh

            If old IsNot Nothing Then old.Dispose()

            ShowLayout()
            Show()
            Activate()

        Catch ex As Exception

            MessageBox.Show(
                ex.ToString(),
                "Could not start a new session",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

            Close()

        End Try

    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)

        MyBase.OnFormClosed(e)

        AppSession.SignOut()

        If db IsNot Nothing Then
            db.Dispose()
            db = Nothing
        End If

    End Sub

End Class