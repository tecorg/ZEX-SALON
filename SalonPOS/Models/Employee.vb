Namespace SalonPOS.Models

    Public Class Employee
        Public Property Id As Integer
        Public Property Name As String
        Public Property Phone As String
        Public Property JobTitle As String = "مصفف / حلاق"
        Public Property BaseSalary As Double = 0.0
        Public Property CommissionRate As Double = 0.0 ' % e.g. 10.0 for 10%
        Public Property HireDate As String = ""
        Public Property IsActive As Boolean = True
        Public Property CreatedAt As DateTime = DateTime.Now

        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class

End Namespace
