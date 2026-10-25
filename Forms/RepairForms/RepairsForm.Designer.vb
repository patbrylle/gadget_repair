Imports System.ComponentModel
Imports System.Diagnostics

Namespace Forms

    Partial Public Class RepairsForm

        Private components As IContainer

        <DebuggerNonUserCode()>
        Protected Overrides Sub Dispose(disposing As Boolean)

            Try
                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                End If

            Finally
                MyBase.Dispose(disposing)
            End Try

        End Sub

        <DebuggerStepThrough()>
        Private Sub InitializeComponent()

            Me.components = New Container()

        End Sub

    End Class

End Namespace