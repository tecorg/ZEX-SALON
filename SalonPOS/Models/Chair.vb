Namespace SalonPOS.Models

    Public Enum ChairStatus
        AVAILABLE
        IN_SERVICE
        WAITING_PAYMENT
    End Enum

    Public Class Chair
        Public Property Id As Integer
        Public Property ChairNumber As Integer
        Public Property Name As String
        Public Property DisplayOrder As Integer = 0
        Public Property IsActive As Boolean = True

        ' Dynamic runtime state
        Public Property CurrentStatus As ChairStatus = ChairStatus.AVAILABLE
        Public Property ActiveSessionId As Integer? = Nothing
        Public Property CurrentEmployeeName As String = ""
        Public Property CurrentCustomerName As String = ""
        Public Property ActiveTotalAmount As Double = 0.0
        Public Property ServiceStartedAt As DateTime? = Nothing

        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class

End Namespace
