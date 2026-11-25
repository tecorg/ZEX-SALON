Imports SalonPOS.Models

Namespace SalonPOS.Services

    Public Class AuthService
        Public Shared Property CurrentUser As User = Nothing
        Public Shared Property CurrentShift As Shift = Nothing

        Public Shared Function IsLoggedIn() As Boolean
            Return CurrentUser IsNot Nothing
        End Function

        Public Shared Function IsAdmin() As Boolean
            Return CurrentUser IsNot Nothing AndAlso CurrentUser.Role = UserRole.ADMIN
        End Function

        Public Shared Function HasPermission(permKey As String) As Boolean
            If CurrentUser Is Nothing Then Return False
            Return CurrentUser.HasPermission(permKey)
        End Function
    End Class

End Namespace
