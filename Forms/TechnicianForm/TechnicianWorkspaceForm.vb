
Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Public Class TechnicianWorkspaceForm
    Inherits Form

    Private ReadOnly mainPanel As New Panel()
    Private ReadOnly contentPanel As New Panel()
    Private ReadOnly titleLabel As New Label()
    Private ReadOnly subtitleLabel As New Label()
    Private ReadOnly jobsPanel As New FlowLayoutPanel()

    Public Sub New()
        Dim tech = TechnicianAuthController.CurrentTechnician

        If tech Is Nothing Then
            Throw New InvalidOperationException(
                "No technician is signed in.")
        End If

        Text = "Gadget Repair - Technician"
        StartPosition = FormStartPosition.CenterScreen
        ClientSize = New Size(390, 780)
        MinimumSize = New Size(360, 650)
        MaximumSize = New Size(430, 950)
        BackColor = Color.FromArgb(242, 245, 249)
        Font = New Font("Segoe UI", 10)

        mainPanel.Dock = DockStyle.Fill
        mainPanel.BackColor = Color.FromArgb(242, 245, 249)
        Controls.Add(mainPanel)

        BuildHeader(tech.TechnicianName)
        BuildJobsSection()
        BuildBottomNavigation()

        AddHandler Shown, Sub(sender, e) LoadRepairs()
        AddHandler Resize, Sub(sender, e) LayoutMobileContent()
    End Sub

    Private Sub BuildHeader(technicianName As String)
        Dim header As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 155,
            .BackColor = Color.FromArgb(22, 43, 75),
            .Padding = New Padding(20)
        }

        Dim appName As New Label With {
            .Text = "GADGET REPAIR HUB",
            .ForeColor = Color.FromArgb(164, 198, 255),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(20, 18)
        }

        titleLabel.Text = "Hello, " & technicianName & "!"
        titleLabel.ForeColor = Color.White
        titleLabel.Font = New Font("Segoe UI", 18, FontStyle.Bold)
        titleLabel.AutoSize = True
        titleLabel.Location = New Point(20, 48)

        subtitleLabel.Text = "Let's check your repair jobs."
        subtitleLabel.ForeColor = Color.Gainsboro
        subtitleLabel.Font = New Font("Segoe UI", 9)
        subtitleLabel.AutoSize = True
        subtitleLabel.Location = New Point(22, 86)

        header.Controls.Add(appName)
        header.Controls.Add(titleLabel)
        header.Controls.Add(subtitleLabel)
        mainPanel.Controls.Add(header)
    End Sub

    Private Sub BuildJobsSection()
        contentPanel.Dock = DockStyle.Fill
        contentPanel.Padding = New Padding(16, 16, 16, 10)
        contentPanel.BackColor = Color.FromArgb(242, 245, 249)

        Dim heading As New Label With {
            .Text = "My Assigned Repairs",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.FromArgb(30, 41, 59),
            .AutoSize = True,
            .Location = New Point(16, 15)
        }

        Dim refreshButton As New Button With {
            .Text = "Refresh",
            .Width = 80,
            .Height = 30,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .ForeColor = Color.FromArgb(37, 99, 235),
            .Location = New Point(250, 12),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }

        AddHandler refreshButton.Click,
            Sub(sender, e) LoadRepairs()

        jobsPanel.Location = New Point(16, 55)
        jobsPanel.Size = New Size(330, 450)
        jobsPanel.Anchor = AnchorStyles.Top Or
                           AnchorStyles.Bottom Or
                           AnchorStyles.Left Or
                           AnchorStyles.Right
        jobsPanel.FlowDirection = FlowDirection.TopDown
        jobsPanel.WrapContents = False
        jobsPanel.AutoScroll = True
        jobsPanel.BackColor = Color.Transparent

        contentPanel.Controls.Add(heading)
        contentPanel.Controls.Add(refreshButton)
        contentPanel.Controls.Add(jobsPanel)
        mainPanel.Controls.Add(contentPanel)
    End Sub

    Private Sub BuildBottomNavigation()
        Dim bottom As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 65,
            .BackColor = Color.White
        }

        Dim jobsButton As New Button With {
            .Text = "My Repairs",
            .Width = 110,
            .Height = 45,
            .FlatStyle = FlatStyle.Flat,
            .ForeColor = Color.FromArgb(37, 99, 235),
            .Location = New Point(15, 10)
        }

        Dim historyButton As New Button With {
            .Text = "History",
            .Width = 90,
            .Height = 45,
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(135, 10)
        }

        Dim logoutButton As New Button With {
            .Text = "Log Out",
            .Width = 90,
            .Height = 45,
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(235, 10)
        }

        AddHandler historyButton.Click,
            Sub(sender, e)
                MessageBox.Show(
                    "Repair history will be added next.",
                    "Repair History")
            End Sub

        AddHandler logoutButton.Click,
            Sub(sender, e)
                TechnicianAuthController.Logout()
                Close()
            End Sub

        bottom.Controls.Add(jobsButton)
        bottom.Controls.Add(historyButton)
        bottom.Controls.Add(logoutButton)
        mainPanel.Controls.Add(bottom)
    End Sub

    Private Sub LayoutMobileContent()
        Dim width As Integer = Math.Max(250, contentPanel.ClientSize.Width - 32)
        jobsPanel.Width = width
        jobsPanel.Height = Math.Max(100, contentPanel.ClientSize.Height - 70)
    End Sub

    Private Sub LoadRepairs()
        Dim tech = TechnicianAuthController.CurrentTechnician
        If tech Is Nothing Then Return

        Try
            Using db As ApplicationDbContext = DbFactory.Create()
                Dim repairs = db.Repairs _
                    .AsNoTracking() _
                    .Where(Function(r) r.TechnicianId.HasValue AndAlso
                        r.TechnicianId.Value = tech.TechnicianId) _
                    .OrderByDescending(Function(r) r.RepairDate) _
                    .ToList()

                jobsPanel.Controls.Clear()

                If repairs.Count = 0 Then
                    Dim emptyLabel As New Label With {
                        .Text = "No repair jobs assigned yet.",
                        .AutoSize = True,
                        .Padding = New Padding(8),
                        .ForeColor = Color.Gray
                    }
                    jobsPanel.Controls.Add(emptyLabel)
                    Return
                End If

                For Each repair In repairs
                    Dim jobCard As New Panel With {
                        .Width = Math.Max(260, jobsPanel.ClientSize.Width - 25),
                        .Height = 135,
                        .BackColor = Color.White,
                        .Margin = New Padding(0, 0, 0, 12),
                        .Padding = New Padding(12)
                    }

                    Dim deviceLabel As New Label With {
                        .Text = repair.Brand & " " & repair.Model,
                        .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                        .AutoSize = True,
                        .Location = New Point(12, 12)
                    }

                    Dim typeLabel As New Label With {
                        .Text = "Device: " & repair.DeviceType,
                        .AutoSize = True,
                        .Location = New Point(12, 42)
                    }

                    Dim statusLabel As New Label With {
                        .Text = "Status: " & repair.Status,
                        .AutoSize = True,
                        .ForeColor = Color.FromArgb(37, 99, 235),
                        .Location = New Point(12, 68)
                    }

                    Dim dateLabel As New Label With {
                        .Text = "Date: " & repair.RepairDate.ToString("MMM dd, yyyy"),
                        .AutoSize = True,
                        .ForeColor = Color.Gray,
                        .Location = New Point(12, 94)
                    }

                    jobCard.Controls.Add(deviceLabel)
                    jobCard.Controls.Add(typeLabel)
                    jobCard.Controls.Add(statusLabel)
                    jobCard.Controls.Add(dateLabel)
                    jobsPanel.Controls.Add(jobCard)
                Next
            End Using

        Catch ex As Exception
            MessageBox.Show(
                "Unable to load repair jobs." & Environment.NewLine & ex.Message,
                "Technician Dashboard",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub

End Class
