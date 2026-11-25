Imports System

Namespace SalonPOS.Models

    Public Enum UserRole
        ADMIN     ' مدير نظام (صلاحيات كاملة)
        MANAGER   ' مشرف
        CASHIER   ' كاشير / بائع
        EMPLOYEE  ' موظف / فني
    End Enum

    Public Class User
        Public Property Id As Integer
        Public Property Username As String
        Public Property PasswordHash As String
        Public Property DisplayName As String
        Public Property Role As UserRole
        Public Property IsActive As Boolean = True
        Public Property PermissionsJson As String = ""
        Public Property CreatedAt As DateTime = DateTime.Now

        ' Roles Arabic Display
        Public ReadOnly Property RoleArabic As String
            Get
                Select Case Role
                    Case UserRole.ADMIN
                        Return "مدير النظام"
                    Case UserRole.MANAGER
                        Return "مشرف عام"
                    Case UserRole.CASHIER
                        Return "كاشير / بائع"
                    Case UserRole.EMPLOYEE
                        Return "فني / موظف"
                    Case Else
                        Return "مستخدم"
                End Select
            End Get
        End Property

        Public Function HasPermission(permName As String) As Boolean
            If Role = UserRole.ADMIN Then Return True

            If String.IsNullOrWhiteSpace(PermissionsJson) Then
                ' Default permissions per role
                If Role = UserRole.MANAGER Then Return True
                If Role = UserRole.CASHIER Then
                    Return permName = "POS" OrElse permName = "Customers" OrElse permName = "Expenses" OrElse permName = "Shifts"
                End If
                Return permName = "POS"
            End If

            Return PermissionsJson.Contains($"""{permName}"":true") OrElse PermissionsJson.Contains($"""{permName}"": true")
        End Function
    End Class

End Namespace
