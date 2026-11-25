Namespace SalonPOS.Models

    Public Class SalonSettings
        Public Property SalonName As String = "ZEX SALON"
        Public Property LogoPath As String = ""
        Public Property Address As String = ""
        Public Property Phone As String = "01022422010"
        Public Property Currency As String = "ج.م"
        Public Property DefaultTaxRate As Double = 0.0
        Public Property EnableTax As Boolean = False ' البدا التلقائي بدون ضريبة
        Public Property FooterMessage As String = "شكراً لزيارتكم ZEX SALON ونتمنى لكم يوماً سعيداً!"
        Public Property SelectedPrinterName As String = "(طابعة النظام الافتراضية)"
        Public Property SecondaryBackupPath As String = ""
        Public Property EnableSecondaryBackup As Boolean = False
    End Class

End Namespace
