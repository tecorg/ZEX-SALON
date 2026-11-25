Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Data
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Models

Namespace SalonPOS.Forms

    Public Class FrmReports
        Inherits Form

        Private shiftRepo As New ShiftRepository()
        Private empRepo As New EmployeeRepository()
        Private userRepo As New UserRepository()
        Private logRepo As New ActivityLogRepository()
        Private expRepo As New ExpenseRepository()

        ' Custom Executive Tabs
        Private pnlTabHeader As Panel
        Private flowTabs As FlowLayoutPanel
        Private btnTabSales As Button
        Private btnTabExpenses As Button
        Private btnTabEmployeesDate As Button
        Private btnTabServices As Button
        Private btnTabShifts As Button
        Private btnTabActivityLogs As Button
        Private pnlTabContent As Panel

        ' DataGrids
        Private dgvSales As DataGridView
        Private dgvServices As DataGridView
        Private dgvShifts As DataGridView

        ' Expenses Tab Controls
        Private pnlExpensesContainer As Panel
        Private pnlExpensesFilter As Panel
        Private cboExpCategoryFilter As ComboBox
        Private dtpExpFromDate As DateTimePicker
        Private dtpExpToDate As DateTimePicker
        Private txtExpSearch As TextBox
        Private btnFilterExpenses As Button
        Private dgvExpenses As DataGridView
        Private lblExpensesSummary As Label

        ' Employees Filter Tab Controls
        Private pnlEmpContainer As Panel
        Private pnlEmpFilter As Panel
        Private cboEmpFilter As ComboBox
        Private dtpEmpFromDate As DateTimePicker
        Private dtpEmpToDate As DateTimePicker
        Private btnFilterEmp As Button
        Private dgvEmpDate As DataGridView
        Private lblEmpSummary As Label

        ' Shifts Tab Controls
        Private pnlShiftsContainer As Panel
        Private pnlShiftsFilter As Panel
        Private cboShiftUserFilter As ComboBox
        Private dtpFromDate As DateTimePicker
        Private dtpToDate As DateTimePicker
        Private btnFilterShifts As Button
        Private pnlShiftsSummary As Panel
        Private lblShiftsSummary As Label

        ' Activity Logs Tab Controls
        Private pnlLogsContainer As Panel
        Private pnlLogsFilter As Panel
        Private cboLogUserFilter As ComboBox
        Private dtpLogFromDate As DateTimePicker
        Private dtpLogToDate As DateTimePicker
        Private txtLogSearch As TextBox
        Private btnFilterLogs As Button
        Private dgvActivityLogs As DataGridView

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Text = "التقارير وسجل نشاطات النظام"
            InitializeLayout()
            LoadSalesReport()
            LoadExpensesReport()
            LoadServicesReport()
            LoadEmployeesDateReport()
            LoadShiftsReport()
            LoadActivityLogsReport()
        End Sub

        Private Sub InitializeLayout()
            Me.Controls.Clear()

            ' 1. Executive Top Tab Bar
            pnlTabHeader = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 52,
                .BackColor = Color.White,
                .RightToLeft = RightToLeft.Yes,
                .Padding = New Padding(12, 6, 12, 6)
            }

            flowTabs = New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .AutoScroll = False,
                .Padding = New Padding(0)
            }

            btnTabSales = CreateTabButton("مبيعات اليوم", AddressOf OnTabSalesClicked)
            btnTabExpenses = CreateTabButton("تقرير المصروفات", AddressOf OnTabExpensesClicked)
            btnTabEmployeesDate = CreateTabButton("أداء وعمولات الفنيين بالمدّة", AddressOf OnTabEmployeesDateClicked)
            btnTabServices = CreateTabButton("أكثر الخدمات مبيعاً", AddressOf OnTabServicesClicked)
            btnTabShifts = CreateTabButton("تقرير وتصفية الورديات", AddressOf OnTabShiftsClicked)
            btnTabActivityLogs = CreateTabButton("سجل نشاطات وحركات النظام", AddressOf OnTabActivityLogsClicked)

            flowTabs.Controls.Add(btnTabSales)
            flowTabs.Controls.Add(btnTabExpenses)
            flowTabs.Controls.Add(btnTabEmployeesDate)
            flowTabs.Controls.Add(btnTabServices)
            flowTabs.Controls.Add(btnTabShifts)
            flowTabs.Controls.Add(btnTabActivityLogs)
            pnlTabHeader.Controls.Add(flowTabs)

            ' 2. Main Content Container
            pnlTabContent = New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = UIStyleHelper.LightBgColor,
                .Padding = New Padding(12)
            }

            dgvSales = CreateReportGrid()
            dgvServices = CreateReportGrid()

            ' Initialize Custom Tab Panels
            InitializeExpensesTab()
            InitializeEmployeesDateTab()
            InitializeShiftsTab()
            InitializeActivityLogsTab()

            Me.Controls.Add(pnlTabContent)
            Me.Controls.Add(pnlTabHeader)

            ' Default show Sales tab
            ShowTabContent(btnTabSales, dgvSales)
        End Sub

        ' =========================================================================
        ' 1. EXPENSES REPORT TAB
        ' =========================================================================
        Private Sub InitializeExpensesTab()
            pnlExpensesContainer = New Panel With {.Dock = DockStyle.Fill}

            pnlExpensesFilter = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 54,
                .BackColor = Color.White,
                .RightToLeft = RightToLeft.Yes,
                .Padding = New Padding(8, 7, 8, 7)
            }

            Dim flowExpFilter As New FlowLayoutPanel With {
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False
            }

            flowExpFilter.Controls.Add(New Label With {.Text = "البند:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})

            cboExpCategoryFilter = New ComboBox With {
                .Width = 140,
                .Height = 36,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 3, 12, 4)
            }
            cboExpCategoryFilter.Items.Add("جميع بنود المصروفات")
            cboExpCategoryFilter.Items.Add("نثريات وشاي")
            cboExpCategoryFilter.Items.Add("فواتير (كهرباء/ماء/إنترنت)")
            cboExpCategoryFilter.Items.Add("خامات ومستلزمات صالون")
            cboExpCategoryFilter.Items.Add("عمولات ومكافآت")
            cboExpCategoryFilter.Items.Add("صيانة وتجهيزات")
            cboExpCategoryFilter.Items.Add("أخرى")
            cboExpCategoryFilter.SelectedIndex = 0

            flowExpFilter.Controls.Add(cboExpCategoryFilter)
            flowExpFilter.Controls.Add(New Label With {.Text = "من:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})

            dtpExpFromDate = New DateTimePicker With {
                .Width = 120,
                .Height = 36,
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Now.AddDays(-30),
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 3, 12, 4)
            }
            flowExpFilter.Controls.Add(dtpExpFromDate)

            flowExpFilter.Controls.Add(New Label With {.Text = "إلى:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})

            dtpExpToDate = New DateTimePicker With {
                .Width = 120,
                .Height = 36,
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Now,
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 3, 12, 4)
            }
            flowExpFilter.Controls.Add(dtpExpToDate)

            flowExpFilter.Controls.Add(New Label With {.Text = "بحث:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})

            txtExpSearch = New TextBox With {
                .Width = 120,
                .Height = 36,
                .Font = UIStyleHelper.FontRegular,
                .Margin = New Padding(4, 3, 12, 4)
            }
            flowExpFilter.Controls.Add(txtExpSearch)

            btnFilterExpenses = New Button With {
                .Text = "🔍 عرض المصروفات",
                .Width = 150,
                .Height = 36,
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 2, 4, 4)
            }
            UIStyleHelper.ApplyButtonFlat(btnFilterExpenses, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnFilterExpenses.Click, Sub() LoadExpensesReport()
            flowExpFilter.Controls.Add(btnFilterExpenses)

            pnlExpensesFilter.Controls.Add(flowExpFilter)

            dgvExpenses = CreateReportGrid()
            dgvExpenses.Dock = DockStyle.Fill

            Dim pnlExpSummary As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 50,
                .BackColor = Color.FromArgb(240, 244, 248),
                .Padding = New Padding(8)
            }
            lblExpensesSummary = New Label With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Text = "جاري تحميل ملخص المصروفات..."
            }
            pnlExpSummary.Controls.Add(lblExpensesSummary)

            pnlExpensesContainer.Controls.Add(dgvExpenses)
            pnlExpensesContainer.Controls.Add(pnlExpensesFilter)
            pnlExpensesContainer.Controls.Add(pnlExpSummary)
        End Sub

        Private Sub LoadExpensesReport()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    Dim sql As String = "
                        SELECT e.Id AS 'رقم المصروف',
                               e.CreatedAt AS 'التاريخ والوقت',
                               e.Category AS 'بند المصروفات',
                               e.Amount AS 'المبلغ (ج.م)',
                               IFNULL(e.Notes, '-') AS 'الملاحظات والبيان',
                               e.ShiftId AS 'رقم الوردية',
                               IFNULL(u.DisplayName, 'مدير النظام') AS 'المسؤول'
                        FROM Expenses e
                        LEFT JOIN Users u ON e.CreatedByUserId = u.Id
                        WHERE e.CreatedAt >= @fromDate AND e.CreatedAt <= @toDate
                    "
                    cmd.Parameters.AddWithValue("@fromDate", dtpExpFromDate.Value.ToString("yyyy-MM-dd 00:00:00"))
                    cmd.Parameters.AddWithValue("@toDate", dtpExpToDate.Value.ToString("yyyy-MM-dd 23:59:59"))

                    If cboExpCategoryFilter.SelectedIndex > 0 Then
                        sql &= " AND e.Category = @cat"
                        cmd.Parameters.AddWithValue("@cat", cboExpCategoryFilter.SelectedItem.ToString())
                    End If

                    Dim searchTxt As String = txtExpSearch.Text.Trim()
                    If Not String.IsNullOrEmpty(searchTxt) Then
                        sql &= " AND (e.Notes LIKE @src OR e.Category LIKE @src)"
                        cmd.Parameters.AddWithValue("@src", $"%{searchTxt}%")
                    End If

                    sql &= " ORDER BY e.Id DESC;"
                    cmd.CommandText = sql

                    Dim dt As New DataTable()
                    dt.Load(cmd.ExecuteReader())
                    dgvExpenses.DataSource = dt

                    Dim totalExp As Double = 0
                    For Each row As DataRow In dt.Rows
                        totalExp += Convert.ToDouble(row("المبلغ (ج.م)"))
                    Next

                    lblExpensesSummary.Text = $"الفترة: {dtpExpFromDate.Value:yyyy/MM/dd} إلى {dtpExpToDate.Value:yyyy/MM/dd}  •  إجمالي عدد المصروفات: {dt.Rows.Count}  •  إجمالي المبلغ المدفوع للمصروفات: {totalExp:N2} ج.م"
                End Using
            End Using
        End Sub

        ' =========================================================================
        ' 2. EMPLOYEES REPORT BY DATE RANGE TAB
        ' =========================================================================
        Private Sub InitializeEmployeesDateTab()
            pnlEmpContainer = New Panel With {.Dock = DockStyle.Fill}

            pnlEmpFilter = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 54,
                .BackColor = Color.White,
                .RightToLeft = RightToLeft.Yes,
                .Padding = New Padding(8, 7, 8, 7)
            }

            Dim flowEmpFilter As New FlowLayoutPanel With {
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False
            }

            flowEmpFilter.Controls.Add(New Label With {.Text = "الموظف/الفني:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})

            cboEmpFilter = New ComboBox With {
                .Width = 160,
                .Height = 36,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 3, 12, 4)
            }

            flowEmpFilter.Controls.Add(cboEmpFilter)
            flowEmpFilter.Controls.Add(New Label With {.Text = "من تاريخ:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})

            dtpEmpFromDate = New DateTimePicker With {
                .Width = 130,
                .Height = 36,
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Now.AddDays(-30),
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 3, 12, 4)
            }
            flowEmpFilter.Controls.Add(dtpEmpFromDate)

            flowEmpFilter.Controls.Add(New Label With {.Text = "إلى تاريخ:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})

            dtpEmpToDate = New DateTimePicker With {
                .Width = 130,
                .Height = 36,
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Now,
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 3, 12, 4)
            }
            flowEmpFilter.Controls.Add(dtpEmpToDate)

            btnFilterEmp = New Button With {
                .Text = "🔍 عرض تقرير الفنيين",
                .Width = 160,
                .Height = 36,
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 2, 4, 4)
            }
            UIStyleHelper.ApplyButtonFlat(btnFilterEmp, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnFilterEmp.Click, Sub() LoadEmployeesDateReport()
            flowEmpFilter.Controls.Add(btnFilterEmp)

            pnlEmpFilter.Controls.Add(flowEmpFilter)

            dgvEmpDate = CreateReportGrid()
            dgvEmpDate.Dock = DockStyle.Fill

            Dim pnlEmpSummary As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 50,
                .BackColor = Color.FromArgb(240, 244, 248),
                .Padding = New Padding(8)
            }
            lblEmpSummary = New Label With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Text = "جاري تحضير ملخص الموظفين..."
            }
            pnlEmpSummary.Controls.Add(lblEmpSummary)

            pnlEmpContainer.Controls.Add(dgvEmpDate)
            pnlEmpContainer.Controls.Add(pnlEmpFilter)
            pnlEmpContainer.Controls.Add(pnlEmpSummary)

            PopulateEmployeeFilterCombo()
        End Sub

        Private Sub PopulateEmployeeFilterCombo()
            cboEmpFilter.Items.Clear()
            cboEmpFilter.Items.Add("جميع الموظفين والفنيين")
            Dim emps = empRepo.GetActiveEmployees()
            For Each emp In emps
                cboEmpFilter.Items.Add(emp.Name)
            Next
            cboEmpFilter.SelectedIndex = 0
        End Sub

        Private Sub LoadEmployeesDateReport()
            Dim selectedEmpName As String = If(cboEmpFilter.SelectedIndex > 0, cboEmpFilter.SelectedItem.ToString(), "")

            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    Dim sql As String = "
                        SELECT e.Name AS 'اسم الموظف / الفني',
                               COUNT(si.Id) AS 'عدد الخدمات المنفذة',
                               IFNULL(SUM(si.SubTotal), 0) AS 'إجمالي إيراد الخدمات',
                               e.CommissionRate AS 'نسبة العمولة (%)',
                               IFNULL(SUM(si.EmployeeCommissionAmount), 0) AS 'إجمالي العمولة المستحقة'
                        FROM Employees e
                        LEFT JOIN SaleItems si ON e.Id = si.EmployeeId
                        LEFT JOIN Sales s ON si.SaleId = s.Id
                        WHERE s.CreatedAt >= @fromDate AND s.CreatedAt <= @toDate
                    "
                    cmd.Parameters.AddWithValue("@fromDate", dtpEmpFromDate.Value.ToString("yyyy-MM-dd 00:00:00"))
                    cmd.Parameters.AddWithValue("@toDate", dtpEmpToDate.Value.ToString("yyyy-MM-dd 23:59:59"))

                    If Not String.IsNullOrEmpty(selectedEmpName) Then
                        sql &= " AND e.Name = @empName"
                        cmd.Parameters.AddWithValue("@empName", selectedEmpName)
                    End If

                    sql &= " GROUP BY e.Id, e.Name ORDER BY IFNULL(SUM(si.SubTotal), 0) DESC;"
                    cmd.CommandText = sql

                    Dim dt As New DataTable()
                    dt.Load(cmd.ExecuteReader())
                    dgvEmpDate.DataSource = dt

                    ' Compute Summary Totals
                    Dim totalRev As Double = 0
                    Dim totalComm As Double = 0
                    Dim totalJobs As Long = 0

                    For Each row As DataRow In dt.Rows
                        totalJobs += Convert.ToInt64(row("عدد الخدمات المنفذة"))
                        totalRev += Convert.ToDouble(row("إجمالي إيراد الخدمات"))
                        totalComm += Convert.ToDouble(row("إجمالي العمولة المستحقة"))
                    Next

                    lblEmpSummary.Text = $"الفترة: {dtpEmpFromDate.Value:yyyy/MM/dd} إلى {dtpEmpToDate.Value:yyyy/MM/dd}  •  إجمالي مبيعات الفنيين: {totalRev:N2} ج.م  •  إجمالي العمولات المستحقة: {totalComm:N2} ج.م  •  إجمالي الخدمات: {totalJobs}"
                End Using
            End Using
        End Sub

        ' =========================================================================
        ' 3. SHIFTS REPORT TAB
        ' =========================================================================
        Private Sub InitializeShiftsTab()
            pnlShiftsContainer = New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = UIStyleHelper.LightBgColor
            }

            pnlShiftsFilter = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 54,
                .BackColor = Color.White,
                .RightToLeft = RightToLeft.Yes,
                .Padding = New Padding(8, 7, 8, 7)
            }

            Dim flowFilter As New FlowLayoutPanel With {
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False
            }

            flowFilter.Controls.Add(New Label With {.Text = "المشغل/المستخدم:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})

            cboShiftUserFilter = New ComboBox With {
                .Width = 150,
                .Height = 36,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 3, 12, 4)
            }
            flowFilter.Controls.Add(cboShiftUserFilter)

            flowFilter.Controls.Add(New Label With {.Text = "من تاريخ:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})

            dtpFromDate = New DateTimePicker With {
                .Width = 130,
                .Height = 36,
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Now.AddDays(-7),
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 3, 12, 4)
            }
            flowFilter.Controls.Add(dtpFromDate)

            flowFilter.Controls.Add(New Label With {.Text = "إلى تاريخ:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})

            dtpToDate = New DateTimePicker With {
                .Width = 130,
                .Height = 36,
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Now,
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 3, 12, 4)
            }
            flowFilter.Controls.Add(dtpToDate)

            btnFilterShifts = New Button With {
                .Text = "🔍 تصفية الورديات",
                .Width = 150,
                .Height = 36,
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 2, 4, 4)
            }
            UIStyleHelper.ApplyButtonFlat(btnFilterShifts, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnFilterShifts.Click, Sub() LoadShiftsReport()
            flowFilter.Controls.Add(btnFilterShifts)

            pnlShiftsFilter.Controls.Add(flowFilter)

            dgvShifts = CreateReportGrid()
            dgvShifts.Dock = DockStyle.Fill

            pnlShiftsSummary = New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 50,
                .BackColor = Color.FromArgb(240, 244, 248),
                .Padding = New Padding(8)
            }

            lblShiftsSummary = New Label With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Text = "جاري تحميل ملخص الورديات..."
            }
            pnlShiftsSummary.Controls.Add(lblShiftsSummary)

            pnlShiftsContainer.Controls.Add(dgvShifts)
            pnlShiftsContainer.Controls.Add(pnlShiftsFilter)
            pnlShiftsContainer.Controls.Add(pnlShiftsSummary)

            PopulateShiftUserCombo()
        End Sub

        Private Sub PopulateShiftUserCombo()
            cboShiftUserFilter.Items.Clear()
            cboShiftUserFilter.Items.Add("جميع المشغلين")
            Dim users = userRepo.GetAllUsers()
            For Each u In users
                cboShiftUserFilter.Items.Add(u.DisplayName)
            Next
            cboShiftUserFilter.SelectedIndex = 0
        End Sub

        ' =========================================================================
        ' 4. ACTIVITY LOGS TAB
        ' =========================================================================
        Private Sub InitializeActivityLogsTab()
            pnlLogsContainer = New Panel With {.Dock = DockStyle.Fill}

            pnlLogsFilter = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 54,
                .BackColor = Color.White,
                .RightToLeft = RightToLeft.Yes,
                .Padding = New Padding(8, 7, 8, 7)
            }

            Dim flowLogsFilter As New FlowLayoutPanel With {
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False
            }

            flowLogsFilter.Controls.Add(New Label With {.Text = "المستخدم:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})

            cboLogUserFilter = New ComboBox With {
                .Width = 140,
                .Height = 36,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4, 3, 12, 4)
            }
            flowLogsFilter.Controls.Add(cboLogUserFilter)

            flowLogsFilter.Controls.Add(New Label With {.Text = "من:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})
            dtpLogFromDate = New DateTimePicker With {.Width = 120, .Height = 36, .Format = DateTimePickerFormat.Short, .Value = DateTime.Now.AddDays(-7), .Font = UIStyleHelper.FontBold, .Margin = New Padding(4, 3, 12, 4)}
            flowLogsFilter.Controls.Add(dtpLogFromDate)

            flowLogsFilter.Controls.Add(New Label With {.Text = "إلى:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})
            dtpLogToDate = New DateTimePicker With {.Width = 120, .Height = 36, .Format = DateTimePickerFormat.Short, .Value = DateTime.Now, .Font = UIStyleHelper.FontBold, .Margin = New Padding(4, 3, 12, 4)}
            flowLogsFilter.Controls.Add(dtpLogToDate)

            flowLogsFilter.Controls.Add(New Label With {.Text = "بحث:", .Height = 36, .AutoSize = True, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Margin = New Padding(4, 8, 4, 4)})
            txtLogSearch = New TextBox With {.Width = 130, .Height = 36, .Font = UIStyleHelper.FontRegular, .Margin = New Padding(4, 3, 12, 4)}
            flowLogsFilter.Controls.Add(txtLogSearch)

            btnFilterLogs = New Button With {.Text = "🔍 عرض السجل", .Width = 140, .Height = 36, .Font = UIStyleHelper.FontBold, .Margin = New Padding(4, 2, 4, 4)}
            UIStyleHelper.ApplyButtonFlat(btnFilterLogs, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnFilterLogs.Click, Sub() LoadActivityLogsReport()
            flowLogsFilter.Controls.Add(btnFilterLogs)

            pnlLogsFilter.Controls.Add(flowLogsFilter)

            dgvActivityLogs = CreateReportGrid()
            dgvActivityLogs.Dock = DockStyle.Fill

            pnlLogsContainer.Controls.Add(dgvActivityLogs)
            pnlLogsContainer.Controls.Add(pnlLogsFilter)

            PopulateLogUserCombo()
        End Sub

        Private Sub PopulateLogUserCombo()
            cboLogUserFilter.Items.Clear()
            cboLogUserFilter.Items.Add("جميع المستخدمين")
            Dim users = userRepo.GetAllUsers()
            For Each u In users
                cboLogUserFilter.Items.Add(u.DisplayName)
            Next
            cboLogUserFilter.SelectedIndex = 0
        End Sub

        Private Sub LoadActivityLogsReport()
            Dim selectedUserId As Integer? = Nothing
            If cboLogUserFilter.SelectedIndex > 0 Then
                Dim users = userRepo.GetAllUsers()
                If cboLogUserFilter.SelectedIndex - 1 < users.Count Then
                    selectedUserId = users(cboLogUserFilter.SelectedIndex - 1).Id
                End If
            End If

            Dim logs = logRepo.GetLogs(selectedUserId, dtpLogFromDate.Value, dtpLogToDate.Value, txtLogSearch.Text.Trim())

            Dim dt As New DataTable()
            dt.Columns.Add("المعرف", GetType(Integer))
            dt.Columns.Add("التاريخ والوقت", GetType(String))
            dt.Columns.Add("اسم المستخدم", GetType(String))
            dt.Columns.Add("نوع الحركة", GetType(String))
            dt.Columns.Add("التفاصيل الموثقة", GetType(String))

            For Each l In logs
                dt.Rows.Add(l.Id, l.CreatedAt.ToString("yyyy-MM-dd hh:mm:ss tt"), l.UserName, l.ActionType, l.Details)
            Next

            dgvActivityLogs.DataSource = Nothing
            dgvActivityLogs.DataSource = dt
        End Sub

        ' Helper UI Components
        Private Function CreateTabButton(text As String, handler As EventHandler) As Button
            Dim btn As New Button With {
                .Text = text,
                .Width = 175,
                .Height = 40,
                .Margin = New Padding(3, 0, 3, 0)
            }
            UIStyleHelper.ApplyButtonFlatWithBorder(btn, Color.FromArgb(245, 247, 250), UIStyleHelper.ZexTextDark, UIStyleHelper.ZexBorder, 1)
            AddHandler btn.Click, handler
            Return btn
        End Function

        Private Sub ShowTabContent(activeBtn As Button, activeControl As Control)
            Dim tabBtns = New Button() {btnTabSales, btnTabExpenses, btnTabEmployeesDate, btnTabServices, btnTabShifts, btnTabActivityLogs}
            For Each btn As Button In tabBtns
                UIStyleHelper.ApplyButtonFlatWithBorder(btn, Color.FromArgb(245, 247, 250), UIStyleHelper.ZexTextDark, UIStyleHelper.ZexBorder, 1)
            Next
            UIStyleHelper.ApplyButtonFlatWithBorder(activeBtn, UIStyleHelper.ZexNavy, Color.White, UIStyleHelper.ZexNavy, 1)

            pnlTabContent.Controls.Clear()
            activeControl.Dock = DockStyle.Fill
            pnlTabContent.Controls.Add(activeControl)
            activeControl.BringToFront()
        End Sub

        Private Function CreateReportGrid() As DataGridView
            Dim dgv As New DataGridView With {
                .Dock = DockStyle.Fill,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            }
            UIStyleHelper.ApplyDataGridStyle(dgv)
            Return dgv
        End Function

        Private Sub OnTabSalesClicked(sender As Object, e As EventArgs)
            ShowTabContent(btnTabSales, dgvSales)
        End Sub

        Private Sub OnTabExpensesClicked(sender As Object, e As EventArgs)
            ShowTabContent(btnTabExpenses, pnlExpensesContainer)
            LoadExpensesReport()
        End Sub

        Private Sub OnTabEmployeesDateClicked(sender As Object, e As EventArgs)
            ShowTabContent(btnTabEmployeesDate, pnlEmpContainer)
            LoadEmployeesDateReport()
        End Sub

        Private Sub OnTabServicesClicked(sender As Object, e As EventArgs)
            ShowTabContent(btnTabServices, dgvServices)
        End Sub

        Private Sub OnTabShiftsClicked(sender As Object, e As EventArgs)
            ShowTabContent(btnTabShifts, pnlShiftsContainer)
            LoadShiftsReport()
        End Sub

        Private Sub OnTabActivityLogsClicked(sender As Object, e As EventArgs)
            ShowTabContent(btnTabActivityLogs, pnlLogsContainer)
            LoadActivityLogsReport()
        End Sub

        Private Sub LoadSalesReport()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        SELECT s.InvoiceNumber AS 'رقم الفاتورة',
                               s.CreatedAt AS 'التاريخ والوقت',
                               e.Name AS 'الموظف',
                               IFNULL(c.Name, 'عميل نقدي') AS 'العميل',
                               s.SubTotal AS 'المجموع',
                               s.Discount AS 'الخصم',
                               s.TotalAmount AS 'الصافي المسدد',
                               s.PaymentMethod AS 'طريقة الدفع'
                        FROM Sales s
                        INNER JOIN Employees e ON s.EmployeeId = e.Id
                        LEFT JOIN Customers c ON s.CustomerId = c.Id
                        WHERE date(s.CreatedAt) = date('now', 'localtime')
                        ORDER BY s.Id DESC;
                    "
                    Dim dt As New DataTable()
                    dt.Load(cmd.ExecuteReader())
                    dgvSales.DataSource = dt
                End Using
            End Using
        End Sub

        Private Sub LoadServicesReport()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        SELECT si.ServiceName AS 'اسم الخدمة',
                               SUM(si.Quantity) AS 'عدد مرات البيع',
                               SUM(si.SubTotal) AS 'إجمالي الإيراد'
                        FROM SaleItems si
                        INNER JOIN Sales s ON si.SaleId = s.Id AND date(s.CreatedAt) = date('now', 'localtime')
                        GROUP BY si.ServiceName
                        ORDER BY SUM(si.Quantity) DESC;
                    "
                    Dim dt As New DataTable()
                    dt.Load(cmd.ExecuteReader())
                    dgvServices.DataSource = dt
                End Using
            End Using
        End Sub

        Private Sub LoadShiftsReport()
            Dim selectedUserId As Integer? = Nothing
            If cboShiftUserFilter IsNot Nothing AndAlso cboShiftUserFilter.SelectedIndex > 0 Then
                Dim users = userRepo.GetAllUsers()
                If cboShiftUserFilter.SelectedIndex - 1 < users.Count Then
                    selectedUserId = users(cboShiftUserFilter.SelectedIndex - 1).Id
                End If
            End If

            Dim shifts = shiftRepo.GetAllShifts(dtpFromDate.Value, dtpToDate.Value, selectedUserId)

            Dim dt As New DataTable()
            dt.Columns.Add("رقم الوردية", GetType(Integer))
            dt.Columns.Add("المستخدم", GetType(String))
            dt.Columns.Add("تاريخ ووقت البدء", GetType(String))
            dt.Columns.Add("تاريخ ووقت الإغلاق", GetType(String))
            dt.Columns.Add("العهدة الافتتاحية", GetType(String))
            dt.Columns.Add("مبيعات نقدي", GetType(String))
            dt.Columns.Add("مبيعات فيزا", GetType(String))
            dt.Columns.Add("المصروفات", GetType(String))
            dt.Columns.Add("الدرج المتوقع", GetType(String))
            dt.Columns.Add("الدرج الفعلي", GetType(String))
            dt.Columns.Add("العجز / الزيادة", GetType(String))
            dt.Columns.Add("حالة الوردية", GetType(String))

            Dim totalCashSales As Double = 0
            Dim totalExpenses As Double = 0
            Dim totalVariance As Double = 0

            For Each sh As Shift In shifts
                Dim endStr As String = If(sh.EndedAt.HasValue, sh.EndedAt.Value.ToString("yyyy-MM-dd hh:mm tt"), "جارية الآن...")
                Dim actStr As String = If(sh.ActualCash.HasValue, $"{sh.ActualCash.Value:N2} ج.م", "-")
                Dim varStr As String = If(sh.Variance.HasValue, $"{sh.Variance.Value:N2} ج.م", "-")
                Dim statusStr As String = If(sh.Status = ShiftStatus.OPEN, "مفتوحة 🟢", "مغلقة 🔒")

                Dim expectedCash As Double = sh.OpeningCash + sh.CashSales - sh.TotalExpenses

                dt.Rows.Add(
                    sh.Id,
                    sh.UserName,
                    sh.StartedAt.ToString("yyyy-MM-dd hh:mm tt"),
                    endStr,
                    $"{sh.OpeningCash:N2} ج.م",
                    $"{sh.CashSales:N2} ج.م",
                    $"{sh.CardSales:N2} ج.م",
                    $"{sh.TotalExpenses:N2} ج.م",
                    $"{expectedCash:N2} ج.م",
                    actStr,
                    varStr,
                    statusStr
                )

                totalCashSales += sh.CashSales
                totalExpenses += sh.TotalExpenses
                If sh.Variance.HasValue Then totalVariance += sh.Variance.Value
            Next

            dgvShifts.DataSource = Nothing
            dgvShifts.DataSource = dt

            lblShiftsSummary.Text = $"إجمالي عدد الورديات: {shifts.Count}  •  إجمالي المبيعات النقدية: {totalCashSales:N2} ج.م  •  إجمالي المصروفات: {totalExpenses:N2} ج.م  •  إجمالي العجز/الزيادة: {totalVariance:N2} ج.م"
        End Sub
    End Class

End Namespace
