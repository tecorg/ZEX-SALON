Imports System.Collections.Generic
Imports System.Data.SQLite
Imports SalonPOS.Models

Namespace SalonPOS.Data.Repositories

    Public Class SessionRepository

        Public Function CreateSession(chairId As Integer, employeeId As Integer, customerId As Integer?) As Integer
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        INSERT INTO ChairSessions (ChairId, EmployeeId, CustomerId, Status, StartedAt, TotalAmount)
                        VALUES (@chair, @emp, @cust, 'IN_SERVICE', @start, 0.0);
                        SELECT last_insert_rowid();
                    "
                    cmd.Parameters.AddWithValue("@chair", chairId)
                    cmd.Parameters.AddWithValue("@emp", employeeId)
                    cmd.Parameters.AddWithValue("@cust", If(customerId.HasValue AndAlso customerId.Value > 0, customerId.Value, DBNull.Value))
                    cmd.Parameters.AddWithValue("@start", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    Return Convert.ToInt32(cmd.ExecuteScalar())
                End Using
            End Using
        End Function

        Public Function GetSessionById(sessionId As Integer) As ChairSession
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        SELECT s.Id, s.ChairId, ch.Name AS ChairName, s.EmployeeId, e.Name AS EmployeeName,
                               s.CustomerId, cust.Name AS CustomerName, s.Status, s.StartedAt, s.TotalAmount
                        FROM ChairSessions s
                        INNER JOIN Chairs ch ON s.ChairId = ch.Id
                        INNER JOIN Employees e ON s.EmployeeId = e.Id
                        LEFT JOIN Customers cust ON s.CustomerId = cust.Id
                        WHERE s.Id = @sid;
                    "
                    cmd.Parameters.AddWithValue("@sid", sessionId)

                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Dim session As New ChairSession With {
                                .Id = Convert.ToInt32(reader("Id")),
                                .ChairId = Convert.ToInt32(reader("ChairId")),
                                .ChairName = reader("ChairName").ToString(),
                                .EmployeeId = Convert.ToInt32(reader("EmployeeId")),
                                .EmployeeName = reader("EmployeeName").ToString(),
                                .CustomerId = If(reader.IsDBNull(reader.GetOrdinal("CustomerId")), CType(Nothing, Integer?), Convert.ToInt32(reader("CustomerId"))),
                                .CustomerName = If(reader.IsDBNull(reader.GetOrdinal("CustomerName")), "عميل نقدي", reader("CustomerName").ToString()),
                                .StartedAt = Convert.ToDateTime(reader("StartedAt")),
                                .TotalAmount = Convert.ToDouble(reader("TotalAmount"))
                            }

                            Dim statusStr = reader("Status").ToString()
                            If statusStr = "IN_SERVICE" Then session.Status = ChairStatus.IN_SERVICE
                            If statusStr = "WAITING_PAYMENT" Then session.Status = ChairStatus.WAITING_PAYMENT

                            session.Items = GetSessionItems(conn, session.Id)
                            Return session
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Function GetSessionByChairId(chairId As Integer) As ChairSession
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id FROM ChairSessions WHERE ChairId = @cid AND Status IN ('IN_SERVICE', 'WAITING_PAYMENT') ORDER BY Id DESC LIMIT 1;"
                    cmd.Parameters.AddWithValue("@cid", chairId)
                    Dim sIdObj = cmd.ExecuteScalar()
                    If sIdObj IsNot Nothing AndAlso Not Convert.IsDBNull(sIdObj) Then
                        Return GetSessionById(Convert.ToInt32(sIdObj))
                    End If
                End Using
            End Using
            Return Nothing
        End Function

        Private Function GetSessionItems(conn As SQLiteConnection, sessionId As Integer) As List(Of ChairSessionItem)
            Dim items As New List(Of ChairSessionItem)()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "
                    SELECT i.Id, i.SessionId, i.ServiceId, i.ServiceName, i.Price, i.Quantity, i.EmployeeId, e.Name AS EmployeeName
                    FROM ChairSessionItems i
                    LEFT JOIN Employees e ON i.EmployeeId = e.Id
                    WHERE i.SessionId = @sid;
                "
                cmd.Parameters.AddWithValue("@sid", sessionId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim item As New ChairSessionItem With {
                            .Id = Convert.ToInt32(reader("Id")),
                            .SessionId = Convert.ToInt32(reader("SessionId")),
                            .ServiceId = Convert.ToInt32(reader("ServiceId")),
                            .ServiceName = reader("ServiceName").ToString(),
                            .Price = Convert.ToDouble(reader("Price")),
                            .Quantity = Convert.ToInt32(reader("Quantity")),
                            .EmployeeId = If(reader.IsDBNull(reader.GetOrdinal("EmployeeId")), CType(Nothing, Integer?), Convert.ToInt32(reader("EmployeeId"))),
                            .EmployeeName = If(reader.IsDBNull(reader.GetOrdinal("EmployeeName")), "", reader("EmployeeName").ToString())
                        }
                        item.Extras = GetSessionItemExtras(conn, item.Id)
                        items.Add(item)
                    End While
                End Using
            End Using
            Return items
        End Function

        Private Function GetSessionItemExtras(conn As SQLiteConnection, sessionItemId As Integer) As List(Of ChairSessionItemExtra)
            Dim extras As New List(Of ChairSessionItemExtra)()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "SELECT Id, SessionItemId, ExtraId, ExtraName, Price FROM ChairSessionItemExtras WHERE SessionItemId = @iid;"
                cmd.Parameters.AddWithValue("@iid", sessionItemId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        extras.Add(New ChairSessionItemExtra With {
                            .Id = Convert.ToInt32(reader("Id")),
                            .SessionItemId = Convert.ToInt32(reader("SessionItemId")),
                            .ExtraId = Convert.ToInt32(reader("ExtraId")),
                            .ExtraName = reader("ExtraName").ToString(),
                            .Price = Convert.ToDouble(reader("Price"))
                        })
                    End While
                End Using
            End Using
            Return extras
        End Function

        Public Sub SaveSessionCart(session As ChairSession)
            If session Is Nothing Then Return

            ' If session is new (Id <= 0), create it in DB first to obtain a valid ChairSessions(Id) FK
            If session.Id <= 0 Then
                session.Id = CreateSession(session.ChairId, session.EmployeeId, session.CustomerId)
            End If

            Using conn = DbConnectionFactory.CreateConnection()
                Using trans = conn.BeginTransaction()
                    Try
                        ' Delete existing items and extras
                        Using cmdDel = conn.CreateCommand()
                            cmdDel.Transaction = trans
                            cmdDel.CommandText = "
                                DELETE FROM ChairSessionItemExtras WHERE SessionItemId IN (SELECT Id FROM ChairSessionItems WHERE SessionId = @sid);
                                DELETE FROM ChairSessionItems WHERE SessionId = @sid;
                            "
                            cmdDel.Parameters.AddWithValue("@sid", session.Id)
                            cmdDel.ExecuteNonQuery()
                        End Using

                        Dim totalSessionAmount As Double = 0.0

                        ' Insert new items and extras
                        For Each item In session.Items
                            Dim itemId As Integer
                            Using cmdItem = conn.CreateCommand()
                                cmdItem.Transaction = trans
                                cmdItem.CommandText = "
                                    INSERT INTO ChairSessionItems (SessionId, ServiceId, ServiceName, Price, Quantity, EmployeeId)
                                    VALUES (@sid, @srvId, @srvName, @price, @qty, @empId);
                                    SELECT last_insert_rowid();
                                "
                                cmdItem.Parameters.AddWithValue("@sid", session.Id)
                                cmdItem.Parameters.AddWithValue("@srvId", item.ServiceId)
                                cmdItem.Parameters.AddWithValue("@srvName", item.ServiceName)
                                cmdItem.Parameters.AddWithValue("@price", item.Price)
                                cmdItem.Parameters.AddWithValue("@qty", item.Quantity)
                                cmdItem.Parameters.AddWithValue("@empId", If(item.EmployeeId.HasValue, item.EmployeeId.Value, DBNull.Value))
                                itemId = Convert.ToInt32(cmdItem.ExecuteScalar())
                            End Using

                            For Each ex In item.Extras
                                Using cmdEx = conn.CreateCommand()
                                    cmdEx.Transaction = trans
                                    cmdEx.CommandText = "INSERT INTO ChairSessionItemExtras (SessionItemId, ExtraId, ExtraName, Price) VALUES (@iid, @exId, @exName, @price);"
                                    cmdEx.Parameters.AddWithValue("@iid", itemId)
                                    cmdEx.Parameters.AddWithValue("@exId", ex.ExtraId)
                                    cmdEx.Parameters.AddWithValue("@exName", ex.ExtraName)
                                    cmdEx.Parameters.AddWithValue("@price", ex.Price)
                                    cmdEx.ExecuteNonQuery()
                                End Using
                            Next

                            totalSessionAmount += item.LineTotal
                        Next

                        ' Update Session TotalAmount and Status
                        Using cmdUpd = conn.CreateCommand()
                            cmdUpd.Transaction = trans
                            cmdUpd.CommandText = "UPDATE ChairSessions SET TotalAmount = @tot, Status = @st WHERE Id = @sid;"
                            cmdUpd.Parameters.AddWithValue("@tot", totalSessionAmount)
                            cmdUpd.Parameters.AddWithValue("@st", session.Status.ToString())
                            cmdUpd.Parameters.AddWithValue("@sid", session.Id)
                            cmdUpd.ExecuteNonQuery()
                        End Using

                        trans.Commit()
                    Catch
                        trans.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Public Sub CloseSession(sessionId As Integer)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "DELETE FROM ChairSessionItemExtras WHERE SessionItemId IN (SELECT Id FROM ChairSessionItems WHERE SessionId = @sid); DELETE FROM ChairSessionItems WHERE SessionId = @sid; DELETE FROM ChairSessions WHERE Id = @sid;"
                    cmd.Parameters.AddWithValue("@sid", sessionId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Class

End Namespace
