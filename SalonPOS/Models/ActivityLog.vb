Imports System

Namespace SalonPOS.Models

    Public Class ActivityLog
        Public Property Id As Integer
        Public Property UserId As Integer
        Public Property UserName As String
        Public Property ActionType As String
        Public Property Details As String
        Public Property CreatedAt As DateTime = DateTime.Now
    End Class

End Namespace
