Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Helpers
Imports SalonPOS.Licensing
Imports SalonPOS.Models
Imports SalonPOS.Services

Namespace SalonPOS.Forms

    Public Class FrmMain
        Inherits Form

        ' Top Executive Header
        Private pnlHeader As Panel
        Private lblSalonTitle As Label
        Private lblDateTime As Label
        Private lblUserInfo As Label
        Private lblLicenseBadge As Label
        Private pnlGoldBorder As Panel

        ' Navigation Bar
        Private pnlNav As Panel
        Private btnPOS As Button
        Private btnCustomers As Button
        Private btnExpenses As Button
        Private btnReports As Button
        Private btnEmployees As Button
        Private btnAdmin As Button
        Private btnShiftToggle As Button

        ' Content Container & Clock
        Private pnlContent As Panel
        Private timerClock As Timer
        Private currentSubForm As Form = Nothing

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Text = "ZEX SALON — نظام إدارة وتشغيل الصالونات"
            Me.WindowState = FormWindowState.Maximized

            InitializeLayout()
            StartClockTimer()
            UpdateShiftButtonState()
            UpdateLicenseBadgeState()

            ' Open POS as initial dashboard
            If AuthService.HasPermission("POS") Then
                OnPosClicked(Nothing, Nothing)
            ElseIf AuthService.HasPermission("Admin") Then
                OnAdminClicked(Nothing, Nothing)
            End If
        End Sub

        Private Sub InitializeLayout()
            Me.Controls.Clear()

            ' 1. Executive Top Header Panel (Deep Navy #103B59)
            pnlHeader = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 55,
                .BackColor = UIStyleHelper.ZexNavy,
                .RightToLeft = RightToLeft.Yes,
                .Padding = New Padding(15, 0, 15, 0)
            }

            lblSalonTitle = New Label With {
                .Dock = DockStyle.Right,
                .AutoSize = False,
                .Width = 220,
                .Font = UIStyleHelper.GetFont(16.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexGold,
                .Text = "ZEX SALON",
                .TextAlign = ContentAlignment.MiddleRight
            }

            lblDateTime = New Label With {
                .Dock = DockStyle.Left,
                .AutoSize = False,
                .Width = 240,
                .Font = UIStyleHelper.GetFont(10.0F, FontStyle.Regular),
                .ForeColor = Color.FromArgb(212, 230, 241),
                .TextAlign = ContentAlignment.MiddleLeft
            }

            lblLicenseBadge = New Label With {
                .Dock = DockStyle.Left,
                .AutoSize = False,
                .Width = 310,
                .Font = UIStyleHelper.GetFont(10.5F, FontStyle.Bold),
                .TextAlign = ContentAlignment.MiddleCenter,
                .Cursor = Cursors.Hand
            }
            AddHandler lblLicenseBadge.Click, AddressOf OnLicenseBadgeClicked

            lblUserInfo = New Label With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            If AuthService.CurrentUser IsNot Nothing Then
                lblUserInfo.Text = $"المستخدم الحالي: {AuthService.CurrentUser.DisplayName} ({AuthService.CurrentUser.RoleArabic})"
            End If

            pnlHeader.Controls.Add(lblUserInfo)
            pnlHeader.Controls.Add(lblLicenseBadge)
            pnlHeader.Controls.Add(lblDateTime)
            pnlHeader.Controls.Add(lblSalonTitle)

            ' Top Header Gold Border Accent
            pnlGoldBorder = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 3,
                .BackColor = UIStyleHelper.ZexGold
            }

            ' 2. Clean Executive Navigation Bar
            pnlNav = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 52,
                .BackColor = Color.White,
                .RightToLeft = RightToLeft.Yes,
                .Padding = New Padding(10, 5, 10, 5)
            }

            btnShiftToggle = New Button With {
                .Dock = DockStyle.Left,
                .Width = 160,
                .Height = 42,
                .Margin = New Padding(5, 0, 5, 0),
                .Font = UIStyleHelper.FontBold
            }
            AddHandler btnShiftToggle.Click, AddressOf OnShiftToggleClicked

            Dim flowNav As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .AutoScroll = False,
                .Padding = New Padding(0)
            }

            btnPOS = CreateNavButton("نقطة البيع (F1)", AddressOf OnPosClicked)
            btnCustomers = CreateNavButton("العملاء (F2)", AddressOf OnCustomersClicked)
            btnExpenses = CreateNavButton("المصروفات (F3)", AddressOf OnExpensesClicked)
            btnReports = CreateNavButton("التقارير (F4)", AddressOf OnReportsClicked)
            btnEmployees = CreateNavButton("الموظفين والرواتب (F5)", AddressOf OnEmployeesClicked)
            btnAdmin = CreateNavButton("الإدارة والإعدادات", AddressOf OnAdminClicked)

            flowNav.Controls.Add(btnPOS)
            flowNav.Controls.Add(btnCustomers)
            flowNav.Controls.Add(btnExpenses)
            flowNav.Controls.Add(btnReports)
            flowNav.Controls.Add(btnEmployees)
            flowNav.Controls.Add(btnAdmin)

            pnlNav.Controls.Add(flowNav)
            pnlNav.Controls.Add(btnShiftToggle)

            ' 3. Main Content Panel
            pnlContent = New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = UIStyleHelper.LightBgColor
            }

            Me.Controls.Add(pnlContent)
            Me.Controls.Add(pnlNav)
            Me.Controls.Add(pnlGoldBorder)
            Me.Controls.Add(pnlHeader)

            ' Global Keyboard Shortcuts
            Me.KeyPreview = True
            AddHandler Me.KeyDown, AddressOf OnFormKeyDown
        End Sub

        Public Sub UpdateLicenseBadgeState()
            Dim valRes = LicenseValidator.ValidateCurrentLicense()
            If valRes.Status = LicenseStatus.ACTIVE Then
                Dim custName As String = If(valRes.License IsNot Nothing AndAlso Not String.IsNullOrEmpty(valRes.License.CustomerName), valRes.License.CustomerName, "مرخص رسمياً")
                lblLicenseBadge.Text = $"🟢 ترخيص نشط: {custName}"
                lblLicenseBadge.ForeColor = Color.FromArgb(46, 204, 113)
            Else
                lblLicenseBadge.Text = $"🔴 غير مفعل (انقر هنا للتفعيل)"
                lblLicenseBadge.ForeColor = Color.FromArgb(231, 76, 60)
            End If
        End Sub

        Private Sub OnLicenseBadgeClicked(sender As Object, e As EventArgs)
            If Not AuthService.HasPermission("Admin") Then
                MessageBox.Show("عفواً، شاشة تفعيل الترخيص مقتصرة على مدير النظام والمخول لهم فقط.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            Dim frmAdmin As New FrmAdminManagement()
            frmAdmin.SelectAboutTab()
            OpenSubForm(frmAdmin)
            HighlightNavButton(btnAdmin)
        End Sub

        Public Sub UpdateShiftButtonState()
            If AuthService.CurrentShift IsNot Nothing AndAlso AuthService.CurrentShift.Status = ShiftStatus.OPEN Then
                btnShiftToggle.Text = "🔒 إغلاق الوردية"
                UIStyleHelper.ApplyButtonFlatWithBorder(btnShiftToggle, Color.FromArgb(254, 242, 242), Color.FromArgb(185, 28, 28), Color.FromArgb(254, 202, 202), 1)
            Else
                btnShiftToggle.Text = "🔑 بدء وردية جديدة"
                UIStyleHelper.ApplyButtonFlatWithBorder(btnShiftToggle, Color.FromArgb(240, 253, 244), Color.FromArgb(22, 101, 52), Color.FromArgb(187, 247, 208), 1)
            End If
        End Sub

        Private Function CreateNavButton(text As String, handler As EventHandler) As Button
            Dim btn As New Button With {
                .Text = text,
                .Width = 160,
                .Height = 42,
                .Margin = New Padding(4, 0, 4, 0),
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold)
            }
            UIStyleHelper.ApplyButtonFlatWithBorder(btn, Color.FromArgb(248, 250, 252), Color.FromArgb(30, 41, 59), Color.FromArgb(226, 232, 240), 1)
            AddHandler btn.Click, Sub(s, e)
                                      handler.Invoke(s, e)
                                  End Sub
            Return btn
        End Function

        Private Sub HighlightNavButton(selectedBtn As Button)
            Dim navBtns = New Button() {btnPOS, btnCustomers, btnExpenses, btnReports, btnEmployees, btnAdmin}
            For Each btn As Button In navBtns
                If btn IsNot Nothing Then
                    UIStyleHelper.ApplyButtonFlatWithBorder(btn, Color.FromArgb(248, 250, 252), Color.FromArgb(30, 41, 59), Color.FromArgb(226, 232, 240), 1)
                End If
            Next
            If selectedBtn IsNot Nothing Then
                UIStyleHelper.ApplyButtonFlatWithBorder(selectedBtn, UIStyleHelper.ZexNavy, Color.White, UIStyleHelper.ZexGold, 1)
            End If
        End Sub

        Private Sub StartClockTimer()
            timerClock = New Timer With {.Interval = 1000}
            AddHandler timerClock.Tick, Sub()
                                            lblDateTime.Text = DateTime.Now.ToString("dd/MM/yyyy - hh:mm:ss tt")
                                        End Sub
            timerClock.Start()
        End Sub

        Public Sub OpenSubForm(childForm As Form)
            If currentSubForm IsNot Nothing Then
                currentSubForm.Close()
                currentSubForm.Dispose()
            End If

            currentSubForm = childForm
            childForm.TopLevel = False
            childForm.FormBorderStyle = FormBorderStyle.None
            childForm.Dock = DockStyle.Fill
            pnlContent.Controls.Add(childForm)
            pnlContent.Tag = childForm
            childForm.Show()
        End Sub

        ' Navigation Click Events with Strict Permission Checks
        Private Sub OnPosClicked(sender As Object, e As EventArgs)
            If Not AuthService.HasPermission("POS") Then
                MessageBox.Show("عفواً، ليس لديك صلاحية للدخول إلى شاشة نقطة البيع (POS).", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            OpenSubForm(New FrmPOS(Me, Nothing))
            HighlightNavButton(btnPOS)
        End Sub

        Private Sub OnCustomersClicked(sender As Object, e As EventArgs)
            If Not AuthService.HasPermission("Customers") Then
                MessageBox.Show("عفواً، ليس لديك صلاحية للدخول إلى شاشة إدارة العملاء.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            OpenSubForm(New FrmCustomers())
            HighlightNavButton(btnCustomers)
        End Sub

        Private Sub OnExpensesClicked(sender As Object, e As EventArgs)
            If Not AuthService.HasPermission("Expenses") Then
                MessageBox.Show("عفواً، ليس لديك صلاحية للدخول إلى شاشة المصروفات.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            OpenSubForm(New FrmExpenses())
            HighlightNavButton(btnExpenses)
        End Sub

        Private Sub OnReportsClicked(sender As Object, e As EventArgs)
            If Not AuthService.HasPermission("Reports") Then
                MessageBox.Show("عفواً، ليس لديك صلاحية للدخول إلى التقارير المالية والورديات.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            OpenSubForm(New FrmReports())
            HighlightNavButton(btnReports)
        End Sub

        Private Sub OnEmployeesClicked(sender As Object, e As EventArgs)
            If Not AuthService.HasPermission("Employees") AndAlso Not AuthService.HasPermission("Admin") Then
                MessageBox.Show("عفواً، هذه الشاشة مقتصرة على مدير النظام والمخول لهم بإدارة شؤون الموظفين والرواتب.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            OpenSubForm(New FrmEmployeesAndPayroll())
            HighlightNavButton(btnEmployees)
        End Sub

        Private Sub OnAdminClicked(sender As Object, e As EventArgs)
            If Not AuthService.HasPermission("Admin") Then
                MessageBox.Show("عفواً، هذه الشاشة مقتصرة على مدير النظام والمخول لهم فقط.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            OpenSubForm(New FrmAdminManagement())
            HighlightNavButton(btnAdmin)
        End Sub

        Private Sub OnShiftToggleClicked(sender As Object, e As EventArgs)
            If Not AuthService.HasPermission("Shifts") Then
                MessageBox.Show("عفواً، ليس لديك صلاحية لفتح أو إغلاق الورديات.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            If AuthService.CurrentShift IsNot Nothing AndAlso AuthService.CurrentShift.Status = ShiftStatus.OPEN Then
                Dim modalClose As New FrmShiftCloseModal()
                modalClose.ShowDialog(Me)
            Else
                Dim modalStart As New FrmStartShiftModal()
                modalStart.ShowDialog(Me)
            End If
            UpdateShiftButtonState()
        End Sub

        Private Sub OnFormKeyDown(sender As Object, e As KeyEventArgs)
            Select Case e.KeyCode
                Case Keys.F1
                    OnPosClicked(Nothing, Nothing)
                    e.Handled = True
                Case Keys.F2
                    OnCustomersClicked(Nothing, Nothing)
                    e.Handled = True
                Case Keys.F3
                    OnExpensesClicked(Nothing, Nothing)
                    e.Handled = True
                Case Keys.F4
                    OnReportsClicked(Nothing, Nothing)
                    e.Handled = True
                Case Keys.F5
                    OnEmployeesClicked(Nothing, Nothing)
                    e.Handled = True
            End Select
        End Sub
    End Class

End Namespace
