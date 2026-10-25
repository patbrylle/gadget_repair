Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports gadget_repair.Data
Imports gadget_repair.Models

Namespace Controllers

    Public Enum ReportKind
        Repairs = 0
        Payments = 1
        Inventory = 2
        Technicians = 3
        Customers = 4
    End Enum

    Public Enum ColumnKind
        Text
        Whole
        Money
        CalendarDate
        Status
    End Enum

    Public Enum ReportAlign
        Left
        Center
        Right
    End Enum

    Public Class ReportColumn
        Public Property Header As String = ""
        Public Property Weight As Single = 1.0F
        Public Property Kind As ColumnKind = ColumnKind.Text
        Public Property Align As ReportAlign = ReportAlign.Left

        Public Sub New(header As String, weight As Single, kind As ColumnKind)
            Me.Header = header
            Me.Weight = weight
            Me.Kind = kind
            If kind = ColumnKind.Whole OrElse kind = ColumnKind.Money Then
                Align = ReportAlign.Right
            End If
        End Sub
    End Class

    Public Class ReportSummaryItem
        Public Property Label As String = ""
        Public Property Value As Object
        Public Property Kind As ColumnKind = ColumnKind.Text
    End Class

    Public Class ReportData
        Public Property Title As String = ""
        Public Property Subtitle As String = ""
        Public Property Columns As New List(Of ReportColumn)
        Public Property Rows As New List(Of Object())
        Public Property Totals As Object() = Nothing
        Public Property Summary As New List(Of ReportSummaryItem)
        Public Property GeneratedAt As DateTime = DateTime.Now

        Public Function Format(kind As ColumnKind, value As Object, money As Func(Of Decimal, String)) As String

            If value Is Nothing OrElse IsDBNull(value) Then Return ""

            Select Case kind
                Case ColumnKind.Money
                    Return money(Convert.ToDecimal(value))
                Case ColumnKind.Whole
                    Return Convert.ToInt64(value).ToString("N0")
                Case ColumnKind.CalendarDate
                    If TypeOf value Is DateTime Then
                        Return DirectCast(value, DateTime).ToString("MMM dd, yyyy")
                    End If
                    Return Convert.ToString(value)
                Case Else
                    Return Convert.ToString(value)
            End Select

        End Function

        Public Sub AddSummary(label As String, value As Object, kind As ColumnKind)
            Summary.Add(New ReportSummaryItem With {.Label = label, .Value = value, .Kind = kind})
        End Sub
    End Class

    Public Class ReportController

        Public Shared ReadOnly PeriodList As String() = {
            "Today", "This Week", "This Month", "Last 30 Days",
            "This Year", "All Time", "Custom"
        }

        Public Shared ReadOnly ReportNames As String() = {
            "Repairs Report", "Payments & Revenue", "Inventory Stock",
            "Technician Performance", "Customer Activity"
        }

        Private ReadOnly db As ApplicationDbContext

        Public Sub New(context As ApplicationDbContext)
            db = context
        End Sub

        ' ============================================================
        ' OPTIONS USED BY THE FORM
        ' ============================================================

        Public Shared Function StatusOptions(kind As ReportKind) As String()

            Select Case kind
                Case ReportKind.Repairs
                    Dim list As New List(Of String)({"All Status"})
                    list.AddRange(RepairController.StatusList)
                    Return list.ToArray()
                Case ReportKind.Payments
                    Return New String() {"All Status", "Paid", "Partially Paid", "Unpaid"}
                Case ReportKind.Inventory
                    Return New String() {"All Status", "In Stock", "Low Stock", "Out of Stock"}
                Case Else
                    Return New String() {}
            End Select

        End Function

        Public Shared Sub ResolvePeriod(
            period As String,
            customFrom As DateTime,
            customTo As DateTime,
            ByRef fromDate As DateTime,
            ByRef toExclusive As DateTime,
            ByRef label As String)

            Dim today As DateTime = DateTime.Today

            Select Case period

                Case "Today"
                    fromDate = today
                    toExclusive = today.AddDays(1)

                Case "This Week"
                    Dim diff As Integer = (CInt(today.DayOfWeek) + 6) Mod 7
                    fromDate = today.AddDays(-diff)
                    toExclusive = fromDate.AddDays(7)

                Case "This Month"
                    fromDate = New DateTime(today.Year, today.Month, 1)
                    toExclusive = fromDate.AddMonths(1)

                Case "Last 30 Days"
                    fromDate = today.AddDays(-29)
                    toExclusive = today.AddDays(1)

                Case "This Year"
                    fromDate = New DateTime(today.Year, 1, 1)
                    toExclusive = fromDate.AddYears(1)

                Case "All Time"
                    fromDate = New DateTime(2000, 1, 1)
                    toExclusive = today.AddYears(50)

                Case Else
                    fromDate = customFrom.Date
                    toExclusive = customTo.Date.AddDays(1)
                    If toExclusive <= fromDate Then toExclusive = fromDate.AddDays(1)

            End Select

            If period = "All Time" Then
                label = "All time"
            ElseIf toExclusive.AddDays(-1).Date = fromDate.Date Then
                label = fromDate.ToString("MMM dd, yyyy")
            Else
                label = fromDate.ToString("MMM dd, yyyy") & " - " &
                        toExclusive.AddDays(-1).ToString("MMM dd, yyyy")
            End If

        End Sub

        ' ============================================================
        ' BUILD
        ' ============================================================

        Public Function Build(
            kind As ReportKind,
            period As String,
            customFrom As DateTime,
            customTo As DateTime,
            statusFilter As String) As ReportData

            If Not AppSession.IsAdmin Then
                Throw New UnauthorizedAccessException("Only an administrator can view reports.")
            End If

            Dim f As DateTime
            Dim t As DateTime
            Dim label As String = ""

            ResolvePeriod(period, customFrom, customTo, f, t, label)

            Select Case kind
                Case ReportKind.Repairs
                    Return BuildRepairs(f, t, label, statusFilter)
                Case ReportKind.Payments
                    Return BuildPayments(f, t, label, statusFilter)
                Case ReportKind.Inventory
                    Return BuildInventory(statusFilter)
                Case ReportKind.Technicians
                    Return BuildTechnicians(f, t, label)
                Case ReportKind.Customers
                    Return BuildCustomers(f, t, label)
            End Select

            Throw New ArgumentException("Unknown report.")

        End Function

        ' ============================================================
        ' HELPERS
        ' ============================================================

        Private Shared Function IsFiltered(status As String) As Boolean
            Return Not String.IsNullOrWhiteSpace(status) AndAlso status <> "All Status"
        End Function

        Private Shared Function Subtitle(periodLabel As String, status As String) As String
            Dim text As String = "Period: " & periodLabel
            If IsFiltered(status) Then text &= "   |   Status: " & status
            Return text
        End Function

        Private Shared Function DeviceText(r As Repair) As String
            Dim d As String = (If(r.Brand, "") & " " & If(r.Model, "")).Trim()
            If d = "" Then d = If(r.DeviceType, "").Trim()
            If d = "" Then d = "-"
            Return d
        End Function

