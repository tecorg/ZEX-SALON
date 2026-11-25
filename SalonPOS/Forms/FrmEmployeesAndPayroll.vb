Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Models
Imports SalonPOS.Printing
Imports SalonPOS.Services

Namespace SalonPOS.Forms

    Public Class FrmEmployeesAndPayroll
        Inherits Form

        Private _repo As New EmployeeHRRepository()

        ' Navigation Pill Buttons
        Private btnTabEmployees As Button
        Private btnTabAttendance As Button
        Private btnTabAdvances As Button
        Private btnTabPayroll As Button
        Private navButtons As List(Of Button)

        ' Sub Panels for each tab
        Private pnlEmployeesContent As Panel
        Private pnlAttendanceContent As Panel
        Private pnlAdvancesContent As Panel
        Private pnlPayrollContent As Panel

        ' --- TAB 1: Employees Controls ---
        Private txtEmpName As TextBox
        Private txtEmpPhone As TextBox
        Private cboEmpJobTitle As ComboBox
        Private txtEmpBaseSalary As TextBox
        Private txtEmpCommission As TextBox
        Private dtpHireDate As DateTimePicker
        Private chkEmpActive As CheckBox
        Private btnSaveEmp As Button
        Private btnClearEmp As Button
        Private dgvEmployees As DataGridView
        Private lblEmployeesSummary As Label
        Private _selectedEmpId As Integer = 0

        ' --- TAB 2: Attendance Controls ---
        Private dtpAttendanceDate As DateTimePicker
        Private btnMarkAllPresent As Button
        Private btnRefreshAttendance As Button
        Private dgvAttendance As DataGridView
        Private lblAttendanceStats As Label

        ' --- TAB 3: Advances Controls ---
        Private cboAdvEmployee As ComboBox
        Private dtpAdvDate As DateTimePicker
        Private txtAdvAmount As TextBox
        Private txtAdvNotes As TextBox
        Private chkAdvAsExpense As CheckBox
        Private btnSaveAdv As Button
        Private cboFilterAdvEmployee As ComboBox
        Private cboFilterAdvStatus As ComboBox
        Private dgvAdvances As DataGridView
        Private lblAdvancesSummary As Label

        ' --- TAB 4: Payroll Controls ---
        Private dtpPayrollMonth As DateTimePicker
        Private btnCalcPayroll As Button
        Private dgvPayrollSummary As DataGridView
        Private dgvPaidHistory As DataGridView
        Private lblPayrollTotalSummary As Label
        Private splitPayroll As SplitContainer

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Text = "إدارة شؤون الموظفين، الحضور والانصراف، والرواتب"
            Me.BackColor = UIStyleHelper.LightBgColor

            InitializeNavigationAndLayout()
            SelectTab(btnTabEmployees, pnlEmployeesContent)

            ' Load initial data
            LoadEmployeesGrid()
            LoadAttendanceGrid()
            LoadAdvancesData()
            LoadPayrollSummary()
        End Sub

        ' =========================================================================
        ' NAVIGATION & TAB MANAGEMENT
        ' =========================================================================

        Private Sub InitializeNavigationAndLayout()
            Me.Controls.Clear()

            ' 1. Top Header with Title and Modern Navigation Pills
            Dim pnlTop As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 100,
                .BackColor = Color.White,
                .Padding = New Padding(16, 10, 16, 8)
            }

            Dim lblTitle As New Label With {
                .Dock = DockStyle.Top,
                .Height = 36,
                .Text = "💼 نظام إدارة شؤون الموظفين والكوادر (HR & Payroll Management)",
                .Font = UIStyleHelper.GetFont(14.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight
            }

            Dim flowTabs As New FlowLayoutPanel With {
                .Dock = DockStyle.Bottom,
                .Height = 48,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(0, 2, 0, 0),
                .WrapContents = False
            }

            btnTabEmployees = CreateTabButton("👥 بيانات الموظفين والرواتب", Sub() SelectTab(btnTabEmployees, pnlEmployeesContent))
            btnTabAttendance = CreateTabButton("🕒 الحضور والانصراف اليومي", Sub() SelectTab(btnTabAttendance, pnlAttendanceContent))
            btnTabAdvances = CreateTabButton("💵 سلف وعهد الموظفين", Sub() SelectTab(btnTabAdvances, pnlAdvancesContent))
            btnTabPayroll = CreateTabButton("📊 مسير الرواتب والعمولات", Sub() SelectTab(btnTabPayroll, pnlPayrollContent))

            navButtons = New List(Of Button) From {btnTabEmployees, btnTabAttendance, btnTabAdvances, btnTabPayroll}

            flowTabs.Controls.Add(btnTabEmployees)
            flowTabs.Controls.Add(btnTabAttendance)
            flowTabs.Controls.Add(btnTabAdvances)
            flowTabs.Controls.Add(btnTabPayroll)

            pnlTop.Controls.Add(lblTitle)
            pnlTop.Controls.Add(flowTabs)

            ' 2. Main Container Panel
            Dim pnlMainContainer As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(16, 12, 16, 12)
            }

            ' Explicit docking hierarchy
            Me.Controls.Add(pnlMainContainer)
            Me.Controls.Add(pnlTop)
            pnlTop.BringToFront()
            pnlMainContainer.SendToBack()

            ' 3. Initialize the 4 Sub-Panels
            BuildEmployeesTab(pnlMainContainer)
            BuildAttendanceTab(pnlMainContainer)
            BuildAdvancesTab(pnlMainContainer)
            BuildPayrollTab(pnlMainContainer)
        End Sub

        Private Function CreateTabButton(text As String, onClick As Action) As Button
            Dim btn As New Button With {
                .Width = 215,
                .Height = 42,
                .Text = text,
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .Cursor = Cursors.Hand,
                .Margin = New Padding(4, 0, 4, 0)
            }
            AddHandler btn.Click, Sub() onClick()
            Return btn
        End Function

        Private Sub SelectTab(activeBtn As Button, activePnl As Panel)
            For Each btn In navButtons
                If btn Is activeBtn Then
                    UIStyleHelper.ApplyButtonFlat(btn, UIStyleHelper.ZexNavy, Color.White)
                Else
                    UIStyleHelper.ApplyButtonFlatWithBorder(btn, Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105), Color.FromArgb(203, 213, 225), 1)
                End If
            Next

            pnlEmployeesContent.Visible = (activePnl Is pnlEmployeesContent)
            pnlAttendanceContent.Visible = (activePnl Is pnlAttendanceContent)
            pnlAdvancesContent.Visible = (activePnl Is pnlAdvancesContent)
            pnlPayrollContent.Visible = (activePnl Is pnlPayrollContent)

            If activePnl Is pnlEmployeesContent Then LoadEmployeesGrid()
            If activePnl Is pnlAttendanceContent Then LoadAttendanceGrid()
            If activePnl Is pnlAdvancesContent Then LoadAdvancesData()
            If activePnl Is pnlPayrollContent Then LoadPayrollSummary()
        End Sub

        ' =========================================================================
        ' TAB 1: EMPLOYEES PROFILES & SALARIES
        ' =========================================================================

        Private Sub BuildEmployeesTab(container As Panel)
            pnlEmployeesContent = New Panel With {.Dock = DockStyle.Fill, .Visible = False}

            ' Entry Form Card (Top)
            Dim pnlEmpForm As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 175,
                .BackColor = Color.White,
                .Padding = New Padding(14, 10, 14, 10),
                .Margin = New Padding(0, 0, 0, 10)
            }

            Dim lblCardTitle As New Label With {
                .Dock = DockStyle.Top,
                .Height = 26,
                .Text = "👤 إضافة وتعديل بيانات موظف / مصفف:",
                .Font = UIStyleHelper.GetFont(11.5F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight
            }
            pnlEmpForm.Controls.Add(lblCardTitle)

            ' Row 1: Inputs with dedicated labels above them (25% each)
            Dim tblRow1 As New TableLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 60,
                .ColumnCount = 4,
                .RowCount = 1,
                .RightToLeft = RightToLeft.Yes,
                .Padding = New Padding(0)
            }
            tblRow1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 28.0F))
            tblRow1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 22.0F))
            tblRow1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 26.0F))
            tblRow1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 24.0F))

            ' Col 0: Name
            Dim pnlCol0 As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(4, 0, 4, 0)}
            Dim lblN As New Label With {.Text = "اسم الموظف / المصفف:", .Dock = DockStyle.Top, .Height = 22, .Font = UIStyleHelper.FontBold, .TextAlign = ContentAlignment.MiddleRight}
            txtEmpName = New TextBox With {.Dock = DockStyle.Top, .Height = 32, .Font = UIStyleHelper.FontRegular, .RightToLeft = RightToLeft.Yes}
            pnlCol0.Controls.Add(txtEmpName)
            pnlCol0.Controls.Add(lblN)
            tblRow1.Controls.Add(pnlCol0, 0, 0)

            ' Col 1: Phone
            Dim pnlCol1 As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(4, 0, 4, 0)}
            Dim lblP As New Label With {.Text = "رقم الهاتف:", .Dock = DockStyle.Top, .Height = 22, .Font = UIStyleHelper.FontBold, .TextAlign = ContentAlignment.MiddleRight}
            txtEmpPhone = New TextBox With {.Dock = DockStyle.Top, .Height = 32, .Font = UIStyleHelper.FontRegular, .RightToLeft = RightToLeft.Yes}
            pnlCol1.Controls.Add(txtEmpPhone)
            pnlCol1.Controls.Add(lblP)
            tblRow1.Controls.Add(pnlCol1, 1, 0)

            ' Col 2: Job Title
            Dim pnlCol2 As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(4, 0, 4, 0)}
            Dim lblJ As New Label With {.Text = "المسمى الوظيفي:", .Dock = DockStyle.Top, .Height = 22, .Font = UIStyleHelper.FontBold, .TextAlign = ContentAlignment.MiddleRight}
            cboEmpJobTitle = New ComboBox With {.Dock = DockStyle.Top, .Height = 32, .Font = UIStyleHelper.FontRegular, .DropDownStyle = ComboBoxStyle.DropDownList, .RightToLeft = RightToLeft.Yes}
            cboEmpJobTitle.Items.AddRange(New String() {"كوافير رئيسي", "مصفف شعر", "حلاق رجالي", "خبير عناية وبشرة", "مساعد تصفيف", "محاسب / كاشير", "عامل نظافة وضيافة"})
            cboEmpJobTitle.SelectedIndex = 0
            pnlCol2.Controls.Add(cboEmpJobTitle)
            pnlCol2.Controls.Add(lblJ)
            tblRow1.Controls.Add(pnlCol2, 2, 0)

            ' Col 3: Hire Date
            Dim pnlCol3 As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(4, 0, 4, 0)}
            Dim lblH As New Label With {.Text = "تاريخ التعيين:", .Dock = DockStyle.Top, .Height = 22, .Font = UIStyleHelper.FontBold, .TextAlign = ContentAlignment.MiddleRight}
            dtpHireDate = New DateTimePicker With {.Dock = DockStyle.Top, .Height = 32, .Font = UIStyleHelper.FontRegular, .Format = DateTimePickerFormat.Short}
            pnlCol3.Controls.Add(dtpHireDate)
            pnlCol3.Controls.Add(lblH)
            tblRow1.Controls.Add(pnlCol3, 3, 0)

            pnlEmpForm.Controls.Add(tblRow1)

            ' Row 2: Salaries, Commission, Active Checkbox, and Action Buttons
            Dim flowRow2 As New FlowLayoutPanel With {
                .Dock = DockStyle.Bottom,
                .Height = 50,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(0, 6, 0, 0),
                .WrapContents = False
            }

            Dim lblSal = New Label With {.Text = "الراتب الأساسي (ج.م):", .Font = UIStyleHelper.FontBold, .Width = 135, .TextAlign = ContentAlignment.MiddleRight}
            txtEmpBaseSalary = New TextBox With {.Width = 100, .Font = UIStyleHelper.FontRegular, .Text = "3500", .RightToLeft = RightToLeft.Yes}
            Dim lblComm = New Label With {.Text = "نسبة العمولة %:", .Font = UIStyleHelper.FontBold, .Width = 105, .TextAlign = ContentAlignment.MiddleRight}
            txtEmpCommission = New TextBox With {.Width = 70, .Font = UIStyleHelper.FontRegular, .Text = "10.0", .RightToLeft = RightToLeft.Yes}
            chkEmpActive = New CheckBox With {.Text = "نشط / على رأس العمل", .Font = UIStyleHelper.FontBold, .Checked = True, .Width = 160}

            btnSaveEmp = New Button With {.Width = 140, .Height = 38, .Text = "💾 حفظ الموظف", .Font = UIStyleHelper.FontBold, .Margin = New Padding(8, 0, 4, 0)}
            UIStyleHelper.ApplyButtonFlat(btnSaveEmp, UIStyleHelper.ActionGreen, Color.White)
            AddHandler btnSaveEmp.Click, AddressOf OnSaveEmployeeClicked

            btnClearEmp = New Button With {.Width = 90, .Height = 38, .Text = "🔄 تفريغ", .Font = UIStyleHelper.FontRegular, .Margin = New Padding(4, 0, 4, 0)}
            UIStyleHelper.ApplyButtonFlat(btnClearEmp, Color.FromArgb(149, 165, 166), Color.White)
            AddHandler btnClearEmp.Click, AddressOf ClearEmployeeForm

            flowRow2.Controls.Add(lblSal)
            flowRow2.Controls.Add(txtEmpBaseSalary)
            flowRow2.Controls.Add(lblComm)
            flowRow2.Controls.Add(txtEmpCommission)
            flowRow2.Controls.Add(chkEmpActive)
            flowRow2.Controls.Add(btnSaveEmp)
            flowRow2.Controls.Add(btnClearEmp)

            pnlEmpForm.Controls.Add(flowRow2)

            ' Grid Container (Bottom)
            Dim pnlGrid As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 10, 0, 0)}

            lblEmployeesSummary = New Label With {
                .Dock = DockStyle.Top,
                .Height = 32,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight,
                .Text = "إجمالي الموظفين: 0 | النشطين: 0 | إجمالي الرواتب الأساسية: 0 ج.م"
            }

            dgvEmployees = New DataGridView With {
                .Dock = DockStyle.Fill,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .ReadOnly = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .RightToLeft = RightToLeft.Yes
            }
            UIStyleHelper.ApplyDataGridStyle(dgvEmployees)
            AddHandler dgvEmployees.CellDoubleClick, AddressOf OnEmployeeGridDoubleClicked

            pnlGrid.Controls.Add(dgvEmployees)
            pnlGrid.Controls.Add(lblEmployeesSummary)

            ' Safe Docking Hierarchy
            pnlEmployeesContent.Controls.Add(pnlGrid)
            pnlEmployeesContent.Controls.Add(pnlEmpForm)
            pnlEmpForm.BringToFront()
            pnlGrid.SendToBack()

            container.Controls.Add(pnlEmployeesContent)
        End Sub

        Private Sub ClearEmployeeForm(sender As Object, e As EventArgs)
            _selectedEmpId = 0
            txtEmpName.Clear()
            txtEmpPhone.Clear()
            cboEmpJobTitle.SelectedIndex = 0
            txtEmpBaseSalary.Text = "3500"
            txtEmpCommission.Text = "10.0"
            dtpHireDate.Value = DateTime.Today
            chkEmpActive.Checked = True
            btnSaveEmp.Text = "💾 حفظ الموظف"
        End Sub

        Private Sub OnSaveEmployeeClicked(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtEmpName.Text) Then
                MessageBox.Show("يرجى إدخال اسم الموظف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtEmpName.Focus()
                Return
            End If

            Dim baseSal As Double = 0.0
            Double.TryParse(txtEmpBaseSalary.Text, baseSal)

            Dim comm As Double = 0.0
            Double.TryParse(txtEmpCommission.Text, comm)

            Dim emp As New Employee With {
                .Id = _selectedEmpId,
                .Name = txtEmpName.Text.Trim(),
                .Phone = txtEmpPhone.Text.Trim(),
                .JobTitle = cboEmpJobTitle.Text.Trim(),
                .BaseSalary = baseSal,
                .CommissionRate = comm,
                .HireDate = dtpHireDate.Value.ToString("yyyy-MM-dd"),
                .IsActive = chkEmpActive.Checked
            }

            Try
                _repo.SaveEmployee(emp)
                MessageBox.Show("تم حفظ بيانات الموظف بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information)
                ClearEmployeeForm(Nothing, Nothing)
                LoadEmployeesGrid()
            Catch ex As Exception
                MessageBox.Show($"تعذر حفظ الموظف: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub OnEmployeeGridDoubleClicked(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then
                Dim row = dgvEmployees.Rows(e.RowIndex)
                _selectedEmpId = Convert.ToInt32(row.Cells("Id").Value)
                txtEmpName.Text = row.Cells("Name").Value.ToString()
                txtEmpPhone.Text = If(row.Cells("Phone").Value IsNot Nothing, row.Cells("Phone").Value.ToString(), "")
                cboEmpJobTitle.Text = If(row.Cells("JobTitle").Value IsNot Nothing, row.Cells("JobTitle").Value.ToString(), "مصفف / حلاق")
                txtEmpBaseSalary.Text = Convert.ToDouble(row.Cells("BaseSalary").Value).ToString("N0")
                txtEmpCommission.Text = Convert.ToDouble(row.Cells("CommissionRate").Value).ToString("N1")
                chkEmpActive.Checked = Convert.ToBoolean(row.Cells("IsActive").Value)

                Dim hDate As String = If(row.Cells("HireDate").Value IsNot Nothing, row.Cells("HireDate").Value.ToString(), "")
                Dim dt As DateTime
                If DateTime.TryParse(hDate, dt) Then dtpHireDate.Value = dt

                btnSaveEmp.Text = "✏️ تحديث الموظف"
            End If
        End Sub

        Private Sub LoadEmployeesGrid()
            Dim employees = _repo.GetAllEmployees()
            dgvEmployees.DataSource = Nothing
            dgvEmployees.Columns.Clear()

            dgvEmployees.AutoGenerateColumns = False

            dgvEmployees.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Id", .DataPropertyName = "Id", .HeaderText = "كود", .FillWeight = 40})
            dgvEmployees.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Name", .DataPropertyName = "Name", .HeaderText = "اسم الموظف", .FillWeight = 120})
            dgvEmployees.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "JobTitle", .DataPropertyName = "JobTitle", .HeaderText = "المسمى الوظيفي", .FillWeight = 100})
            dgvEmployees.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Phone", .DataPropertyName = "Phone", .HeaderText = "الهاتف", .FillWeight = 90})
            dgvEmployees.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "BaseSalary", .DataPropertyName = "BaseSalary", .HeaderText = "الراتب الأساسي", .FillWeight = 85, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"}})
            dgvEmployees.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "CommissionRate", .DataPropertyName = "CommissionRate", .HeaderText = "نسبة العمولة %", .FillWeight = 75, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N1"}})
            dgvEmployees.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "HireDate", .DataPropertyName = "HireDate", .HeaderText = "تاريخ التعيين", .FillWeight = 80})
            dgvEmployees.Columns.Add(New DataGridViewCheckBoxColumn With {.Name = "IsActive", .DataPropertyName = "IsActive", .HeaderText = "نشط", .FillWeight = 50})

            dgvEmployees.DataSource = employees

            Dim totalEmps = employees.Count
            Dim activeEmps = employees.FindAll(Function(x) x.IsActive).Count
            Dim totalSalaries = 0.0
            For Each e In employees
                If e.IsActive Then totalSalaries += e.BaseSalary
            Next
            lblEmployeesSummary.Text = $"إجمالي الموظفين: {totalEmps}  |  النشطين: {activeEmps}  |  إجمالي الرواتب الأساسية: {totalSalaries:N0} ج.م"
        End Sub

        ' =========================================================================
        ' TAB 2: ATTENDANCE & TIME TRACKING
        ' =========================================================================

        Private Sub BuildAttendanceTab(container As Panel)
            pnlAttendanceContent = New Panel With {.Dock = DockStyle.Fill, .Visible = False}

            ' Top Action Bar
            Dim pnlTopBar As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 65,
                .BackColor = Color.White,
                .Padding = New Padding(12)
            }

            Dim flowAtt As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False
            }

            Dim lblD = New Label With {.Text = "تاريخ الحضور:", .Font = UIStyleHelper.FontBold, .Width = 90, .TextAlign = ContentAlignment.MiddleRight}
            dtpAttendanceDate = New DateTimePicker With {.Width = 140, .Font = UIStyleHelper.FontRegular, .Format = DateTimePickerFormat.Short, .Value = DateTime.Today}
            AddHandler dtpAttendanceDate.ValueChanged, Sub() LoadAttendanceGrid()

            btnMarkAllPresent = New Button With {
                .Width = 205,
                .Height = 38,
                .Text = "⚡ تسجيل حضور الكل (حاضر)",
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(10, 0, 10, 0)
            }
            UIStyleHelper.ApplyButtonFlat(btnMarkAllPresent, Color.FromArgb(41, 128, 185), Color.White)
            AddHandler btnMarkAllPresent.Click, AddressOf OnMarkAllPresentClicked

            btnRefreshAttendance = New Button With {
                .Width = 110,
                .Height = 38,
                .Text = "🔄 تحديث",
                .Font = UIStyleHelper.FontRegular
            }
            UIStyleHelper.ApplyButtonFlat(btnRefreshAttendance, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnRefreshAttendance.Click, Sub() LoadAttendanceGrid()

            flowAtt.Controls.Add(lblD)
            flowAtt.Controls.Add(dtpAttendanceDate)
            flowAtt.Controls.Add(btnMarkAllPresent)
            flowAtt.Controls.Add(btnRefreshAttendance)
            pnlTopBar.Controls.Add(flowAtt)

            ' Grid Container
            Dim pnlAttGrid As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 10, 0, 0)}

            lblAttendanceStats = New Label With {
                .Dock = DockStyle.Top,
                .Height = 32,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight,
                .Text = "حاضر: 0 | غائب: 0 | متأخر: 0 | لم يسجل: 0"
            }

            dgvAttendance = New DataGridView With {
                .Dock = DockStyle.Fill,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .RightToLeft = RightToLeft.Yes
            }
            UIStyleHelper.ApplyDataGridStyle(dgvAttendance)
            AddHandler dgvAttendance.CellContentClick, AddressOf OnAttendanceCellActionClicked
            AddHandler dgvAttendance.CellPainting, AddressOf OnAttendanceCellPainting

            pnlAttGrid.Controls.Add(dgvAttendance)
            pnlAttGrid.Controls.Add(lblAttendanceStats)

            ' Explicit Safe Docking Hierarchy
            pnlAttendanceContent.Controls.Add(pnlAttGrid)
            pnlAttendanceContent.Controls.Add(pnlTopBar)
            pnlTopBar.BringToFront()
            pnlAttGrid.SendToBack()

            container.Controls.Add(pnlAttendanceContent)
        End Sub

        Private Sub LoadAttendanceGrid()
            Dim selDate = dtpAttendanceDate.Value.ToString("yyyy-MM-dd")
            Dim attList = _repo.GetAttendanceForDate(selDate)

            dgvAttendance.DataSource = Nothing
            dgvAttendance.Columns.Clear()
            dgvAttendance.AutoGenerateColumns = False

            dgvAttendance.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "EmployeeId", .DataPropertyName = "EmployeeId", .HeaderText = "كود", .FillWeight = 40, .ReadOnly = True})
            dgvAttendance.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "EmployeeName", .DataPropertyName = "EmployeeName", .HeaderText = "اسم الموظف", .FillWeight = 130, .ReadOnly = True})
            dgvAttendance.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Status", .DataPropertyName = "Status", .HeaderText = "الحالة", .FillWeight = 85, .ReadOnly = True})
            dgvAttendance.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "CheckInTime", .DataPropertyName = "CheckInTime", .HeaderText = "وقت الحضور", .FillWeight = 95, .ReadOnly = True})
            dgvAttendance.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "CheckOutTime", .DataPropertyName = "CheckOutTime", .HeaderText = "وقت الانصراف", .FillWeight = 95, .ReadOnly = True})
            dgvAttendance.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "WorkHours", .DataPropertyName = "WorkHours", .HeaderText = "ساعات العمل", .FillWeight = 75, .ReadOnly = True, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N1"}})
            dgvAttendance.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Notes", .DataPropertyName = "Notes", .HeaderText = "ملاحظات", .FillWeight = 110, .ReadOnly = True})

            ' Action buttons (rendered beautifully via CellPainting)
            Dim btnInCol As New DataGridViewButtonColumn With {
                .Name = "ColCheckIn",
                .HeaderText = "تسجيل حضور",
                .Text = "حضور الآن",
                .UseColumnTextForButtonValue = True,
                .FillWeight = 85,
                .FlatStyle = FlatStyle.Flat
            }
            Dim btnOutCol As New DataGridViewButtonColumn With {
                .Name = "ColCheckOut",
                .HeaderText = "تسجيل انصراف",
                .Text = "انصراف الآن",
                .UseColumnTextForButtonValue = True,
                .FillWeight = 85,
                .FlatStyle = FlatStyle.Flat
            }
            Dim btnAbsentCol As New DataGridViewButtonColumn With {
                .Name = "ColAbsent",
                .HeaderText = "تسجيل غياب",
                .Text = "تسجيل غياب",
                .UseColumnTextForButtonValue = True,
                .FillWeight = 80,
                .FlatStyle = FlatStyle.Flat
            }

            dgvAttendance.Columns.Add(btnInCol)
            dgvAttendance.Columns.Add(btnOutCol)
            dgvAttendance.Columns.Add(btnAbsentCol)

            dgvAttendance.DataSource = attList

            ' Stats
            Dim presentCount = attList.FindAll(Function(x) x.Status = "حاضر").Count
            Dim absentCount = attList.FindAll(Function(x) x.Status = "غائب").Count
            Dim lateCount = attList.FindAll(Function(x) x.Status = "متأخر").Count
            Dim notRecorded = attList.FindAll(Function(x) x.Status = "لم يسجل").Count
            lblAttendanceStats.Text = $"حاضر: {presentCount}  |  غائب: {absentCount}  |  متأخر: {lateCount}  |  لم يسجل: {notRecorded}"
        End Sub

        Private Sub OnAttendanceCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
            If e.RowIndex < 0 Then Return

            Dim colName = dgvAttendance.Columns(e.ColumnIndex).Name
            If colName = "ColCheckIn" OrElse colName = "ColCheckOut" OrElse colName = "ColAbsent" Then
                e.PaintBackground(e.CellBounds, True)

                Dim btnRect = New Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8)
                Dim bgCol = Color.FromArgb(22, 101, 52) ' Green
                Dim btnText = "حضور الآن"

                If colName = "ColCheckOut" Then
                    bgCol = UIStyleHelper.ZexNavy
                    btnText = "انصراف الآن"
                ElseIf colName = "ColAbsent" Then
                    bgCol = Color.FromArgb(192, 57, 43)
                    btnText = "تسجيل غياب"
                End If

                Using brBg As New SolidBrush(bgCol),
                      brText As New SolidBrush(Color.White),
                      sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}

                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                    Using path = GetRoundedRectPath(btnRect, 4)
                        e.Graphics.FillPath(brBg, path)
                    End Using
                    e.Graphics.DrawString(btnText, UIStyleHelper.GetFont(9.5F, FontStyle.Bold), brText, btnRect, sf)
                End Using

                e.Handled = True
            End If
        End Sub

        Private Function GetRoundedRectPath(rect As Rectangle, radius As Integer) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim d = radius * 2
            path.AddArc(rect.X, rect.Y, d, d, 180, 90)
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
            path.CloseFigure()
            Return path
        End Function

        Private Sub OnAttendanceCellActionClicked(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 Then Return
            Dim row = dgvAttendance.Rows(e.RowIndex)
            Dim empId = Convert.ToInt32(row.Cells("EmployeeId").Value)
            Dim selDate = dtpAttendanceDate.Value.ToString("yyyy-MM-dd")

            Dim colName = dgvAttendance.Columns(e.ColumnIndex).Name
            If colName = "ColCheckIn" Then
                _repo.RecordQuickCheckIn(empId, selDate)
                LoadAttendanceGrid()
            ElseIf colName = "ColCheckOut" Then
                _repo.RecordQuickCheckOut(empId, selDate)
                LoadAttendanceGrid()
            ElseIf colName = "ColAbsent" Then
                Dim att = New EmployeeAttendance With {
                    .EmployeeId = empId,
                    .AttendanceDate = selDate,
                    .Status = "غائب",
                    .WorkHours = 0.0,
                    .Notes = "غياب مسجل"
                }
                _repo.SaveAttendance(att)
                LoadAttendanceGrid()
            End If
        End Sub

        Private Sub OnMarkAllPresentClicked(sender As Object, e As EventArgs)
            Dim selDate = dtpAttendanceDate.Value.ToString("yyyy-MM-dd")
            Dim res = MessageBox.Show($"هل ترغب في تسجيل حضور جميع الموظفين النشطين لتاريخ ({selDate}) دفعة واحدة؟", "تأكيد الحضور الجماعي", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If res = DialogResult.Yes Then
                _repo.MarkAllPresent(selDate)
                LoadAttendanceGrid()
                MessageBox.Show("تم تسجيل حضور الكل بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Sub

        ' =========================================================================
        ' TAB 3: ADVANCES (سلف الموظفين)
        ' =========================================================================

        Private Sub BuildAdvancesTab(container As Panel)
            pnlAdvancesContent = New Panel With {.Dock = DockStyle.Fill, .Visible = False}

            ' Entry Form Card (Top)
            Dim grpAdvForm As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 115,
                .BackColor = Color.White,
                .Padding = New Padding(12),
                .Margin = New Padding(0, 0, 0, 10)
            }

            Dim lblAdvTitle As New Label With {
                .Dock = DockStyle.Top,
                .Height = 24,
                .Text = "💵 صرف وتسجيل سلفة جديدة لموظف:",
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight
            }
            grpAdvForm.Controls.Add(lblAdvTitle)

            Dim flowAdv As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(0, 4, 0, 0),
                .WrapContents = False
            }

            ' Employee
            Dim pnlE As New Panel With {.Width = 180, .Height = 55}
            pnlE.Controls.Add(New Label With {.Text = "الموظف:", .Dock = DockStyle.Top, .Height = 20, .Font = UIStyleHelper.FontBold})
            cboAdvEmployee = New ComboBox With {.Dock = DockStyle.Top, .Font = UIStyleHelper.FontRegular, .DropDownStyle = ComboBoxStyle.DropDownList, .RightToLeft = RightToLeft.Yes}
            pnlE.Controls.Add(cboAdvEmployee)

            ' Date
            Dim pnlD As New Panel With {.Width = 140, .Height = 55, .Margin = New Padding(8, 0, 8, 0)}
            pnlD.Controls.Add(New Label With {.Text = "تاريخ السلفة:", .Dock = DockStyle.Top, .Height = 20, .Font = UIStyleHelper.FontBold})
            dtpAdvDate = New DateTimePicker With {.Dock = DockStyle.Top, .Font = UIStyleHelper.FontRegular, .Format = DateTimePickerFormat.Short, .Value = DateTime.Today}
            pnlD.Controls.Add(dtpAdvDate)

            ' Amount
            Dim pnlA As New Panel With {.Width = 120, .Height = 55, .Margin = New Padding(8, 0, 8, 0)}
            pnlA.Controls.Add(New Label With {.Text = "المبلغ (ج.م):", .Dock = DockStyle.Top, .Height = 20, .Font = UIStyleHelper.FontBold})
            txtAdvAmount = New TextBox With {.Dock = DockStyle.Top, .Font = UIStyleHelper.FontRegular, .Text = "200", .RightToLeft = RightToLeft.Yes}
            pnlA.Controls.Add(txtAdvAmount)

            ' Notes
            Dim pnlN As New Panel With {.Width = 230, .Height = 55, .Margin = New Padding(8, 0, 8, 0)}
            pnlN.Controls.Add(New Label With {.Text = "البيان / ملاحظات:", .Dock = DockStyle.Top, .Height = 20, .Font = UIStyleHelper.FontBold})
            txtAdvNotes = New TextBox With {.Dock = DockStyle.Top, .Font = UIStyleHelper.FontRegular, .Text = "سلفة على حساب الراتب", .RightToLeft = RightToLeft.Yes}
            pnlN.Controls.Add(txtAdvNotes)

            ' Checkbox & Save Button
            Dim pnlBtn As New FlowLayoutPanel With {.Width = 330, .Height = 55, .FlowDirection = FlowDirection.LeftToRight, .RightToLeft = RightToLeft.Yes, .Margin = New Padding(8, 0, 0, 0)}
            chkAdvAsExpense = New CheckBox With {.Text = "تسجيل كمصروف من درج الكاشير", .Font = UIStyleHelper.FontBold, .Checked = True, .Width = 240, .Height = 22}
            btnSaveAdv = New Button With {.Width = 150, .Height = 36, .Text = "💵 صرف السلفة", .Font = UIStyleHelper.FontBold}
            UIStyleHelper.ApplyButtonFlat(btnSaveAdv, Color.FromArgb(230, 126, 34), Color.White)
            AddHandler btnSaveAdv.Click, AddressOf OnSaveAdvanceClicked

            pnlBtn.Controls.Add(chkAdvAsExpense)
            pnlBtn.Controls.Add(btnSaveAdv)

            flowAdv.Controls.Add(pnlE)
            flowAdv.Controls.Add(pnlD)
            flowAdv.Controls.Add(pnlA)
            flowAdv.Controls.Add(pnlN)
            flowAdv.Controls.Add(pnlBtn)
            grpAdvForm.Controls.Add(flowAdv)

            ' Filter & Grid Container
            Dim pnlAdvGrid As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 10, 0, 0)}

            Dim pnlFilter As New Panel With {.Dock = DockStyle.Top, .Height = 45, .BackColor = Color.White, .Padding = New Padding(8)}
            Dim flowFilter As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .RightToLeft = RightToLeft.Yes, .FlowDirection = FlowDirection.LeftToRight}

            flowFilter.Controls.Add(New Label With {.Text = "فلترة حسب الموظف:", .Font = UIStyleHelper.FontBold, .Width = 130, .TextAlign = ContentAlignment.MiddleRight})
            cboFilterAdvEmployee = New ComboBox With {.Width = 170, .Font = UIStyleHelper.FontRegular, .DropDownStyle = ComboBoxStyle.DropDownList, .RightToLeft = RightToLeft.Yes}
            AddHandler cboFilterAdvEmployee.SelectedIndexChanged, Sub() LoadAdvancesData()

            flowFilter.Controls.Add(cboFilterAdvEmployee)
            flowFilter.Controls.Add(New Label With {.Text = "الحالة:", .Font = UIStyleHelper.FontBold, .Width = 60, .TextAlign = ContentAlignment.MiddleRight})
            cboFilterAdvStatus = New ComboBox With {.Width = 150, .Font = UIStyleHelper.FontRegular, .DropDownStyle = ComboBoxStyle.DropDownList, .RightToLeft = RightToLeft.Yes}
            cboFilterAdvStatus.Items.AddRange(New String() {"الكل", "سلف معلقة (لم تخصم)", "سلف تم خصمها"})
            cboFilterAdvStatus.SelectedIndex = 1 ' Default pending
            AddHandler cboFilterAdvStatus.SelectedIndexChanged, Sub() LoadAdvancesData()

            flowFilter.Controls.Add(cboFilterAdvStatus)
            pnlFilter.Controls.Add(flowFilter)

            lblAdvancesSummary = New Label With {
                .Dock = DockStyle.Top,
                .Height = 28,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight,
                .Text = "إجمالي السلف المعلقة: 0 ج.م"
            }

            dgvAdvances = New DataGridView With {
                .Dock = DockStyle.Fill,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .ReadOnly = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .RightToLeft = RightToLeft.Yes
            }
            UIStyleHelper.ApplyDataGridStyle(dgvAdvances)
            AddHandler dgvAdvances.CellContentClick, AddressOf OnAdvancesCellActionClicked
            AddHandler dgvAdvances.CellPainting, AddressOf OnAdvancesCellPainting

            pnlAdvGrid.Controls.Add(dgvAdvances)
            pnlAdvGrid.Controls.Add(lblAdvancesSummary)
            pnlAdvGrid.Controls.Add(pnlFilter)

            ' Explicit Safe Docking Hierarchy
            pnlAdvancesContent.Controls.Add(pnlAdvGrid)
            pnlAdvancesContent.Controls.Add(grpAdvForm)
            grpAdvForm.BringToFront()
            pnlAdvGrid.SendToBack()

            container.Controls.Add(pnlAdvancesContent)
        End Sub

        Private Sub OnAdvancesCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
            If e.RowIndex < 0 Then Return
            If dgvAdvances.Columns(e.ColumnIndex).Name = "ColDeleteAdv" Then
                e.PaintBackground(e.CellBounds, True)
                Dim btnRect = New Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8)
                Using brBg As New SolidBrush(Color.FromArgb(220, 38, 38)),
                      brText As New SolidBrush(Color.White),
                      sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                    Using path = GetRoundedRectPath(btnRect, 4)
                        e.Graphics.FillPath(brBg, path)
                    End Using
                    e.Graphics.DrawString("إلغاء السلفة", UIStyleHelper.GetFont(9.5F, FontStyle.Bold), brText, btnRect, sf)
                End Using
                e.Handled = True
            End If
        End Sub

        Private Sub LoadAdvancesData()
            ' Populate Combos
            Dim emps = _repo.GetAllEmployees(activeOnly:=True)

            If cboAdvEmployee.Items.Count = 0 Then
                cboAdvEmployee.DisplayMember = "Name"
                cboAdvEmployee.ValueMember = "Id"
                cboAdvEmployee.DataSource = New List(Of Employee)(emps)
            End If

            If cboFilterAdvEmployee.Items.Count = 0 Then
                cboFilterAdvEmployee.Items.Add("جميع الموظفين")
                For Each e In emps
                    cboFilterAdvEmployee.Items.Add(e.Name)
                Next
                cboFilterAdvEmployee.SelectedIndex = 0
            End If

            ' Filter parameters
            Dim empIdFilter As Integer? = Nothing
            If cboFilterAdvEmployee.SelectedIndex > 0 Then
                Dim selectedName = cboFilterAdvEmployee.SelectedItem.ToString()
                Dim matched = emps.Find(Function(x) x.Name = selectedName)
                If matched IsNot Nothing Then empIdFilter = matched.Id
            End If

            Dim pendingOnly As Boolean = (cboFilterAdvStatus.SelectedIndex = 1)
            Dim list = _repo.GetAdvances(employeeId:=empIdFilter, pendingOnly:=pendingOnly)

            If cboFilterAdvStatus.SelectedIndex = 2 Then
                list = list.FindAll(Function(x) x.IsDeducted)
            End If

            dgvAdvances.DataSource = Nothing
            dgvAdvances.Columns.Clear()
            dgvAdvances.AutoGenerateColumns = False

            dgvAdvances.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Id", .DataPropertyName = "Id", .HeaderText = "رقم السلفة", .FillWeight = 50})
            dgvAdvances.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "EmployeeName", .DataPropertyName = "EmployeeName", .HeaderText = "اسم الموظف", .FillWeight = 120})
            dgvAdvances.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "AdvanceDate", .DataPropertyName = "AdvanceDate", .HeaderText = "التاريخ", .FillWeight = 90})
            dgvAdvances.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Amount", .DataPropertyName = "Amount", .HeaderText = "المبلغ (ج.م)", .FillWeight = 90, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"}})
            dgvAdvances.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Notes", .DataPropertyName = "Notes", .HeaderText = "البيان / ملاحظات", .FillWeight = 160})
            dgvAdvances.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "StatusStr", .HeaderText = "حالة الخصم", .FillWeight = 100})

            Dim delCol As New DataGridViewButtonColumn With {
                .Name = "ColDeleteAdv",
                .HeaderText = "إجراء",
                .Text = "إلغاء السلفة",
                .UseColumnTextForButtonValue = True,
                .FillWeight = 85,
                .FlatStyle = FlatStyle.Flat
            }
            dgvAdvances.Columns.Add(delCol)

            dgvAdvances.DataSource = list

            ' Populate StatusStr
            Dim totalPending As Double = 0.0
            Dim totalAll As Double = 0.0
            For i As Integer = 0 To list.Count - 1
                Dim adv = list(i)
                totalAll += adv.Amount
                If Not adv.IsDeducted Then
                    totalPending += adv.Amount
                    dgvAdvances.Rows(i).Cells("StatusStr").Value = "⏳ معلقة (لم تخصم)"
                Else
                    dgvAdvances.Rows(i).Cells("StatusStr").Value = "✓ تم الخصم من الراتب"
                End If
            Next

            lblAdvancesSummary.Text = $"إجمالي السلف المعلقة المطلوب خصمها: {totalPending:N0} ج.م  |  إجمالي السلف المعروضة: {totalAll:N0} ج.م"
        End Sub

        Private Sub OnSaveAdvanceClicked(sender As Object, e As EventArgs)
            If cboAdvEmployee.SelectedItem Is Nothing Then
                MessageBox.Show("يرجى اختيار الموظف أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim amount As Double = 0.0
            If Not Double.TryParse(txtAdvAmount.Text, amount) OrElse amount <= 0 Then
                MessageBox.Show("يرجى إدخال مبلغ سلفة صحيح أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtAdvAmount.Focus()
                Return
            End If

            Dim selectedEmp = CType(cboAdvEmployee.SelectedItem, Employee)
            Dim advDate = dtpAdvDate.Value.ToString("yyyy-MM-dd")

            Dim adv As New EmployeeAdvance With {
                .EmployeeId = selectedEmp.Id,
                .EmployeeName = selectedEmp.Name,
                .AdvanceDate = advDate,
                .Amount = amount,
                .Notes = txtAdvNotes.Text.Trim(),
                .IsDeducted = False
            }

            Try
                Dim shiftId = If(AuthService.CurrentShift IsNot Nothing AndAlso AuthService.CurrentShift.Status = ShiftStatus.OPEN, AuthService.CurrentShift.Id, 0)
                Dim userId = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.Id, 1)

                _repo.SaveAdvance(adv, createExpense:=chkAdvAsExpense.Checked, currentShiftId:=shiftId, currentUserId:=userId)

                MessageBox.Show($"تم تسجيل وصرف سلفة بقيمة {amount:N0} ج.م للموظف ({selectedEmp.Name}) بنجاح!", "نجاح الصرف", MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtAdvAmount.Text = "200"
                txtAdvNotes.Text = "سلفة على حساب الراتب"
                LoadAdvancesData()
            Catch ex As Exception
                MessageBox.Show($"تعذر تسجيل السلفة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub OnAdvancesCellActionClicked(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 Then Return
            If dgvAdvances.Columns(e.ColumnIndex).Name = "ColDeleteAdv" Then
                Dim row = dgvAdvances.Rows(e.RowIndex)
                Dim advId = Convert.ToInt32(row.Cells("Id").Value)
                Dim empName = row.Cells("EmployeeName").Value.ToString()
                Dim amt = Convert.ToDouble(row.Cells("Amount").Value)

                Dim confirmRes = MessageBox.Show($"هل أنت متأكد من إلغاء وحذف سلفة الموظف ({empName}) بقيمة {amt:N0} ج.م؟", "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If confirmRes = DialogResult.Yes Then
                    Dim ok = _repo.DeleteAdvance(advId)
                    If ok Then
                        MessageBox.Show("تم حذف السلفة بنجاح.", "تم الإلغاء", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        LoadAdvancesData()
                    Else
                        MessageBox.Show("لا يمكن حذف هذه السلفة لأنه تم خصمها بالفعل من راتب مصروف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                End If
            End If
        End Sub

        ' =========================================================================
        ' TAB 4: PAYROLL, COMMISSIONS & SETTLEMENTS
        ' =========================================================================

        Private Sub BuildPayrollTab(container As Panel)
            pnlPayrollContent = New Panel With {.Dock = DockStyle.Fill, .Visible = False}

            ' Top Month Selector Bar
            Dim pnlMonthBar As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 65,
                .BackColor = Color.White,
                .Padding = New Padding(12)
            }

            Dim flowMonth As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False
            }

            flowMonth.Controls.Add(New Label With {.Text = "شهر مسير الرواتب:", .Font = UIStyleHelper.FontBold, .Width = 140, .TextAlign = ContentAlignment.MiddleRight})
            dtpPayrollMonth = New DateTimePicker With {
                .Width = 160,
                .Font = UIStyleHelper.FontRegular,
                .CustomFormat = "yyyy-MM",
                .Format = DateTimePickerFormat.Custom,
                .Value = DateTime.Today
            }
            AddHandler dtpPayrollMonth.ValueChanged, Sub() LoadPayrollSummary()

            btnCalcPayroll = New Button With {
                .Width = 220,
                .Height = 38,
                .Text = "📊 احتساب مسير رواتب الشهر",
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(12, 0, 12, 0)
            }
            UIStyleHelper.ApplyButtonFlat(btnCalcPayroll, UIStyleHelper.ActionGreen, Color.White)
            AddHandler btnCalcPayroll.Click, Sub() LoadPayrollSummary()

            flowMonth.Controls.Add(dtpPayrollMonth)
            flowMonth.Controls.Add(btnCalcPayroll)
            pnlMonthBar.Controls.Add(flowMonth)

            ' Split Container: Top Grid = Calculation Summary / Bottom Grid = Paid History
            splitPayroll = New SplitContainer With {
                .Dock = DockStyle.Fill,
                .Orientation = Orientation.Horizontal,
                .SplitterDistance = 270,
                .Panel1MinSize = 160,
                .Panel2MinSize = 130
            }

            ' Panel 1: Monthly Calculation Grid
            Dim pnlP1 As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 10, 0, 0)}
            lblPayrollTotalSummary = New Label With {
                .Dock = DockStyle.Top,
                .Height = 30,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight,
                .Text = "إجمالي الرواتب الأساسية: 0 ج.م | إجمالي العمولات: 0 ج.م | السلف المطلوب خصمها: 0 ج.م | صافي الرواتب: 0 ج.م"
            }

            dgvPayrollSummary = New DataGridView With {
                .Dock = DockStyle.Fill,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .ReadOnly = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .RightToLeft = RightToLeft.Yes
            }
            UIStyleHelper.ApplyDataGridStyle(dgvPayrollSummary)
            AddHandler dgvPayrollSummary.CellContentClick, AddressOf OnPayrollSummaryCellActionClicked
            AddHandler dgvPayrollSummary.CellPainting, AddressOf OnPayrollSummaryCellPainting

            pnlP1.Controls.Add(dgvPayrollSummary)
            pnlP1.Controls.Add(lblPayrollTotalSummary)
            splitPayroll.Panel1.Controls.Add(pnlP1)

            ' Panel 2: Paid Payrolls History
            Dim pnlP2 As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 8, 0, 0)}
            Dim lblHistTitle As New Label With {
                .Dock = DockStyle.Top,
                .Height = 28,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.FromArgb(39, 174, 96),
                .TextAlign = ContentAlignment.MiddleRight,
                .Text = "📋 سجل الرواتب والمستحقات المعتمدة والمصروفة لهذا الشهر:"
            }

            dgvPaidHistory = New DataGridView With {
                .Dock = DockStyle.Fill,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .ReadOnly = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .RightToLeft = RightToLeft.Yes
            }
            UIStyleHelper.ApplyDataGridStyle(dgvPaidHistory)
            AddHandler dgvPaidHistory.CellContentClick, AddressOf OnPaidHistoryCellActionClicked
            AddHandler dgvPaidHistory.CellPainting, AddressOf OnPaidHistoryCellPainting

            pnlP2.Controls.Add(dgvPaidHistory)
            pnlP2.Controls.Add(lblHistTitle)
            splitPayroll.Panel2.Controls.Add(pnlP2)

            ' Explicit Safe Docking Hierarchy
            pnlPayrollContent.Controls.Add(splitPayroll)
            pnlPayrollContent.Controls.Add(pnlMonthBar)
            pnlMonthBar.BringToFront()
            splitPayroll.SendToBack()

            ' Responsive Split Ratio Handler
            AddHandler pnlPayrollContent.Resize, Sub()
                If pnlPayrollContent.Height > 380 Then
                    Try
                        splitPayroll.SplitterDistance = CInt(pnlPayrollContent.Height * 0.48)
                    Catch
                    End Try
                End If
            End Sub

            container.Controls.Add(pnlPayrollContent)
        End Sub

        Private Sub OnPayrollSummaryCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
            If e.RowIndex < 0 Then Return
            If dgvPayrollSummary.Columns(e.ColumnIndex).Name = "ColProcessPay" Then
                e.PaintBackground(e.CellBounds, True)
                Dim btnRect = New Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8)
                Using brBg As New SolidBrush(UIStyleHelper.ActionGreen),
                      brText As New SolidBrush(Color.White),
                      sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                    Using path = GetRoundedRectPath(btnRect, 4)
                        e.Graphics.FillPath(brBg, path)
                    End Using
                    e.Graphics.DrawString("اعتماد وصرف", UIStyleHelper.GetFont(9.5F, FontStyle.Bold), brText, btnRect, sf)
                End Using
                e.Handled = True
            End If
        End Sub

        Private Sub OnPaidHistoryCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
            If e.RowIndex < 0 Then Return
            Dim colName = dgvPaidHistory.Columns(e.ColumnIndex).Name
            If colName = "ColPrintSlip" OrElse colName = "ColDeletePay" Then
                e.PaintBackground(e.CellBounds, True)
                Dim btnRect = New Rectangle(e.CellBounds.X + 3, e.CellBounds.Y + 4, e.CellBounds.Width - 6, e.CellBounds.Height - 8)
                Dim bgCol = UIStyleHelper.ZexNavy
                Dim btnText = "طباعة سند"

                If colName = "ColDeletePay" Then
                    bgCol = Color.FromArgb(220, 38, 38)
                    btnText = "تراجع"
                End If

                Using brBg As New SolidBrush(bgCol),
                      brText As New SolidBrush(Color.White),
                      sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                    Using path = GetRoundedRectPath(btnRect, 4)
                        e.Graphics.FillPath(brBg, path)
                    End Using
                    e.Graphics.DrawString(btnText, UIStyleHelper.GetFont(9.0F, FontStyle.Bold), brText, btnRect, sf)
                End Using
                e.Handled = True
            End If
        End Sub

        Private Sub LoadPayrollSummary()
            Dim ym = dtpPayrollMonth.Value.ToString("yyyy-MM")
            Dim summaries = _repo.GetAllEmployeesPayrollSummary(ym)
            Dim paidList = _repo.GetPayrollsForMonth(ym)

            ' 1. Master Calculation Grid
            dgvPayrollSummary.DataSource = Nothing
            dgvPayrollSummary.Columns.Clear()
            dgvPayrollSummary.AutoGenerateColumns = False

            dgvPayrollSummary.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "EmployeeId", .DataPropertyName = "EmployeeId", .HeaderText = "كود", .FillWeight = 40})
            dgvPayrollSummary.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "EmployeeName", .DataPropertyName = "EmployeeName", .HeaderText = "اسم الموظف", .FillWeight = 110})
            dgvPayrollSummary.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "JobTitle", .DataPropertyName = "JobTitle", .HeaderText = "المسمى الوظيفي", .FillWeight = 90})
            dgvPayrollSummary.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "BaseSalary", .DataPropertyName = "BaseSalary", .HeaderText = "الراتب الأساسي", .FillWeight = 85, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"}})
            dgvPayrollSummary.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "TotalSalesAmount", .DataPropertyName = "TotalSalesAmount", .HeaderText = "مبيعات الموظف", .FillWeight = 85, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"}})
            dgvPayrollSummary.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "TotalCommissions", .DataPropertyName = "TotalCommissions", .HeaderText = "العمولة المستحقة", .FillWeight = 85, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"}})
            dgvPayrollSummary.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "TotalPendingAdvances", .DataPropertyName = "TotalPendingAdvances", .HeaderText = "سلف للخصم", .FillWeight = 80, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"}})
            dgvPayrollSummary.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "SuggestedNetSalary", .DataPropertyName = "SuggestedNetSalary", .HeaderText = "صافي المستحق", .FillWeight = 95, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0", .Font = UIStyleHelper.FontBold}})
            dgvPayrollSummary.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "DaysPresent", .DataPropertyName = "DaysPresent", .HeaderText = "حضور", .FillWeight = 50})

            Dim btnPayCol As New DataGridViewButtonColumn With {
                .Name = "ColProcessPay",
                .HeaderText = "إجراء الصرف",
                .Text = "اعتماد وصرف",
                .UseColumnTextForButtonValue = True,
                .FillWeight = 95,
                .FlatStyle = FlatStyle.Flat
            }
            dgvPayrollSummary.Columns.Add(btnPayCol)

            dgvPayrollSummary.DataSource = summaries

            ' Calculate Totals
            Dim totalBase As Double = 0.0
            Dim totalComm As Double = 0.0
            Dim totalAdv As Double = 0.0
            Dim totalNet As Double = 0.0
            For Each s In summaries
                totalBase += s.BaseSalary
                totalComm += s.TotalCommissions
                totalAdv += s.TotalPendingAdvances
                totalNet += s.SuggestedNetSalary
            Next
            lblPayrollTotalSummary.Text = $"الرواتب الأساسية: {totalBase:N0} ج.م  |  العمولات: {totalComm:N0} ج.م  |  سلف للخصم: {totalAdv:N0} ج.م  |  صافي الرواتب المستحقة: {totalNet:N0} ج.م"

            ' 2. Paid History Grid
            dgvPaidHistory.DataSource = Nothing
            dgvPaidHistory.Columns.Clear()
            dgvPaidHistory.AutoGenerateColumns = False

            dgvPaidHistory.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Id", .DataPropertyName = "Id", .HeaderText = "رقم السند", .FillWeight = 50})
            dgvPaidHistory.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "EmployeeName", .DataPropertyName = "EmployeeName", .HeaderText = "اسم الموظف", .FillWeight = 110})
            dgvPaidHistory.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "PaymentDate", .DataPropertyName = "PaymentDate", .HeaderText = "تاريخ الصرف", .FillWeight = 85})
            dgvPaidHistory.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "BaseSalary", .DataPropertyName = "BaseSalary", .HeaderText = "الأساسي", .FillWeight = 75, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"}})
            dgvPaidHistory.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "TotalCommission", .DataPropertyName = "TotalCommission", .HeaderText = "العمولة", .FillWeight = 75, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"}})
            dgvPaidHistory.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "TotalBonuses", .DataPropertyName = "TotalBonuses", .HeaderText = "مكافآت", .FillWeight = 65, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"}})
            dgvPaidHistory.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "TotalAdvances", .DataPropertyName = "TotalAdvances", .HeaderText = "سلف مخصومة", .FillWeight = 75, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"}})
            dgvPaidHistory.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "TotalDeductions", .DataPropertyName = "TotalDeductions", .HeaderText = "استقطاعات", .FillWeight = 65, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"}})
            dgvPaidHistory.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "PaidAmount", .DataPropertyName = "PaidAmount", .HeaderText = "الصافي المصروف", .FillWeight = 90, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0", .Font = UIStyleHelper.FontBold}})
            dgvPaidHistory.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "PaymentMethod", .DataPropertyName = "PaymentMethod", .HeaderText = "طريقة الصرف", .FillWeight = 80})

            Dim btnPrintCol As New DataGridViewButtonColumn With {
                .Name = "ColPrintSlip",
                .HeaderText = "طباعة",
                .Text = "طباعة سند",
                .UseColumnTextForButtonValue = True,
                .FillWeight = 75,
                .FlatStyle = FlatStyle.Flat
            }
            Dim btnDelPayCol As New DataGridViewButtonColumn With {
                .Name = "ColDeletePay",
                .HeaderText = "إلغاء",
                .Text = "تراجع",
                .UseColumnTextForButtonValue = True,
                .FillWeight = 65,
                .FlatStyle = FlatStyle.Flat
            }
            dgvPaidHistory.Columns.Add(btnPrintCol)
            dgvPaidHistory.Columns.Add(btnDelPayCol)

            dgvPaidHistory.DataSource = paidList
        End Sub

        Private Sub OnPayrollSummaryCellActionClicked(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 Then Return
            If dgvPayrollSummary.Columns(e.ColumnIndex).Name = "ColProcessPay" Then
                Dim row = dgvPayrollSummary.Rows(e.RowIndex)
                Dim empId = Convert.ToInt32(row.Cells("EmployeeId").Value)
                Dim ym = dtpPayrollMonth.Value.ToString("yyyy-MM-dd").Substring(0, 7)

                Dim emp = _repo.GetEmployeeById(empId)
                Dim summaryList = _repo.GetAllEmployeesPayrollSummary(ym)
                Dim summary = summaryList.Find(Function(x) x.EmployeeId = empId)

                If emp IsNot Nothing AndAlso summary IsNot Nothing Then
                    Using modal As New FrmProcessSalaryModal(emp, summary, ym)
                        If modal.ShowDialog(Me) = DialogResult.OK Then
                            LoadPayrollSummary()
                        End If
                    End Using
                End If
            End If
        End Sub

        Private Sub OnPaidHistoryCellActionClicked(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 Then Return
            Dim row = dgvPaidHistory.Rows(e.RowIndex)
            Dim pId = Convert.ToInt32(row.Cells("Id").Value)
            Dim ym = dtpPayrollMonth.Value.ToString("yyyy-MM")
            Dim paidList = _repo.GetPayrollsForMonth(ym)
            Dim currentPay = paidList.Find(Function(x) x.Id = pId)
            If currentPay Is Nothing Then Return

            Dim colName = dgvPaidHistory.Columns(e.ColumnIndex).Name
            If colName = "ColPrintSlip" Then
                Dim emp = _repo.GetEmployeeById(currentPay.EmployeeId)
                ReceiptPrinter.PrintSalarySlip(currentPay, emp)
            ElseIf colName = "ColDeletePay" Then
                Dim confirmRes = MessageBox.Show($"هل أنت متأكد من التراجع عن صرف هذا الراتب للموظف ({currentPay.EmployeeName})؟{vbCrLf}سيتم إعادة السلف المخصومة إلى حالة (معلقة).", "تأكيد التراجع", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                If confirmRes = DialogResult.Yes Then
                    _repo.DeletePayroll(pId)
                    MessageBox.Show("تم التراجع عن صرف الراتب بنجاح.", "تم التراجع", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    LoadPayrollSummary()
                End If
            End If
        End Sub

    End Class

End Namespace
