Imports System
Imports System.Windows.Forms
Imports gadget_repair.Data
Imports gadget_repair.Forms

Friend Module Program

    <STAThread()>
    Friend Sub Main(args As String())

        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        ' ============================================================
        ' DATABASE INITIALIZATION
        ' ============================================================

        Try

            DbInitializer.Initialize()

        Catch ex As Exception

            ' ex.ToString() includes the type, inner exceptions and stack trace
            MessageBox.Show(
                "Database error:" & vbCrLf & ex.ToString(),
                "Gadget Repair",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            )

            Return

        End Try

        ' ============================================================
        ' LOGIN (admin and technician share one form)
        ' ============================================================

        AppSession.SignOut()

        Using login As New LoginForm()

            If login.ShowDialog() <> DialogResult.OK Then
                Return
            End If

        End Using

        ' ============================================================
        ' MAIN APPLICATION
        ' ============================================================

        Using db As ApplicationDbContext = DbFactory.Create()
            Application.Run(New MainWindow(db))
        End Using

        AppSession.SignOut()

    End Sub

End Module