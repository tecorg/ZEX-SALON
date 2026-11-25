Imports System
Imports System.Collections.Generic
Imports System.Data.SQLite
Imports SalonPOS.Models

Namespace SalonPOS.Data.Repositories

    Public Class ActivityLogRepository

        Public Shared Sub Log(userId As Integer, userName As String, actionType As String, details As String)
            Try
                Using conn = DbConnectionFactory.CreateConnection()
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "
                            INSERT INTO ActivityLogs (UserId, UserName, ActionType, Details, CreatedAt)
                            VALUES (@u, @un, @a, @d, @c);
                        "
                        cmd.Parameters.AddWithValue("@u", userId)
                        cmd.Parameters.AddWithValue("@un", If(userName, "مستخدم"))
                        cmd.Parameters.AddWithValue("@a", If(actionType, "عمومية"))
                        cmd.Parameters.AddWithValue("@d", If(details, ""))
                        cmd.Parameters.AddWithValue("@c", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                        cmd.ExecuteNonQuery()
                    End Using
                End Using
            Catch
                ' Ignore logging failures to prevent blocking main UI actions
            End Try
        End Sub

        Public Function GetLogs(userId As Integer?, fromDate As DateTime?, toDate As DateTime?, Optional search As String = "") As List(Of ActivityLog)
            Dim list As New List(Of ActivityLog)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    Dim sql As String = "SELECT Id, UserId, UserName, ActionType, Details, CreatedAt FROM ActivityLogs"
                    Dim conds As New List(Of String)()

                    If userId.HasValue AndAlso userId.Value > 0 Then
                        conds.Add("UserId = @userId")
                        cmd.Parameters.AddWithValue("@userId", userId.Value)
                    End If

                    If fromDate.HasValue Then
                        conds.Add("CreatedAt >= @fromDate")
                        cmd.Parameters.AddWithValue("@fromDate", fromDate.Value.ToString("yyyy-MM-dd 00:00:00"))
                    End If

                    If toDate.HasValue Then
                        conds.Add("CreatedAt <= @toDate")
                        cmd.Parameters.AddWithValue("@toDate", toDate.Value.ToString("yyyy-MM-dd 23:59:59"))
                    End If

                    If Not String.IsNullOrWhiteSpace(search) Then
                        conds.Add("(UserName LIKE @search OR ActionType LIKE @search OR Details LIKE @search)")
                        cmd.Parameters.AddWithValue("@search", $"%{search.Trim()}%")
                    End If

                    If conds.Count > 0 Then
                        sql &= " WHERE " & String.Join(" AND ", conds)
                    End If

                    sql &= " ORDER BY Id DESC LIMIT 500;"
                    cmd.CommandText = sql

                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            list.Add(New ActivityLog With {
                                .Id = DbConnectionFactory.ToInt(reader("Id")),
                                .UserId = DbConnectionFactory.ToInt(reader("UserId")),
                                .UserName = DbConnectionFactory.ToStr(reader("UserName")),
                                .ActionType = DbConnectionFactory.ToStr(reader("ActionType")),
                                .Details = DbConnectionFactory.ToStr(reader("Details")),
                                .CreatedAt = Convert.ToDateTime(reader("CreatedAt"))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function
    End Class

End Namespace
