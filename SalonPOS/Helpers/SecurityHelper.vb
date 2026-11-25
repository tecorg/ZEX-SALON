Imports System.Security.Cryptography
Imports System.Text

Namespace SalonPOS.Helpers

    Public Class SecurityHelper
        Public Shared Function HashPassword(password As String) As String
            If String.IsNullOrEmpty(password) Then Return ""

            Using shaAlgo As SHA256 = SHA256.Create()
                Dim bytes As Byte() = Encoding.UTF8.GetBytes(password & "SalonPOS_Salt_2026")
                Dim hash As Byte() = shaAlgo.ComputeHash(bytes)
                Dim sb As New StringBuilder()
                For Each b As Byte In hash
                    sb.Append(b.ToString("x2"))
                Next
                Return sb.ToString()
            End Using
        End Function
    End Class

End Namespace
