Namespace SalonPOS.Models

    Public Class Customer
        Public Property Id As Integer
        Public Property Name As String
        Public Property Phone As String
        Public Property Notes As String
        Public Property CreatedAt As DateTime = DateTime.Now

        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class

End Namespace
