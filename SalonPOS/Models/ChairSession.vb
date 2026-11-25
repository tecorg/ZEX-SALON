Imports System.Collections.Generic

Namespace SalonPOS.Models

    Public Class ChairSession
        Public Property Id As Integer
        Public Property ChairId As Integer
        Public Property ChairName As String
        Public Property EmployeeId As Integer
        Public Property EmployeeName As String
        Public Property CustomerId As Integer?
        Public Property CustomerName As String = "عميل نقدي"
        Public Property Status As ChairStatus = ChairStatus.IN_SERVICE
        Public Property StartedAt As DateTime = DateTime.Now
        Public Property TotalAmount As Double = 0.0

        Public Property Items As New List(Of ChairSessionItem)()
    End Class

    Public Class ChairSessionItem
        Public Property Id As Integer
        Public Property SessionId As Integer
        Public Property ServiceId As Integer
        Public Property ServiceName As String
        Public Property Price As Double
        Public Property Quantity As Integer = 1
        Public Property EmployeeId As Integer? ' Optional overridden employee for this line
        Public Property EmployeeName As String

        Public Property Extras As New List(Of ChairSessionItemExtra)()

        Public ReadOnly Property LineTotal As Double
            Get
                Dim extrasSum As Double = 0
                If Extras IsNot Nothing Then
                    For Each ex In Extras
                        extrasSum += ex.Price
                    Next
                End If
                Return (Price + extrasSum) * Quantity
            End Get
        End Property
    End Class

    Public Class ChairSessionItemExtra
        Public Property Id As Integer
        Public Property SessionItemId As Integer
        Public Property ExtraId As Integer
        Public Property ExtraName As String
        Public Property Price As Double
    End Class

End Namespace
