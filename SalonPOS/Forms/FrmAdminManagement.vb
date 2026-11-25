Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Licensing
Imports SalonPOS.Models
Imports SalonPOS.Services

Namespace SalonPOS.Forms

    Public Class FrmAdminManagement
        Inherits Form

        ' Custom Executive Tabs
        Private pnlTabHeader As Panel
        Private flowTabs As FlowLayoutPanel
        Private btnTabSettings As Button
        Private btnTabUsers As Button
        Private btnTabEmp As Button
        Private btnTabChairs As Button
        Private btnTabServices As Button
        Private btnTabAbout As Button
        Private pnlTabContent As Panel

        ' Tab Content Panels
        Private pnlSettingsContainer As Panel
        Private pnlUsersContainer As Panel
        Private pnlEmpContainer As Panel
        Private pnlChairContainer As Panel
        Private pnlServiceContainer As Panel
        Private pnlAboutContainer As Panel

        ' Grids & Repositories
        Private dgvUsers As DataGridView
        Private btnAddUser As Button
        Private btnEditUser As Button
        Private btnDeleteUser As Button
        Private userRepo As New UserRepository()

        Private dgvEmployees As DataGridView
        Private btnAddEmp As Button
        Private empRepo As New EmployeeRepository()

        Private dgvChairs As DataGridView
        Private btnAddChair As Button
        Private chairRepo As New ChairRepository()

        Private dgvServices As DataGridView
        Private btnAddService As Button
        Private srvRepo As New ServiceRepository()

        ' License UI Controls in About Tab
        Private txtAdminMachineId As TextBox
        Private btnAdminCopyMachineId As Button
        Private txtAdminLicenseKey As TextBox
        Private btnAdminActivate As Button
        Private lblAdminLicenseStatus As Label

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Text = "إدارة البيانات والترخيص والمستخدمين"
            InitializeLayout()
            ConfigureGridColumns()
            LoadAllData()
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

            btnTabSettings = CreateTabButton("إعدادات الصالون والضريبة", Sub(s, e) ShowTabContainer(btnTabSettings, pnlSettingsContainer))
            btnTabUsers = CreateTabButton("المستخدمون والصلاحيات", Sub(s, e) ShowTabContainer(btnTabUsers, pnlUsersContainer))
            btnTabEmp = CreateTabButton("الموظفون / الفنيون", Sub(s, e) ShowTabContainer(btnTabEmp, pnlEmpContainer))
            btnTabChairs = CreateTabButton("الكراسي", Sub(s, e) ShowTabContainer(btnTabChairs, pnlChairContainer))
            btnTabServices = CreateTabButton("الخدمات والإضافات", Sub(s, e) ShowTabContainer(btnTabServices, pnlServiceContainer))
            btnTabAbout = CreateTabButton("تفعيل الترخيص وحول البرنامج", Sub(s, e) ShowTabContainer(btnTabAbout, pnlAboutContainer))

            ' First added item appears at the FAR RIGHT edge in RTL
            flowTabs.Controls.Add(btnTabSettings)
            flowTabs.Controls.Add(btnTabUsers)
            flowTabs.Controls.Add(btnTabEmp)
            flowTabs.Controls.Add(btnTabChairs)
            flowTabs.Controls.Add(btnTabServices)
            flowTabs.Controls.Add(btnTabAbout)
            pnlTabHeader.Controls.Add(flowTabs)

            ' 2. Main Content Container
            pnlTabContent = New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = UIStyleHelper.LightBgColor,
                .Padding = New Padding(12)
            }

            ' Build Settings Container
            pnlSettingsContainer = New Panel With {.Dock = DockStyle.Fill}
            Dim frmSet As New FrmSettings()
            frmSet.TopLevel = False
            frmSet.FormBorderStyle = FormBorderStyle.None
            frmSet.Dock = DockStyle.Fill
            pnlSettingsContainer.Controls.Add(frmSet)
            frmSet.Show()

            ' Build Users Container
            pnlUsersContainer = New Panel With {.Dock = DockStyle.Fill}
            Dim pnlUsersTop As New Panel With {.Dock = DockStyle.Top, .Height = 50, .Padding = New Padding(10), .RightToLeft = RightToLeft.Yes}

            btnAddUser = New Button With {.Text = "+ إضافة مستخدم جديد", .Dock = DockStyle.Right, .Width = 180, .Font = UIStyleHelper.FontBold}
            UIStyleHelper.ApplyButtonFlat(btnAddUser, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnAddUser.Click, AddressOf OnAddUserClicked

            btnEditUser = New Button With {.Text = "✏️ تعديل بيانات وصلاحيات", .Dock = DockStyle.Right, .Width = 200, .Font = UIStyleHelper.FontBold, .Margin = New Padding(0, 0, 10, 0)}
            UIStyleHelper.ApplyButtonFlat(btnEditUser, Color.FromArgb(41, 128, 185), Color.White)
            AddHandler btnEditUser.Click, AddressOf OnEditUserClicked

            btnDeleteUser = New Button With {.Text = "🗑 حذف المستخدم", .Dock = DockStyle.Left, .Width = 150, .Font = UIStyleHelper.FontBold}
            UIStyleHelper.ApplyButtonFlat(btnDeleteUser, Color.FromArgb(231, 76, 60), Color.White)
            AddHandler btnDeleteUser.Click, AddressOf OnDeleteUserClicked

            pnlUsersTop.Controls.Add(btnEditUser)
            pnlUsersTop.Controls.Add(btnAddUser)
            pnlUsersTop.Controls.Add(btnDeleteUser)

            dgvUsers = CreateGrid()
            pnlUsersContainer.Controls.Add(dgvUsers)
            pnlUsersContainer.Controls.Add(pnlUsersTop)

            ' Build Employee Panel Container
            pnlEmpContainer = New Panel With {.Dock = DockStyle.Fill}
            Dim pnlEmpTop As New Panel With {.Dock = DockStyle.Top, .Height = 50, .Padding = New Padding(10)}
            btnAddEmp = New Button With {.Text = "+ إضافة موظف جديد", .Dock = DockStyle.Right, .Width = 180}
            UIStyleHelper.ApplyButtonFlat(btnAddEmp, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnAddEmp.Click, AddressOf OnAddEmpClicked
            pnlEmpTop.Controls.Add(btnAddEmp)
            dgvEmployees = CreateGrid()
            pnlEmpContainer.Controls.Add(dgvEmployees)
            pnlEmpContainer.Controls.Add(pnlEmpTop)

            ' Build Chair Panel Container
            pnlChairContainer = New Panel With {.Dock = DockStyle.Fill}
            Dim pnlChairTop As New Panel With {.Dock = DockStyle.Top, .Height = 50, .Padding = New Padding(10)}
            btnAddChair = New Button With {.Text = "+ إضافة كرسي جديد", .Dock = DockStyle.Right, .Width = 180}
            UIStyleHelper.ApplyButtonFlat(btnAddChair, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnAddChair.Click, AddressOf OnAddChairClicked
            pnlChairTop.Controls.Add(btnAddChair)
            dgvChairs = CreateGrid()
            pnlChairContainer.Controls.Add(dgvChairs)
            pnlChairContainer.Controls.Add(pnlChairTop)

            ' Build Service Panel Container
            pnlServiceContainer = New Panel With {.Dock = DockStyle.Fill}
            Dim pnlSrvTop As New Panel With {.Dock = DockStyle.Top, .Height = 50, .Padding = New Padding(10)}
            btnAddService = New Button With {.Text = "+ إضافة خدمة جديدة", .Dock = DockStyle.Right, .Width = 180}
            UIStyleHelper.ApplyButtonFlat(btnAddService, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnAddService.Click, AddressOf OnAddServiceClicked
            pnlSrvTop.Controls.Add(btnAddService)
            dgvServices = CreateGrid()
            pnlServiceContainer.Controls.Add(dgvServices)
            pnlServiceContainer.Controls.Add(pnlSrvTop)

            ' Build About Container
            pnlAboutContainer = New Panel With {.Dock = DockStyle.Fill}
            BuildAboutLicenseTab()

            Me.Controls.Add(pnlTabContent)
            Me.Controls.Add(pnlTabHeader)

            ' Default show Settings tab as FIRST tab
            ShowTabContainer(btnTabSettings, pnlSettingsContainer)
        End Sub

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

        Private Sub ShowTabContainer(activeBtn As Button, activePanel As Panel)
            Dim tabBtns = New Button() {btnTabSettings, btnTabUsers, btnTabEmp, btnTabChairs, btnTabServices, btnTabAbout}
            For Each btn As Button In tabBtns
                UIStyleHelper.ApplyButtonFlatWithBorder(btn, Color.FromArgb(245, 247, 250), UIStyleHelper.ZexTextDark, UIStyleHelper.ZexBorder, 1)
            Next
            UIStyleHelper.ApplyButtonFlatWithBorder(activeBtn, UIStyleHelper.ZexNavy, Color.White, UIStyleHelper.ZexNavy, 1)

            pnlTabContent.Controls.Clear()
            activePanel.Dock = DockStyle.Fill
            pnlTabContent.Controls.Add(activePanel)
            activePanel.BringToFront()
        End Sub

        Private Function CreateGrid() As DataGridView
            Dim dgv As New DataGridView With {
                .Dock = DockStyle.Fill,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            }
            UIStyleHelper.ApplyDataGridStyle(dgv)
            Return dgv
        End Function

        Private Sub ConfigureGridColumns()
            ' Users Grid
            dgvUsers.Columns.Clear()
            dgvUsers.AutoGenerateColumns = False

            dgvUsers.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Id",
                .HeaderText = "المعرف",
                .Width = 70,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontBold}
            })
            dgvUsers.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Username",
                .HeaderText = "اسم الدخول",
                .Width = 140,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy}
            })
            dgvUsers.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "DisplayName",
                .HeaderText = "الاسم الكامل / العرض",
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexTextDark}
            })
            dgvUsers.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "RoleArabic",
                .HeaderText = "الدور / الرتبة",
                .Width = 150,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontBold}
            })
            dgvUsers.Columns.Add(New DataGridViewCheckBoxColumn With {
                .DataPropertyName = "IsActive",
                .HeaderText = "نشط",
                .Width = 70
            })
            dgvUsers.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "CreatedAt",
                .HeaderText = "تاريخ التسجيل",
                .Width = 160,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontRegular}
            })

            ' Employees Grid
            dgvEmployees.Columns.Clear()
            dgvEmployees.AutoGenerateColumns = False

            dgvEmployees.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Id",
                .HeaderText = "رقم الموظف",
                .Width = 90,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontBold}
            })
            dgvEmployees.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Name",
                .HeaderText = "اسم الموظف / الفني",
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexTextDark}
            })
            dgvEmployees.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Phone",
                .HeaderText = "رقم الهاتف",
                .Width = 160,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontRegular}
            })
            dgvEmployees.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "CommissionRate",
                .HeaderText = "نسبة العمولة (%)",
                .Width = 130,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontBold}
            })

            ' Chairs Grid
            dgvChairs.Columns.Clear()
            dgvChairs.AutoGenerateColumns = False

            dgvChairs.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Id",
                .HeaderText = "رقم المعرف",
                .Width = 90,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontBold}
            })
            dgvChairs.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "ChairNumber",
                .HeaderText = "رقم الكرسي",
                .Width = 110,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontBold}
            })
            dgvChairs.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Name",
                .HeaderText = "اسم / وصف الكرسي",
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexTextDark}
            })

            ' Services Grid
            dgvServices.Columns.Clear()
            dgvServices.AutoGenerateColumns = False

            dgvServices.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Id",
                .HeaderText = "رقم الخدمة",
                .Width = 90,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontBold}
            })
            dgvServices.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Name",
                .HeaderText = "اسم الخدمة",
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexTextDark}
            })
            dgvServices.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Price",
                .HeaderText = "سعر الخدمة",
                .Width = 130,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy}
            })
            dgvServices.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "DurationMinutes",
                .HeaderText = "المدة التقديرية (دقيقة)",
                .Width = 160,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontRegular}
            })
        End Sub

        Private Sub LoadAllData()
            dgvUsers.DataSource = Nothing
            dgvUsers.DataSource = userRepo.GetAllUsers()

            dgvEmployees.DataSource = Nothing
            dgvEmployees.DataSource = empRepo.GetActiveEmployees()

            dgvChairs.DataSource = Nothing
            dgvChairs.DataSource = chairRepo.GetAllChairs()

            dgvServices.DataSource = Nothing
            dgvServices.DataSource = srvRepo.GetServices()
        End Sub

        Private Sub OnAddUserClicked(sender As Object, e As EventArgs)
            Using frm As New FrmUserModal()
                If frm.ShowDialog() = DialogResult.OK Then
                    userRepo.SaveUser(frm.UserToEdit, frm.NewPassword)
                    LoadAllData()
                    MessageBox.Show("تم إضافة المستخدم بنجاح وتخصيص صلاحياته!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using
        End Sub

        Private Sub OnEditUserClicked(sender As Object, e As EventArgs)
            If dgvUsers.CurrentRow IsNot Nothing AndAlso dgvUsers.CurrentRow.DataBoundItem IsNot Nothing Then
                Dim selectedUser As User = CType(dgvUsers.CurrentRow.DataBoundItem, User)
                Using frm As New FrmUserModal(selectedUser)
                    If frm.ShowDialog() = DialogResult.OK Then
                        userRepo.SaveUser(frm.UserToEdit, frm.NewPassword)
                        LoadAllData()
                        MessageBox.Show("تم تعديل صلاحيات وحساب المستخدم بنجاح!", "تم التحديث", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If
                End Using
            Else
                MessageBox.Show("يرجى اختيار مستخدم من الجدول لتعديله أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End Sub

        Private Sub OnDeleteUserClicked(sender As Object, e As EventArgs)
            If dgvUsers.CurrentRow IsNot Nothing AndAlso dgvUsers.CurrentRow.DataBoundItem IsNot Nothing Then
                Dim selectedUser As User = CType(dgvUsers.CurrentRow.DataBoundItem, User)
                If selectedUser.Username.ToLower() = "admin" Then
                    MessageBox.Show("عفواً، لا يمكن حذف حساب المدير الرئيسي (admin).", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                If MessageBox.Show($"هل أنت تأكد من رغبتك في حذف المستخدم ({selectedUser.DisplayName})؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                    userRepo.DeleteUser(selectedUser.Id)
                    LoadAllData()
                End If
            Else
                MessageBox.Show("يرجى اختيار مستخدم للحذف أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End Sub

        Private Sub OnAddEmpClicked(sender As Object, e As EventArgs)
            Dim name = InputBox("أدخل اسم الموظف الجديد:", "إضافة موظف")
            If Not String.IsNullOrWhiteSpace(name) Then
                empRepo.SaveEmployee(New Employee With {.Id = 0, .Name = name.Trim(), .Phone = "", .CommissionRate = 0.0, .IsActive = True})
                LoadAllData()
            End If
        End Sub

        Private Sub OnAddChairClicked(sender As Object, e As EventArgs)
            Dim name = InputBox("أدخل اسم/وصف الكرسي الجديد:", "إضافة كرسي")
            If Not String.IsNullOrWhiteSpace(name) Then
                Dim allCh = chairRepo.GetAllChairs()
                Dim nextNum As Integer = allCh.Count + 1
                chairRepo.SaveChair(New Chair With {.Id = 0, .ChairNumber = nextNum, .Name = name.Trim(), .DisplayOrder = nextNum, .IsActive = True})
                LoadAllData()
            End If
        End Sub

        Private Sub OnAddServiceClicked(sender As Object, e As EventArgs)
            Dim name = InputBox("أدخل اسم الخدمة الجديدة:", "إضافة خدمة")
            If Not String.IsNullOrWhiteSpace(name) Then
                Dim priceStr = InputBox("أدخل سعر الخدمة:", "سعر الخدمة", "50")
                Dim price As Double = 0
                Double.TryParse(priceStr, price)
                Dim cats = srvRepo.GetCategories()
                Dim catId As Integer = If(cats.Count > 0, cats(0).Id, 1)
                srvRepo.SaveService(New ServiceItem With {.Id = 0, .CategoryId = catId, .Name = name.Trim(), .Price = price, .DurationMinutes = 30, .IsActive = True})
                LoadAllData()
            End If
        End Sub

        Private Sub BuildAboutLicenseTab()
            pnlAboutContainer.Controls.Clear()

            Dim pnlScroll As New Panel With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .Padding = New Padding(20),
                .RightToLeft = RightToLeft.Yes
            }

            ' Card 1: System Info Header
            Dim cardSys As New GroupBox With {
                .Dock = DockStyle.Top,
                .Height = 110,
                .Text = "  معلومات النظام والترخيص الرسمية  ",
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .BackColor = Color.White,
                .Padding = New Padding(16)
            }

            Dim lblSysInfo As New Label With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Text = "نظام ZEX SALON — لإدارة وتشغيل الصالونات ومراكز التجميل (إصدار Professional v2.5)" & vbCrLf &
                        "الدعم الفني والخدمات: 01022422010 | 01096060003 | الموقع الإلكتروني: Nowzex.com"
            }
            cardSys.Controls.Add(lblSysInfo)

            Dim pnlSpace1 As New Panel With {.Dock = DockStyle.Top, .Height = 16}

            ' Card 2: Interactive License Activation Form
            Dim cardLicense As New GroupBox With {
                .Dock = DockStyle.Top,
                .Height = 350,
                .Text = "  تفعيل الترخيص ومعرف الجهاز (Machine Activation)  ",
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .BackColor = Color.White,
                .Padding = New Padding(20)
            }

            ' Machine ID Row
            Dim pnlMidRow As New Panel With {.Dock = DockStyle.Top, .Height = 50, .Margin = New Padding(0, 10, 0, 10)}
            Dim lblM As New Label With {
                .Dock = DockStyle.Right,
                .Width = 180,
                .Text = "معرف الجهاز (Machine ID):",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight
            }

            txtAdminMachineId = New TextBox With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.GetFont(13.0F, FontStyle.Bold),
                .ReadOnly = True,
                .Text = MachineFingerprint.GetMachineId(),
                .BackColor = Color.FromArgb(240, 244, 248),
                .TextAlign = HorizontalAlignment.Center,
                .BorderStyle = BorderStyle.FixedSingle
            }

            btnAdminCopyMachineId = New Button With {
                .Dock = DockStyle.Left,
                .Width = 160,
                .Text = "📋 نسخ معرف الجهاز",
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnAdminCopyMachineId, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnAdminCopyMachineId.Click, Sub()
                                                        Clipboard.SetText(txtAdminMachineId.Text)
                                                        MessageBox.Show("تم نسخ معرف الجهاز إلى الحافظة بنجاح!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                    End Sub

            pnlMidRow.Controls.Add(txtAdminMachineId)
            pnlMidRow.Controls.Add(btnAdminCopyMachineId)
            pnlMidRow.Controls.Add(lblM)

            ' Status Badge Row
            lblAdminLicenseStatus = New Label With {
                .Dock = DockStyle.Top,
                .Height = 40,
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            ' License Key Input Row
            Dim pnlKeyRow As New Panel With {.Dock = DockStyle.Top, .Height = 110, .Margin = New Padding(0, 10, 0, 10)}
            Dim lblK As New Label With {
                .Dock = DockStyle.Right,
                .Width = 180,
                .Text = "أدخل كود التفعيل:",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight
            }

            txtAdminLicenseKey = New TextBox With {
                .Dock = DockStyle.Fill,
                .Height = 100,
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical,
                .Font = New Font("Consolas", 10.5F, FontStyle.Regular),
                .RightToLeft = RightToLeft.No,
                .BorderStyle = BorderStyle.FixedSingle,
                .BackColor = Color.White
            }
            pnlKeyRow.Controls.Add(txtAdminLicenseKey)
            pnlKeyRow.Controls.Add(lblK)

            ' Activate Button
            btnAdminActivate = New Button With {
                .Dock = DockStyle.Top,
                .Height = 50,
                .Text = "🔑 تفعيل الترخيص الآن",
                .Font = UIStyleHelper.GetFont(13.0F, FontStyle.Bold)
            }
            UIStyleHelper.ApplyButtonFlat(btnAdminActivate, UIStyleHelper.ActionGreen, Color.White)
            AddHandler btnAdminActivate.Click, AddressOf OnAdminActivateClicked

            cardLicense.Controls.Add(btnAdminActivate)
            cardLicense.Controls.Add(pnlKeyRow)
            cardLicense.Controls.Add(lblAdminLicenseStatus)
            cardLicense.Controls.Add(pnlMidRow)

            pnlScroll.Controls.Add(cardLicense)
            pnlScroll.Controls.Add(pnlSpace1)
            pnlScroll.Controls.Add(cardSys)

            pnlAboutContainer.Controls.Add(pnlScroll)

            RefreshAdminLicenseStatus()
        End Sub

        Public Sub SelectAboutTab()
            ShowTabContainer(btnTabAbout, pnlAboutContainer)
        End Sub

        Public Sub SelectSettingsTab()
            ShowTabContainer(btnTabSettings, pnlSettingsContainer)
        End Sub

        Private Sub RefreshAdminLicenseStatus()
            Dim res = LicenseValidator.ValidateCurrentLicense()
            If res.Status = LicenseStatus.ACTIVE Then
                Dim custName As String = If(res.License IsNot Nothing AndAlso Not String.IsNullOrEmpty(res.License.CustomerName), res.License.CustomerName, "صالون الأناقة والجمال")
                Dim expStr As String = If(res.License IsNot Nothing AndAlso res.License.IsLifetime, "مفتوح - مدى الحياة", If(res.License IsNot Nothing, res.License.ExpiryDate.ToString("yyyy/MM/dd"), "سارٍ"))
                lblAdminLicenseStatus.Text = $"🟢 حالة الترخيص: نشط ومفعل رسمياً  |  المرخص له: ({custName})  |  الصلاحية: ({expStr})"
                lblAdminLicenseStatus.ForeColor = Color.DarkGreen
            Else
                lblAdminLicenseStatus.Text = $"🔴 حالة الترخيص: غير مفعل أو مفقود ({res.Message})"
                lblAdminLicenseStatus.ForeColor = Color.Red
            End If
        End Sub

        Private Sub OnAdminActivateClicked(sender As Object, e As EventArgs)
            Dim keyInput As String = txtAdminLicenseKey.Text.Trim()
            If String.IsNullOrWhiteSpace(keyInput) Then
                MessageBox.Show("يرجى إدخال كود التفعيل أولاً في الخانة المخصصة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim currentMid As String = MachineFingerprint.GetMachineId()
            Dim valResult = LicenseValidator.ValidateKey(keyInput, currentMid)

            If valResult.Status = LicenseStatus.ACTIVE Then
                LicenseValidator.SaveLicense(keyInput, valResult.License.CustomerName, valResult.License.MachineId, valResult.License.ExpiryDate)
                ActivityLogRepository.Log(If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.Id, 1), If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.DisplayName, "مدير"), "تفعيل ترخيص", $"تم تفعيل الترخيص بنجاح للعميل: {valResult.License.CustomerName}")
                MessageBox.Show($"تم تفعيل ترخيص البرنامج بنجاح!{vbCrLf}مرحباً بك: {valResult.License.CustomerName}", "نجاح التفعيل", MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtAdminLicenseKey.Clear()
                RefreshAdminLicenseStatus()
            Else
                MessageBox.Show($"تعذر تفعيل الترخيص:{vbCrLf}{valResult.Message}", "فشل التفعيل", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End Sub
    End Class

End Namespace
