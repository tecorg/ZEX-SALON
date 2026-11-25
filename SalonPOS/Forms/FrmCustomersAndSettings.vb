Imports System
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Windows.Forms
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Models
Imports SalonPOS.Services

Namespace SalonPOS.Forms

    Public Class FrmCustomers
        Inherits Form

        Private custRepo As New CustomerRepository()
        Private txtSearch As TextBox
        Private dgvCustomers As DataGridView

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Text = "إدارة وسجل العملاء"
            InitializeLayout()
            ConfigureGridColumns()
            LoadCustomers()
        End Sub

        Private Sub InitializeLayout()
            ' Top Search Box
            Dim pnlTop As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 60,
                .BackColor = Color.White,
                .Padding = New Padding(15, 12, 15, 12)
            }

            Dim lblS As New Label With {
                .Text = "بحث سريع في العملاء:",
                .Dock = DockStyle.Right,
                .Width = 140,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight
            }

            txtSearch = New TextBox With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.FontRegular,
                .RightToLeft = RightToLeft.Yes
            }
            AddHandler txtSearch.TextChanged, Sub() LoadCustomers()

            pnlTop.Controls.Add(txtSearch)
            pnlTop.Controls.Add(lblS)

            ' DataGrid
            dgvCustomers = New DataGridView With {
                .Dock = DockStyle.Fill
            }
            UIStyleHelper.ApplyDataGridStyle(dgvCustomers)

            Me.Controls.Add(dgvCustomers)
            Me.Controls.Add(pnlTop)
        End Sub

        Private Sub ConfigureGridColumns()
            dgvCustomers.Columns.Clear()
            dgvCustomers.AutoGenerateColumns = False

            Dim colId As New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Id",
                .HeaderText = "رقم العميل",
                .Width = 90,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontBold}
            }

            Dim colName As New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Name",
                .HeaderText = "اسم العميل",
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexTextDark}
            }

            Dim colPhone As New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Phone",
                .HeaderText = "رقم الهاتف",
                .Width = 160,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontRegular}
            }

            Dim colNotes As New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Notes",
                .HeaderText = "ملاحظات",
                .Width = 220,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft, .Font = UIStyleHelper.FontRegular}
            }

            Dim colDate As New DataGridViewTextBoxColumn With {
                .DataPropertyName = "CreatedAt",
                .HeaderText = "تاريخ التسجيل",
                .Width = 180,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontRegular}
            }

            dgvCustomers.Columns.Add(colId)
            dgvCustomers.Columns.Add(colName)
            dgvCustomers.Columns.Add(colPhone)
            dgvCustomers.Columns.Add(colNotes)
            dgvCustomers.Columns.Add(colDate)
        End Sub

        Private Sub LoadCustomers()
            Dim list = custRepo.SearchCustomers(txtSearch.Text)
            dgvCustomers.DataSource = Nothing
            dgvCustomers.DataSource = list
        End Sub
    End Class

    ' =========================================================================
    ' 2. FRMSETTINGS (شاشة إعدادات الصالون والضريبة والطابعة والنسخ الاحتياطي)
    ' =========================================================================
    Public Class FrmSettings
        Inherits Form

        Private settingsRepo As New SettingsRepository()
        Private currentSettings As SalonSettings

        Private pnlScroll As Panel
        Private txtSalonName As TextBox
        Private txtPhone As TextBox
        Private txtAddress As TextBox
        Private txtCurrency As TextBox
        Private txtFooterMsg As TextBox

        Private picLogoPreview As PictureBox
        Private btnSelectLogo As Button
        Private btnRemoveLogo As Button

        Private txtTaxRate As TextBox
        Private chkEnableTax As CheckBox
        Private cboPrinters As ComboBox

        Private btnSaveSettings As Button
        Private btnBackupNow As Button
        Private btnBackupExport As Button
        Private btnRestoreBackup As Button
        Private btnOpenBackupFolder As Button
        Private btnResetTrainingData As Button

        Private chkEnableSecondaryBackup As CheckBox
        Private txtSecondaryBackupPath As TextBox
        Private btnBrowseSecondaryBackup As Button
        Private btnClearSecondaryBackup As Button

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Text = "إعدادات الصالون والضريبة والنسخ الاحتياطي"
            Me.BackColor = UIStyleHelper.LightBgColor

            currentSettings = settingsRepo.GetSettings()
            InitializeLayout()
            LoadSettingsData()
        End Sub

        Private Sub InitializeLayout()
            Me.Controls.Clear()

            pnlScroll = New Panel With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .Padding = New Padding(24, 16, 24, 24),
                .RightToLeft = RightToLeft.Yes
            }

            ' -----------------------------------------------------------------
            ' CARD 1: Salon Main Info & Branding (Full Width)
            ' -----------------------------------------------------------------
            Dim cardInfo As New GroupBox With {
                .Dock = DockStyle.Top,
                .Height = 360,
                .Text = "  بيانات الصالون والنشاط التجارية (بيانات اختيارية - غير إجبارية)  ",
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .BackColor = Color.White,
                .Padding = New Padding(16),
                .Margin = New Padding(0, 0, 0, 16)
            }

            ' Left Container inside Card 1 for Logo Upload Box
            Dim pnlLogoBox As New Panel With {
                .Dock = DockStyle.Left,
                .Width = 200,
                .BackColor = Color.FromArgb(248, 250, 252),
                .BorderStyle = BorderStyle.FixedSingle,
                .Padding = New Padding(8)
            }

            Dim lblLogoTitle As New Label With {
                .Dock = DockStyle.Top,
                .Height = 28,
                .Text = "شعار الصالون (اختياري):",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            picLogoPreview = New PictureBox With {
                .Dock = DockStyle.Top,
                .Height = 160,
                .SizeMode = PictureBoxSizeMode.Zoom,
                .BorderStyle = BorderStyle.FixedSingle,
                .BackColor = Color.White
            }

            btnSelectLogo = New Button With {
                .Dock = DockStyle.Top,
                .Height = 38,
                .Text = "📷 اختر اللوجو",
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(0, 8, 0, 4)
            }
            UIStyleHelper.ApplyButtonFlat(btnSelectLogo, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnSelectLogo.Click, AddressOf OnSelectLogoClicked

            btnRemoveLogo = New Button With {
                .Dock = DockStyle.Top,
                .Height = 34,
                .Text = "❌ حذف اللوجو",
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnRemoveLogo, Color.FromArgb(192, 57, 43), Color.White)
            AddHandler btnRemoveLogo.Click, AddressOf OnRemoveLogoClicked

            pnlLogoBox.Controls.Add(btnRemoveLogo)
            pnlLogoBox.Controls.Add(btnSelectLogo)
            pnlLogoBox.Controls.Add(picLogoPreview)
            pnlLogoBox.Controls.Add(lblLogoTitle)

            ' Right Inputs Container
            Dim pnlInputsContainer As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(12, 0, 0, 0)
            }

            ' Row 1: Salon Name & Currency
            Dim pnlRow1 As New Panel With {.Dock = DockStyle.Top, .Height = 65}
            Dim lblN As New Label With {.Text = "اسم الصالون / النشاط التجارى:", .Dock = DockStyle.Right, .Width = 200, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy}
            txtSalonName = New TextBox With {.Dock = DockStyle.Fill, .Font = UIStyleHelper.FontRegular, .RightToLeft = RightToLeft.Yes}
            Dim lblC As New Label With {.Text = "رمز العملة (مثل ج.م):", .Dock = DockStyle.Left, .Width = 140, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy}
            txtCurrency = New TextBox With {.Dock = DockStyle.Left, .Width = 100, .Font = UIStyleHelper.FontRegular, .RightToLeft = RightToLeft.Yes, .TextAlign = HorizontalAlignment.Center}
            pnlRow1.Controls.Add(txtSalonName)
            pnlRow1.Controls.Add(lblN)
            pnlRow1.Controls.Add(txtCurrency)
            pnlRow1.Controls.Add(lblC)

            ' Row 2: Phone & Address
            Dim pnlRow2 As New Panel With {.Dock = DockStyle.Top, .Height = 65}
            Dim lblP As New Label With {.Text = "رقم الهاتف / التواصل:", .Dock = DockStyle.Right, .Width = 200, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy}
            txtPhone = New TextBox With {.Dock = DockStyle.Fill, .Font = UIStyleHelper.FontRegular, .RightToLeft = RightToLeft.Yes}
            pnlRow2.Controls.Add(txtPhone)
            pnlRow2.Controls.Add(lblP)

            ' Row 3: Address
            Dim pnlRow3 As New Panel With {.Dock = DockStyle.Top, .Height = 65}
            Dim lblA As New Label With {.Text = "العنوان الكامل (اختياري):", .Dock = DockStyle.Right, .Width = 200, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy}
            txtAddress = New TextBox With {.Dock = DockStyle.Fill, .Font = UIStyleHelper.FontRegular, .RightToLeft = RightToLeft.Yes}
            pnlRow3.Controls.Add(txtAddress)
            pnlRow3.Controls.Add(lblA)

            ' Row 4: Footer Message
            Dim pnlRow4 As New Panel With {.Dock = DockStyle.Top, .Height = 65}
            Dim lblF As New Label With {.Text = "رسالة أسفل الفاتورة (Footer):", .Dock = DockStyle.Right, .Width = 200, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy}
            txtFooterMsg = New TextBox With {.Dock = DockStyle.Fill, .Font = UIStyleHelper.FontRegular, .RightToLeft = RightToLeft.Yes}
            pnlRow4.Controls.Add(txtFooterMsg)
            pnlRow4.Controls.Add(lblF)

            pnlInputsContainer.Controls.Add(pnlRow4)
            pnlInputsContainer.Controls.Add(pnlRow3)
            pnlInputsContainer.Controls.Add(pnlRow2)
            pnlInputsContainer.Controls.Add(pnlRow1)

            cardInfo.Controls.Add(pnlInputsContainer)
            cardInfo.Controls.Add(pnlLogoBox)

            ' Spacer
            Dim pnlSpace1 As New Panel With {.Dock = DockStyle.Top, .Height = 16}

            ' -----------------------------------------------------------------
            ' CARD 2: Printer Selection Settings
            ' -----------------------------------------------------------------
            Dim cardPrinter As New GroupBox With {
                .Dock = DockStyle.Top,
                .Height = 100,
                .Text = "  إعدادات وتحديد طابعة الفواتير والتقارير  ",
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .BackColor = Color.White,
                .Padding = New Padding(16),
                .Margin = New Padding(0, 0, 0, 16)
            }

            Dim pnlPrintRow As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(8, 8, 8, 8)}

            Dim lblPrinterLabel As New Label With {
                .Dock = DockStyle.Right,
                .Width = 180,
                .Text = "اختر طابعة الفواتير:",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight
            }

            cboPrinters = New ComboBox With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .RightToLeft = RightToLeft.Yes
            }

            cboPrinters.Items.Add("(طابعة النظام الافتراضية)")
            Try
                For Each pName As String In PrinterSettings.InstalledPrinters
                    cboPrinters.Items.Add(pName)
                Next
            Catch
            End Try
            cboPrinters.SelectedIndex = 0

            pnlPrintRow.Controls.Add(cboPrinters)
            pnlPrintRow.Controls.Add(lblPrinterLabel)
            cardPrinter.Controls.Add(pnlPrintRow)

            ' Spacer
            Dim pnlSpace2 As New Panel With {.Dock = DockStyle.Top, .Height = 16}

            ' -----------------------------------------------------------------
            ' CARD 3: Tax Settings (Default: WITHOUT TAX)
            ' -----------------------------------------------------------------
            Dim cardTax As New GroupBox With {
                .Dock = DockStyle.Top,
                .Height = 110,
                .Text = "  إعدادات الضريبة والعمل بدون ضريبة  ",
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .BackColor = Color.White,
                .Padding = New Padding(16),
                .Margin = New Padding(0, 0, 0, 16)
            }

            Dim flowTax As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .Padding = New Padding(8, 12, 8, 8)
            }

            chkEnableTax = New CheckBox With {
                .Text = "تفعيل إتاحة الضريبة على الفاتورة (افتراضياً: بدون ضريبة)",
                .AutoSize = True,
                .Font = UIStyleHelper.GetFont(11.5F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .Checked = False,
                .Margin = New Padding(4, 6, 24, 4)
            }
            AddHandler chkEnableTax.CheckedChanged, Sub()
                                                         txtTaxRate.Enabled = chkEnableTax.Checked
                                                         txtTaxRate.BackColor = If(chkEnableTax.Checked, Color.White, Color.FromArgb(240, 244, 248))
                                                     End Sub

            Dim lblTaxLabel As New Label With {
                .Text = "نسبة الضريبة (%):",
                .AutoSize = True,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .Margin = New Padding(4, 8, 4, 4)
            }

            txtTaxRate = New TextBox With {
                .Width = 100,
                .Height = 36,
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                .RightToLeft = RightToLeft.Yes,
                .TextAlign = HorizontalAlignment.Center,
                .Text = "14.0",
                .Enabled = False,
                .BackColor = Color.FromArgb(240, 244, 248),
                .BorderStyle = BorderStyle.FixedSingle,
                .Margin = New Padding(4, 4, 4, 4)
            }

            flowTax.Controls.Add(chkEnableTax)
            flowTax.Controls.Add(lblTaxLabel)
            flowTax.Controls.Add(txtTaxRate)
            cardTax.Controls.Add(flowTax)

            ' Spacer
            Dim pnlSpace3 As New Panel With {.Dock = DockStyle.Top, .Height = 16}

            ' -----------------------------------------------------------------
            ' CARD 4: Database Backup & Security (Full Feature GroupBox)
            ' -----------------------------------------------------------------
            Dim cardBackup As New GroupBox With {
                .Dock = DockStyle.Top,
                .Height = 310,
                .Text = "  إدارة وتفعيل النسخ الاحتياطي واستعادة البيانات (Database Backup & Security)  ",
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .BackColor = Color.White,
                .Padding = New Padding(16),
                .Margin = New Padding(0, 0, 0, 16)
            }

            Dim lblBackupNotice As New Label With {
                .Dock = DockStyle.Top,
                .Height = 28,
                .Text = "✓ يتم حفظ نسخة احتياطية آمنة تلقائياً، ويمكنك تصدير أو استعادة النسخة الاحتياطية في أي وقت لضمان الأمان الفائق.",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.DarkGreen,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            Dim flowBackupBtns As New FlowLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 110,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(4)
            }

            btnBackupNow = New Button With {
                .Width = 220,
                .Height = 46,
                .Text = "📦 إنشاء نسخة احتياطية فورية",
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4)
            }
            UIStyleHelper.ApplyButtonFlat(btnBackupNow, UIStyleHelper.ActionGreen, Color.White)
            AddHandler btnBackupNow.Click, AddressOf OnBackupNowClicked

            btnBackupExport = New Button With {
                .Width = 220,
                .Height = 46,
                .Text = "📂 تصدير لمجلد مخصص / فلاشة",
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4)
            }
            UIStyleHelper.ApplyButtonFlat(btnBackupExport, Color.FromArgb(41, 128, 185), Color.White)
            AddHandler btnBackupExport.Click, AddressOf OnBackupExportClicked

            btnRestoreBackup = New Button With {
                .Width = 220,
                .Height = 46,
                .Text = "🔄 استعادة نسخة احتياطية سابقة",
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4)
            }
            UIStyleHelper.ApplyButtonFlat(btnRestoreBackup, Color.FromArgb(192, 57, 43), Color.White)
            AddHandler btnRestoreBackup.Click, AddressOf OnRestoreBackupClicked

            btnOpenBackupFolder = New Button With {
                .Width = 200,
                .Height = 46,
                .Text = "📁 فتح مجلد النسخ الاحتياطية",
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4)
            }
            UIStyleHelper.ApplyButtonFlat(btnOpenBackupFolder, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnOpenBackupFolder.Click, Sub() BackupService.OpenBackupsFolder()

            btnResetTrainingData = New Button With {
                .Width = 250,
                .Height = 46,
                .Text = "🧹 تصفير بيانات التجربة للبدء الفعلي",
                .Font = UIStyleHelper.FontBold,
                .Margin = New Padding(4)
            }
            UIStyleHelper.ApplyButtonFlat(btnResetTrainingData, Color.FromArgb(230, 126, 34), Color.White)
            AddHandler btnResetTrainingData.Click, AddressOf OnResetTrainingDataClicked

            flowBackupBtns.Controls.Add(btnBackupNow)
            flowBackupBtns.Controls.Add(btnBackupExport)
            flowBackupBtns.Controls.Add(btnRestoreBackup)
            flowBackupBtns.Controls.Add(btnOpenBackupFolder)
            flowBackupBtns.Controls.Add(btnResetTrainingData)

            Dim pnlSecondaryBackup As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 85,
                .Padding = New Padding(4, 10, 4, 4),
                .RightToLeft = RightToLeft.Yes
            }

            chkEnableSecondaryBackup = New CheckBox With {
                .Dock = DockStyle.Top,
                .Height = 28,
                .Text = "تفعيل النسخ الاحتياطي التلقائي المزدوج على مسار إضافي (فلاشة USB / هارد خارجي)",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy
            }

            Dim pnlPathRow As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 40,
                .Padding = New Padding(0, 4, 0, 0)
            }

            btnClearSecondaryBackup = New Button With {
                .Dock = DockStyle.Left,
                .Width = 90,
                .Text = "❌ إزالة",
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnClearSecondaryBackup, Color.FromArgb(192, 57, 43), Color.White)
            AddHandler btnClearSecondaryBackup.Click, Sub()
                                                         txtSecondaryBackupPath.Text = ""
                                                         chkEnableSecondaryBackup.Checked = False
                                                     End Sub

            btnBrowseSecondaryBackup = New Button With {
                .Dock = DockStyle.Left,
                .Width = 170,
                .Text = "📂 اختيار مسار الفلاشة",
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnBrowseSecondaryBackup, Color.FromArgb(41, 128, 185), Color.White)
            AddHandler btnBrowseSecondaryBackup.Click, AddressOf OnBrowseSecondaryBackupClicked

            txtSecondaryBackupPath = New TextBox With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.FontRegular,
                .ReadOnly = True,
                .BackColor = Color.FromArgb(245, 248, 252)
            }

            pnlPathRow.Controls.Add(txtSecondaryBackupPath)
            pnlPathRow.Controls.Add(btnBrowseSecondaryBackup)
            pnlPathRow.Controls.Add(btnClearSecondaryBackup)

            pnlSecondaryBackup.Controls.Add(pnlPathRow)
            pnlSecondaryBackup.Controls.Add(chkEnableSecondaryBackup)

            cardBackup.Controls.Add(pnlSecondaryBackup)
            cardBackup.Controls.Add(flowBackupBtns)
            cardBackup.Controls.Add(lblBackupNotice)

            ' Spacer
            Dim pnlSpace4 As New Panel With {.Dock = DockStyle.Top, .Height = 16}

            ' -----------------------------------------------------------------
            ' CARD 5: Save Settings Button
            ' -----------------------------------------------------------------
            Dim cardAction As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 65
            }

            btnSaveSettings = New Button With {
                .Dock = DockStyle.Fill,
                .Height = 54,
                .Text = "💾 حفظ وتطبيق كافة إعدادات الصالون والضريبة والنشاط",
                .Font = UIStyleHelper.GetFont(14.0F, FontStyle.Bold)
            }
            UIStyleHelper.ApplyButtonFlat(btnSaveSettings, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnSaveSettings.Click, AddressOf OnSaveSettingsClicked

            cardAction.Controls.Add(btnSaveSettings)

            pnlScroll.Controls.Add(cardAction)
            pnlScroll.Controls.Add(pnlSpace4)
            pnlScroll.Controls.Add(cardBackup)
            pnlScroll.Controls.Add(pnlSpace3)
            pnlScroll.Controls.Add(cardTax)
            pnlScroll.Controls.Add(pnlSpace2)
            pnlScroll.Controls.Add(cardPrinter)
            pnlScroll.Controls.Add(pnlSpace1)
            pnlScroll.Controls.Add(cardInfo)

            Me.Controls.Add(pnlScroll)
        End Sub

        Private Sub LoadSettingsData()
            txtSalonName.Text = If(Not String.IsNullOrEmpty(currentSettings.SalonName), currentSettings.SalonName, "ZEX SALON")
            txtPhone.Text = If(currentSettings.Phone, "")
            txtAddress.Text = If(currentSettings.Address, "")
            txtCurrency.Text = If(Not String.IsNullOrEmpty(currentSettings.Currency), currentSettings.Currency, "ج.م")
            txtFooterMsg.Text = If(currentSettings.FooterMessage, "")
            txtTaxRate.Text = currentSettings.DefaultTaxRate.ToString("N1")
            chkEnableTax.Checked = currentSettings.EnableTax
            txtTaxRate.Enabled = currentSettings.EnableTax
            txtTaxRate.BackColor = If(currentSettings.EnableTax, Color.White, Color.FromArgb(240, 244, 248))

            chkEnableSecondaryBackup.Checked = currentSettings.EnableSecondaryBackup
            txtSecondaryBackupPath.Text = If(currentSettings.SecondaryBackupPath, "")

            ' Set Selected Printer
            If Not String.IsNullOrWhiteSpace(currentSettings.SelectedPrinterName) AndAlso cboPrinters.Items.Contains(currentSettings.SelectedPrinterName) Then
                cboPrinters.SelectedItem = currentSettings.SelectedPrinterName
            Else
                cboPrinters.SelectedIndex = 0
            End If

            If Not String.IsNullOrEmpty(currentSettings.LogoPath) AndAlso File.Exists(currentSettings.LogoPath) Then
                Try
                    picLogoPreview.Image = Image.FromFile(currentSettings.LogoPath)
                Catch
                    picLogoPreview.Image = Nothing
                End Try
            Else
                picLogoPreview.Image = Nothing
            End If
        End Sub

        Private Sub OnBrowseSecondaryBackupClicked(sender As Object, e As EventArgs)
            Using fbd As New FolderBrowserDialog()
                fbd.Description = "اختر مجلد الفلاشة الخارجية أو الهارد الإضافي لحفظ النسخ الاحتياطية تلقائياً"
                If fbd.ShowDialog() = DialogResult.OK Then
                    txtSecondaryBackupPath.Text = fbd.SelectedPath
                    chkEnableSecondaryBackup.Checked = True
                End If
            End Using
        End Sub

        Private Sub OnSelectLogoClicked(sender As Object, e As EventArgs)
            Using ofd As New OpenFileDialog()
                ofd.Filter = "ملفات الصور|*.png;*.jpg;*.jpeg;*.bmp"
                ofd.Title = "اختر شعار الصالون"
                If ofd.ShowDialog() = DialogResult.OK Then
                    currentSettings.LogoPath = ofd.FileName
                    picLogoPreview.Image = Image.FromFile(ofd.FileName)
                End If
            End Using
        End Sub

        Private Sub OnRemoveLogoClicked(sender As Object, e As EventArgs)
            currentSettings.LogoPath = ""
            picLogoPreview.Image = Nothing
        End Sub

        Private Sub OnSaveSettingsClicked(sender As Object, e As EventArgs)
            currentSettings.SalonName = If(String.IsNullOrWhiteSpace(txtSalonName.Text), "ZEX SALON", txtSalonName.Text.Trim())
            currentSettings.Phone = txtPhone.Text.Trim()
            currentSettings.Address = txtAddress.Text.Trim()
            currentSettings.Currency = If(String.IsNullOrWhiteSpace(txtCurrency.Text), "ج.م", txtCurrency.Text.Trim())
            currentSettings.FooterMessage = txtFooterMsg.Text.Trim()
            currentSettings.EnableTax = chkEnableTax.Checked
            currentSettings.SelectedPrinterName = cboPrinters.SelectedItem.ToString()
            currentSettings.EnableSecondaryBackup = chkEnableSecondaryBackup.Checked
            currentSettings.SecondaryBackupPath = txtSecondaryBackupPath.Text.Trim()

            Dim taxVal As Double = 0
            Double.TryParse(txtTaxRate.Text, taxVal)
            currentSettings.DefaultTaxRate = taxVal

            settingsRepo.SaveSettings(currentSettings)
            MessageBox.Show("تم حفظ وتطبيق إعدادات الصالون والضريبة والنسخ الاحتياطي بنجاح!", "نجاح الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Sub OnBackupNowClicked(sender As Object, e As EventArgs)
            Try
                Dim backupResult = BackupService.CreateBackupMultiLocation()
                Dim localPath As String = backupResult.Item1
                Dim secondaryPath As String = backupResult.Item2

                Dim uId As Integer = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.Id, 1)
                Dim uName As String = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.DisplayName, "المستخدم")
                ActivityLogRepository.Log(uId, uName, "إنشاء نسخة احتياطية", $"تم إنشاء نسخة احتياطية في: {localPath}" & If(Not String.IsNullOrEmpty(secondaryPath), $" ونسخة إضافية في: {secondaryPath}", ""))

                Dim msg As String = $"تم إنشاء النسخة الاحتياطية بنجاح!{vbCrLf}{vbCrLf}1. المجلد المحلي الأساسي:{vbCrLf}{localPath}"
                If Not String.IsNullOrEmpty(secondaryPath) Then
                    msg &= $"{vbCrLf}{vbCrLf}2. الفلاشة / المسار الخارجي المزدوج:{vbCrLf}{secondaryPath}"
                ElseIf chkEnableSecondaryBackup.Checked AndAlso Not String.IsNullOrWhiteSpace(txtSecondaryBackupPath.Text) Then
                    msg &= $"{vbCrLf}{vbCrLf}⚠️ تنبيه: تعذر النسخ الإضافي للفلاشة ({txtSecondaryBackupPath.Text}) يرجى التأكد من توصيل الفلاشة بالجهاز."
                End If

                MessageBox.Show(msg, "نجاح النسخ الاحتياطي", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show($"تعذر إنشاء النسخة الاحتياطية: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub OnBackupExportClicked(sender As Object, e As EventArgs)
            Using fbd As New FolderBrowserDialog()
                fbd.Description = "اختر المجلد المطلوب حفظ النسخة الاحتياطية فيه (مثال: فلاشة خارجية / القرص D)"
                If fbd.ShowDialog() = DialogResult.OK Then
                    Try
                        Dim backupPath As String = BackupService.CreateBackup(fbd.SelectedPath)
                        Dim uId As Integer = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.Id, 1)
                        Dim uName As String = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.DisplayName, "المستخدم")
                        ActivityLogRepository.Log(uId, uName, "تصدير نسخة احتياطية", $"تم تصدير نسخة احتياطية إلى: {backupPath}")

                        MessageBox.Show($"تم تصدير النسخة الاحتياطية بنجاح!{vbCrLf}المسار المختار: {backupPath}", "نجاح التصدير", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Catch ex As Exception
                        MessageBox.Show($"تعذر تصدير النسخة الاحتياطية: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using
        End Sub

        Private Sub OnRestoreBackupClicked(sender As Object, e As EventArgs)
            Using ofd As New OpenFileDialog()
                ofd.Filter = "ملفات قاعدة البيانات (*.db)|*.db|جميع الملفات (*.*)|*.*"
                ofd.Title = "اختر ملف النسخة الاحتياطية المراد استعادتها"
                If ofd.ShowDialog() = DialogResult.OK Then
                    Dim confirmRes = MessageBox.Show($"⚠️ تحذير هادئ:{vbCrLf}هل أنت متأكد من رغبتك في استعادة قاعدة البيانات من الملف المحدد؟{vbCrLf}{ofd.FileName}{vbCrLf}سيتم استبدال البيانات الحالية بالنسخة الاحتياطية المحددة.", "تأكيد استعادة النسخة الاحتياطية", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

                    If confirmRes = DialogResult.Yes Then
                        Try
                            BackupService.RestoreBackup(ofd.FileName)
                            Dim uId As Integer = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.Id, 1)
                            Dim uName As String = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.DisplayName, "المستخدم")
                            ActivityLogRepository.Log(uId, uName, "استعادة نسخة احتياطية", $"تم استعادة قاعدة البيانات بنجاح من الملف: {ofd.FileName}")

                            MessageBox.Show("تم استعادة قاعدة البيانات والنسخة الاحتياطية بنجاح! يفضل إعادة تشغيل البرنامج لضمان تحديث كل الشاشات.", "نجاح الاستعادة", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Catch ex As Exception
                            MessageBox.Show($"تعذر استعادة النسخة الاحتياطية: {ex.Message}", "خطأ في الاستعادة", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        End Try
                    End If
                End If
            End Using
        End Sub

        Private Sub OnResetTrainingDataClicked(sender As Object, e As EventArgs)
            Dim confirmRes = MessageBox.Show(
                $"⚠️ تنبيه تصفير بيانات التدريب والبدء الفعلي:{vbCrLf}{vbCrLf}" &
                $"هل أنت متأكد من رغبتك في تصفير كافة الفواتير، الورديات، المصروفات، والعملاء التجريبيين؟{vbCrLf}{vbCrLf}" &
                $"✓ سيتم أخذ نسخة احتياطية تلقائياً قبل الحذف في مجلد Backups.{vbCrLf}" &
                $"✓ سيتم الحفاظ على: بيانات الصالون، ترخيص البرنامج، وقائمة الخدمات والأسعار.{vbCrLf}" &
                $"✓ ستبدأ الفاتورة القادمة من رقم (1).",
                "تأكيد تصفير بيانات التدريب والبدء الفعلي",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning)

            If confirmRes = DialogResult.Yes Then
                Try
                    BackupService.ResetTrainingData()
                    Dim uId As Integer = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.Id, 1)
                    Dim uName As String = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.DisplayName, "المستخدم")
                    ActivityLogRepository.Log(uId, uName, "تصفير بيانات التدريب", "تم تصفير جميع العمليات التجريبية والبدء الفعلي لقاعدة البيانات")

                    MessageBox.Show(
                        $"تم تصفير كافة المعاملات التجريبية بنجاح!{vbCrLf}{vbCrLf}" &
                        $"✓ تم مسح الفواتير لتبدأ الفاتورة القادمة من رقم 1.{vbCrLf}" &
                        $"✓ تم تصفير الورديات والمصروفات والعملاء التجريبيين.{vbCrLf}" &
                        $"✓ تم الاحتفاظ بالخدمات وقوائم الأسعار والترخيص.{vbCrLf}{vbCrLf}" &
                        $"يرجى إعادة تشغيل البرنامج للبدء الفعلي مع العميل.",
                        "تم التصفير بنجاح",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show($"تعذر تصفير البيانات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Sub
    End Class

End Namespace
