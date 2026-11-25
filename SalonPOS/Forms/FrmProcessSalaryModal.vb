Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Models
Imports SalonPOS.Printing
Imports SalonPOS.Services

Namespace SalonPOS.Forms

    Public Class FrmProcessSalaryModal
        Inherits Form

        Private _emp As Employee
        Private _summary As EmployeePayrollSummaryDto
        Private _yearMonth As String
        Private _repo As New EmployeeHRRepository()

        Private lblEmpName As Label
        Private lblJobTitle As Label
        Private lblBaseSalary As Label
        Private lblSalesComm As Label

        Private chkListAdvances As CheckedListBox
        Private lblTotalAdvancesDeducted As Label

        Private txtBonuses As TextBox
        Private txtDeductions As TextBox
        Private txtNotes As TextBox
        Private cboPaymentMethod As ComboBox

        Private lblNetSalary As Label
        Private btnConfirm As Button
        Private btnCancel As Button

        Public Property PayrollResult As EmployeePayroll

        Public Sub New(emp As Employee, summary As EmployeePayrollSummaryDto, yearMonth As String)
            UIStyleHelper.ApplyFormDefaults(Me)
            _emp = emp
            _summary = summary
            _yearMonth = yearMonth

            Me.Text = $"اعتماد وصرف مستحقات الموظف: {_emp.Name} - شهر ({_yearMonth})"
            Me.Size = New Size(620, 680)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = Color.FromArgb(248, 250, 252)

            InitializeComponents()
            LoadPendingAdvances()
            Recalculate()
        End Sub

        Private Sub InitializeComponents()
            Me.Controls.Clear()

            ' 1. Top Header Card
            Dim pnlHeader As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 85,
                .BackColor = UIStyleHelper.ZexNavy,
                .Padding = New Padding(16, 12, 16, 12)
            }

            lblEmpName = New Label With {
                .Dock = DockStyle.Top,
                .Height = 32,
                .Text = $"👤 {_emp.Name}",
                .Font = UIStyleHelper.GetFont(14.0F, FontStyle.Bold),
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleRight
            }

            lblJobTitle = New Label With {
                .Dock = DockStyle.Top,
                .Height = 24,
                .Text = $"المسمى الوظيفي: {_emp.JobTitle}  |  نسبة العمولة: {_emp.CommissionRate:N1}%  |  شهر الصرف: {_yearMonth}",
                .Font = UIStyleHelper.GetFont(10.5F, FontStyle.Regular),
                .ForeColor = Color.FromArgb(226, 232, 240),
                .TextAlign = ContentAlignment.MiddleRight
            }

            pnlHeader.Controls.Add(lblJobTitle)
            pnlHeader.Controls.Add(lblEmpName)
            Me.Controls.Add(pnlHeader)

            ' 2. Main Content Container
            Dim pnlBody As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(20, 15, 20, 15),
                .AutoScroll = True
            }

            ' Card 1: Entitlements (الاستحقاقات)
            Dim grpEntitlements As New GroupBox With {
                .Dock = DockStyle.Top,
                .Height = 90,
                .Text = "  1. الاستحقاقات والأرباح المكتسبة (Entitlements)  ",
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .BackColor = Color.White,
                .Padding = New Padding(12)
            }

            Dim flowEnt As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight
            }

            lblBaseSalary = New Label With {
                .Width = 260,
                .Height = 45,
                .Text = $"الراتب الأساسي:{vbCrLf}{_summary.BaseSalary:N0} ج.م",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.FromArgb(44, 62, 80),
                .TextAlign = ContentAlignment.MiddleRight
            }

            lblSalesComm = New Label With {
                .Width = 280,
                .Height = 45,
                .Text = $"عمولات الخدمات والمبيعات:{vbCrLf}{_summary.TotalCommissions:N0} ج.م (من مبيعات {_summary.TotalSalesAmount:N0} ج.م)",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.FromArgb(39, 174, 96),
                .TextAlign = ContentAlignment.MiddleRight
            }

            flowEnt.Controls.Add(lblBaseSalary)
            flowEnt.Controls.Add(lblSalesComm)
            grpEntitlements.Controls.Add(flowEnt)

            ' Card 2: Pending Advances to Deduct
            Dim grpAdvances As New GroupBox With {
                .Dock = DockStyle.Top,
                .Height = 135,
                .Text = "  2. السلف المعلقة المطلوب خصمها من الراتب  ",
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .BackColor = Color.White,
                .Padding = New Padding(12),
                .Margin = New Padding(0, 10, 0, 0)
            }

            chkListAdvances = New CheckedListBox With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.FontRegular,
                .CheckOnClick = True,
                .RightToLeft = RightToLeft.Yes
            }
            AddHandler chkListAdvances.ItemCheck, Sub() Me.BeginInvoke(New Action(AddressOf Recalculate))

            lblTotalAdvancesDeducted = New Label With {
                .Dock = DockStyle.Bottom,
                .Height = 24,
                .Text = "إجمالي السلف المحددة للخصم: 0 ج.م",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.FromArgb(192, 57, 43),
                .TextAlign = ContentAlignment.MiddleRight
            }

            grpAdvances.Controls.Add(chkListAdvances)
            grpAdvances.Controls.Add(lblTotalAdvancesDeducted)

            ' Card 3: Bonuses & Deductions Inputs
            Dim grpAdjustments As New GroupBox With {
                .Dock = DockStyle.Top,
                .Height = 100,
                .Text = "  3. مكافآت واستقطاعات إضافية  ",
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .BackColor = Color.White,
                .Padding = New Padding(12),
                .Margin = New Padding(0, 10, 0, 0)
            }

            Dim tblAdj As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 2,
                .RightToLeft = RightToLeft.Yes
            }
            tblAdj.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
            tblAdj.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))

            Dim lblBon = New Label With {.Text = "مكافآت وإضافي (+):", .Font = UIStyleHelper.FontBold, .ForeColor = Color.DarkGreen, .Dock = DockStyle.Fill}
            Dim lblDed = New Label With {.Text = "خصومات وجزاءات (-):", .Font = UIStyleHelper.FontBold, .ForeColor = Color.DarkRed, .Dock = DockStyle.Fill}
            txtBonuses = New TextBox With {.Dock = DockStyle.Fill, .Font = UIStyleHelper.FontRegular, .Text = "0", .RightToLeft = RightToLeft.Yes}
            txtDeductions = New TextBox With {.Dock = DockStyle.Fill, .Font = UIStyleHelper.FontRegular, .Text = "0", .RightToLeft = RightToLeft.Yes}

            AddHandler txtBonuses.TextChanged, AddressOf Recalculate
            AddHandler txtDeductions.TextChanged, AddressOf Recalculate

            tblAdj.Controls.Add(lblBon, 0, 0)
            tblAdj.Controls.Add(txtBonuses, 0, 1)
            tblAdj.Controls.Add(lblDed, 1, 0)
            tblAdj.Controls.Add(txtDeductions, 1, 1)
            grpAdjustments.Controls.Add(tblAdj)

            ' Card 4: Payment Details & Notes
            Dim grpPayDetails As New GroupBox With {
                .Dock = DockStyle.Top,
                .Height = 90,
                .Text = "  4. طريقة الصرف والملاحظات  ",
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .BackColor = Color.White,
                .Padding = New Padding(12),
                .Margin = New Padding(0, 10, 0, 0)
            }

            Dim tblPay As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 2,
                .RightToLeft = RightToLeft.Yes
            }
            tblPay.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35.0F))
            tblPay.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 65.0F))

            Dim lblMethod = New Label With {.Text = "طريقة الصرف:", .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Dock = DockStyle.Fill}
            Dim lblNotes = New Label With {.Text = "ملاحظات السند:", .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy, .Dock = DockStyle.Fill}
            cboPaymentMethod = New ComboBox With {.Dock = DockStyle.Fill, .Font = UIStyleHelper.FontRegular, .DropDownStyle = ComboBoxStyle.DropDownList, .RightToLeft = RightToLeft.Yes}
            cboPaymentMethod.Items.AddRange(New String() {"نقدي (خزينة الصالون)", "تحويل بنكي / محفظة", "شبكة / فيزا"})
            cboPaymentMethod.SelectedIndex = 0

            txtNotes = New TextBox With {.Dock = DockStyle.Fill, .Font = UIStyleHelper.FontRegular, .Text = $"صرف راتب ومستحقات شهر {_yearMonth}", .RightToLeft = RightToLeft.Yes}

            tblPay.Controls.Add(lblMethod, 0, 0)
            tblPay.Controls.Add(cboPaymentMethod, 0, 1)
            tblPay.Controls.Add(lblNotes, 1, 0)
            tblPay.Controls.Add(txtNotes, 1, 1)
            grpPayDetails.Controls.Add(tblPay)

            ' Add in correct order
            pnlBody.Controls.Add(grpPayDetails)
            pnlBody.Controls.Add(grpAdjustments)
            pnlBody.Controls.Add(grpAdvances)
            pnlBody.Controls.Add(grpEntitlements)
            Me.Controls.Add(pnlBody)

            ' 3. Bottom Summary & Confirmation Bar
            Dim pnlBottom As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 90,
                .BackColor = Color.White,
                .Padding = New Padding(16, 12, 16, 12)
            }

            lblNetSalary = New Label With {
                .Dock = DockStyle.Right,
                .Width = 320,
                .Font = UIStyleHelper.GetFont(15.0F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(39, 174, 96),
                .TextAlign = ContentAlignment.MiddleRight,
                .Text = "صافي الراتب المستحق: 0 ج.م"
            }

            btnConfirm = New Button With {
                .Dock = DockStyle.Left,
                .Width = 190,
                .Height = 50,
                .Text = "💳 اعتماد وصرف الآن",
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold)
            }
            UIStyleHelper.ApplyButtonFlat(btnConfirm, UIStyleHelper.ActionGreen, Color.White)
            AddHandler btnConfirm.Click, AddressOf OnConfirmClicked

            btnCancel = New Button With {
                .Dock = DockStyle.Left,
                .Width = 110,
                .Height = 50,
                .Text = "إلغاء",
                .Font = UIStyleHelper.FontRegular,
                .Margin = New Padding(8, 0, 0, 0)
            }
            UIStyleHelper.ApplyButtonFlat(btnCancel, Color.FromArgb(149, 165, 166), Color.White)
            AddHandler btnCancel.Click, Sub() Me.DialogResult = DialogResult.Cancel

            pnlBottom.Controls.Add(btnCancel)
            pnlBottom.Controls.Add(btnConfirm)
            pnlBottom.Controls.Add(lblNetSalary)
            Me.Controls.Add(pnlBottom)
        End Sub

        Private Class AdvanceCheckItem
            Public Property Advance As EmployeeAdvance
            Public Overrides Function ToString() As String
                Return $"{Advance.AdvanceDate} | سلفة بمبلغ: {Advance.Amount:N0} ج.م - {Advance.Notes}"
            End Function
        End Class

        Private Sub LoadPendingAdvances()
            chkListAdvances.Items.Clear()
            Dim advances = _repo.GetAdvances(employeeId:=_emp.Id, pendingOnly:=True)
            For Each adv In advances
                Dim item = New AdvanceCheckItem With {.Advance = adv}
                chkListAdvances.Items.Add(item, isChecked:=True) ' Default check all
            Next
        End Sub

        Private Sub Recalculate(Optional sender As Object = Nothing, Optional e As EventArgs = Nothing)
            Dim totalAdvances As Double = 0.0
            For Each item As Object In chkListAdvances.CheckedItems
                Dim advItem = TryCast(item, AdvanceCheckItem)
                If advItem IsNot Nothing Then
                    totalAdvances += advItem.Advance.Amount
                End If
            Next
            lblTotalAdvancesDeducted.Text = $"إجمالي السلف المحددة للخصم: {totalAdvances:N0} ج.م"

            Dim bonuses As Double = 0.0
            Double.TryParse(txtBonuses.Text, bonuses)

            Dim deductions As Double = 0.0
            Double.TryParse(txtDeductions.Text, deductions)

            Dim baseSal = _summary.BaseSalary
            Dim comm = _summary.TotalCommissions

            Dim net = (baseSal + comm + bonuses) - (totalAdvances + deductions)
            If net < 0 Then net = 0

            lblNetSalary.Text = $"صافي الراتب المستحق: {net:N0} ج.م"
        End Sub

        Private Sub OnConfirmClicked(sender As Object, e As EventArgs)
            Dim bonuses As Double = 0.0
            Double.TryParse(txtBonuses.Text, bonuses)

            Dim deductions As Double = 0.0
            Double.TryParse(txtDeductions.Text, deductions)

            Dim selectedAdvIds As New List(Of Integer)()
            Dim totalAdvances As Double = 0.0
            For Each item As Object In chkListAdvances.CheckedItems
                Dim advItem = TryCast(item, AdvanceCheckItem)
                If advItem IsNot Nothing Then
                    selectedAdvIds.Add(advItem.Advance.Id)
                    totalAdvances += advItem.Advance.Amount
                End If
            Next

            Dim net = (_summary.BaseSalary + _summary.TotalCommissions + bonuses) - (totalAdvances + deductions)
            If net < 0 Then net = 0

            Dim confirmRes = MessageBox.Show(
                $"هل أنت متأكد من اعتماد وصرف الراتب التالي؟{vbCrLf}{vbCrLf}" &
                $"الموظف: {_emp.Name}{vbCrLf}" &
                $"شهر الاستحقاق: {_yearMonth}{vbCrLf}" &
                $"صافي المبلغ المصروف: {net:N0} ج.م{vbCrLf}" &
                $"السلف المخصومة: {totalAdvances:N0} ج.م{vbCrLf}" &
                $"طريقة الصرف: {cboPaymentMethod.Text}",
                "تأكيد صرف الراتب",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)

            If confirmRes = DialogResult.Yes Then
                Try
                    Dim payroll As New EmployeePayroll With {
                        .EmployeeId = _emp.Id,
                        .EmployeeName = _emp.Name,
                        .JobTitle = _emp.JobTitle,
                        .PayrollMonth = _yearMonth,
                        .BaseSalary = _summary.BaseSalary,
                        .TotalCommission = _summary.TotalCommissions,
                        .TotalBonuses = bonuses,
                        .TotalAdvances = totalAdvances,
                        .TotalDeductions = deductions,
                        .NetSalary = net,
                        .PaidAmount = net,
                        .PaymentDate = DateTime.Now.ToString("yyyy-MM-dd"),
                        .PaymentMethod = cboPaymentMethod.Text,
                        .Status = "مصروف",
                        .Notes = txtNotes.Text.Trim()
                    }

                    Dim pId = _repo.ProcessPayroll(payroll, selectedAdvIds)
                    payroll.Id = pId
                    Me.PayrollResult = payroll

                    ' Log activity
                    Dim uId As Integer = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.Id, 1)
                    Dim uName As String = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.DisplayName, "المستخدم")
                    ActivityLogRepository.Log(uId, uName, "صرف راتب", $"تم صرف راتب الموظف {_emp.Name} لشهر {_yearMonth} بقيمة {net:N0} ج.م")

                    ' Ask to print receipt voucher
                    Dim printRes = MessageBox.Show(
                        $"تم اعتماد وصرف الراتب بنجاح!{vbCrLf}{vbCrLf}هل ترغب في طباعة سند صرف الراتب (قسيمة استلام) للموظف؟",
                        "نجاح الصرف والطباعة",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information)

                    If printRes = DialogResult.Yes Then
                        ReceiptPrinter.PrintSalarySlip(payroll, _emp)
                    End If

                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                Catch ex As Exception
                    MessageBox.Show($"تعذر صرف الراتب: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Sub

    End Class

End Namespace
