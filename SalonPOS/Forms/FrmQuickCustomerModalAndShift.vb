Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Models
Imports SalonPOS.Services

Namespace SalonPOS.Forms

    Public Class FrmQuickCustomerModal
        Inherits Form

        Public Property NewCustomerId As Integer
        Public Property NewCustomerName As String

        Private txtName As TextBox
        Private txtPhone As TextBox
        Private btnSave As Button
        Private btnCancel As Button
        Private custRepo As New CustomerRepository()

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Size = New Size(380, 260)
            Me.Text = "إضافة عميل جديد سريع"
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False

            Dim lblN As New Label With {.Text = "اسم العميل:", .Location = New Point(20, 20), .Size = New Size(320, 22), .Font = UIStyleHelper.FontBold}
            txtName = New TextBox With {.Location = New Point(20, 45), .Size = New Size(320, 30), .Font = UIStyleHelper.FontRegular}

            Dim lblP As New Label With {.Text = "رقم الهاتف (اختياري):", .Location = New Point(20, 85), .Size = New Size(320, 22), .Font = UIStyleHelper.FontBold}
            txtPhone = New TextBox With {.Location = New Point(20, 110), .Size = New Size(320, 30), .Font = UIStyleHelper.FontRegular}

            btnSave = New Button With {.Text = "حفظ وإعادة التحديد", .Location = New Point(160, 160), .Size = New Size(180, 40)}
            UIStyleHelper.ApplyButtonFlat(btnSave, UIStyleHelper.ActionGreen, Color.White)
            AddHandler btnSave.Click, AddressOf OnSaveClicked

            btnCancel = New Button With {.Text = "إلغاء", .Location = New Point(20, 160), .Size = New Size(120, 40)}
            UIStyleHelper.ApplyButtonFlat(btnCancel, Color.Gray, Color.White)
            AddHandler btnCancel.Click, Sub() Me.Close()

            Me.Controls.Add(btnCancel)
            Me.Controls.Add(btnSave)
            Me.Controls.Add(txtPhone)
            Me.Controls.Add(lblP)
            Me.Controls.Add(txtName)
            Me.Controls.Add(lblN)
        End Sub

        Private Sub OnSaveClicked(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtName.Text) Then
                MessageBox.Show("يرجى إدخال اسم العميل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            NewCustomerName = txtName.Text.Trim()
            NewCustomerId = custRepo.QuickAddCustomer(NewCustomerName, txtPhone.Text.Trim())

            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
    End Class

    ' =========================================================================
    ' 1. MODAL FOR STARTING A NEW SHIFT (بدء وردية جديدة)
    ' =========================================================================
    Public Class FrmStartShiftModal
        Inherits Form

        Private shiftRepo As New ShiftRepository()
        Private txtOpeningCash As TextBox
        Private btnStartShift As Button
        Private btnCancel As Button

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Size = New Size(420, 330)
            Me.Text = "🔑 بدء وردية جديدة"
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            InitializeLayout()
        End Sub

        Private Sub InitializeLayout()
            Dim userName As String = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.DisplayName, "المستخدم الحالي")

            Dim lblUserHeader As New Label With {
                .Dock = DockStyle.Top,
                .Height = 45,
                .Text = $"بدء وردية جديدة باسم: {userName}",
                .Font = UIStyleHelper.FontSubtitle,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.FromArgb(240, 244, 248)
            }

            Dim lblOpeningPrompt As New Label With {
                .Text = "المبلغ الافتتاحي بالخزينة (العهدة الابتدائية):",
                .Location = New Point(20, 60),
                .Size = New Size(360, 25),
                .Font = UIStyleHelper.FontBold
            }

            txtOpeningCash = New TextBox With {
                .Location = New Point(20, 90),
                .Size = New Size(360, 38),
                .Font = UIStyleHelper.GetFont(14.0F, FontStyle.Bold),
                .RightToLeft = RightToLeft.Yes,
                .Text = "0.00"
            }

            ' Preset Cash Buttons
            Dim flowPreset As New FlowLayoutPanel With {
                .Location = New Point(20, 135),
                .Size = New Size(360, 42),
                .RightToLeft = RightToLeft.Yes
            }

            Dim presets = New Double() {0, 100, 200, 500}
            For Each p As Double In presets
                Dim btnP As New Button With {
                    .Text = $"{p:N0} ج.م",
                    .Width = 80,
                    .Height = 34,
                    .Margin = New Padding(3),
                    .Tag = p
                }
                UIStyleHelper.ApplyButtonFlat(btnP, Color.FromArgb(235, 240, 245), UIStyleHelper.ZexNavy)
                AddHandler btnP.Click, Sub(senderObj, eArgs)
                                           txtOpeningCash.Text = $"{CType(CType(senderObj, Button).Tag, Double):N2}"
                                       End Sub
                flowPreset.Controls.Add(btnP)
            Next

            btnStartShift = New Button With {
                .Text = "🔑 فتح الوردية الآن",
                .Location = New Point(180, 205),
                .Size = New Size(200, 48),
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnStartShift, Color.FromArgb(39, 174, 96), Color.White)
            AddHandler btnStartShift.Click, AddressOf OnStartShiftClicked

            btnCancel = New Button With {
                .Text = "إلغاء",
                .Location = New Point(20, 205),
                .Size = New Size(140, 48),
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnCancel, Color.Gray, Color.White)
            AddHandler btnCancel.Click, Sub() Me.Close()

            Me.Controls.Add(btnCancel)
            Me.Controls.Add(btnStartShift)
            Me.Controls.Add(flowPreset)
            Me.Controls.Add(txtOpeningCash)
            Me.Controls.Add(lblOpeningPrompt)
            Me.Controls.Add(lblUserHeader)
        End Sub

        Private Sub OnStartShiftClicked(sender As Object, e As EventArgs)
            Dim openVal As Double = 0
            Double.TryParse(txtOpeningCash.Text, openVal)

            Dim userId As Integer = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.Id, 1)
            Dim newShift = shiftRepo.OpenShift(userId, openVal)
            AuthService.CurrentShift = newShift

            MessageBox.Show($"تم فتح الوردية رقم (#{newShift.Id}) بنجاح بالمبلغ الافتتاحي: {openVal:N2} ج.م", "فتح الوردية", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
    End Class

    ' =========================================================================
    ' 2. MODAL FOR CLOSING SHIFT (إغلاق وتصفية الوردية وتفاصيل الأموال)
    ' =========================================================================
    Public Class FrmShiftCloseModal
        Inherits Form

        Private shiftRepo As New ShiftRepository()
        Private activeShift As Shift

        Private lblShiftInfo As Label
        Private lblExpectedCash As Label
        Private txtActualCash As TextBox
        Private lblVariance As Label
        Private chkPrintShiftReceipt As CheckBox
        Private btnCloseShift As Button
        Private btnCancel As Button

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Size = New Size(520, 600)
            Me.Text = "🔒 إغلاق وتصفية الوردية وجرد الخزينة"
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            Dim uId As Integer = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.Id, 1)
            activeShift = shiftRepo.GetActiveShift(uId)

            InitializeLayout()
            CalculateVariance()
        End Sub

        Private Sub InitializeLayout()
            lblShiftInfo = New Label With {
                .Dock = DockStyle.Top,
                .Height = 45,
                .Font = UIStyleHelper.FontSubtitle,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Text = "تقرير وتفاصيل أموال الوردية الحالية",
                .BackColor = Color.FromArgb(240, 244, 248)
            }

            Dim pnlInfo As New Panel With {
                .Location = New Point(20, 55),
                .Size = New Size(464, 300),
                .BackColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle,
                .Padding = New Padding(12),
                .RightToLeft = RightToLeft.Yes
            }

            If activeShift IsNot Nothing Then
                Dim openingCash As Double = activeShift.OpeningCash
                Dim cashSales As Double = activeShift.CashSales
                Dim cardSales As Double = activeShift.CardSales
                Dim transferSales As Double = activeShift.TransferSales
                Dim expenses As Double = activeShift.TotalExpenses
                Dim expectedCash As Double = openingCash + cashSales - expenses
                Dim uName As String = If(Not String.IsNullOrEmpty(activeShift.UserName), activeShift.UserName, "الكاشير")

                pnlInfo.Controls.Add(New Label With {.Text = $"رقم الوردية: #{activeShift.Id}  •  المسؤول: {uName}", .Location = New Point(10, 10), .Size = New Size(440, 25), .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy})
                pnlInfo.Controls.Add(New Label With {.Text = $"وقت البدء: {activeShift.StartedAt:yyyy-MM-dd hh:mm tt}", .Location = New Point(10, 38), .Size = New Size(440, 22), .Font = UIStyleHelper.FontRegular, .ForeColor = Color.DimGray})
                pnlInfo.Controls.Add(New Label With {.Text = $"💵 العهدة الافتتاحية بالدرج: {openingCash:N2} ج.م", .Location = New Point(10, 68), .Size = New Size(440, 24), .Font = UIStyleHelper.FontBold})
                pnlInfo.Controls.Add(New Label With {.Text = $"🟢 إجمالي المبيعات النقدية (الكاش): +{cashSales:N2} ج.م", .Location = New Point(10, 96), .Size = New Size(440, 24), .Font = UIStyleHelper.FontBold, .ForeColor = Color.FromArgb(39, 174, 96)})
                pnlInfo.Controls.Add(New Label With {.Text = $"💳 مبيعات البطاقات (الفيزا): {cardSales:N2} ج.م (غير مضافة للدرج)", .Location = New Point(10, 124), .Size = New Size(440, 22), .Font = UIStyleHelper.FontRegular, .ForeColor = Color.DimGray})
                pnlInfo.Controls.Add(New Label With {.Text = $"📲 مبيعات التحويل الشبكي: {transferSales:N2} ج.م (غير مضافة للدرج)", .Location = New Point(10, 148), .Size = New Size(440, 22), .Font = UIStyleHelper.FontRegular, .ForeColor = Color.DimGray})
                pnlInfo.Controls.Add(New Label With {.Text = $"🔴 إجمالي المصروفات المدفوعة: -{expenses:N2} ج.م", .Location = New Point(10, 174), .Size = New Size(440, 24), .Font = UIStyleHelper.FontBold, .ForeColor = Color.Red})
                pnlInfo.Controls.Add(New Label With {.Text = $"------------------------------------------------------------", .Location = New Point(10, 202), .Size = New Size(440, 15)})

                lblExpectedCash = New Label With {
                    .Text = $"🧮 المبلغ المتوقع بالخزينة: {expectedCash:N2} ج.م",
                    .Location = New Point(10, 224),
                    .Size = New Size(440, 35),
                    .Font = UIStyleHelper.GetFont(13.0F, FontStyle.Bold),
                    .ForeColor = UIStyleHelper.ZexNavy
                }
                pnlInfo.Controls.Add(lblExpectedCash)
            Else
                pnlInfo.Controls.Add(New Label With {.Text = "لا توجد وردية مفتوحة حالياً للنظام.", .Location = New Point(10, 80), .Size = New Size(440, 30), .Font = UIStyleHelper.FontBold, .ForeColor = Color.Red})
            End If

            ' Actual Cash Entry Section
            Dim lblA As New Label With {
                .Text = "المبلغ الفعلي بالخزينة:",
                .Location = New Point(260, 370),
                .Size = New Size(220, 25),
                .Font = UIStyleHelper.FontBold,
                .RightToLeft = RightToLeft.Yes
            }

            txtActualCash = New TextBox With {
                .Location = New Point(260, 400),
                .Size = New Size(220, 35),
                .Font = UIStyleHelper.FontPrice,
                .Text = If(activeShift IsNot Nothing, $"{(activeShift.OpeningCash + activeShift.CashSales - activeShift.TotalExpenses):N0}", "0")
            }
            AddHandler txtActualCash.TextChanged, Sub() CalculateVariance()

            lblVariance = New Label With {
                .Location = New Point(20, 370),
                .Size = New Size(220, 65),
                .Font = UIStyleHelper.FontBold,
                .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.FromArgb(245, 247, 250),
                .Text = "الفرق: 0 ج.م"
            }

            chkPrintShiftReceipt = New CheckBox With {
                .Text = "طباعة تقرير جرد الوردية إلكترونياً على الطابعة الحرارية",
                .Location = New Point(20, 450),
                .Size = New Size(464, 30),
                .Checked = True,
                .Font = UIStyleHelper.FontBold,
                .RightToLeft = RightToLeft.Yes
            }

            btnCloseShift = New Button With {
                .Text = "🔒 تأكيد إغلاق الوردية والتصفية",
                .Location = New Point(190, 490),
                .Size = New Size(294, 50),
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnCloseShift, Color.FromArgb(192, 57, 43), Color.White)
            AddHandler btnCloseShift.Click, AddressOf OnCloseShiftClicked

            btnCancel = New Button With {
                .Text = "إلغاء",
                .Location = New Point(20, 490),
                .Size = New Size(150, 50),
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnCancel, Color.Gray, Color.White)
            AddHandler btnCancel.Click, Sub() Me.Close()

            Me.Controls.Add(btnCancel)
            Me.Controls.Add(btnCloseShift)
            Me.Controls.Add(chkPrintShiftReceipt)
            Me.Controls.Add(lblVariance)
            Me.Controls.Add(txtActualCash)
            Me.Controls.Add(lblA)
            Me.Controls.Add(pnlInfo)
            Me.Controls.Add(lblShiftInfo)
        End Sub

        Private Sub CalculateVariance()
            If activeShift Is Nothing Then Return
            Dim actual As Double = 0
            Double.TryParse(txtActualCash.Text, actual)
            Dim expected As Double = activeShift.OpeningCash + activeShift.CashSales - activeShift.TotalExpenses
            Dim variance As Double = actual - expected

            If variance = 0 Then
                lblVariance.ForeColor = Color.FromArgb(39, 174, 96)
                lblVariance.Text = "الفرق: 0.00 ج.م" & vbCrLf & "🟢 مطابقة تامة 100%"
            ElseIf variance < 0 Then
                lblVariance.ForeColor = Color.Red
                lblVariance.Text = $"الفرق: {variance:N2} ج.م" & vbCrLf & "⚠️ عجز بالخزينة"
            Else
                lblVariance.ForeColor = UIStyleHelper.ZexNavy
                lblVariance.Text = $"الفرق: +{variance:N2} ج.م" & vbCrLf & "🔵 زيادة بالخزينة"
            End If
        End Sub

        Private Sub OnCloseShiftClicked(sender As Object, e As EventArgs)
            If activeShift Is Nothing Then
                Me.Close()
                Return
            End If

            Dim actual As Double = 0
            Double.TryParse(txtActualCash.Text, actual)

            shiftRepo.CloseShift(activeShift.Id, actual)
            activeShift.ActualCash = actual
            activeShift.Variance = actual - (activeShift.OpeningCash + activeShift.CashSales - activeShift.TotalExpenses)
            activeShift.Status = ShiftStatus.CLOSED
            activeShift.EndedAt = DateTime.Now

            AuthService.CurrentShift = Nothing

            ' Auto Print Shift Summary
            If chkPrintShiftReceipt.Checked Then
                Try
                    Printing.ReceiptPrinter.PrintShiftReport(activeShift)
                Catch exPrint As Exception
                    ' Non-fatal
                End Try
            End If

            MessageBox.Show($"تم إغلاق الوردية رقم (#{activeShift.Id}) وتصفية الحسابات بنجاح!", "إغلاق الوردية", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
    End Class

End Namespace
