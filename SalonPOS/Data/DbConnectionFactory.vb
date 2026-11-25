Imports System
Imports System.Data.SQLite
Imports System.IO
Imports System.Reflection

Namespace SalonPOS.Data

    Public Class DbConnectionFactory
        Private Shared ReadOnly DbFileName As String = "salon_pos.db"

        Public Shared ReadOnly Property DatabasePath As String
            Get
                Dim exeDir As String = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                Return Path.Combine(exeDir, DbFileName)
            End Get
        End Property

        Public Shared Function GetConnectionString() As String
            Return $"Data Source={DatabasePath};"
        End Function

        Public Shared Function CreateConnection() As SQLiteConnection
            Dim conn As New SQLiteConnection(GetConnectionString())
            conn.Open()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "PRAGMA foreign_keys = ON;"
                cmd.ExecuteNonQuery()
            End Using
            Return conn
        End Function

        Public Shared Function ToBool(val As Object) As Boolean
            If val Is Nothing OrElse IsDBNull(val) Then Return False
            Dim str As String = val.ToString().Trim()
            If str = "1" OrElse str.Equals("true", StringComparison.OrdinalIgnoreCase) Then Return True
            If str = "0" OrElse str.Equals("false", StringComparison.OrdinalIgnoreCase) Then Return False
            Dim num As Integer = 0
            If Integer.TryParse(str, num) Then Return num <> 0
            Return False
        End Function

        Public Shared Function ToDbl(val As Object) As Double
            If val Is Nothing OrElse IsDBNull(val) Then Return 0.0
            Dim res As Double = 0.0
            If Double.TryParse(val.ToString(), res) Then Return res
            Return 0.0
        End Function

        Public Shared Function ToInt(val As Object) As Integer
            If val Is Nothing OrElse IsDBNull(val) Then Return 0
            Dim res As Integer = 0
            If Integer.TryParse(val.ToString(), res) Then Return res
            Return 0
        End Function

        Public Shared Function ToStr(val As Object) As String
            If val Is Nothing OrElse IsDBNull(val) Then Return ""
            Return val.ToString()
        End Function
    End Class

End Namespace
