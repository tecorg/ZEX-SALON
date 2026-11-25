Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Models

Namespace SalonPOS.Forms

    Public Class FrmStartServiceModal
        Inherits Form

        Private _targetChair As Chair
        Private empRepo As New EmployeeRepository()
        Private sessionRepo As New SessionRepository()

        Private selectedEmployeeId As Integer = 0
        Private selectedCustomerId As Integer? = Nothing
        Private selectedCustomerName As String = "عميل نقدي"

        Private lblTitle As Label
        Private flowEmployees As FlowLayoutPanel
        Private rdoWalkIn As RadioButton
        Private rdoSelectCustomer As RadioButton
        Private btnQuickAddCustomer As Button
        Private lblSelectedCustomerInfo As Label
        Private btnStart As Button
        Private btnCancel As Button

        Public Sub New(chair As Chair)
            Me._targetChair = chair
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Size = New Size(540, 480)
            Me.Text = $"بدء خدمة جديدة - {chair.Name}"

            InitializeLayout()
            LoadEmployees()
        End Sub

        Private Sub InitializeLayout()
            lblTitle = New Label With {
                .Dock = DockStyle.Top,
                .Height = 45,
                .Text = $"اختر الموظف لبدء العمل على {Me._targetChair.Name}",
                .Font = UIStyleHelper.FontSubtitle,
                .ForeColor = Color.FromArgb(44, 62, 80),
                .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.FromArgb(235, 240, 245)
            }

            ' Employee Cards Flow
            flowEmployees = New FlowLayoutPanel With {
                .Location = New Point(20, 60),
                .Size = New Size(480, 180),
                .AutoScroll = True,
                .FlowDirection = FlowDirection.RightToLeft
            }

            ' Customer Section Panel
            Dim pnlCust As New GroupBox With {
                .Text = "اختيار العميل (اختياري)",
                .Location = New Point(20, 250),
                .Size = New Size(480, 120),
                .Font = UIStyleHelper.FontBold
            }

            rdoWalkIn = New RadioButton With {
                .Text = "عميل نقدي (Walk-in)",
                .Location = New Point(280, 30),
                .Size = New Size(180, 30),
                .Checked = True,
                .Font = UIStyleHelper.FontRegular
            }
            AddHandler rdoWalkIn.CheckedChanged, Sub()
                                                     If rdoWalkIn.Checked Then
                                                         selectedCustomerId = Nothing
                                                         selectedCustomerName = "عميل نقدي"
                                                         lblSelectedCustomerInfo.Text = "العميل: عميل نقدي"
                                                     End If
                                                 End Sub

            rdoSelectCustomer = New RadioButton With {
                .Text = "اختيار عميل مسجل",
                .Location = New Point(130, 30),
                .Size = New Size(140, 30),
                .Font = UIStyleHelper.FontRegular
            }
            AddHandler rdoSelectCustomer.CheckedChanged, AddressOf OnSelectCustomerChecked

            btnQuickAddCustomer = New Button With {
                .Text = "+ عميل جديد",
                .Location = New Point(15, 27),
                .Size = New Size(100, 35)
            }
            UIStyleHelper.ApplyButtonFlat(btnQuickAddCustomer, UIStyleHelper.PrimaryColor, Color.White)
            AddHandler btnQuickAddCustomer.Click, AddressOf OnQuickAddCustomerClicked

            lblSelectedCustomerInfo = New Label With {
                .Location = New Point(15, 75),
                .Size = New Size(450, 30),
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.FromArgb(41, 128, 185),
                .Text = "العميل: عميل نقدي",
                .TextAlign = ContentAlignment.MiddleRight
            }

            pnlCust.Controls.Add(rdoWalkIn)
            pnlCust.Controls.Add(rdoSelectCustomer)
            pnlCust.Controls.Add(btnQuickAddCustomer)
            pnlCust.Controls.Add(lblSelectedCustomerInfo)

            ' Action Buttons
            btnStart = New Button With {
                .Text = "بدء الخدمة وفتح نقطة البيع",
                .Location = New Point(150, 390),
                .Size = New Size(220, 45)
            }
            UIStyleHelper.ApplyButtonFlat(btnStart, UIStyleHelper.ActionGreen, Color.White)
            AddHandler btnStart.Click, AddressOf OnStartClicked

            btnCancel = New Button With {
                .Text = "إلغاء",
                .Location = New Point(20, 390),
                .Size = New Size(110, 45)
            }
            UIStyleHelper.ApplyButtonFlat(btnCancel, Color.Gray, Color.White)
            AddHandler btnCancel.Click, Sub() Me.Close()

            Me.Controls.Add(btnCancel)
            Me.Controls.Add(btnStart)
            Me.Controls.Add(pnlCust)
            Me.Controls.Add(flowEmployees)
            Me.Controls.Add(lblTitle)
        End Sub

        Private Sub LoadEmployees()
            flowEmployees.Controls.Clear()
            Dim employees = empRepo.GetActiveEmployees()
            For Each emp In employees
                Dim btn As New Button With {
                    .Text = emp.Name,
                    .Tag = emp.Id,
                    .Size = New Size(105, 70),
                    .Margin = New Padding(6),
                    .Font = UIStyleHelper.FontBold,
                    .Cursor = Cursors.Hand
                }
                UIStyleHelper.ApplyButtonFlat(btn, Color.FromArgb(235, 240, 245), Color.FromArgb(44, 62, 80))
                AddHandler btn.Click, AddressOf OnEmployeeButtonClicked
                flowEmployees.Controls.Add(btn)
            Next

            ' Select first employee by default if available
            If flowEmployees.Controls.Count > 0 Then
                OnEmployeeButtonClicked(flowEmployees.Controls(0), EventArgs.Empty)
            End If
        End Sub

        Private Sub OnEmployeeButtonClicked(sender As Object, e As EventArgs)
            For Each btn As Button In flowEmployees.Controls
                UIStyleHelper.ApplyButtonFlat(btn, Color.FromArgb(235, 240, 245), Color.FromArgb(44, 62, 80))
            Next

            Dim selectedBtn As Button = CType(sender, Button)
            selectedEmployeeId = CInt(selectedBtn.Tag)
            UIStyleHelper.ApplyButtonFlat(selectedBtn, UIStyleHelper.PrimaryColor, Color.White)
        End Sub

        Private Sub OnSelectCustomerChecked(sender As Object, e As EventArgs)
            If rdoSelectCustomer.Checked Then
                Dim custSearchModal As New FrmCustomersModal()
                If custSearchModal.ShowDialog(Me) = DialogResult.OK AndAlso custSearchModal.SelectedCustomer IsNot Nothing Then
                    selectedCustomerId = custSearchModal.SelectedCustomer.Id
                    selectedCustomerName = custSearchModal.SelectedCustomer.Name
                    lblSelectedCustomerInfo.Text = $"العميل المحدد: {selectedCustomerName}"
                Else
                    rdoWalkIn.Checked = True
                End If
            End If
        End Sub

        Private Sub OnQuickAddCustomerClicked(sender As Object, e As EventArgs)
            Dim quickModal As New FrmQuickCustomerModal()
            If quickModal.ShowDialog(Me) = DialogResult.OK Then
                selectedCustomerId = quickModal.NewCustomerId
                selectedCustomerName = quickModal.NewCustomerName
                lblSelectedCustomerInfo.Text = $"العميل الجديد: {selectedCustomerName}"
                rdoSelectCustomer.Checked = True
            End If
        End Sub

        Private Sub OnStartClicked(sender As Object, e As EventArgs)
            If selectedEmployeeId <= 0 Then
                MessageBox.Show("يرجى اختيار الموظف أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Create new Chair Session in Database
            Dim sessionId As Integer = sessionRepo.CreateSession(Me._targetChair.Id, selectedEmployeeId, selectedCustomerId)
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
    End Class

    Public Class FrmCustomersModal
        Inherits Form

        Public Property SelectedCustomer As Customer
        Private txtSearch As TextBox
        Private lstCustomers As ListBox
        Private btnSelect As Button
        Private custRepo As New CustomerRepository()

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Size = New Size(400, 400)
            Me.Text = "بحث واختيار عميل"
            Me.FormBorderStyle = FormBorderStyle.FixedDialog

            txtSearch = New TextBox With {.Location = New Point(20, 20), .Size = New Size(345, 30), .Font = UIStyleHelper.FontRegular}
            AddHandler txtSearch.TextChanged, Sub() LoadData()

            lstCustomers = New ListBox With {.Location = New Point(20, 60), .Size = New Size(345, 230), .Font = UIStyleHelper.FontRegular}

            btnSelect = New Button With {.Text = "اختيار", .Location = New Point(140, 310), .Size = New Size(120, 40)}
            UIStyleHelper.ApplyButtonFlat(btnSelect, UIStyleHelper.PrimaryColor, Color.White)
            AddHandler btnSelect.Click, AddressOf OnSelectClicked

            Me.Controls.Add(btnSelect)
            Me.Controls.Add(lstCustomers)
            Me.Controls.Add(txtSearch)

            LoadData()
        End Sub

        Private Sub LoadData()
            lstCustomers.Items.Clear()
            Dim customers = custRepo.SearchCustomers(txtSearch.Text)
            For Each c In customers
                lstCustomers.Items.Add(c)
            Next
        End Sub

        Private Sub OnSelectClicked(sender As Object, e As EventArgs)
            If lstCustomers.SelectedItem IsNot Nothing Then
                SelectedCustomer = CType(lstCustomers.SelectedItem, Customer)
                Me.DialogResult = DialogResult.OK
                Me.Close()
            End If
        End Sub
    End Class

End Namespace
