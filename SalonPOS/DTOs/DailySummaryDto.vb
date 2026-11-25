Namespace SalonPOS.DTOs

    Public Class DailySummaryDto
        Public Property TotalSales As Double = 0.0
        Public Property CashSales As Double = 0.0
        Public Property CardSales As Double = 0.0
        Public Property TransferSales As Double = 0.0
        Public Property CustomerCount As Integer = 0
        Public Property ServiceCount As Integer = 0
        Public Property TotalExpenses As Double = 0.0
        Public Property NetProfit As Double = 0.0
    End Class

End Namespace
