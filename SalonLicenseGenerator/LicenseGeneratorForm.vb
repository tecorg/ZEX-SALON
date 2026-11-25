Imports System
Imports System.Drawing
Imports System.Windows.Forms

Namespace SalonLicenseGenerator

    Public Class LicenseGeneratorForm
        Inherits Form

        Private txtCustomerName As TextBox
        Private txtMachineId As TextBox
        Private cboDuration As ComboBox
        Private dtpCustomExpiry As DateTimePicker
        Private chkLifetime As CheckBox
        Private btnGenerate As Button
        Private txtLicenseKeyResult As TextBox
        Private btnCopy As Button

        Public Sub New()
            Me.RightToLeft = RightToLeft.Yes
            Me.RightToLeftLayout = True
            Me.Font = New Font("Tajawal", 10.0F, FontStyle.Regular)
            Me.Size = New Size(580, 520)
            Me.Text = "ZEX Salon License Manager - مولّد ترخيص صالون POS"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False

            InitializeLayout()
        End Sub

        Private Sub InitializeLayout()
            Dim lblHeader As New Label With {
                .Dock = DockStyle.Top,
                .Height = 55,
                .Text = "ZEX Salon License Manager#إصدار تراخيص مشفرة بـ RSA-256",
                .Font = New Font("Tajawal", 14.0F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(41, 128, 185),
                .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.FromArgb(240, 244, 248)
            }

            Dim lblC As New Label With {.Text = "اسم العميل / الصالون:", .Location = New Point(20, 75), .Size = New Size(520, 25), .Font = New Font("Tajawal", 10.0F, FontStyle.Bold)}
            txtCustomerName = New TextBox With {.Location = New Point(20, 100), .Size = New Size(520, 30), .Text = "صالون الأناقة والجمال"}

            Dim lblM As New Label With {.Text = "معرف جهاز العميل (Machine ID):", .Location = New Point(20, 140), .Size = New Size(520, 25), .Font = New Font("Tajawal", 10.0F, FontStyle.Bold)}
            txtMachineId = New TextBox With {.Location = New Point(20, 165), .Size = New Size(520, 30), .Text = "SALON-A7F9-D20C"}

            Dim lblD As New Label With {.Text = "مدة الترخيص:", .Location = New Point(320, 210), .Size = New Size(220, 25), .Font = New Font("Tajawal", 10.0F, FontStyle.Bold)}
            cboDuration = New ComboBox With {.Location = New Point(320, 235), .Size = New Size(220, 30), .DropDownStyle = ComboBoxStyle.DropDownList}
            cboDuration.Items.AddRange(New String() {"سنة واحدة (1 Year)", "6 أشهر (6 Months)", "شهر واحد (1 Month)", "مدى الحياة (Lifetime)", "تاريخ مخصص"})
            cboDuration.SelectedIndex = 0
            AddHandler cboDuration.SelectedIndexChanged, AddressOf OnDurationChanged

            dtpCustomExpiry = New DateTimePicker With {.Location = New Point(20, 235), .Size = New Size(280, 30), .Format = DateTimePickerFormat.Short, .Value = DateTime.Now.AddYears(1)}

            btnGenerate = New Button With {
                .Text = "توليد كود الترخيص الرقمي",
                .Location = New Point(180, 285),
                .Size = New Size(220, 45),
                .BackColor = Color.FromArgb(39, 174, 96),
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Tajawal", 10.5F, FontStyle.Bold),
                .Cursor = Cursors.Hand
            }
            AddHandler btnGenerate.Click, AddressOf OnGenerateClicked

            txtLicenseKeyResult = New TextBox With {
                .Location = New Point(20, 340),
                .Size = New Size(520, 90),
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical,
                .Font = New Font("Consolas", 9.5F, FontStyle.Regular),
                .ReadOnly = True
            }

            btnCopy = New Button With {
                .Text = "نسخ كود الترخيص (Copy License Key)",
                .Location = New Point(150, 440),
                .Size = New Size(280, 38),
                .BackColor = Color.FromArgb(41, 128, 185),
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Tajawal", 9.5F, FontStyle.Bold)
            }
            AddHandler btnCopy.Click, AddressOf OnCopyClicked

            Me.Controls.Add(btnCopy)
            Me.Controls.Add(txtLicenseKeyResult)
            Me.Controls.Add(btnGenerate)
            Me.Controls.Add(dtpCustomExpiry)
            Me.Controls.Add(cboDuration)
            Me.Controls.Add(lblD)
            Me.Controls.Add(txtMachineId)
            Me.Controls.Add(lblM)
            Me.Controls.Add(txtCustomerName)
            Me.Controls.Add(lblC)
            Me.Controls.Add(lblHeader)
        End Sub

        Private Sub OnDurationChanged(sender As Object, e As EventArgs)
            Select Case cboDuration.SelectedIndex
                Case 0 ' 1 Year
                    dtpCustomExpiry.Value = DateTime.Now.AddYears(1)
                    dtpCustomExpiry.Enabled = False
                Case 1 ' 6 Months
                    dtpCustomExpiry.Value = DateTime.Now.AddMonths(6)
                    dtpCustomExpiry.Enabled = False
                Case 2 ' 1 Month
                    dtpCustomExpiry.Value = DateTime.Now.AddMonths(1)
                    dtpCustomExpiry.Enabled = False
                Case 3 ' Lifetime
                    dtpCustomExpiry.Value = DateTime.Now.AddYears(50)
                    dtpCustomExpiry.Enabled = False
                Case 4 ' Custom
                    dtpCustomExpiry.Enabled = True
            End Select
        End Sub

        Private Sub OnGenerateClicked(sender As Object, e As EventArgs)
            Dim custName As String = txtCustomerName.Text.Trim()
            Dim mId As String = txtMachineId.Text.Trim()

            If String.IsNullOrEmpty(custName) OrElse String.IsNullOrEmpty(mId) Then
                MessageBox.Show("يرجى إدخال اسم العميل و Machine ID.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim isLifetime As Boolean = (cboDuration.SelectedIndex = 3)
            Dim expDate As DateTime = dtpCustomExpiry.Value

            Try
                Dim generatedKey As String = RsaKeyManager.GenerateLicenseKey(custName, mId, expDate, isLifetime)
                txtLicenseKeyResult.Text = generatedKey
                MessageBox.Show("تم توليد كود الترخيص والتوقيع بنجاح!", "تم التوليد", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show($"خطأ أثناء التوليد: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub OnCopyClicked(sender As Object, e As EventArgs)
            If Not String.IsNullOrEmpty(txtLicenseKeyResult.Text) Then
                Clipboard.SetText(txtLicenseKeyResult.Text)
                MessageBox.Show("تم نسخ كود الترخيص إلى الحافظة (Clipboard)!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Sub
    End Class

End Namespace
