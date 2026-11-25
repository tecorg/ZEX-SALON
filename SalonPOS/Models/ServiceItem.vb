Imports System.Collections.Generic

Namespace SalonPOS.Models

    Public Class ServiceItem
        Public Property Id As Integer
        Public Property CategoryId As Integer
        Public Property CategoryName As String
        Public Property Name As String
        Public Property Price As Double
        Public Property DurationMinutes As Integer = 30
        Public Property IsActive As Boolean = True

        ' Associated extras list
        Public Property Extras As New List(Of ServiceExtra)()

        Public Overrides Function ToString() As String
            Return $"{Name} ({Price:N0} ج.م)"
        End Function
    End Class

End Namespace
