Imports System
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Windows.Forms
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Models

Namespace SalonPOS.Printing

    Public Class ReceiptPrinter
        Private Shared _saleToPrint As Sale

        Public Shared Sub PrintReceipt(sale As Sale)
            If sale Is Nothing Then Return
            _saleToPrint = sale

            Try
                ' Check if any printer is installed on this system
                If PrinterSettings.InstalledPrinters.Count = 0 Then
                    MessageBox.Show("لم يتم تعيين أو العثور على طابعة مثبتة بالجهاز. تم حفظ الفاتورة بنجاح بدون طباعة ورقية.", "تنبيه الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                Dim settingsRepo As New SettingsRepository()
                Dim s As SalonSettings = settingsRepo.GetSettings()

                Dim pd As New PrintDocument()

                ' CRITICAL FIX: Use StandardPrintController to suppress the WinForms PrintProgressDialog
                ' This prevents background thread UI crashes when closing modal payment dialogs.
                pd.PrintController = New StandardPrintController()

                ' Apply Selected Printer if configured
                If Not String.IsNullOrWhiteSpace(s.SelectedPrinterName) AndAlso s.SelectedPrinterName <> "(طابعة النظام الافتراضية)" Then
                    pd.PrinterSettings.PrinterName = s.SelectedPrinterName
                End If

                ' Validate printer settings; fallback to system default printer if specified printer is unavailable
                If Not pd.PrinterSettings.IsValid Then
                    pd.PrinterSettings = New PrinterSettings()
                    pd.PrintController = New StandardPrintController()
                End If

                AddHandler pd.PrintPage, AddressOf OnPrintPage
                pd.Print()

            Catch ex As Exception
                MessageBox.Show($"تنبيه الطباعة: تعذر إرسال أمر الطباعة إلى الطابعة المحددة ({ex.Message}).{vbCrLf}يرجى التأكد من توصيل وتفعيل الطابعة.{vbCrLf}ملاحظة: تم حفظ الفاتورة بنجاح في قاعدة البيانات.", "تنبيه الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Shared Sub OnPrintPage(sender As Object, e As PrintPageEventArgs)
            Try
                If _saleToPrint Is Nothing Then Return

                Dim settingsRepo As New SettingsRepository()
                Dim s As SalonSettings = settingsRepo.GetSettings()

                Dim g As Graphics = e.Graphics
                Using fontHeader As Font = UIStyleHelper.GetFont(13.0F, FontStyle.Bold)
                    Using fontBody As Font = UIStyleHelper.GetFont(9.0F, FontStyle.Regular)
                        Using fontBold As Font = UIStyleHelper.GetFont(9.5F, FontStyle.Bold)

                            Dim y As Single = 10.0F
                            Dim leftMargin As Single = 10.0F
                            Dim rightMargin As Single = 270.0F

                            ' Draw Optional Logo if provided and file exists
                            If Not String.IsNullOrEmpty(s.LogoPath) AndAlso File.Exists(s.LogoPath) Then
                                Try
                                    Using img As Image = Image.FromFile(s.LogoPath)
                                        g.DrawImage(img, New RectangleF(leftMargin + 100, y, 70, 50))
                                        y += 55.0F
                                    End Using
                                Catch
                                    ' Skip logo if image format invalid
                                End Try
                            End If

                            ' Draw Salon Name (Default: ZEX SALON)
                            Dim salonTitle As String = If(Not String.IsNullOrEmpty(s.SalonName), s.SalonName, "ZEX SALON")
                            g.DrawString(salonTitle, fontHeader, Brushes.Black, New RectangleF(leftMargin, y, rightMargin, 25.0F))
                            y += 28.0F

                            ' Draw Optional Phone & Address
                            If Not String.IsNullOrEmpty(s.Phone) Then
                                g.DrawString($"هاتف: {s.Phone}", fontBody, Brushes.Black, leftMargin, y)
                                y += 18.0F
                            End If
                            If Not String.IsNullOrEmpty(s.Address) Then
                                g.DrawString($"العنوان: {s.Address}", fontBody, Brushes.Black, leftMargin, y)
                                y += 18.0F
                            End If

                            y += 5.0F
                            g.DrawString("----------------------------------------", fontBody, Brushes.Black, leftMargin, y)
                            y += 15.0F

                            ' Draw Invoice Info
                            g.DrawString($"فاتورة رقم: {If(_saleToPrint.InvoiceNumber, "")}", fontBold, Brushes.Black, leftMargin, y)
                            y += 20.0F

                            Dim dateStr As String = _saleToPrint.CreatedAt.ToString("yyyy-MM-dd hh:mm tt")
                            g.DrawString($"التاريخ: {dateStr}", fontBody, Brushes.Black, leftMargin, y)
                            y += 20.0F
                            g.DrawString($"الموظف: {If(_saleToPrint.EmployeeName, "")} | الكرسي: {If(_saleToPrint.ChairName, "")}", fontBody, Brushes.Black, leftMargin, y)
                            y += 20.0F
                            g.DrawString($"العميل: {If(_saleToPrint.CustomerName, "عميل نقدي")}", fontBody, Brushes.Black, leftMargin, y)
                            y += 22.0F

                            g.DrawString("----------------------------------------", fontBody, Brushes.Black, leftMargin, y)
                            y += 15.0F

                            Dim curr As String = If(Not String.IsNullOrEmpty(s.Currency), s.Currency, "ج.م")

                            ' Draw Items
                            If _saleToPrint.Items IsNot Nothing Then
                                For Each item As SaleItem In _saleToPrint.Items
                                    g.DrawString($"{item.ServiceName} (x{item.Quantity})", fontBold, Brushes.Black, leftMargin, y)
                                    g.DrawString($"{item.SubTotal:N0} {curr}", fontBold, Brushes.Black, rightMargin - 65.0F, y)
                                    y += 18.0F

                                    If item.Extras IsNot Nothing Then
                                        For Each ex As SaleItemExtra In item.Extras
                                            g.DrawString($"  + {ex.ExtraName}", fontBody, Brushes.Black, leftMargin, y)
                                            g.DrawString($"+{ex.Price:N0}", fontBody, Brushes.Black, rightMargin - 65.0F, y)
                                            y += 16.0F
                                        Next
                                    End If
                                Next
                            End If

                            g.DrawString("----------------------------------------", fontBody, Brushes.Black, leftMargin, y)
                            y += 20.0F

                            If _saleToPrint.Discount > 0 Then
                                g.DrawString($"المجموع قبل الخصم: {_saleToPrint.SubTotal:N2} {curr}", fontBody, Brushes.Black, leftMargin, y)
                                y += 18.0F
                                g.DrawString($"الخصم المطبق: -{_saleToPrint.Discount:N2} {curr}", fontBold, Brushes.Black, leftMargin, y)
                                y += 20.0F
                            End If

                            ' Optional Tax Line
                            If s.EnableTax AndAlso s.DefaultTaxRate > 0 Then
                                Dim taxAmt As Double = _saleToPrint.TotalAmount * (s.DefaultTaxRate / (100 + s.DefaultTaxRate))
                                g.DrawString($"الصافي: {(_saleToPrint.TotalAmount - taxAmt):N2} {curr}", fontBody, Brushes.Black, leftMargin, y)
                                y += 18.0F
                                g.DrawString($"الضريبة ({s.DefaultTaxRate}%): {taxAmt:N2} {curr}", fontBody, Brushes.Black, leftMargin, y)
                                y += 20.0F
                            End If

                            ' Totals
                            g.DrawString($"الإجمالي المطلوب: {_saleToPrint.TotalAmount:N2} {curr}", fontHeader, Brushes.Black, leftMargin, y)
                            y += 25.0F
                            g.DrawString($"طريقة الدفع: {_saleToPrint.PaymentMethod}", fontBody, Brushes.Black, leftMargin, y)
                            y += 20.0F

                            If _saleToPrint.PaymentMethod = PaymentMethod.CASH Then
                                g.DrawString($"المستلم: {_saleToPrint.CashReceived:N0} | الباقي: {_saleToPrint.ChangeAmount:N0}", fontBody, Brushes.Black, leftMargin, y)
                                y += 20.0F
                            End If

                            g.DrawString("----------------------------------------", fontBody, Brushes.Black, leftMargin, y)
                            y += 20.0F

                            Dim footerMsg As String = If(Not String.IsNullOrEmpty(s.FooterMessage), s.FooterMessage, "شكراً لزيارتكم ZEX SALON ونتمنى لكم يوماً سعيداً!")
                            g.DrawString(footerMsg, fontBody, Brushes.Black, leftMargin, y)

                        End Using
                    End Using
                End Using

            Catch ex As Exception
                ' Prevent any GDI+ rendering exception from crashing process
            Finally
                e.HasMorePages = False
            End Try
        End Sub

        Private Shared _shiftToPrint As Shift
        Private Shared _payrollToPrint As EmployeePayroll
        Private Shared _empToPrint As Employee

        Public Shared Sub PrintShiftReport(shift As Shift)
            If shift Is Nothing Then Return
            _shiftToPrint = shift

            Try
                If PrinterSettings.InstalledPrinters.Count = 0 Then Return

                Dim settingsRepo As New SettingsRepository()
                Dim s As SalonSettings = settingsRepo.GetSettings()

                Dim pd As New PrintDocument()
                pd.PrintController = New StandardPrintController()

                If Not String.IsNullOrWhiteSpace(s.SelectedPrinterName) AndAlso s.SelectedPrinterName <> "(طابعة النظام الافتراضية)" Then
                    pd.PrinterSettings.PrinterName = s.SelectedPrinterName
                End If

                If Not pd.PrinterSettings.IsValid Then
                    pd.PrinterSettings = New PrinterSettings()
                    pd.PrintController = New StandardPrintController()
                End If

                AddHandler pd.PrintPage, AddressOf OnPrintShiftPage
                pd.Print()
            Catch ex As Exception
                ' Non-fatal
            End Try
        End Sub

        Private Shared Sub OnPrintShiftPage(sender As Object, e As PrintPageEventArgs)
            Try
                If _shiftToPrint Is Nothing Then Return

                Dim settingsRepo As New SettingsRepository()
                Dim s As SalonSettings = settingsRepo.GetSettings()

                Dim g As Graphics = e.Graphics
                Dim y As Single = 10.0F
                Dim leftMargin As Single = 10.0F
                Dim curr As String = If(Not String.IsNullOrEmpty(s.Currency), s.Currency, "ج.م")

                Using fontTitle As Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                      fontHeader As Font = UIStyleHelper.GetFont(10.0F, FontStyle.Bold),
                      fontBody As Font = UIStyleHelper.GetFont(9.0F, FontStyle.Regular)

                    Dim salonTitle As String = If(Not String.IsNullOrEmpty(s.SalonName), s.SalonName, "ZEX SALON")
                    g.DrawString(salonTitle, fontTitle, Brushes.Black, leftMargin, y)
                    y += 24.0F
                    g.DrawString("*** تقرير جرد وتصفية الوردية ***", fontHeader, Brushes.Black, leftMargin, y)
                    y += 22.0F
                    g.DrawString($"رقم الوردية: #{_shiftToPrint.Id}", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F
                    g.DrawString($"المستخدم: {_shiftToPrint.UserName}", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F
                    g.DrawString($"وقت البدء: {_shiftToPrint.StartedAt:yyyy-MM-dd hh:mm tt}", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F
                    Dim endStr As String = If(_shiftToPrint.EndedAt.HasValue, _shiftToPrint.EndedAt.Value.ToString("yyyy-MM-dd hh:mm tt"), DateTime.Now.ToString("yyyy-MM-dd hh:mm tt"))
                    g.DrawString($"وقت الإغلاق: {endStr}", fontBody, Brushes.Black, leftMargin, y)
                    y += 20.0F
                    g.DrawString("----------------------------------------", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F

                    g.DrawString($"العهدة الافتتاحية: {_shiftToPrint.OpeningCash:N2} {curr}", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F
                    g.DrawString($"المبيعات النقدية: {_shiftToPrint.CashSales:N2} {curr}", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F
                    g.DrawString($"مبيعات البطاقات (فيزا): {_shiftToPrint.CardSales:N2} {curr}", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F
                    g.DrawString($"مبيعات التحويل الشبكي: {_shiftToPrint.TransferSales:N2} {curr}", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F
                    g.DrawString($"إجمالي المصروفات: -{_shiftToPrint.TotalExpenses:N2} {curr}", fontBody, Brushes.Black, leftMargin, y)
                    y += 20.0F
                    g.DrawString("----------------------------------------", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F

                    Dim expCash As Double = _shiftToPrint.OpeningCash + _shiftToPrint.CashSales - _shiftToPrint.TotalExpenses
                    Dim actCash As Double = If(_shiftToPrint.ActualCash.HasValue, _shiftToPrint.ActualCash.Value, expCash)
                    Dim varCash As Double = If(_shiftToPrint.Variance.HasValue, _shiftToPrint.Variance.Value, 0.0)

                    g.DrawString($"الدرج المتوقع: {expCash:N2} {curr}", fontHeader, Brushes.Black, leftMargin, y)
                    y += 20.0F
                    g.DrawString($"العد الفعلي بالخزينة: {actCash:N2} {curr}", fontHeader, Brushes.Black, leftMargin, y)
                    y += 20.0F
                    g.DrawString($"العجز / الزيادة: {varCash:N2} {curr}", fontHeader, Brushes.Black, leftMargin, y)
                    y += 22.0F
                    g.DrawString("----------------------------------------", fontBody, Brushes.Black, leftMargin, y)
                    y += 20.0F
                    g.DrawString("تم مطابقة الخزينة واكتفاء الفرز بنجاح", fontBody, Brushes.Black, leftMargin, y)
                End Using
            Catch ex As Exception
            Finally
                e.HasMorePages = False
            End Try
        End Sub

        Public Shared Sub PrintSalarySlip(payroll As EmployeePayroll, employee As Employee)
            If payroll Is Nothing Then Return
            _payrollToPrint = payroll
            _empToPrint = employee

            Try
                If PrinterSettings.InstalledPrinters.Count = 0 Then
                    MessageBox.Show("لم يتم العثور على طابعة مثبتة بالجهاز.", "تنبيه الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                Dim settingsRepo As New SettingsRepository()
                Dim s As SalonSettings = settingsRepo.GetSettings()

                Dim pd As New PrintDocument()
                pd.PrintController = New StandardPrintController()

                If Not String.IsNullOrWhiteSpace(s.SelectedPrinterName) AndAlso s.SelectedPrinterName <> "(طابعة النظام الافتراضية)" Then
                    pd.PrinterSettings.PrinterName = s.SelectedPrinterName
                End If

                If Not pd.PrinterSettings.IsValid Then
                    pd.PrinterSettings = New PrinterSettings()
                    pd.PrintController = New StandardPrintController()
                End If

                AddHandler pd.PrintPage, AddressOf OnPrintSalarySlipPage
                pd.Print()
            Catch ex As Exception
                MessageBox.Show($"تعذر إرسال أمر الطباعة: {ex.Message}", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Shared Sub OnPrintSalarySlipPage(sender As Object, e As PrintPageEventArgs)
            Try
                Dim g As Graphics = e.Graphics
                Dim sRepo As New SettingsRepository()
                Dim s = sRepo.GetSettings()

                Using fontHeader As Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                      fontTitle As Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                      fontBody As Font = UIStyleHelper.GetFont(9.0F, FontStyle.Regular),
                      fontBold As Font = UIStyleHelper.GetFont(9.5F, FontStyle.Bold),
                      fontBig As Font = UIStyleHelper.GetFont(13.0F, FontStyle.Bold)

                    Dim y As Single = 10.0F
                    Dim leftMargin As Single = 10.0F
                    Dim rightMargin As Single = 280.0F
                    Dim curr = If(Not String.IsNullOrEmpty(s.Currency), s.Currency, "ج.م")

                    ' Salon Title
                    Dim salonTitle As String = If(Not String.IsNullOrEmpty(s.SalonName), s.SalonName, "ZEX SALON")
                    g.DrawString(salonTitle, fontHeader, Brushes.Black, leftMargin, y)
                    y += 24.0F

                    g.DrawString("سند صرف راتب ومستحقات", fontTitle, Brushes.Black, leftMargin, y)
                    y += 20.0F
                    g.DrawString($"التاريخ: {_payrollToPrint.PaymentDate} | شهر: {_payrollToPrint.PayrollMonth}", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F
                    g.DrawString($"الموظف: {_payrollToPrint.EmployeeName} ({_payrollToPrint.JobTitle})", fontBold, Brushes.Black, leftMargin, y)
                    y += 18.0F
                    g.DrawString("----------------------------------------", fontBody, Brushes.Black, leftMargin, y)
                    y += 16.0F

                    ' Line items
                    g.DrawString("الراتب الأساسي:", fontBody, Brushes.Black, leftMargin, y)
                    g.DrawString($"{_payrollToPrint.BaseSalary:N0} {curr}", fontBold, Brushes.Black, rightMargin - 70.0F, y)
                    y += 18.0F

                    If _payrollToPrint.TotalCommission > 0 Then
                        g.DrawString("+ عمولات الخدمات والمبيعات:", fontBody, Brushes.Black, leftMargin, y)
                        g.DrawString($"+{_payrollToPrint.TotalCommission:N0} {curr}", fontBold, Brushes.Black, rightMargin - 70.0F, y)
                        y += 18.0F
                    End If

                    If _payrollToPrint.TotalBonuses > 0 Then
                        g.DrawString("+ مكافآت وحوافز إضافية:", fontBody, Brushes.Black, leftMargin, y)
                        g.DrawString($"+{_payrollToPrint.TotalBonuses:N0} {curr}", fontBold, Brushes.Black, rightMargin - 70.0F, y)
                        y += 18.0F
                    End If

                    If _payrollToPrint.TotalAdvances > 0 Then
                        g.DrawString("- سلف مخصومة من الراتب:", fontBody, Brushes.Black, leftMargin, y)
                        g.DrawString($"-{_payrollToPrint.TotalAdvances:N0} {curr}", fontBold, Brushes.Black, rightMargin - 70.0F, y)
                        y += 18.0F
                    End If

                    If _payrollToPrint.TotalDeductions > 0 Then
                        g.DrawString("- استقطاعات وجزاءات:", fontBody, Brushes.Black, leftMargin, y)
                        g.DrawString($"-{_payrollToPrint.TotalDeductions:N0} {curr}", fontBold, Brushes.Black, rightMargin - 70.0F, y)
                        y += 18.0F
                    End If

                    g.DrawString("----------------------------------------", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F

                    g.DrawString("صافي الراتب المصروف:", fontBig, Brushes.Black, leftMargin, y)
                    g.DrawString($"{_payrollToPrint.PaidAmount:N0} {curr}", fontBig, Brushes.Black, rightMargin - 85.0F, y)
                    y += 24.0F

                    g.DrawString($"طريقة الصرف: {_payrollToPrint.PaymentMethod}", fontBody, Brushes.Black, leftMargin, y)
                    y += 18.0F

                    If Not String.IsNullOrWhiteSpace(_payrollToPrint.Notes) Then
                        g.DrawString($"ملاحظات: {_payrollToPrint.Notes}", fontBody, Brushes.Black, leftMargin, y)
                        y += 18.0F
                    End If

                    y += 15.0F
                    g.DrawString("توقيع المستلم: ............................", fontBody, Brushes.Black, leftMargin, y)
                    y += 22.0F
                    g.DrawString("توقيع الإدارة: ............................", fontBody, Brushes.Black, leftMargin, y)
                    y += 20.0F
                    g.DrawString("شكراً لجهودكم وتفانيكم في العمل", fontBody, Brushes.Black, leftMargin, y)
                End Using
            Catch
            Finally
                e.HasMorePages = False
            End Try
        End Sub
    End Class

End Namespace
