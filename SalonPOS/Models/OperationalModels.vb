Namespace SalonPOS.Models

    Public Class Expense
        Public Property Id As Integer
        Public Property Amount As Double
        Public Property Category As String
        Public Property Notes As String
        Public Property ShiftId As Integer
        Public Property CreatedByUserId As Integer
        Public Property CreatedAt As DateTime = DateTime.Now
    End Class

    Public Enum ShiftStatus
        OPEN
        CLOSED
    End Enum

    Public Class Shift
        Public Property Id As Integer
        Public Property UserId As Integer
        Public Property UserName As String
        Public Property StartedAt As DateTime = DateTime.Now
        Public Property EndedAt As DateTime?
        Public Property OpeningCash As Double = 0.0
        Public Property CashSales As Double = 0.0
        Public Property CardSales As Double = 0.0
        Public Property TransferSales As Double = 0.0
        Public Property TotalExpenses As Double = 0.0
        Public Property ExpectedCash As Double = 0.0
        Public Property ActualCash As Double?
        Public Property Variance As Double?
        Public Property Status As ShiftStatus = ShiftStatus.OPEN
    End Class

    Public Class LicenseData
        Public Property LicenseKey As String
        Public Property CustomerName As String
        Public Property ProductName As String = "Simple Salon POS"
        Public Property Edition As String = "Professional"
        Public Property MachineId As String
        Public Property ActivationDate As DateTime
        Public Property ExpiryDate As DateTime
        Public Property IsLifetime As Boolean = False
        Public Property MaxDevices As Integer = 1
        Public Property Signature As String
    End Class

End Namespace
