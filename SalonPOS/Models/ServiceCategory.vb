Namespace SalonPOS.Models

    Public Class ServiceCategory
        Public Property Id As Integer
        Public Property Name As String
        Public Property IconName As String
        Public Property DisplayOrder As Integer = 0
        Public Property IsActive As Boolean = True

        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class

End Namespace
