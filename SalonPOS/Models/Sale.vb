Imports System.Collections.Generic

Namespace SalonPOS.Models

    Public Enum PaymentMethod
        CASH
        CARD
        TRANSFER
        SPLIT
    End Enum

    Public Class Sale
        Public Property Id As Integer
        Public Property InvoiceNumber As String
        Public Property SessionId As Integer?
        Public Property ChairId As Integer
        Public Property ChairName As String
        Public Property EmployeeId As Integer
        Public Property EmployeeName As String
        Public Property CustomerId As Integer?
        Public Property CustomerName As String = "عميل نقدي"
        Public Property SubTotal As Double
        Public Property Discount As Double = 0.0
        Public Property TotalAmount As Double
        Public Property PaymentMethod As PaymentMethod = PaymentMethod.CASH
        Public Property CashReceived As Double = 0.0
        Public Property ChangeAmount As Double = 0.0
        Public Property ShiftId As Integer
        Public Property CreatedByUserId As Integer
        Public Property CreatedAt As DateTime = DateTime.Now

        Public Property Items As New List(Of SaleItem)()
    End Class

    Public Class SaleItem
        Public Property Id As Integer
        Public Property SaleId As Integer
        Public Property ServiceId As Integer
        Public Property ServiceName As String
        Public Property Price As Double
        Public Property Quantity As Integer = 1
        Public Property SubTotal As Double
        Public Property EmployeeId As Integer
        Public Property EmployeeName As String
        Public Property EmployeeCommissionAmount As Double = 0.0

        Public Property Extras As New List(Of SaleItemExtra)()
    End Class

    Public Class SaleItemExtra
        Public Property Id As Integer
        Public Property SaleItemId As Integer
        Public Property ExtraId As Integer
        Public Property ExtraName As String
        Public Property Price As Double
    End Class

End Namespace
