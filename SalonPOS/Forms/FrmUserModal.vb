Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Helpers
Imports SalonPOS.Models

Namespace SalonPOS.Forms

    Public Class FrmUserModal
        Inherits Form

        Public Property UserToEdit As User
        Public Property NewPassword As String = ""

        Private txtUsername As TextBox
        Private txtDisplayName As TextBox
        Private txtPassword As TextBox
        Private cboRole As ComboBox
        Private chkIsActive As CheckBox

        ' Granular Permissions CheckBoxes
        Private chkPermPOS As CheckBox
        Private chkPermCustomers As CheckBox
        Private chkPermExpenses As CheckBox
        Private chkPermReports As CheckBox
        Private chkPermAdmin As CheckBox
        Private chkPermShifts As CheckBox
        Private chkPermDiscounts As CheckBox

        Private btnSave As Button
        Private btnCancel As Button

        Public Sub New(Optional u As User = Nothing)
            Me.UserToEdit = If(u, New User With {.Id = 0, .Role = UserRole.CASHIER, .IsActive = True})
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Size = New Size(580, 560)
            Me.Text = If(UserToEdit.Id = 0, "إضافة مستخدم جديد للنظام", $"تعديل بيانات وصلاحيات المستخدم ({UserToEdit.DisplayName})")
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            InitializeLayout()
            LoadUserData()
        End Sub

        Private Sub InitializeLayout()
            Me.Controls.Clear()

            ' Main Form Group
            Dim grpInfo As New GroupBox With {
                .Text = "بيانات الحساب والدخول",
                .Location = New Point(20, 15),
                .Size = New Size(520, 210),
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .RightToLeft = RightToLeft.Yes
            }

            grpInfo.Controls.Add(New Label With {.Text = "اسم المستخدم (Username):", .Location = New Point(330, 30), .Size = New Size(170, 25)})
            txtUsername = New TextBox With {.Location = New Point(30, 30), .Size = New Size(290, 30), .Font = UIStyleHelper.FontRegular}

            grpInfo.Controls.Add(New Label With {.Text = "الاسم الكامل / العرض:", .Location = New Point(330, 75), .Size = New Size(170, 25)})
            txtDisplayName = New TextBox With {.Location = New Point(30, 75), .Size = New Size(290, 30), .Font = UIStyleHelper.FontRegular}

            grpInfo.Controls.Add(New Label With {.Text = "كلمة المرور:", .Location = New Point(330, 120), .Size = New Size(170, 25)})
            txtPassword = New TextBox With {.Location = New Point(30, 120), .Size = New Size(290, 30), .Font = UIStyleHelper.FontRegular, .UseSystemPasswordChar = True}

            grpInfo.Controls.Add(New Label With {.Text = "الدور / الرتبة:", .Location = New Point(330, 165), .Size = New Size(170, 25)})
            cboRole = New ComboBox With {
                .Location = New Point(180, 165),
                .Size = New Size(140, 30),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = UIStyleHelper.FontBold
            }
            cboRole.Items.Add("مدير نظام (ADMIN)")
            cboRole.Items.Add("مشرف عام (MANAGER)")
            cboRole.Items.Add("كاشير / بائع (CASHIER)")
            cboRole.Items.Add("موظف / فني (EMPLOYEE)")
            cboRole.SelectedIndex = 2

            chkIsActive = New CheckBox With {
                .Text = "حساب نشط",
                .Location = New Point(30, 165),
                .Size = New Size(130, 30),
                .Checked = True
            }

            grpInfo.Controls.Add(chkIsActive)
            grpInfo.Controls.Add(cboRole)
            grpInfo.Controls.Add(txtPassword)
            grpInfo.Controls.Add(txtDisplayName)
            grpInfo.Controls.Add(txtUsername)

            ' GroupBox 2: Granular Permissions
            Dim grpPerms As New GroupBox With {
                .Text = "صلاحيات الوصول والعمليات التفصيلية بالبرنامج",
                .Location = New Point(20, 235),
                .Size = New Size(520, 200),
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .RightToLeft = RightToLeft.Yes
            }

            chkPermPOS = New CheckBox With {.Text = "دخول نقطة البيع POS (F1)", .Location = New Point(270, 30), .Size = New Size(230, 30), .Checked = True}
            chkPermCustomers = New CheckBox With {.Text = "إدارة سجل العملاء (F2)", .Location = New Point(20, 30), .Size = New Size(230, 30), .Checked = True}

            chkPermExpenses = New CheckBox With {.Text = "تسجيل وإدارة المصروفات (F3)", .Location = New Point(270, 70), .Size = New Size(230, 30), .Checked = True}
            chkPermReports = New CheckBox With {.Text = "الاطلاع على التقارير والورديات (F4)", .Location = New Point(20, 70), .Size = New Size(230, 30), .Checked = False}

            chkPermAdmin = New CheckBox With {.Text = "دخول الإدارة والخدمات والنسخ الاحتياطي", .Location = New Point(270, 110), .Size = New Size(230, 30), .Checked = False}
            chkPermShifts = New CheckBox With {.Text = "فتح وإغلاق الورديات اليومية", .Location = New Point(20, 110), .Size = New Size(230, 30), .Checked = True}

            chkPermDiscounts = New CheckBox With {.Text = "إمكانية تطبيق الخصومات على الفواتير", .Location = New Point(270, 150), .Size = New Size(230, 30), .Checked = False}

            grpPerms.Controls.Add(chkPermDiscounts)
            grpPerms.Controls.Add(chkPermShifts)
            grpPerms.Controls.Add(chkPermAdmin)
            grpPerms.Controls.Add(chkPermReports)
            grpPerms.Controls.Add(chkPermExpenses)
            grpPerms.Controls.Add(chkPermCustomers)
            grpPerms.Controls.Add(chkPermPOS)

            ' Action Buttons
            btnSave = New Button With {
                .Text = "💾 حفظ البيانات والصلاحيات",
                .Location = New Point(280, 450),
                .Size = New Size(260, 48),
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnSave, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnSave.Click, AddressOf OnSaveClicked

            btnCancel = New Button With {
                .Text = "إلغاء",
                .Location = New Point(20, 450),
                .Size = New Size(140, 48),
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnCancel, Color.Gray, Color.White)
            AddHandler btnCancel.Click, Sub() Me.Close()

            Me.Controls.Add(btnCancel)
            Me.Controls.Add(btnSave)
            Me.Controls.Add(grpPerms)
            Me.Controls.Add(grpInfo)
        End Sub

        Private Sub LoadUserData()
            If UserToEdit IsNot Nothing Then
                txtUsername.Text = UserToEdit.Username
                txtDisplayName.Text = UserToEdit.DisplayName
                txtPassword.Text = ""

                Select Case UserToEdit.Role
                    Case UserRole.ADMIN
                        cboRole.SelectedIndex = 0
                    Case UserRole.MANAGER
                        cboRole.SelectedIndex = 1
                    Case UserRole.CASHIER
                        cboRole.SelectedIndex = 2
                    Case UserRole.EMPLOYEE
                        cboRole.SelectedIndex = 3
                End Select

                chkIsActive.Checked = UserToEdit.IsActive

                ' Check JSON permissions
                chkPermPOS.Checked = UserToEdit.HasPermission("POS")
                chkPermCustomers.Checked = UserToEdit.HasPermission("Customers")
                chkPermExpenses.Checked = UserToEdit.HasPermission("Expenses")
                chkPermReports.Checked = UserToEdit.HasPermission("Reports")
                chkPermAdmin.Checked = UserToEdit.HasPermission("Admin")
                chkPermShifts.Checked = UserToEdit.HasPermission("Shifts")
                chkPermDiscounts.Checked = UserToEdit.HasPermission("Discounts")
            End If
        End Sub

        Private Sub OnSaveClicked(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtUsername.Text) OrElse String.IsNullOrWhiteSpace(txtDisplayName.Text) Then
                MessageBox.Show("يرجى أدخال اسم المستخدم والاسم الكامل أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            UserToEdit.Username = txtUsername.Text.Trim()
            UserToEdit.DisplayName = txtDisplayName.Text.Trim()
            NewPassword = txtPassword.Text.Trim()

            Select Case cboRole.SelectedIndex
                Case 0
                    UserToEdit.Role = UserRole.ADMIN
                Case 1
                    UserToEdit.Role = UserRole.MANAGER
                Case 2
                    UserToEdit.Role = UserRole.CASHIER
                Case 3
                    UserToEdit.Role = UserRole.EMPLOYEE
            End Select

            UserToEdit.IsActive = chkIsActive.Checked

            ' Build JSON permissions string
            Dim permDict As New System.Collections.Generic.Dictionary(Of String, Boolean)()
            permDict("POS") = chkPermPOS.Checked
            permDict("Customers") = chkPermCustomers.Checked
            permDict("Expenses") = chkPermExpenses.Checked
            permDict("Reports") = chkPermReports.Checked
            permDict("Admin") = chkPermAdmin.Checked
            permDict("Shifts") = chkPermShifts.Checked
            permDict("Discounts") = chkPermDiscounts.Checked

            Dim jsonPairs As New System.Collections.Generic.List(Of String)()
            For Each kvp In permDict
                jsonPairs.Add($"""{kvp.Key}"":{kvp.Value.ToString().ToLower()}")
            Next
            UserToEdit.PermissionsJson = "{" & String.Join(",", jsonPairs) & "}"

            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
    End Class

End Namespace
