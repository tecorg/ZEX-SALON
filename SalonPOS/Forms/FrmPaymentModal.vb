Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Models
Imports SalonPOS.Printing
Imports SalonPOS.Services

Namespace SalonPOS.Forms

    Public Class FrmPaymentModal
        Inherits Form

        Private currentSession As ChairSession
        Private saleRepo As New SaleRepository()
        Private settingsRepo As New SettingsRepository()
        Private currentSettings As SalonSettings

        Private baseSubTotal As Double = 0.0
        Private calculatedDiscount As Double = 0.0
        Private calculatedGrandTotal As Double = 0.0
        Private calculatedTaxAmount As Double = 0.0

        Private lblTotalAmount As Label
        Private rdoCash As RadioButton
        Private rdoCard As RadioButton
        Private rdoTransfer As RadioButton
        Private rdoSplit As RadioButton

        Private txtDiscount As TextBox
        Private cboDiscountType As ComboBox
        Private lblDiscountNotice As Label

        Private lblCashReceivedPrompt As Label
        Private txtCashReceived As TextBox
        Private lblChangeAmount As Label
        Private chkPrintReceipt As CheckBox
        Private btnConfirmPay As Button
        Private btnCancel As Button

        Public Sub New(session As ChairSession)
            Me.currentSession = session
            currentSettings = settingsRepo.GetSettings()
            baseSubTotal = session.TotalAmount

            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Size = New Size(540, 620)
            Me.Text = "إتمام وتأكيد الدفع والخصم"
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            InitializeLayout()
            RecalculateTotals()
        End Sub

        Private Sub InitializeLayout()
            Me.Controls.Clear()

            ' 1. Top Total Banner
            Dim pnlTotal As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 85,
                .BackColor = Color.FromArgb(240, 248, 240)
            }

            Dim curr As String = If(Not String.IsNullOrEmpty(currentSettings.Currency), currentSettings.Currency, "ج.م")

            Dim lblPrompt As New Label With {
                .Dock = DockStyle.Top,
                .Height = 25,
                .Text = "المبلغ الإجمالي المطلوب سداده:",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.DimGray,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            lblTotalAmount = New Label With {
                .Dock = DockStyle.Fill,
                .Text = $"{baseSubTotal:N2} {curr}",
                .Font = UIStyleHelper.GetFont(20.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ActionGreen,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            pnlTotal.Controls.Add(lblTotalAmount)
            pnlTotal.Controls.Add(lblPrompt)

            ' 2. Discount Section GroupBox
            Dim grpDiscount As New GroupBox With {
                .Text = "الخصومات المطبقة على الفاتورة",
                .Location = New Point(20, 95),
                .Size = New Size(480, 85),
                .Font = UIStyleHelper.FontBold,
                .RightToLeft = RightToLeft.Yes
            }

            grpDiscount.Controls.Add(New Label With {.Text = "قيمة الخصم:", .Location = New Point(370, 30), .Size = New Size(90, 25)})

            txtDiscount = New TextBox With {
                .Location = New Point(230, 30),
                .Size = New Size(130, 30),
                .Font = UIStyleHelper.FontPrice,
                .Text = "0"
            }

            cboDiscountType = New ComboBox With {
                .Location = New Point(70, 30),
                .Size = New Size(150, 30),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = UIStyleHelper.FontBold
            }
            cboDiscountType.Items.Add($"مبلغ ثابت ({curr})")
            cboDiscountType.Items.Add("نسبة مئوية (%)")
            cboDiscountType.SelectedIndex = 0

            lblDiscountNotice = New Label With {
                .Location = New Point(10, 60),
                .Size = New Size(450, 20),
                .Font = UIStyleHelper.GetFont(8.5F, FontStyle.Regular),
                .ForeColor = Color.Red,
                .TextAlign = ContentAlignment.MiddleLeft
            }

            ' Permission Lock Check for Discounts
            If Not AuthService.HasPermission("Discounts") Then
                txtDiscount.Enabled = False
                cboDiscountType.Enabled = False
                lblDiscountNotice.Text = "🔒 الخصم غير مفعل: يتطلب صلاحية تطبيق الخصومات (Discounts)."
            Else
                lblDiscountNotice.Text = "✓ يمكنك إدخال قيمة أو نسبة الخصم المباشر على الفاتورة."
                lblDiscountNotice.ForeColor = Color.DarkGreen
            End If

            AddHandler txtDiscount.TextChanged, Sub() RecalculateTotals()
            AddHandler cboDiscountType.SelectedIndexChanged, Sub() RecalculateTotals()

            grpDiscount.Controls.Add(lblDiscountNotice)
            grpDiscount.Controls.Add(cboDiscountType)
            grpDiscount.Controls.Add(txtDiscount)

            ' 3. Payment Method Group Box
            Dim grpMethods As New GroupBox With {
                .Text = "طريقة الدفع والتحصيل",
                .Location = New Point(20, 190),
                .Size = New Size(480, 80),
                .Font = UIStyleHelper.FontBold,
                .RightToLeft = RightToLeft.Yes
            }

            rdoCash = New RadioButton With {.Text = "نقدي", .Location = New Point(370, 30), .Size = New Size(90, 30), .Checked = True}
            rdoCard = New RadioButton With {.Text = "بطاقة", .Location = New Point(260, 30), .Size = New Size(90, 30)}
            rdoTransfer = New RadioButton With {.Text = "تحويل", .Location = New Point(150, 30), .Size = New Size(90, 30)}
            rdoSplit = New RadioButton With {.Text = "مختلط", .Location = New Point(30, 30), .Size = New Size(90, 30)}

            AddHandler rdoCash.CheckedChanged, AddressOf OnPaymentMethodChanged
            AddHandler rdoCard.CheckedChanged, AddressOf OnPaymentMethodChanged
            AddHandler rdoTransfer.CheckedChanged, AddressOf OnPaymentMethodChanged
            AddHandler rdoSplit.CheckedChanged, AddressOf OnPaymentMethodChanged

            grpMethods.Controls.Add(rdoCash)
            grpMethods.Controls.Add(rdoCard)
            grpMethods.Controls.Add(rdoTransfer)
            grpMethods.Controls.Add(rdoSplit)

            ' 4. Cash Calculation Section
            lblCashReceivedPrompt = New Label With {
                .Location = New Point(300, 285),
                .Size = New Size(200, 25),
                .Text = "المبلغ المستلم من العميل:",
                .Font = UIStyleHelper.FontBold,
                .RightToLeft = RightToLeft.Yes
            }

            txtCashReceived = New TextBox With {
                .Location = New Point(300, 315),
                .Size = New Size(200, 35),
                .Font = UIStyleHelper.FontPrice,
                .Text = $"{baseSubTotal:N0}"
            }
            AddHandler txtCashReceived.TextChanged, Sub() CalculateChange()

            lblChangeAmount = New Label With {
                .Location = New Point(20, 285),
                .Size = New Size(260, 65),
                .Font = UIStyleHelper.GetFont(15.0F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(41, 128, 185),
                .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.FromArgb(240, 244, 248),
                .Text = $"الباقي: 0.00 {curr}"
            }

            ' Options Checkbox
            chkPrintReceipt = New CheckBox With {
                .Text = "طباعة الفاتورة تلقائياً بعد السداد",
                .Location = New Point(20, 365),
                .Size = New Size(480, 30),
                .Checked = True,
                .Font = UIStyleHelper.FontBold,
                .RightToLeft = RightToLeft.Yes
            }

            ' Action Buttons
            btnConfirmPay = New Button With {
                .Text = "تأكيد الدفع وإغلاق (F9)",
                .Location = New Point(210, 500),
                .Size = New Size(290, 52),
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnConfirmPay, UIStyleHelper.ActionGreen, Color.White)
            AddHandler btnConfirmPay.Click, AddressOf OnConfirmPayClicked

            btnCancel = New Button With {
                .Text = "إلغاء",
                .Location = New Point(20, 500),
                .Size = New Size(170, 52),
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnCancel, Color.Gray, Color.White)
            AddHandler btnCancel.Click, Sub() Me.Close()

            Me.Controls.Add(btnCancel)
            Me.Controls.Add(btnConfirmPay)
            Me.Controls.Add(chkPrintReceipt)
            Me.Controls.Add(lblChangeAmount)
            Me.Controls.Add(txtCashReceived)
            Me.Controls.Add(lblCashReceivedPrompt)
            Me.Controls.Add(grpMethods)
            Me.Controls.Add(grpDiscount)
            Me.Controls.Add(pnlTotal)
        End Sub

        Private Sub RecalculateTotals()
            ' Calculate Discount
            Dim discInput As Double = 0.0
            Double.TryParse(txtDiscount.Text, discInput)

            If discInput > 0 AndAlso AuthService.HasPermission("Discounts") Then
                If cboDiscountType.SelectedIndex = 1 Then
                    ' Percentage %
                    calculatedDiscount = baseSubTotal * (discInput / 100.0)
                Else
                    ' Fixed Amount
                    calculatedDiscount = discInput
                End If
            Else
                calculatedDiscount = 0.0
            End If

            calculatedDiscount = Math.Min(baseSubTotal, calculatedDiscount)
            Dim afterDisc As Double = Math.Max(0.0, baseSubTotal - calculatedDiscount)

            ' Optional Tax
            If currentSettings.EnableTax AndAlso currentSettings.DefaultTaxRate > 0 Then
                calculatedTaxAmount = afterDisc * (currentSettings.DefaultTaxRate / 100.0)
                calculatedGrandTotal = afterDisc + calculatedTaxAmount
            Else
                calculatedTaxAmount = 0.0
                calculatedGrandTotal = afterDisc
            End If

            Dim curr As String = If(Not String.IsNullOrEmpty(currentSettings.Currency), currentSettings.Currency, "ج.م")
            lblTotalAmount.Text = $"{calculatedGrandTotal:N2} {curr}"
            If calculatedDiscount > 0 Then
                lblTotalAmount.Text &= $" (صافي بعد خصم {calculatedDiscount:N2} {curr})"
            End If

            CalculateChange()
        End Sub

        Private Sub OnPaymentMethodChanged(sender As Object, e As EventArgs)
            If rdoCash.Checked Then
                txtCashReceived.Enabled = True
                txtCashReceived.Text = $"{calculatedGrandTotal:N0}"
            Else
                txtCashReceived.Enabled = False
                txtCashReceived.Text = $"{calculatedGrandTotal:N0}"
            End If
            CalculateChange()
        End Sub

        Private Sub CalculateChange()
            Dim received As Double = 0
            Double.TryParse(txtCashReceived.Text, received)
            Dim change As Double = received - calculatedGrandTotal
            Dim curr As String = If(Not String.IsNullOrEmpty(currentSettings.Currency), currentSettings.Currency, "ج.م")

            If change >= 0 Then
                lblChangeAmount.ForeColor = Color.FromArgb(41, 128, 185)
                lblChangeAmount.Text = $"الباقي للعميل:#{vbCrLf}{change:N2} {curr}"
            Else
                lblChangeAmount.ForeColor = Color.Red
                lblChangeAmount.Text = $"المتبقي على العميل:#{vbCrLf}{Math.Abs(change):N2} {curr}"
            End If
        End Sub

        Private Sub OnConfirmPayClicked(sender As Object, e As EventArgs)
            btnConfirmPay.Enabled = False
            btnCancel.Enabled = False
            Try
                Dim receivedVal As Double = 0
                Double.TryParse(txtCashReceived.Text, receivedVal)

                Dim selectedMethod As PaymentMethod = PaymentMethod.CASH
                If rdoCard.Checked Then selectedMethod = PaymentMethod.CARD
                If rdoTransfer.Checked Then selectedMethod = PaymentMethod.TRANSFER
                If rdoSplit.Checked Then selectedMethod = PaymentMethod.SPLIT

                ' Verify active shift & user
                Dim activeShift = AuthService.CurrentShift
                Dim shiftId As Integer = If(activeShift IsNot Nothing, activeShift.Id, 1)
                Dim userId As Integer = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.Id, 1)
                Dim userName As String = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.DisplayName, "كاشير")

                ' Create Sale Record
                Dim saleObj As New Sale With {
                    .SessionId = currentSession.Id,
                    .ChairId = currentSession.ChairId,
                    .ChairName = currentSession.ChairName,
                    .EmployeeId = currentSession.EmployeeId,
                    .EmployeeName = currentSession.EmployeeName,
                    .CustomerId = If(currentSession.CustomerId.HasValue AndAlso currentSession.CustomerId.Value > 0, currentSession.CustomerId, CType(Nothing, Integer?)),
                    .CustomerName = currentSession.CustomerName,
                    .SubTotal = baseSubTotal,
                    .Discount = calculatedDiscount,
                    .TotalAmount = calculatedGrandTotal,
                    .PaymentMethod = selectedMethod,
                    .CashReceived = receivedVal,
                    .ChangeAmount = Math.Max(0, receivedVal - calculatedGrandTotal),
                    .ShiftId = shiftId,
                    .CreatedByUserId = userId
                }

                Dim empRepo As New EmployeeRepository()
                Dim activeEmps = empRepo.GetActiveEmployees()

                For Each item As ChairSessionItem In currentSession.Items
                    Dim empIdToUse = If(item.EmployeeId.HasValue, item.EmployeeId.Value, currentSession.EmployeeId)
                    Dim empRate As Double = 0.1
                    If empIdToUse > 0 Then
                        Dim matchedEmp = activeEmps.Find(Function(x) x.Id = empIdToUse)
                        If matchedEmp IsNot Nothing AndAlso matchedEmp.CommissionRate >= 0 Then
                            empRate = matchedEmp.CommissionRate / 100.0
                        End If
                    End If

                    Dim saleItem As New SaleItem With {
                        .ServiceId = item.ServiceId,
                        .ServiceName = item.ServiceName,
                        .Price = item.Price,
                        .Quantity = item.Quantity,
                        .SubTotal = item.LineTotal,
                        .EmployeeId = empIdToUse,
                        .EmployeeName = If(Not String.IsNullOrEmpty(item.EmployeeName), item.EmployeeName, currentSession.EmployeeName),
                        .EmployeeCommissionAmount = item.LineTotal * empRate
                    }

                    For Each ex As ChairSessionItemExtra In item.Extras
                        saleItem.Extras.Add(New SaleItemExtra With {
                            .ExtraId = ex.ExtraId,
                            .ExtraName = ex.ExtraName,
                            .Price = ex.Price
                        })
                    Next

                    saleObj.Items.Add(saleItem)
                Next

                ' Save Sale to Database
                Dim invoiceNo As String = saleRepo.SaveSale(saleObj)

                ' Log Activity in ActivityLogs
                ActivityLogRepository.Log(userId, userName, "إتمام فاتورة", $"تم إتمام وتسديد فاتورة رقم {invoiceNo} بمبلغ إجمالي: {calculatedGrandTotal:N2} ج.م (خصم: {calculatedDiscount:N2} ج.م - العميل: {saleObj.CustomerName})")

                ' Auto Print Receipt if checked
                If chkPrintReceipt.Checked Then
                    Try
                        ReceiptPrinter.PrintReceipt(saleObj)
                    Catch exPrint As Exception
                        ' Non-fatal print warning
                    End Try
                End If

                MessageBox.Show($"تم حفظ وسداد الفاتورة بنجاح!{vbCrLf}رقم الفاتورة: {invoiceNo}", "نجاح العملية", MessageBoxButtons.OK, MessageBoxIcon.Information)

                Me.DialogResult = DialogResult.OK
                Me.Close()
            Catch ex As Exception
                btnConfirmPay.Enabled = True
                btnCancel.Enabled = True
                MessageBox.Show($"تعذر إتمام عملية السداد: {ex.Message}", "خطأ في السداد", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub
    End Class

End Namespace
