Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Helpers
Imports SalonPOS.Licensing

Namespace SalonPOS.Forms

    Public Class FrmLicenseActivation
        Inherits Form

        Private txtMachineId As TextBox
        Private btnCopyMachineId As Button
        Private txtLicenseKey As TextBox
        Private btnActivate As Button
        Private btnClose As Button
        Private lblStatus As Label

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Size = New Size(620, 520)
            Me.Text = "تفعيل ترخيص برنامج ZEX SALON"
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            InitializeLayout()
            CheckCurrentStatus()
        End Sub

        Private Sub InitializeLayout()
            ' Top Banner
            Dim pnlHeader As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 70,
                .BackColor = UIStyleHelper.ZexNavy,
                .Padding = New Padding(15)
            }

            Dim lblTitle As New Label With {
                .Dock = DockStyle.Fill,
                .Text = "تفعيل ترخيص برنامج ZEX SALON",
                .Font = UIStyleHelper.GetFont(16.0F, FontStyle.Bold),
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            pnlHeader.Controls.Add(lblTitle)

            ' Container Panel
            Dim pnlBody As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(24),
                .RightToLeft = RightToLeft.Yes
            }

            ' Machine ID Section
            Dim lblM As New Label With {
                .Dock = DockStyle.Top,
                .Height = 28,
                .Text = "معرف الجهاز الخاص بك (Machine ID):",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight
            }

            Dim pnlMidRow As New Panel With {.Dock = DockStyle.Top, .Height = 44}
            txtMachineId = New TextBox With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.GetFont(13.0F, FontStyle.Bold),
                .ReadOnly = True,
                .Text = MachineFingerprint.GetMachineId(),
                .BackColor = Color.FromArgb(240, 244, 248),
                .TextAlign = HorizontalAlignment.Center,
                .BorderStyle = BorderStyle.FixedSingle
            }

            btnCopyMachineId = New Button With {
                .Dock = DockStyle.Left,
                .Width = 150,
                .Text = "📋 نسخ المعرف",
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnCopyMachineId, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnCopyMachineId.Click, Sub()
                                                   Clipboard.SetText(txtMachineId.Text)
                                                   MessageBox.Show("تم نسخ معرف الجهاز إلى الحافظة بنجاح!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                               End Sub

            pnlMidRow.Controls.Add(txtMachineId)
            pnlMidRow.Controls.Add(btnCopyMachineId)

            ' Status Badge
            lblStatus = New Label With {
                .Dock = DockStyle.Top,
                .Height = 35,
                .Font = UIStyleHelper.FontBold,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Margin = New Padding(0, 10, 0, 10)
            }

            ' License Key Input Section
            Dim lblK As New Label With {
                .Dock = DockStyle.Top,
                .Height = 28,
                .Text = "أدخل كود التفعيل (License Key):",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight
            }

            txtLicenseKey = New TextBox With {
                .Dock = DockStyle.Top,
                .Height = 120,
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical,
                .Font = New Font("Consolas", 10.5F, FontStyle.Regular),
                .RightToLeft = RightToLeft.No,
                .BorderStyle = BorderStyle.FixedSingle
            }

            ' Action Buttons
            Dim pnlActions As New Panel With {.Dock = DockStyle.Bottom, .Height = 55}

            btnActivate = New Button With {
                .Dock = DockStyle.Right,
                .Width = 220,
                .Height = 50,
                .Text = "🔑 تفعيل البرنامج الآن",
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnActivate, UIStyleHelper.ActionGreen, Color.White)
            AddHandler btnActivate.Click, AddressOf OnActivateClicked

            btnClose = New Button With {
                .Dock = DockStyle.Left,
                .Width = 140,
                .Height = 50,
                .Text = "إلغاء وإغلاق",
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnClose, Color.Gray, Color.White)
            AddHandler btnClose.Click, Sub() Me.Close()

            pnlActions.Controls.Add(btnActivate)
            pnlActions.Controls.Add(btnClose)

            pnlBody.Controls.Add(pnlActions)
            pnlBody.Controls.Add(txtLicenseKey)
            pnlBody.Controls.Add(lblK)
            pnlBody.Controls.Add(lblStatus)
            pnlBody.Controls.Add(pnlMidRow)
            pnlBody.Controls.Add(lblM)

            Me.Controls.Add(pnlBody)
            Me.Controls.Add(pnlHeader)
        End Sub

        Private Sub CheckCurrentStatus()
            Dim res = LicenseValidator.ValidateCurrentLicense()
            If res.Status = LicenseStatus.ACTIVE Then
                lblStatus.Text = $"🟢 الترخيص نشط ومفعل باسم: {res.License.CustomerName}"
                lblStatus.ForeColor = Color.DarkGreen
            Else
                lblStatus.Text = $"🔴 حالة الترخيص: {res.Message}"
                lblStatus.ForeColor = Color.Red
            End If
        End Sub

        Private Sub OnActivateClicked(sender As Object, e As EventArgs)
            Dim keyInput As String = txtLicenseKey.Text.Trim()
            If String.IsNullOrWhiteSpace(keyInput) Then
                MessageBox.Show("يرجى إدخال كود التفعيل أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim currentMid As String = MachineFingerprint.GetMachineId()
            Dim valResult = LicenseValidator.ValidateKey(keyInput, currentMid)

            If valResult.Status = LicenseStatus.ACTIVE Then
                LicenseValidator.SaveLicense(keyInput, valResult.License.CustomerName, valResult.License.MachineId, valResult.License.ExpiryDate)
                MessageBox.Show($"تم تفعيل ترخيص البرنامج بنجاح!{vbCrLf}مرخص باسم: {valResult.License.CustomerName}", "تم التفعيل بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Else
                MessageBox.Show($"تعذر تفعيل الترخيص:{vbCrLf}{valResult.Message}", "فشل التفعيل", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End Sub
    End Class

End Namespace
