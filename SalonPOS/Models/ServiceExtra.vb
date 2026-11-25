Namespace SalonPOS.Models

    Public Class ServiceExtra
        Public Property Id As Integer
        Public Property ServiceId As Integer
        Public Property Name As String
        Public Property Price As Double
        Public Property IsActive As Boolean = True

        Public Overrides Function ToString() As String
            Return $"{Name} (+{Price:N0} ج.م)"
        End Function
    End Class

End Namespace
