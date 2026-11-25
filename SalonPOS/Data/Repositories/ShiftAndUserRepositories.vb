Imports System
Imports System.Collections.Generic
Imports System.Data.SQLite
Imports SalonPOS.Models

Namespace SalonPOS.Data.Repositories

    Public Class ShiftRepository

        Public Function GetActiveShift(userId As Integer) As Shift
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        SELECT s.Id, s.UserId, u.DisplayName AS UserName, s.StartedAt, s.EndedAt, s.OpeningCash,
                               s.CashSales, s.CardSales, s.TransferSales, s.TotalExpenses, s.ExpectedCash,
                               s.ActualCash, s.Variance, s.Status
                        FROM Shifts s
                        INNER JOIN Users u ON s.UserId = u.Id
                        WHERE s.Status = 'OPEN'
                        ORDER BY s.Id DESC LIMIT 1;
                    "
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Return MapShift(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Function OpenShift(userId As Integer, openingCash As Double) As Shift
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        INSERT INTO Shifts (UserId, StartedAt, OpeningCash, CashSales, CardSales, TransferSales, TotalExpenses, ExpectedCash, Status)
                        VALUES (@u, @start, @op, 0.0, 0.0, 0.0, 0.0, @op, 'OPEN');
                        SELECT last_insert_rowid();
                    "
                    cmd.Parameters.AddWithValue("@u", userId)
                    cmd.Parameters.AddWithValue("@start", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    cmd.Parameters.AddWithValue("@op", openingCash)
                    Dim newId = Convert.ToInt32(cmd.ExecuteScalar())
                    Dim uName As String = If(Services.AuthService.CurrentUser IsNot Nothing, Services.AuthService.CurrentUser.DisplayName, "مستخدم")
                    ActivityLogRepository.Log(userId, uName, "بدء وردية", $"تم فتح وردية جديدة رقم {newId} بمبلغ افتتاح قدره: {openingCash:N2} ج.م")
                    Return GetActiveShift(userId)
                End Using
            End Using
        End Function

        Public Sub CloseShift(shiftId As Integer, actualCash As Double)
            Using conn = DbConnectionFactory.CreateConnection()
                Dim activeShift = GetShiftById(conn, shiftId)
                If activeShift Is Nothing Then Return

                Dim expectedCash As Double = activeShift.OpeningCash + activeShift.CashSales - activeShift.TotalExpenses
                Dim variance As Double = actualCash - expectedCash

                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        UPDATE Shifts
                        SET EndedAt = @end,
                            ExpectedCash = @exp,
                            ActualCash = @act,
                            Variance = @var,
                            Status = 'CLOSED'
                        WHERE Id = @sid;
                    "
                    cmd.Parameters.AddWithValue("@end", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    cmd.Parameters.AddWithValue("@exp", expectedCash)
                    cmd.Parameters.AddWithValue("@act", actualCash)
                    cmd.Parameters.AddWithValue("@var", variance)
                    cmd.Parameters.AddWithValue("@sid", shiftId)
                    cmd.ExecuteNonQuery()
                End Using

                Dim uName As String = If(Services.AuthService.CurrentUser IsNot Nothing, Services.AuthService.CurrentUser.DisplayName, "مستخدم")
                Dim uId As Integer = If(Services.AuthService.CurrentUser IsNot Nothing, Services.AuthService.CurrentUser.Id, activeShift.UserId)
                ActivityLogRepository.Log(uId, uName, "إغلاق وردية", $"تم إغلاق الوردية رقم {shiftId} - درج متوقع: {expectedCash:N2} ج.م - درج فعلي: {actualCash:N2} ج.م (عجز/زيادة: {variance:N2} ج.م)")
            End Using
        End Sub

        Public Function GetAllShifts(fromDate As DateTime?, toDate As DateTime?, Optional userId As Integer? = Nothing) As List(Of Shift)
            Dim list As New List(Of Shift)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    Dim sql As String = "
                        SELECT s.Id, s.UserId, u.DisplayName AS UserName, s.StartedAt, s.EndedAt, s.OpeningCash,
                               s.CashSales, s.CardSales, s.TransferSales, s.TotalExpenses, s.ExpectedCash,
                               s.ActualCash, s.Variance, s.Status
                        FROM Shifts s
                        INNER JOIN Users u ON s.UserId = u.Id
                    "
                    Dim conds As New List(Of String)()
                    If fromDate.HasValue Then
                        conds.Add("s.StartedAt >= @fromDate")
                        cmd.Parameters.AddWithValue("@fromDate", fromDate.Value.ToString("yyyy-MM-dd 00:00:00"))
                    End If
                    If toDate.HasValue Then
                        conds.Add("s.StartedAt <= @toDate")
                        cmd.Parameters.AddWithValue("@toDate", toDate.Value.ToString("yyyy-MM-dd 23:59:59"))
                    End If
                    If userId.HasValue AndAlso userId.Value > 0 Then
                        conds.Add("s.UserId = @uid")
                        cmd.Parameters.AddWithValue("@uid", userId.Value)
                    End If

                    If conds.Count > 0 Then
                        sql &= " WHERE " & String.Join(" AND ", conds)
                    End If

                    sql &= " ORDER BY s.Id DESC;"
                    cmd.CommandText = sql

                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            list.Add(MapShift(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Private Function GetShiftById(conn As SQLiteConnection, shiftId As Integer) As Shift
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "
                    SELECT s.Id, s.UserId, u.DisplayName AS UserName, s.StartedAt, s.EndedAt, s.OpeningCash,
                           s.CashSales, s.CardSales, s.TransferSales, s.TotalExpenses, s.ExpectedCash,
                           s.ActualCash, s.Variance, s.Status
                    FROM Shifts s
                    INNER JOIN Users u ON s.UserId = u.Id
                    WHERE s.Id = @id;
                "
                cmd.Parameters.AddWithValue("@id", shiftId)
                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        Return MapShift(reader)
                    End If
                End Using
            End Using
            Return Nothing
        End Function

        Private Function MapShift(reader As SQLiteDataReader) As Shift
            Dim statusStr As String = DbConnectionFactory.ToStr(reader("Status")).Trim().ToUpper()
            Dim sStatus As ShiftStatus = ShiftStatus.OPEN
            If statusStr = "CLOSED" Then sStatus = ShiftStatus.CLOSED

            Dim s As New Shift With {
                .Id = DbConnectionFactory.ToInt(reader("Id")),
                .UserId = DbConnectionFactory.ToInt(reader("UserId")),
                .UserName = DbConnectionFactory.ToStr(reader("UserName")),
                .StartedAt = Convert.ToDateTime(reader("StartedAt")),
                .OpeningCash = DbConnectionFactory.ToDbl(reader("OpeningCash")),
                .CashSales = DbConnectionFactory.ToDbl(reader("CashSales")),
                .CardSales = DbConnectionFactory.ToDbl(reader("CardSales")),
                .TransferSales = DbConnectionFactory.ToDbl(reader("TransferSales")),
                .TotalExpenses = DbConnectionFactory.ToDbl(reader("TotalExpenses")),
                .ExpectedCash = DbConnectionFactory.ToDbl(reader("ExpectedCash")),
                .Status = sStatus
            }

            If Not reader.IsDBNull(reader.GetOrdinal("EndedAt")) Then
                s.EndedAt = Convert.ToDateTime(reader("EndedAt"))
            End If
            If Not reader.IsDBNull(reader.GetOrdinal("ActualCash")) Then
                s.ActualCash = DbConnectionFactory.ToDbl(reader("ActualCash"))
            End If
            If Not reader.IsDBNull(reader.GetOrdinal("Variance")) Then
                s.Variance = DbConnectionFactory.ToDbl(reader("Variance"))
            End If

            Return s
        End Function
    End Class

    Public Class ExpenseRepository
        Public Function GetTodayExpenses() As List(Of Expense)
            Dim list As New List(Of Expense)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, Amount, Category, Notes, ShiftId, CreatedByUserId, CreatedAt FROM Expenses WHERE date(CreatedAt) = date('now', 'localtime') ORDER BY Id DESC;"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            list.Add(New Expense With {
                                .Id = Convert.ToInt32(reader("Id")),
                                .Amount = Convert.ToDouble(reader("Amount")),
                                .Category = reader("Category").ToString(),
                                .Notes = If(reader.IsDBNull(reader.GetOrdinal("Notes")), "", reader("Notes").ToString()),
                                .ShiftId = Convert.ToInt32(reader("ShiftId")),
                                .CreatedByUserId = Convert.ToInt32(reader("CreatedByUserId")),
                                .CreatedAt = Convert.ToDateTime(reader("CreatedAt"))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Sub AddExpense(exp As Expense)
            If exp Is Nothing Then Return
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "INSERT INTO Expenses (Amount, Category, Notes, ShiftId, CreatedByUserId, CreatedAt) VALUES (@a, @c, @n, @s, @u, @created);"
                    cmd.Parameters.AddWithValue("@a", exp.Amount)
                    cmd.Parameters.AddWithValue("@c", If(exp.Category, "عمومية"))
                    cmd.Parameters.AddWithValue("@n", If(exp.Notes, ""))
                    cmd.Parameters.AddWithValue("@s", exp.ShiftId)
                    cmd.Parameters.AddWithValue("@u", exp.CreatedByUserId)
                    cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Class

    Public Class UserRepository

        Public Function Authenticate(username As String, password As String) As User
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, Username, PasswordHash, DisplayName, Role, IsActive, PermissionsJson, CreatedAt FROM Users WHERE Username = @u AND PasswordHash = @p AND IsActive = 1;"
                    cmd.Parameters.AddWithValue("@u", username.Trim())
                    cmd.Parameters.AddWithValue("@p", Helpers.SecurityHelper.HashPassword(password))

                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Return MapUser(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Function GetAllUsers() As List(Of User)
            Dim list As New List(Of User)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, Username, PasswordHash, DisplayName, Role, IsActive, PermissionsJson, CreatedAt FROM Users ORDER BY Id ASC;"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            list.Add(MapUser(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Sub SaveUser(u As User, newPassword As String)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    If u.Id = 0 Then
                        cmd.CommandText = "
                            INSERT INTO Users (Username, PasswordHash, DisplayName, Role, IsActive, PermissionsJson, CreatedAt)
                            VALUES (@u, @p, @d, @r, @a, @pj, @c);
                        "
                        cmd.Parameters.AddWithValue("@p", Helpers.SecurityHelper.HashPassword(If(String.IsNullOrEmpty(newPassword), "123456", newPassword)))
                        cmd.Parameters.AddWithValue("@c", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    Else
                        If Not String.IsNullOrEmpty(newPassword) Then
                            cmd.CommandText = "
                                UPDATE Users
                                SET Username = @u, PasswordHash = @p, DisplayName = @d, Role = @r, IsActive = @a, PermissionsJson = @pj
                                WHERE Id = @id;
                            "
                            cmd.Parameters.AddWithValue("@p", Helpers.SecurityHelper.HashPassword(newPassword))
                        Else
                            cmd.CommandText = "
                                UPDATE Users
                                SET Username = @u, DisplayName = @d, Role = @r, IsActive = @a, PermissionsJson = @pj
                                WHERE Id = @id;
                            "
                        End If
                        cmd.Parameters.AddWithValue("@id", u.Id)
                    End If

                    cmd.Parameters.AddWithValue("@u", u.Username.Trim())
                    cmd.Parameters.AddWithValue("@d", u.DisplayName.Trim())
                    cmd.Parameters.AddWithValue("@r", u.Role.ToString())
                    cmd.Parameters.AddWithValue("@a", If(u.IsActive, 1, 0))
                    cmd.Parameters.AddWithValue("@pj", If(u.PermissionsJson, ""))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub DeleteUser(userId As Integer)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "DELETE FROM Users WHERE Id = @id AND Username <> 'admin';"
                    cmd.Parameters.AddWithValue("@id", userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Private Function MapUser(reader As SQLiteDataReader) As User
            Dim roleStr As String = If(reader.IsDBNull(reader.GetOrdinal("Role")), "CASHIER", reader("Role").ToString().Trim().ToUpper())
            Dim uRole As UserRole = UserRole.CASHIER
            Select Case roleStr
                Case "ADMIN"
                    uRole = UserRole.ADMIN
                Case "MANAGER"
                    uRole = UserRole.MANAGER
                Case "CASHIER"
                    uRole = UserRole.CASHIER
                Case "EMPLOYEE"
                    uRole = UserRole.EMPLOYEE
            End Select

            Dim u As New User With {
                .Id = Convert.ToInt32(reader("Id")),
                .Username = reader("Username").ToString(),
                .PasswordHash = reader("PasswordHash").ToString(),
                .DisplayName = reader("DisplayName").ToString(),
                .Role = uRole,
                .IsActive = DbConnectionFactory.ToBool(reader("IsActive")),
                .CreatedAt = Convert.ToDateTime(reader("CreatedAt"))
            }

            If Not reader.IsDBNull(reader.GetOrdinal("PermissionsJson")) Then
                u.PermissionsJson = reader("PermissionsJson").ToString()
            End If

            Return u
        End Function
    End Class

End Namespace