Private Shared Function CustomerNameOf(map As Dictionary(Of Integer, String), id As Integer) As String     
       Dim name As String = Nothing
            If map.TryGetValue(id, name) AndAlso Not String.IsNullOrWhiteSpace(name) Then Return name
            Return "Unknown customer"
        End Function

        Private Function CustomerLookup() As Dictionary(Of Integer, String)
            Return db.Customers.AsNoTracking().ToList().
                ToDictionary(Function(c) c.CustomerId, Function(c) If(c.CustomerName, ""))
        End Function

        Private Function PaidLookup() As Dictionary(Of Integer, Decimal)
            Return db.Payments.AsNoTracking().ToList().
                GroupBy(Function(p) p.RepairId).
                ToDictionary(Function(g) g.Key, Function(g) g.Sum(Function(p) p.AmountPaid))
        End Function

        Private Shared Function PaidOf(map As Dictionary(Of Integer, Decimal), repairId As Integer) As Decimal
            Dim v As Decimal = 0D
            map.TryGetValue(repairId, v)
            Return v
        End Function

        Private Shared Function IsDone(status As String) As Boolean
            Return status = "Completed" OrElse status = "Delivered"
        End Function

        ' ============================================================
        ' 1. REPAIRS
        ' ============================================================

        Private Function BuildRepairs(f As DateTime, t As DateTime, periodLabel As String, statusFilter As String) As ReportData

            Dim q As IQueryable(Of Repair) =
                db.Repairs.AsNoTracking().
                    Include(Function(r) r.Technician).
                    Where(Function(r) r.RepairDate >= f AndAlso r.RepairDate < t)

            If IsFiltered(statusFilter) Then
                q = q.Where(Function(r) r.Status = statusFilter)
            End If

            Dim list As List(Of Repair) = q.OrderByDescending(Function(r) r.RepairDate).ToList()
            Dim customers As Dictionary(Of Integer, String) = CustomerLookup()

            Dim d As New ReportData()
            d.Title = "Repairs Report"
            d.Subtitle = Subtitle(periodLabel, statusFilter)

            d.Columns.Add(New ReportColumn("TICKET", 0.8F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("DATE", 1.1F, ColumnKind.CalendarDate))
            d.Columns.Add(New ReportColumn("CUSTOMER", 1.8F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("DEVICE", 1.8F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("TECHNICIAN", 1.4F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("STATUS", 1.3F, ColumnKind.Status))
            d.Columns.Add(New ReportColumn("COST", 1.1F, ColumnKind.Money))

            Dim totalValue As Decimal = 0D
            Dim done As Integer = 0
            Dim pendingCount As Integer = 0

            For Each r As Repair In list

                               d.Rows.Add(New Object() {
                    "#" & r.RepairId, r.RepairDate, CustomerNameOf(customers, r.CustomerId),
                    DeviceText(r), PaymentController.TechnicianName(r), r.Status, r.Cost})

                If r.Status <> "Cancelled" Then totalValue += r.Cost
                If IsDone(r.Status) Then done += 1
                If Not IsDone(r.Status) AndAlso r.Status <> "Cancelled" Then pendingCount += 1

            Next

            d.Totals = New Object() {"TOTAL", Nothing, Nothing, Nothing, Nothing, Nothing, totalValue}

            d.AddSummary("Total Repairs", list.Count, ColumnKind.Whole)
            d.AddSummary("Completed / Delivered", done, ColumnKind.Whole)
            d.AddSummary("Still Open", pendingCount, ColumnKind.Whole)
            d.AddSummary("Total Value", totalValue, ColumnKind.Money)

            Return d

        End Function

        ' ============================================================
        ' 2. PAYMENTS & REVENUE
        ' ============================================================

        Private Function BuildPayments(f As DateTime, t As DateTime, periodLabel As String, statusFilter As String) As ReportData

            Dim repairs As List(Of Repair) =
                db.Repairs.AsNoTracking().
                    Include(Function(r) r.Technician).
                    Where(Function(r) (r.Status = "Completed" OrElse r.Status = "Delivered") AndAlso
                                      r.CompletionDate.HasValue AndAlso
                                      r.CompletionDate.Value >= f AndAlso
                                      r.CompletionDate.Value < t).
                    OrderByDescending(Function(r) r.CompletionDate).
                    ToList()

            Dim customers As Dictionary(Of Integer, String) = CustomerLookup()
            Dim paidMap As Dictionary(Of Integer, Decimal) = PaidLookup()

            Dim d As New ReportData()
            d.Title = "Payments & Revenue Report"
            d.Subtitle = Subtitle(periodLabel & " (by completion date)", statusFilter)

            d.Columns.Add(New ReportColumn("TICKET", 0.8F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("COMPLETED", 1.1F, ColumnKind.CalendarDate))
            d.Columns.Add(New ReportColumn("CUSTOMER", 1.8F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("TECHNICIAN", 1.4F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("FINAL COST", 1.1F, ColumnKind.Money))
            d.Columns.Add(New ReportColumn("PAID", 1.1F, ColumnKind.Money))
            d.Columns.Add(New ReportColumn("BALANCE", 1.1F, ColumnKind.Money))
            d.Columns.Add(New ReportColumn("STATUS", 1.3F, ColumnKind.Status))

            Dim billed As Decimal = 0D
            Dim collected As Decimal = 0D
            Dim outstanding As Decimal = 0D
            Dim paidCount As Integer = 0
            Dim openCount As Integer = 0
            Dim shown As Integer = 0

            For Each r As Repair In repairs

                Dim got As Decimal = PaidOf(paidMap, r.RepairId)
                Dim status As String = PaymentController.ComputeStatus(r.Cost, got)

                If IsFiltered(statusFilter) AndAlso status <> statusFilter Then Continue For

                Dim balance As Decimal = Math.Max(0D, r.Cost - got)

                                d.Rows.Add(New Object() {
                    "#" & r.RepairId, r.CompletionDate.Value, CustomerNameOf(customers, r.CustomerId),
                    PaymentController.TechnicianName(r), r.Cost, got, balance, status})

                billed += r.Cost
                collected += got
                outstanding += balance
                shown += 1

                If status = "Paid" Then paidCount += 1 Else openCount += 1

            Next

            d.Totals = New Object() {"TOTAL", Nothing, Nothing, Nothing, billed, collected, outstanding, Nothing}

            d.AddSummary("Total Billed", billed, ColumnKind.Money)
            d.AddSummary("Collected", collected, ColumnKind.Money)
            d.AddSummary("Outstanding", outstanding, ColumnKind.Money)
            d.AddSummary("Fully Paid", paidCount, ColumnKind.Whole)
            d.AddSummary("Open Balances", openCount, ColumnKind.Whole)

            Return d

        End Function

        ' ============================================================
        ' 3. INVENTORY
        ' ============================================================

        Private Function BuildInventory(statusFilter As String) As ReportData

            Dim items As List(Of Inventory) =
                db.Inventories.AsNoTracking().OrderBy(Function(i) i.ItemName).ToList()

            Dim d As New ReportData()
            d.Title = "Inventory Stock Report"
            d.Subtitle = "As of " & DateTime.Now.ToString("MMM dd, yyyy h:mm tt") &
                         If(IsFiltered(statusFilter), "   |   Status: " & statusFilter, "")

            d.Columns.Add(New ReportColumn("ITEM", 2.3F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("CATEGORY", 1.5F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("QTY", 0.7F, ColumnKind.Whole))
            d.Columns.Add(New ReportColumn("MIN", 0.7F, ColumnKind.Whole))
            d.Columns.Add(New ReportColumn("UNIT COST", 1.1F, ColumnKind.Money))
            d.Columns.Add(New ReportColumn("SELLING PRICE", 1.2F, ColumnKind.Money))
            d.Columns.Add(New ReportColumn("STOCK VALUE", 1.2F, ColumnKind.Money))
            d.Columns.Add(New ReportColumn("STATUS", 1.2F, ColumnKind.Status))

            Dim units As Integer = 0
            Dim value As Decimal = 0D
            Dim lowCount As Integer = 0
            Dim outCount As Integer = 0
            Dim shown As Integer = 0

            For Each i As Inventory In items

                Dim status As String = InventoryController.StockStatus(i.Quantity, i.MinimumStock)

                If IsFiltered(statusFilter) AndAlso status <> statusFilter Then Continue For

                Dim stockValue As Decimal = i.Quantity * i.UnitCost

                d.Rows.Add(New Object() {
                    i.ItemName, i.Category, i.Quantity, i.MinimumStock,
                    i.UnitCost, i.SellingPrice, stockValue, status})

                shown += 1
                units += Math.Max(0, i.Quantity)
                value += stockValue

                If status = "Low Stock" Then lowCount += 1
                If status = "Out of Stock" Then outCount += 1

            Next

            d.Totals = New Object() {"TOTAL", Nothing, units, Nothing, Nothing, Nothing, value, Nothing}

            d.AddSummary("Items", shown, ColumnKind.Whole)
            d.AddSummary("Units in Stock", units, ColumnKind.Whole)
            d.AddSummary("Stock Value", value, ColumnKind.Money)
            d.AddSummary("Low Stock", lowCount, ColumnKind.Whole)
            d.AddSummary("Out of Stock", outCount, ColumnKind.Whole)

            Return d

        End Function

        ' ============================================================
        ' 4. TECHNICIAN PERFORMANCE
        ' ============================================================

        Private Function BuildTechnicians(f As DateTime, t As DateTime, periodLabel As String) As ReportData

            Dim repairs As List(Of Repair) =
                db.Repairs.AsNoTracking().
                    Where(Function(r) r.TechnicianId.HasValue AndAlso
                                      r.RepairDate >= f AndAlso r.RepairDate < t).
                    ToList()

            Dim paidMap As Dictionary(Of Integer, Decimal) = PaidLookup()

            Dim techs As List(Of Technician) =
                db.Technicians.AsNoTracking().OrderBy(Function(x) x.TechnicianName).ToList()

            Dim d As New ReportData()
            d.Title = "Technician Performance Report"
            d.Subtitle = "Period: " & periodLabel & " (by repair date)"

            d.Columns.Add(New ReportColumn("TECHNICIAN", 2.0F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("SPECIALIZATION", 1.8F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("ASSIGNED", 0.9F, ColumnKind.Whole))
            d.Columns.Add(New ReportColumn("COMPLETED", 1.0F, ColumnKind.Whole))
            d.Columns.Add(New ReportColumn("OPEN", 0.7F, ColumnKind.Whole))
            d.Columns.Add(New ReportColumn("BILLED", 1.2F, ColumnKind.Money))
            d.Columns.Add(New ReportColumn("COLLECTED", 1.2F, ColumnKind.Money))
            d.Columns.Add(New ReportColumn("RATE", 0.8F, ColumnKind.Text) With {.Align = ReportAlign.Right})

            Dim tAssigned As Integer = 0
            Dim tDone As Integer = 0
            Dim tOpen As Integer = 0
            Dim tBilled As Decimal = 0D
            Dim tCollected As Decimal = 0D
            Dim techCount As Integer = 0

            For Each tc As Technician In techs

                Dim id As Integer = tc.TechnicianId
                Dim mine As List(Of Repair) = repairs.Where(Function(r) r.TechnicianId.Value = id).ToList()

                If mine.Count = 0 AndAlso tc.Status = "Inactive" Then Continue For

                Dim assigned As Integer = mine.Count
                Dim done As Integer = mine.Where(Function(r) IsDone(r.Status)).Count()
                Dim openCount As Integer = mine.Where(Function(r) Not IsDone(r.Status) AndAlso r.Status <> "Cancelled").Count()
                Dim billed As Decimal = 0D
                Dim collected As Decimal = 0D

                For Each r As Repair In mine
                    If IsDone(r.Status) Then
                        billed += r.Cost
                        collected += Math.Min(r.Cost, PaidOf(paidMap, r.RepairId))
                    End If
                Next

                Dim rate As String = "-"
                If assigned > 0 Then rate = Math.Round(done * 100.0 / assigned).ToString("0") & "%"

                d.Rows.Add(New Object() {
                    tc.TechnicianName, tc.Specialization, assigned, done, openCount, billed, collected, rate})

                techCount += 1
                tAssigned += assigned
                tDone += done
                tOpen += openCount
                tBilled += billed
                tCollected += collected

            Next

            d.Totals = New Object() {"TOTAL", Nothing, tAssigned, tDone, tOpen, tBilled, tCollected, Nothing}

            d.AddSummary("Technicians", techCount, ColumnKind.Whole)
            d.AddSummary("Repairs Assigned", tAssigned, ColumnKind.Whole)
            d.AddSummary("Completed", tDone, ColumnKind.Whole)
            d.AddSummary("Billed", tBilled, ColumnKind.Money)
            d.AddSummary("Collected", tCollected, ColumnKind.Money)

            Return d

        End Function

        ' ============================================================
        ' 5. CUSTOMER ACTIVITY
        ' ============================================================

        Private Function BuildCustomers(f As DateTime, t As DateTime, periodLabel As String) As ReportData

            Dim repairs As List(Of Repair) =
                db.Repairs.AsNoTracking().
                    Where(Function(r) r.RepairDate >= f AndAlso r.RepairDate < t).
                    ToList()

            Dim custMap As Dictionary(Of Integer, Customer) =
                db.Customers.AsNoTracking().ToList().ToDictionary(Function(c) c.CustomerId)

            Dim paidMap As Dictionary(Of Integer, Decimal) = PaidLookup()

            Dim d As New ReportData()
            d.Title = "Customer Activity Report"
            d.Subtitle = "Period: " & periodLabel & " (by repair date)"

            d.Columns.Add(New ReportColumn("CUSTOMER", 2.0F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("CONTACT", 1.4F, ColumnKind.Text))
            d.Columns.Add(New ReportColumn("REPAIRS", 0.8F, ColumnKind.Whole))
            d.Columns.Add(New ReportColumn("COMPLETED", 1.0F, ColumnKind.Whole))
            d.Columns.Add(New ReportColumn("BILLED", 1.2F, ColumnKind.Money))
            d.Columns.Add(New ReportColumn("PAID", 1.2F, ColumnKind.Money))
            d.Columns.Add(New ReportColumn("BALANCE", 1.2F, ColumnKind.Money))

            Dim tmp As New List(Of Tuple(Of Decimal, Object()))()
            Dim tRepairs As Integer = 0
            Dim tDone As Integer = 0
            Dim tBilled As Decimal = 0D
            Dim tPaid As Decimal = 0D
            Dim tBalance As Decimal = 0D

            For Each g In repairs.GroupBy(Function(r) r.CustomerId)

                Dim cust As Customer = Nothing
                custMap.TryGetValue(g.Key, cust)

                Dim billed As Decimal = 0D
                Dim paid As Decimal = 0D
                Dim balance As Decimal = 0D
                Dim done As Integer = 0

                For Each r As Repair In g
                    If IsDone(r.Status) Then
                        Dim got As Decimal = Math.Min(r.Cost, PaidOf(paidMap, r.RepairId))
                        billed += r.Cost
                        paid += got
                        balance += Math.Max(0D, r.Cost - got)
                        done += 1
                    End If
                Next

                Dim count As Integer = g.Count()

                tmp.Add(Tuple.Create(billed, New Object() {
                    If(cust Is Nothing, "Unknown customer", cust.CustomerName),
                    If(cust Is Nothing, "", cust.ContactNumber),
                    count, done, billed, paid, balance}))

                tRepairs += count
                tDone += done
                tBilled += billed
                tPaid += paid
                tBalance += balance

            Next

            For Each item In tmp.OrderByDescending(Function(x) x.Item1)
                d.Rows.Add(item.Item2)
            Next

            d.Totals = New Object() {"TOTAL", Nothing, tRepairs, tDone, tBilled, tPaid, tBalance}

            d.AddSummary("Customers", tmp.Count, ColumnKind.Whole)
            d.AddSummary("Repairs", tRepairs, ColumnKind.Whole)
            d.AddSummary("Billed", tBilled, ColumnKind.Money)
            d.AddSummary("Collected", tPaid, ColumnKind.Money)
            d.AddSummary("Outstanding", tBalance, ColumnKind.Money)

            Return d

        End Function

    End Class

End Namespace