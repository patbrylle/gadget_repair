Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports gadget_repair.Controllers

Namespace Services

    ' ================================================================
    ' CANVAS (same drawing code feeds the printer and the PDF writer)
    ' ================================================================
    Public Interface IReportCanvas
        Sub FillRect(x As Single, y As Single, w As Single, h As Single, ink As Color)
        Sub StrokeRect(x As Single, y As Single, w As Single, h As Single, ink As Color, lineWidth As Single)
        Sub Line(x1 As Single, y1 As Single, x2 As Single, y2 As Single, ink As Color, lineWidth As Single)
        Sub Text(s As String, x As Single, y As Single, w As Single, h As Single,
                 size As Single, bold As Boolean, ink As Color, align As ReportAlign)
    End Interface

    ' ---------------- GDI+ (print / preview) ----------------
    Public Class GdiCanvas
        Implements IReportCanvas

        Private ReadOnly g As Graphics

        Public Sub New(graphics As Graphics)
            g = graphics
        End Sub

        Public Sub FillRect(x As Single, y As Single, w As Single, h As Single, ink As Color) _
            Implements IReportCanvas.FillRect
            Using b As New SolidBrush(ink)
                g.FillRectangle(b, x, y, w, h)
            End Using
        End Sub

        Public Sub StrokeRect(x As Single, y As Single, w As Single, h As Single, ink As Color, lineWidth As Single) _
            Implements IReportCanvas.StrokeRect
            Using p As New Pen(ink, lineWidth)
                g.DrawRectangle(p, x, y, w, h)
            End Using
        End Sub

        Public Sub Line(x1 As Single, y1 As Single, x2 As Single, y2 As Single, ink As Color, lineWidth As Single) _
            Implements IReportCanvas.Line
            Using p As New Pen(ink, lineWidth)
                g.DrawLine(p, x1, y1, x2, y2)
            End Using
        End Sub

        Public Sub Text(s As String, x As Single, y As Single, w As Single, h As Single,
                        size As Single, bold As Boolean, ink As Color, align As ReportAlign) _
            Implements IReportCanvas.Text

            If String.IsNullOrEmpty(s) Then Return

            Dim font As Font = Theme.UiFont(size, If(bold, FontStyle.Bold, FontStyle.Regular))

            Using sf As New StringFormat(StringFormatFlags.NoWrap)
                sf.Trimming = StringTrimming.EllipsisCharacter
                sf.LineAlignment = StringAlignment.Center
                sf.Alignment = If(align = ReportAlign.Right, StringAlignment.Far,
                                  If(align = ReportAlign.Center, StringAlignment.Center, StringAlignment.Near))

                Using b As New SolidBrush(ink)
                    g.DrawString(s, font, b, New RectangleF(x, y, w, h), sf)
                End Using
            End Using

        End Sub

    End Class

    ' ---------------- minimal PDF writer (Helvetica, no packages) ----------------
    Public Class PdfCanvas
        Implements IReportCanvas

        Private ReadOnly pages As New List(Of StringBuilder)()
        Private cur As StringBuilder

        ' Helvetica widths for ASCII 32..126 (1/1000 em)
        Private Shared ReadOnly HelvW() As Integer = {
            278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
            556, 556, 556, 556, 556, 556, 556, 556, 556, 556,
            278, 278, 584, 584, 584, 556, 1015,
            667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611,
            278, 278, 278, 469, 556, 333,
            556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, 556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500,
            334, 260, 334, 584}

        Public Sub NewPage()
            cur = New StringBuilder()
            pages.Add(cur)
        End Sub

        Private Shared Function Num(v As Single) As String
            Return v.ToString("0.##", CultureInfo.InvariantCulture)
        End Function

        Private Shared Function Rgb(c As Color) As String
            Return (c.R / 255.0).ToString("0.###", CultureInfo.InvariantCulture) & " " &
                   (c.G / 255.0).ToString("0.###", CultureInfo.InvariantCulture) & " " &
                   (c.B / 255.0).ToString("0.###", CultureInfo.InvariantCulture)
        End Function

        Private Shared Function Y(v As Single) As Single
            Return ReportLayout.PageH - v
        End Function

        Public Shared Function Measure(s As String, size As Single, bold As Boolean) As Single

            Dim total As Integer = 0

            For Each ch As Char In s
                Dim code As Integer = AscW(ch)
                total += If(code >= 32 AndAlso code <= 126, HelvW(code - 32), 556)
            Next

            Return total * size / 1000.0F * If(bold, 1.06F, 1.0F)

        End Function

        Private Shared Function Fit(s As String, w As Single, size As Single, bold As Boolean) As String

            If Measure(s, size, bold) <= w Then Return s

            Dim n As Integer = s.Length

            While n > 0 AndAlso Measure(s.Substring(0, n) & "...", size, bold) > w
                n -= 1
            End While

            Return If(n <= 0, "", s.Substring(0, n) & "...")

        End Function

        Private Shared Function Escape(s As String) As String

            Dim sb As New StringBuilder()

            For Each ch As Char In s
                Dim code As Integer = AscW(ch)
                If code < 32 Then
                    sb.Append(" "c)
                ElseIf code > 255 Then
                    sb.Append("?"c)
                ElseIf ch = "("c OrElse ch = ")"c OrElse ch = "\"c Then
                    sb.Append("\"c).Append(ch)
                Else
                    sb.Append(ch)
                End If
            Next

            Return sb.ToString()

        End Function

        Public Sub FillRect(x As Single, y2 As Single, w As Single, h As Single, ink As Color) _
            Implements IReportCanvas.FillRect
            cur.Append(Rgb(ink)).Append(" rg ").Append(Num(x)).Append(" ").Append(Num(Y(y2 + h))).
                Append(" ").Append(Num(w)).Append(" ").Append(Num(h)).Append(" re f").Append(vbLf)
        End Sub

        Public Sub StrokeRect(x As Single, y2 As Single, w As Single, h As Single, ink As Color, lineWidth As Single) _
            Implements IReportCanvas.StrokeRect
            cur.Append(Rgb(ink)).Append(" RG ").Append(Num(lineWidth)).Append(" w ").
                Append(Num(x)).Append(" ").Append(Num(Y(y2 + h))).Append(" ").
                Append(Num(w)).Append(" ").Append(Num(h)).Append(" re S").Append(vbLf)
        End Sub

        Public Sub Line(x1 As Single, y1 As Single, x2 As Single, y2 As Single, ink As Color, lineWidth As Single) _
            Implements IReportCanvas.Line
            cur.Append(Rgb(ink)).Append(" RG ").Append(Num(lineWidth)).Append(" w ").
                Append(Num(x1)).Append(" ").Append(Num(Y(y1))).Append(" m ").
                Append(Num(x2)).Append(" ").Append(Num(Y(y2))).Append(" l S").Append(vbLf)
        End Sub

        Public Sub Text(s As String, x As Single, y2 As Single, w As Single, h As Single,
                        size As Single, bold As Boolean, ink As Color, align As ReportAlign) _
            Implements IReportCanvas.Text

            If String.IsNullOrEmpty(s) Then Return

            Dim shown As String = Fit(s, w, size, bold)

            If shown = "" Then Return

            Dim tw As Single = Measure(shown, size, bold)
            Dim px As Single = x

            If align = ReportAlign.Right Then
                px = x + w - tw
            ElseIf align = ReportAlign.Center Then
                px = x + (w - tw) / 2.0F
            End If

            Dim baseline As Single = y2 + h / 2.0F + size * 0.35F

            cur.Append(Rgb(ink)).Append(" rg BT /").Append(If(bold, "F2", "F1")).Append(" ").
                Append(Num(size)).Append(" Tf ").Append(Num(px)).Append(" ").Append(Num(Y(baseline))).
                Append(" Td (").Append(Escape(shown)).Append(") Tj ET").Append(vbLf)

        End Sub

        Private Shared Sub Put(fs As FileStream, text As String)
            Dim bytes() As Byte = Encoding.Latin1.GetBytes(text)
            fs.Write(bytes, 0, bytes.Length)
        End Sub

        Public Sub Save(path As String)

            If pages.Count = 0 Then NewPage()

            Dim pc As Integer = pages.Count
            Dim offsets As New List(Of Long)()

            Using fs As New FileStream(path, FileMode.Create, FileAccess.Write)

                Put(fs, "%PDF-1.4" & vbLf)

                offsets.Add(fs.Position)
                Put(fs, "1 0 obj" & vbLf & "<< /Type /Catalog /Pages 2 0 R >>" & vbLf & "endobj" & vbLf)

                Dim kids As New StringBuilder()
                For i As Integer = 0 To pc - 1
                    kids.Append(5 + 2 * i).Append(" 0 R ")
                Next

                offsets.Add(fs.Position)
                Put(fs, "2 0 obj" & vbLf & "<< /Type /Pages /Kids [" & kids.ToString() & "] /Count " & pc & " >>" & vbLf & "endobj" & vbLf)

                offsets.Add(fs.Position)
                Put(fs, "3 0 obj" & vbLf & "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>" & vbLf & "endobj" & vbLf)

                offsets.Add(fs.Position)
                Put(fs, "4 0 obj" & vbLf & "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>" & vbLf & "endobj" & vbLf)

                For i As Integer = 0 To pc - 1

                    Dim pageId As Integer = 5 + 2 * i
                    Dim contentId As Integer = 6 + 2 * i
                    Dim content As String = pages(i).ToString()

                    offsets.Add(fs.Position)
                    Put(fs, pageId & " 0 obj" & vbLf &
                        "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " & Num(ReportLayout.PageW) & " " & Num(ReportLayout.PageH) &
                        "] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " & contentId & " 0 R >>" & vbLf & "endobj" & vbLf)

                    offsets.Add(fs.Position)
                    Put(fs, contentId & " 0 obj" & vbLf & "<< /Length " & content.Length & " >>" & vbLf &
                        "stream" & vbLf & content & "endstream" & vbLf & "endobj" & vbLf)

                Next

                Dim xrefPos As Long = fs.Position
                Dim size As Integer = offsets.Count + 1

                Put(fs, "xref" & vbLf & "0 " & size & vbLf & "0000000000 65535 f " & vbLf)

                For Each o As Long In offsets
                    Put(fs, o.ToString("0000000000") & " 00000 n " & vbLf)
                Next

                Put(fs, "trailer" & vbLf & "<< /Size " & size & " /Root 1 0 R >>" & vbLf &
                    "startxref" & vbLf & xrefPos & vbLf & "%%EOF" & vbLf)

            End Using

        End Sub

    End Class

    ' ================================================================
    ' LAYOUT (A4 landscape, units = points)
    ' ================================================================
    Public Class ReportLayout

        Public Const PageW As Single = 841.89F
        Public Const PageH As Single = 595.28F

        Private Const Margin As Single = 36.0F
        Private Const RowH As Single = 20.0F
        Private Const HeadH As Single = 24.0F
        Private Const BottomLimit As Single = PageH - 44.0F

        Private Shared ReadOnly Ink As Color = Color.FromArgb(24, 30, 46)
        Private Shared ReadOnly Muted As Color = Color.FromArgb(110, 120, 140)
        Private Shared ReadOnly Rule As Color = Color.FromArgb(210, 215, 225)
        Private Shared ReadOnly HeadFill As Color = Color.FromArgb(27, 35, 56)
        Private Shared ReadOnly Zebra As Color = Color.FromArgb(244, 246, 250)
        Private Shared ReadOnly TotalFill As Color = Color.FromArgb(225, 232, 242)

        Private Shared Function ItemCount(d As ReportData) As Integer
            Return d.Rows.Count + If(d.Totals IsNot Nothing, 1, 0)
        End Function

        ' each entry: {startIndex, count}
        Public Shared Function Paginate(d As ReportData) As List(Of Integer())

            Dim total As Integer = ItemCount(d)
            Dim top1 As Single = If(d.Summary.Count > 0, 190.0F, 130.0F)
            Dim cap1 As Integer = Math.Max(1, CInt(Math.Floor((BottomLimit - top1 - HeadH) / RowH)))
            Dim capN As Integer = Math.Max(1, CInt(Math.Floor((BottomLimit - 64.0F - HeadH) / RowH)))

            Dim pages As New List(Of Integer())()
            Dim start As Integer = 0
            Dim cap As Integer = cap1

            Do
                Dim cnt As Integer = Math.Max(0, Math.Min(cap, total - start))
                pages.Add(New Integer() {start, cnt})
                start += cnt
                cap = capN
            Loop While start < total

            Return pages

        End Function

        Private Shared Function StatusInk(s As String) As Color

            Select Case s
                Case "Paid", "Completed", "In Stock"
                    Return Color.FromArgb(0, 140, 90)
                Case "Delivered", "In Progress"
                    Return Color.FromArgb(0, 120, 190)
                Case "Pending", "Partially Paid", "Low Stock"
                    Return Color.FromArgb(190, 130, 0)
                Case "Waiting for Parts"
                    Return Color.FromArgb(210, 110, 0)
                Case "Cancelled", "Unpaid", "Out of Stock"
                    Return Color.FromArgb(200, 40, 70)
                Case Else
                    Return Ink
            End Select

        End Function

        Public Shared Sub DrawPage(
            c As IReportCanvas,
            d As ReportData,
            shop As ShopInfo,
            pages As List(Of Integer()),
            pageIndex As Integer,
            money As Func(Of Decimal, String))

            Dim contentW As Single = PageW - 2 * Margin
            Dim y As Single

            If pageIndex = 0 Then

                c.Text(shop.Name, Margin, 36, 520, 20, 14, True, Ink, ReportAlign.Left)
                c.Text("Generated " & d.GeneratedAt.ToString("MMM dd, yyyy h:mm tt"),
                       PageW - Margin - 250, 36, 250, 20, 8, False, Muted, ReportAlign.Right)

                Dim info As String = shop.Address

                If shop.Contact <> "" Then info = (info & "   |   " & shop.Contact).Trim(" "c, "|"c)
                If shop.Email <> "" Then info = (info & "   |   " & shop.Email).Trim(" "c, "|"c)

                c.Text(info, Margin, 56, contentW, 14, 8, False, Muted, ReportAlign.Left)
                c.Line(Margin, 74, PageW - Margin, 74, Rule, 1.0F)

                c.Text(d.Title, Margin, 82, contentW, 26, 18, True, Ink, ReportAlign.Left)
                c.Text(d.Subtitle, Margin, 108, contentW, 14, 9, False, Muted, ReportAlign.Left)

                y = 130

                If d.Summary.Count > 0 Then
                    DrawSummary(c, d, money, y, contentW)
                    y = 190
                End If

            Else

                c.Text(d.Title, Margin, 36, 500, 16, 10, True, Ink, ReportAlign.Left)
                c.Text(shop.Name, PageW - Margin - 300, 36, 300, 16, 8, False, Muted, ReportAlign.Right)
                c.Line(Margin, 56, PageW - Margin, 56, Rule, 1.0F)
                y = 64

            End If

            ' ---------- table ----------
            Dim weightSum As Single = 0.0F

            For Each col As ReportColumn In d.Columns
                weightSum += col.Weight
            Next

            Dim xs(d.Columns.Count) As Single
            xs(0) = Margin

            For i As Integer = 0 To d.Columns.Count - 1
                xs(i + 1) = xs(i) + contentW * d.Columns(i).Weight / weightSum
            Next

            c.FillRect(Margin, y, contentW, HeadH, HeadFill)

            For i As Integer = 0 To d.Columns.Count - 1
                c.Text(d.Columns(i).Header, xs(i) + 6, y, xs(i + 1) - xs(i) - 12, HeadH,
                       7.5F, True, Color.White, d.Columns(i).Align)
            Next

            Dim slice As Integer() = pages(pageIndex)
            Dim total As Integer = ItemCount(d)

            If total = 0 Then
                c.Text("No records found for the selected filters.", Margin, y + HeadH + 10, contentW, 20,
                       9, False, Muted, ReportAlign.Center)
            End If

            For k As Integer = 0 To slice(1) - 1

                Dim idx As Integer = slice(0) + k
                Dim isTotal As Boolean = (d.Totals IsNot Nothing AndAlso idx = d.Rows.Count)
                Dim row As Object() = If(isTotal, d.Totals, d.Rows(idx))
                Dim ry As Single = y + HeadH + k * RowH

                If isTotal Then
                    c.FillRect(Margin, ry, contentW, RowH, TotalFill)
                ElseIf k Mod 2 = 1 Then
                    c.FillRect(Margin, ry, contentW, RowH, Zebra)
                End If

                For i As Integer = 0 To d.Columns.Count - 1

                    Dim col As ReportColumn = d.Columns(i)
                    Dim text As String = d.Format(col.Kind, row(i), money)
                    Dim ink2 As Color = If(col.Kind = ColumnKind.Status, StatusInk(text), Ink)
                    Dim bold As Boolean = isTotal OrElse col.Kind = ColumnKind.Status

                    c.Text(text, xs(i) + 6, ry, xs(i + 1) - xs(i) - 12, RowH, 8.0F, bold, ink2, col.Align)

                Next

                c.Line(Margin, ry + RowH, PageW - Margin, ry + RowH, Rule, 0.5F)

            Next

            ' ---------- footer ----------
            c.Line(Margin, PageH - 34, PageW - Margin, PageH - 34, Rule, 0.5F)
            c.Text(shop.Name & "  |  " & d.Title, Margin, PageH - 32, 500, 14, 7.5F, False, Muted, ReportAlign.Left)
            c.Text("Page " & (pageIndex + 1) & " of " & pages.Count,
                   PageW - Margin - 120, PageH - 32, 120, 14, 7.5F, False, Muted, ReportAlign.Right)

        End Sub

        Private Shared Sub DrawSummary(
            c As IReportCanvas,
            d As ReportData,
            money As Func(Of Decimal, String),
            y As Single,
            contentW As Single)

            Dim n As Integer = d.Summary.Count
            Dim gap As Single = 10.0F
            Dim bw As Single = (contentW - gap * (n - 1)) / n

            For i As Integer = 0 To n - 1

                Dim bx As Single = Margin + i * (bw + gap)
                Dim item As ReportSummaryItem = d.Summary(i)

                c.FillRect(bx, y, bw, 46, Zebra)
                c.StrokeRect(bx, y, bw, 46, Rule, 0.75F)
                c.Text(item.Label.ToUpper(), bx + 10, y + 6, bw - 20, 12, 7, True, Muted, ReportAlign.Left)
                c.Text(d.Format(item.Kind, item.Value, money), bx + 10, y + 20, bw - 20, 22,
                       13, True, Ink, ReportAlign.Left)

            Next

        End Sub

    End Class

    ' ================================================================
    ' EXPORT / PRINT ENTRY POINTS
    ' ================================================================
    Public Class ReportExporter

        Public Shared Sub ExportPdf(d As ReportData, shop As ShopInfo, path As String)

            Dim pdf As New PdfCanvas()
            Dim pages As List(Of Integer()) = ReportLayout.Paginate(d)

            ' Helvetica has no peso sign, so the PDF writes "PHP"
            Dim money As Func(Of Decimal, String) =
                Function(m) "PHP " & m.ToString("#,##0.00", CultureInfo.InvariantCulture)

            For i As Integer = 0 To pages.Count - 1
                pdf.NewPage()
                ReportLayout.DrawPage(pdf, d, shop, pages, i, money)
            Next

            pdf.Save(path)

        End Sub

        Public Shared Sub PrintPreview(owner As IWin32Window, d As ReportData, shop As ShopInfo)

            Dim pages As List(Of Integer()) = ReportLayout.Paginate(d)
            Dim index As Integer = 0
            Dim money As Func(Of Decimal, String) = Function(m) Theme.FormatPeso(m)

            Using doc As New PrintDocument()

                doc.DocumentName = d.Title
                doc.DefaultPageSettings.Landscape = True
                doc.DefaultPageSettings.Margins = New Margins(0, 0, 0, 0)

                AddHandler doc.BeginPrint, Sub(s As Object, e As PrintEventArgs) index = 0

                AddHandler doc.PrintPage,
                    Sub(s As Object, e As PrintPageEventArgs)

                        Dim g As Graphics = e.Graphics

                        g.PageUnit = GraphicsUnit.Point
                        g.SmoothingMode = SmoothingMode.AntiAlias
                        g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit

                        Dim wPt As Single = e.PageBounds.Width * 0.72F
                        Dim hPt As Single = e.PageBounds.Height * 0.72F
                        Dim scale As Single = Math.Min(wPt / ReportLayout.PageW, hPt / ReportLayout.PageH)

                        g.ScaleTransform(scale, scale)

                        ReportLayout.DrawPage(New GdiCanvas(g), d, shop, pages, index, money)

                        index += 1
                        e.HasMorePages = index < pages.Count

                    End Sub

                Using dlg As New PrintPreviewDialog()
                    dlg.Document = doc
                    dlg.UseAntiAlias = True
                    dlg.Size = New Size(1100, 760)
                    dlg.StartPosition = FormStartPosition.CenterParent
                    dlg.ShowDialog(owner)
                End Using

            End Using

        End Sub

    End Class

End Namespace