Imports System
Imports ClosedXML.Excel
Imports gadget_repair.Controllers

Namespace Services

    Public Class ExcelExporter

        Public Shared Sub Export(d As ReportData, shop As ShopInfo, path As String)

            Dim colCount As Integer = d.Columns.Count
            Dim moneyFmt As String = """" & ChrW(&H20B1) & """#,##0.00"
            Dim plainMoney As Func(Of Decimal, String) = Function(m) m.ToString("#,##0.00")

            Dim widths(colCount - 1) As Integer

            For c As Integer = 0 To colCount - 1
                widths(c) = Math.Max(10, d.Columns(c).Header.Length + 2)
            Next

            Using wb As New XLWorkbook()

                Dim ws As IXLWorksheet = wb.Worksheets.Add("Report")
                Dim r As Integer = 1

                ws.Cell(r, 1).Value = shop.Name
                ws.Cell(r, 1).Style.Font.Bold = True
                ws.Cell(r, 1).Style.Font.FontSize = 14
                r += 1

                Dim contact As String = shop.Address
                If shop.Contact <> "" Then contact = (contact & "  |  " & shop.Contact).Trim(" "c, "|"c)
                If shop.Email <> "" Then contact = (contact & "  |  " & shop.Email).Trim(" "c, "|"c)

                If contact <> "" Then
                    ws.Cell(r, 1).Value = contact
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.Gray
                    r += 1
                End If

                ws.Cell(r, 1).Value = d.Title
                ws.Cell(r, 1).Style.Font.Bold = True
                ws.Cell(r, 1).Style.Font.FontSize = 16
                r += 1

                ws.Cell(r, 1).Value = d.Subtitle
                r += 1

                ws.Cell(r, 1).Value = "Generated " & d.GeneratedAt.ToString("MMM dd, yyyy h:mm tt")
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.Gray
                r += 2

                For Each s As ReportSummaryItem In d.Summary
                    ws.Cell(r, 1).Value = s.Label
                    SetCell(ws.Cell(r, 2), s.Kind, s.Value, moneyFmt)
                    ws.Cell(r, 2).Style.Font.Bold = True
                    ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
                    r += 1
                Next

                If d.Summary.Count > 0 Then r += 1

                ' ---- table header ----
                Dim headerRow As Integer = r

                For c As Integer = 0 To colCount - 1
                    ws.Cell(r, c + 1).Value = d.Columns(c).Header
                Next

                Dim head As IXLRange = ws.Range(r, 1, r, colCount)
                head.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2338")
                head.Style.Font.FontColor = XLColor.White
                head.Style.Font.Bold = True
                r += 1

                ' ---- rows ----
                For Each row As Object() In d.Rows

                    For c As Integer = 0 To colCount - 1
                        SetCell(ws.Cell(r, c + 1), d.Columns(c).Kind, row(c), moneyFmt)
                        widths(c) = Math.Max(widths(c), d.Format(d.Columns(c).Kind, row(c), plainMoney).Length + 2)
                    Next

                    If (r - headerRow) Mod 2 = 0 Then
                        ws.Range(r, 1, r, colCount).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F5F9")
                    End If

                    r += 1

                Next

                Dim lastDataRow As Integer = r - 1

                ' ---- totals ----
                If d.Totals IsNot Nothing Then

                    For c As Integer = 0 To colCount - 1
                        SetCell(ws.Cell(r, c + 1), d.Columns(c).Kind, d.Totals(c), moneyFmt)
                    Next

                    Dim tot As IXLRange = ws.Range(r, 1, r, colCount)
                    tot.Style.Font.Bold = True
                    tot.Style.Border.TopBorder = XLBorderStyleValues.Thin
                    tot.Style.Fill.BackgroundColor = XLColor.FromHtml("#E1E8F2")

                End If

                ' ---- alignment, widths, freeze, filter ----
                For c As Integer = 0 To colCount - 1

                    If d.Columns(c).Align = ReportAlign.Right Then
                        ws.Range(headerRow, c + 1, r, c + 1).Style.Alignment.Horizontal =
                            XLAlignmentHorizontalValues.Right
                    End If

                    ws.Column(c + 1).Width = Math.Min(48, Math.Max(12, widths(c)))

                Next

                ws.SheetView.FreezeRows(headerRow)

                If d.Rows.Count > 0 Then
                    ws.Range(headerRow, 1, lastDataRow, colCount).SetAutoFilter()
                End If

                ws.PageSetup.PageOrientation = XLPageOrientation.Landscape

                wb.SaveAs(path)

            End Using

        End Sub

        Private Shared Sub SetCell(cell As IXLCell, kind As ColumnKind, value As Object, moneyFmt As String)

            If value Is Nothing Then Return

            Select Case kind

                Case ColumnKind.Money
                    cell.Value = Convert.ToDouble(value)
                    cell.Style.NumberFormat.Format = moneyFmt

                Case ColumnKind.Whole
                    cell.Value = Convert.ToDouble(value)
                    cell.Style.NumberFormat.Format = "#,##0"

                Case ColumnKind.CalendarDate
                    If TypeOf value Is DateTime Then
                        cell.Value = DirectCast(value, DateTime)
                        cell.Style.NumberFormat.Format = "mmm dd, yyyy"
                    Else
                        cell.Value = Convert.ToString(value)
                    End If

                Case Else
                    cell.Value = Convert.ToString(value)

            End Select

        End Sub

    End Class

End Namespace