Imports System
Imports System.Collections.Generic
Imports System.Data.SQLite
Imports SalonPOS.Models

Namespace SalonPOS.Data.Repositories

    Public Class EmployeeHRRepository

        ' =========================================================================
        ' 1. EMPLOYEE MASTER CRUD
        ' =========================================================================

        Public Function GetAllEmployees(Optional activeOnly As Boolean = False) As List(Of Employee)
            Dim list As New List(Of Employee)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    Dim sql As String = "SELECT Id, Name, Phone, JobTitle, BaseSalary, CommissionRate, HireDate, IsActive, CreatedAt FROM Employees "
                    If activeOnly Then
                        sql &= "WHERE IsActive = 1 "
                    End If
                    sql &= "ORDER BY IsActive DESC, Name ASC;"
                    cmd.CommandText = sql

                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            list.Add(MapEmployee(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Function GetEmployeeById(id As Integer) As Employee
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, Name, Phone, JobTitle, BaseSalary, CommissionRate, HireDate, IsActive, CreatedAt FROM Employees WHERE Id = @id;"
                    cmd.Parameters.AddWithValue("@id", id)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Return MapEmployee(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Function SaveEmployee(emp As Employee) As Integer
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    If emp.Id = 0 Then
                        cmd.CommandText = "
INSERT INTO Employees (Name, Phone, JobTitle, BaseSalary, CommissionRate, HireDate, IsActive, CreatedAt)
VALUES (@n, @p, @j, @b, @c, @h, @a, @created);
SELECT last_insert_rowid();
"
                    Else
                        cmd.CommandText = "
UPDATE Employees 
SET Name = @n, Phone = @p, JobTitle = @j, BaseSalary = @b, CommissionRate = @c, HireDate = @h, IsActive = @a 
WHERE Id = @id;
"
                        cmd.Parameters.AddWithValue("@id", emp.Id)
                    End If

                    cmd.Parameters.AddWithValue("@n", emp.Name.Trim())
                    cmd.Parameters.AddWithValue("@p", If(String.IsNullOrWhiteSpace(emp.Phone), "", emp.Phone.Trim()))
                    cmd.Parameters.AddWithValue("@j", If(String.IsNullOrWhiteSpace(emp.JobTitle), "مصفف / حلاق", emp.JobTitle.Trim()))
                    cmd.Parameters.AddWithValue("@b", emp.BaseSalary)
                    cmd.Parameters.AddWithValue("@c", emp.CommissionRate)
                    cmd.Parameters.AddWithValue("@h", If(String.IsNullOrWhiteSpace(emp.HireDate), DateTime.Now.ToString("yyyy-MM-dd"), emp.HireDate))
                    cmd.Parameters.AddWithValue("@a", If(emp.IsActive, 1, 0))
                    cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))

                    If emp.Id = 0 Then
                        emp.Id = Convert.ToInt32(cmd.ExecuteScalar())
                    Else
                        cmd.ExecuteNonQuery()
                    End If
                    Return emp.Id
                End Using
            End Using
        End Function

        Public Sub DeleteEmployee(id As Integer)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    ' Soft-delete to preserve foreign key historical data
                    cmd.CommandText = "UPDATE Employees SET IsActive = 0 WHERE Id = @id;"
                    cmd.Parameters.AddWithValue("@id", id)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Private Function MapEmployee(reader As IDataReader) As Employee
            Dim emp As New Employee()
            emp.Id = Convert.ToInt32(reader("Id"))
            emp.Name = reader("Name").ToString()
            emp.Phone = If(reader.IsDBNull(reader.GetOrdinal("Phone")), "", reader("Phone").ToString())
            emp.CommissionRate = Convert.ToDouble(reader("CommissionRate"))
            emp.IsActive = DbConnectionFactory.ToBool(reader("IsActive"))
            emp.CreatedAt = Convert.ToDateTime(reader("CreatedAt"))

            If Not reader.IsDBNull(reader.GetOrdinal("JobTitle")) Then
                emp.JobTitle = reader("JobTitle").ToString()
            Else
                emp.JobTitle = "مصفف / حلاق"
            End If

            If Not reader.IsDBNull(reader.GetOrdinal("BaseSalary")) Then
                emp.BaseSalary = Convert.ToDouble(reader("BaseSalary"))
            Else
                emp.BaseSalary = 0.0
            End If

            If Not reader.IsDBNull(reader.GetOrdinal("HireDate")) Then
                emp.HireDate = reader("HireDate").ToString()
            Else
                emp.HireDate = ""
            End If

            Return emp
        End Function

        ' =========================================================================
        ' 2. ATTENDANCE & TIME TRACKING
        ' =========================================================================

        Public Function GetAttendanceForDate(attendanceDate As String) As List(Of EmployeeAttendance)
            Dim list As New List(Of EmployeeAttendance)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    ' Join Active Employees with Attendance for that day, so all employees show up even if not checked-in yet
                    cmd.CommandText = "
SELECT e.Id AS EmpId, e.Name AS EmpName, e.JobTitle,
       a.Id AS AttId, a.AttendanceDate, a.CheckInTime, a.CheckOutTime, a.Status, a.WorkHours, a.Notes, a.CreatedAt
FROM Employees e
LEFT JOIN EmployeeAttendance a ON e.Id = a.EmployeeId AND a.AttendanceDate = @date
WHERE e.IsActive = 1
ORDER BY e.Name ASC;
"
                    cmd.Parameters.AddWithValue("@date", attendanceDate)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim att As New EmployeeAttendance()
                            att.EmployeeId = Convert.ToInt32(reader("EmpId"))
                            att.EmployeeName = reader("EmpName").ToString()
                            att.AttendanceDate = attendanceDate

                            If Not reader.IsDBNull(reader.GetOrdinal("AttId")) Then
                                att.Id = Convert.ToInt32(reader("AttId"))
                                att.CheckInTime = If(reader.IsDBNull(reader.GetOrdinal("CheckInTime")), "", reader("CheckInTime").ToString())
                                att.CheckOutTime = If(reader.IsDBNull(reader.GetOrdinal("CheckOutTime")), "", reader("CheckOutTime").ToString())
                                att.Status = If(reader.IsDBNull(reader.GetOrdinal("Status")), "حاضر", reader("Status").ToString())
                                att.WorkHours = If(reader.IsDBNull(reader.GetOrdinal("WorkHours")), 0.0, Convert.ToDouble(reader("WorkHours")))
                                att.Notes = If(reader.IsDBNull(reader.GetOrdinal("Notes")), "", reader("Notes").ToString())
                            Else
                                att.Id = 0
                                att.CheckInTime = ""
                                att.CheckOutTime = ""
                                att.Status = "لم يسجل"
                                att.WorkHours = 0.0
                                att.Notes = ""
                            End If
                            list.Add(att)
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Sub SaveAttendance(att As EmployeeAttendance)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    If att.Id = 0 Then
                        ' Check if record already exists for this employee and date
                        cmd.CommandText = "SELECT Id FROM EmployeeAttendance WHERE EmployeeId = @empId AND AttendanceDate = @date;"
                        cmd.Parameters.AddWithValue("@empId", att.EmployeeId)
                        cmd.Parameters.AddWithValue("@date", att.AttendanceDate)
                        Dim existingId = cmd.ExecuteScalar()
                        If existingId IsNot Nothing AndAlso Not DBNull.Value.Equals(existingId) Then
                            att.Id = Convert.ToInt32(existingId)
                        End If
                    End If

                    cmd.Parameters.Clear()
                    If att.Id = 0 Then
                        cmd.CommandText = "
INSERT INTO EmployeeAttendance (EmployeeId, AttendanceDate, CheckInTime, CheckOutTime, Status, WorkHours, Notes, CreatedAt)
VALUES (@empId, @date, @in, @out, @st, @wh, @notes, @created);
SELECT last_insert_rowid();
"
                    Else
                        cmd.CommandText = "
UPDATE EmployeeAttendance
SET CheckInTime = @in, CheckOutTime = @out, Status = @st, WorkHours = @wh, Notes = @notes
WHERE Id = @id;
"
                        cmd.Parameters.AddWithValue("@id", att.Id)
                    End If

                    cmd.Parameters.AddWithValue("@empId", att.EmployeeId)
                    cmd.Parameters.AddWithValue("@date", att.AttendanceDate)
                    cmd.Parameters.AddWithValue("@in", If(String.IsNullOrWhiteSpace(att.CheckInTime), DBNull.Value, CObj(att.CheckInTime)))
                    cmd.Parameters.AddWithValue("@out", If(String.IsNullOrWhiteSpace(att.CheckOutTime), DBNull.Value, CObj(att.CheckOutTime)))
                    cmd.Parameters.AddWithValue("@st", att.Status)
                    cmd.Parameters.AddWithValue("@wh", att.WorkHours)
                    cmd.Parameters.AddWithValue("@notes", If(att.Notes, ""))
                    cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))

                    If att.Id = 0 Then
                        att.Id = Convert.ToInt32(cmd.ExecuteScalar())
                    Else
                        cmd.ExecuteNonQuery()
                    End If
                End Using
            End Using
        End Sub

        Public Sub RecordQuickCheckIn(employeeId As Integer, attendanceDate As String)
            Dim nowTime = DateTime.Now.ToString("hh:mm tt")
            Dim attList = GetAttendanceForDate(attendanceDate)
            Dim current = attList.Find(Function(x) x.EmployeeId = employeeId)
            If current Is Nothing Then
                current = New EmployeeAttendance With {
                    .EmployeeId = employeeId,
                    .AttendanceDate = attendanceDate,
                    .CheckInTime = nowTime,
                    .Status = "حاضر"
                }
            Else
                current.CheckInTime = nowTime
                current.Status = "حاضر"
            End If
            CalculateHours(current)
            SaveAttendance(current)
        End Sub

        Public Sub RecordQuickCheckOut(employeeId As Integer, attendanceDate As String)
            Dim nowTime = DateTime.Now.ToString("hh:mm tt")
            Dim attList = GetAttendanceForDate(attendanceDate)
            Dim current = attList.Find(Function(x) x.EmployeeId = employeeId)
            If current Is Nothing Then
                current = New EmployeeAttendance With {
                    .EmployeeId = employeeId,
                    .AttendanceDate = attendanceDate,
                    .CheckOutTime = nowTime,
                    .Status = "حاضر"
                }
            Else
                current.CheckOutTime = nowTime
                If current.Status = "لم يسجل" Then current.Status = "حاضر"
            End If
            CalculateHours(current)
            SaveAttendance(current)
        End Sub

        Public Sub MarkAllPresent(attendanceDate As String)
            Dim employees = GetAllEmployees(activeOnly:=True)
            Dim defaultCheckIn = "10:00 ص"
            For Each emp In employees
                Dim att As New EmployeeAttendance With {
                    .EmployeeId = emp.Id,
                    .AttendanceDate = attendanceDate,
                    .CheckInTime = defaultCheckIn,
                    .Status = "حاضر",
                    .WorkHours = 8.0,
                    .Notes = "تسجيل جماعي"
                }
                SaveAttendance(att)
            Next
        End Sub

        Private Sub CalculateHours(att As EmployeeAttendance)
            If Not String.IsNullOrWhiteSpace(att.CheckInTime) AndAlso Not String.IsNullOrWhiteSpace(att.CheckOutTime) Then
                Dim t1 As DateTime, t2 As DateTime
                If DateTime.TryParse(att.CheckInTime, t1) AndAlso DateTime.TryParse(att.CheckOutTime, t2) Then
                    Dim diff = t2 - t1
                    If diff.TotalHours < 0 Then diff = diff.Add(TimeSpan.FromHours(24))
                    att.WorkHours = Math.Round(diff.TotalHours, 1)
                End If
            End If
        End Sub

        Public Function GetMonthlyAttendanceStats(employeeId As Integer, yearMonth As String) As Tuple(Of Integer, Integer, Integer)
            ' Returns (DaysPresent, DaysAbsent, DaysLate)
            Dim present As Integer = 0
            Dim absent As Integer = 0
            Dim late As Integer = 0

            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
SELECT Status, COUNT(*) AS Cnt
FROM EmployeeAttendance
WHERE EmployeeId = @empId AND substr(AttendanceDate, 1, 7) = @ym
GROUP BY Status;
"
                    cmd.Parameters.AddWithValue("@empId", employeeId)
                    cmd.Parameters.AddWithValue("@ym", yearMonth)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim st = reader("Status").ToString()
                            Dim c = Convert.ToInt32(reader("Cnt"))
                            Select Case st
                                Case "حاضر"
                                    present += c
                                Case "غائب"
                                    absent += c
                                Case "متأخر"
                                    late += c
                            End Select
                        End While
                    End Using
                End Using
            End Using
            Return Tuple.Create(present, absent, late)
        End Function

        ' =========================================================================
        ' 3. ADVANCES (السلف)
        ' =========================================================================

        Public Function GetAdvances(Optional employeeId As Integer? = Nothing, Optional pendingOnly As Boolean = False) As List(Of EmployeeAdvance)
            Dim list As New List(Of EmployeeAdvance)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    Dim sql As String = "
SELECT a.Id, a.EmployeeId, e.Name AS EmployeeName, a.AdvanceDate, a.Amount, a.Notes, a.IsDeducted, a.DeductedPayrollId, a.CreatedAt
FROM EmployeeAdvances a
INNER JOIN Employees e ON a.EmployeeId = e.Id
WHERE 1=1
"
                    If employeeId.HasValue AndAlso employeeId.Value > 0 Then
                        sql &= " AND a.EmployeeId = @empId"
                        cmd.Parameters.AddWithValue("@empId", employeeId.Value)
                    End If
                    If pendingOnly Then
                        sql &= " AND a.IsDeducted = 0"
                    End If
                    sql &= " ORDER BY a.AdvanceDate DESC, a.Id DESC;"
                    cmd.CommandText = sql

                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim adv As New EmployeeAdvance()
                            adv.Id = Convert.ToInt32(reader("Id"))
                            adv.EmployeeId = Convert.ToInt32(reader("EmployeeId"))
                            adv.EmployeeName = reader("EmployeeName").ToString()
                            adv.AdvanceDate = reader("AdvanceDate").ToString()
                            adv.Amount = Convert.ToDouble(reader("Amount"))
                            adv.Notes = If(reader.IsDBNull(reader.GetOrdinal("Notes")), "", reader("Notes").ToString())
                            adv.IsDeducted = DbConnectionFactory.ToBool(reader("IsDeducted"))
                            If Not reader.IsDBNull(reader.GetOrdinal("DeductedPayrollId")) Then
                                adv.DeductedPayrollId = Convert.ToInt32(reader("DeductedPayrollId"))
                            End If
                            adv.CreatedAt = Convert.ToDateTime(reader("CreatedAt"))
                            list.Add(adv)
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Function SaveAdvance(adv As EmployeeAdvance, Optional createExpense As Boolean = False, Optional currentShiftId As Integer = 0, Optional currentUserId As Integer = 1) As Integer
            Using conn = DbConnectionFactory.CreateConnection()
                conn.Open()
                Using trans = conn.BeginTransaction()
                    Try
                        Using cmd = conn.CreateCommand()
                            cmd.Transaction = trans
                            If adv.Id = 0 Then
                                cmd.CommandText = "
INSERT INTO EmployeeAdvances (EmployeeId, AdvanceDate, Amount, Notes, IsDeducted, DeductedPayrollId, CreatedAt)
VALUES (@empId, @date, @amount, @notes, @ded, @payId, @created);
SELECT last_insert_rowid();
"
                            Else
                                cmd.CommandText = "
UPDATE EmployeeAdvances
SET EmployeeId = @empId, AdvanceDate = @date, Amount = @amount, Notes = @notes, IsDeducted = @ded, DeductedPayrollId = @payId
WHERE Id = @id;
"
                                cmd.Parameters.AddWithValue("@id", adv.Id)
                            End If

                            cmd.Parameters.AddWithValue("@empId", adv.EmployeeId)
                            cmd.Parameters.AddWithValue("@date", adv.AdvanceDate)
                            cmd.Parameters.AddWithValue("@amount", adv.Amount)
                            cmd.Parameters.AddWithValue("@notes", If(adv.Notes, ""))
                            cmd.Parameters.AddWithValue("@ded", If(adv.IsDeducted, 1, 0))
                            cmd.Parameters.AddWithValue("@payId", If(adv.DeductedPayrollId.HasValue, CObj(adv.DeductedPayrollId.Value), DBNull.Value))
                            cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))

                            If adv.Id = 0 Then
                                adv.Id = Convert.ToInt32(cmd.ExecuteScalar())
                            Else
                                cmd.ExecuteNonQuery()
                            End If
                        End Using

                        ' Optionally insert into Expenses so drawer cash balances accurately
                        If createExpense AndAlso adv.Amount > 0 AndAlso currentShiftId > 0 Then
                            Using cmdExp = conn.CreateCommand()
                                cmdExp.Transaction = trans
                                cmdExp.CommandText = "
INSERT INTO Expenses (Amount, Category, Notes, ShiftId, CreatedByUserId, CreatedAt)
VALUES (@amt, 'سلف موظفين', @notes, @shift, @user, @created);
"
                                cmdExp.Parameters.AddWithValue("@amt", adv.Amount)
                                cmdExp.Parameters.AddWithValue("@notes", $"سلفة للموظف {adv.EmployeeName}: {adv.Notes}")
                                cmdExp.Parameters.AddWithValue("@shift", currentShiftId)
                                cmdExp.Parameters.AddWithValue("@user", currentUserId)
                                cmdExp.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                                cmdExp.ExecuteNonQuery()
                            End Using
                        End If

                        trans.Commit()
                        Return adv.Id
                    Catch
                        trans.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Function DeleteAdvance(id As Integer) As Boolean
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    ' Only delete if not yet deducted from payroll
                    cmd.CommandText = "DELETE FROM EmployeeAdvances WHERE Id = @id AND IsDeducted = 0;"
                    cmd.Parameters.AddWithValue("@id", id)
                    Dim rows = cmd.ExecuteNonQuery()
                    Return rows > 0
                End Using
            End Using
        End Function

        ' =========================================================================
        ' 4. PAYROLL & COMMISSION CALCULATIONS
        ' =========================================================================

        Public Function GetEmployeeCommissionsForMonth(employeeId As Integer, yearMonth As String) As Tuple(Of Double, Double)
            ' Returns (TotalSalesAmount, TotalCommissions)
            Dim totalSales As Double = 0.0
            Dim totalComm As Double = 0.0

            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
SELECT IFNULL(SUM(si.SubTotal), 0) AS TotalSales,
       IFNULL(SUM(si.EmployeeCommissionAmount), 0) AS TotalCommission
FROM SaleItems si
INNER JOIN Sales s ON si.SaleId = s.Id
WHERE si.EmployeeId = @empId AND substr(s.CreatedAt, 1, 7) = @ym;
"
                    cmd.Parameters.AddWithValue("@empId", employeeId)
                    cmd.Parameters.AddWithValue("@ym", yearMonth)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            totalSales = Convert.ToDouble(reader("TotalSales"))
                            totalComm = Convert.ToDouble(reader("TotalCommission"))
                        End If
                    End Using
                End Using
            End Using
            Return Tuple.Create(totalSales, totalComm)
        End Function

        Public Function GetEmployeePendingAdvances(employeeId As Integer) As Double
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT IFNULL(SUM(Amount), 0) FROM EmployeeAdvances WHERE EmployeeId = @empId AND IsDeducted = 0;"
                    cmd.Parameters.AddWithValue("@empId", employeeId)
                    Dim res = cmd.ExecuteScalar()
                    Return If(res IsNot Nothing AndAlso Not DBNull.Value.Equals(res), Convert.ToDouble(res), 0.0)
                End Using
            End Using
        End Function

        Public Function GetEmployeePendingAdvanceIds(employeeId As Integer) As List(Of Integer)
            Dim ids As New List(Of Integer)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id FROM EmployeeAdvances WHERE EmployeeId = @empId AND IsDeducted = 0;"
                    cmd.Parameters.AddWithValue("@empId", employeeId)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            ids.Add(Convert.ToInt32(reader("Id")))
                        End While
                    End Using
                End Using
            End Using
            Return ids
        End Function

        Public Function GetAllEmployeesPayrollSummary(yearMonth As String) As List(Of EmployeePayrollSummaryDto)
            Dim list As New List(Of EmployeePayrollSummaryDto)()
            Dim employees = GetAllEmployees(activeOnly:=True)

            For Each emp In employees
                Dim commData = GetEmployeeCommissionsForMonth(emp.Id, yearMonth)
                Dim pendingAdvances = GetEmployeePendingAdvances(emp.Id)
                Dim attStats = GetMonthlyAttendanceStats(emp.Id, yearMonth)

                Dim dto As New EmployeePayrollSummaryDto With {
                    .EmployeeId = emp.Id,
                    .EmployeeName = emp.Name,
                    .JobTitle = emp.JobTitle,
                    .BaseSalary = emp.BaseSalary,
                    .CommissionRate = emp.CommissionRate,
                    .TotalSalesAmount = commData.Item1,
                    .TotalCommissions = commData.Item2,
                    .TotalPendingAdvances = pendingAdvances,
                    .DaysPresent = attStats.Item1,
                    .DaysAbsent = attStats.Item2,
                    .DaysLate = attStats.Item3,
                    .SuggestedNetSalary = Math.Max(0, (emp.BaseSalary + commData.Item2) - pendingAdvances)
                }
                list.Add(dto)
            Next

            Return list
        End Function

        ' =========================================================================
        ' 5. PAYROLL SETTLEMENT & CLOSING
        ' =========================================================================

        Public Function ProcessPayroll(payroll As EmployeePayroll, advanceIdsToDeduct As List(Of Integer)) As Integer
            Using conn = DbConnectionFactory.CreateConnection()
                conn.Open()
                Using trans = conn.BeginTransaction()
                    Try
                        ' 1. Insert Payroll Record
                        Dim payrollId As Integer
                        Using cmd = conn.CreateCommand()
                            cmd.Transaction = trans
                            cmd.CommandText = "
INSERT INTO EmployeePayrolls (EmployeeId, PayrollMonth, BaseSalary, TotalCommission, TotalBonuses, TotalAdvances, TotalDeductions, NetSalary, PaidAmount, PaymentDate, PaymentMethod, Status, Notes, CreatedAt)
VALUES (@empId, @ym, @base, @comm, @bonuses, @adv, @ded, @net, @paid, @payDate, @method, @st, @notes, @created);
SELECT last_insert_rowid();
"
                            cmd.Parameters.AddWithValue("@empId", payroll.EmployeeId)
                            cmd.Parameters.AddWithValue("@ym", payroll.PayrollMonth)
                            cmd.Parameters.AddWithValue("@base", payroll.BaseSalary)
                            cmd.Parameters.AddWithValue("@comm", payroll.TotalCommission)
                            cmd.Parameters.AddWithValue("@bonuses", payroll.TotalBonuses)
                            cmd.Parameters.AddWithValue("@adv", payroll.TotalAdvances)
                            cmd.Parameters.AddWithValue("@ded", payroll.TotalDeductions)
                            cmd.Parameters.AddWithValue("@net", payroll.NetSalary)
                            cmd.Parameters.AddWithValue("@paid", payroll.PaidAmount)
                            cmd.Parameters.AddWithValue("@payDate", If(String.IsNullOrWhiteSpace(payroll.PaymentDate), DateTime.Now.ToString("yyyy-MM-dd"), payroll.PaymentDate))
                            cmd.Parameters.AddWithValue("@method", payroll.PaymentMethod)
                            cmd.Parameters.AddWithValue("@st", payroll.Status)
                            cmd.Parameters.AddWithValue("@notes", If(payroll.Notes, ""))
                            cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))

                            payrollId = Convert.ToInt32(cmd.ExecuteScalar())
                            payroll.Id = payrollId
                        End Using

                        ' 2. Mark Deducted Advances
                        If advanceIdsToDeduct IsNot Nothing AndAlso advanceIdsToDeduct.Count > 0 Then
                            For Each advId In advanceIdsToDeduct
                                Using cmdAdv = conn.CreateCommand()
                                    cmdAdv.Transaction = trans
                                    cmdAdv.CommandText = "UPDATE EmployeeAdvances SET IsDeducted = 1, DeductedPayrollId = @payId WHERE Id = @advId;"
                                    cmdAdv.Parameters.AddWithValue("@payId", payrollId)
                                    cmdAdv.Parameters.AddWithValue("@advId", advId)
                                    cmdAdv.ExecuteNonQuery()
                                End Using
                            Next
                        End If

                        trans.Commit()
                        Return payrollId
                    Catch
                        trans.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Function GetPayrollsForMonth(yearMonth As String) As List(Of EmployeePayroll)
            Dim list As New List(Of EmployeePayroll)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
SELECT p.Id, p.EmployeeId, e.Name AS EmployeeName, e.JobTitle, p.PayrollMonth,
       p.BaseSalary, p.TotalCommission, p.TotalBonuses, p.TotalAdvances, p.TotalDeductions,
       p.NetSalary, p.PaidAmount, p.PaymentDate, p.PaymentMethod, p.Status, p.Notes, p.CreatedAt
FROM EmployeePayrolls p
INNER JOIN Employees e ON p.EmployeeId = e.Id
WHERE p.PayrollMonth = @ym
ORDER BY p.Id DESC;
"
                    cmd.Parameters.AddWithValue("@ym", yearMonth)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim p As New EmployeePayroll()
                            p.Id = Convert.ToInt32(reader("Id"))
                            p.EmployeeId = Convert.ToInt32(reader("EmployeeId"))
                            p.EmployeeName = reader("EmployeeName").ToString()
                            p.JobTitle = If(reader.IsDBNull(reader.GetOrdinal("JobTitle")), "", reader("JobTitle").ToString())
                            p.PayrollMonth = reader("PayrollMonth").ToString()
                            p.BaseSalary = Convert.ToDouble(reader("BaseSalary"))
                            p.TotalCommission = Convert.ToDouble(reader("TotalCommission"))
                            p.TotalBonuses = Convert.ToDouble(reader("TotalBonuses"))
                            p.TotalAdvances = Convert.ToDouble(reader("TotalAdvances"))
                            p.TotalDeductions = Convert.ToDouble(reader("TotalDeductions"))
                            p.NetSalary = Convert.ToDouble(reader("NetSalary"))
                            p.PaidAmount = Convert.ToDouble(reader("PaidAmount"))
                            p.PaymentDate = If(reader.IsDBNull(reader.GetOrdinal("PaymentDate")), "", reader("PaymentDate").ToString())
                            p.PaymentMethod = reader("PaymentMethod").ToString()
                            p.Status = reader("Status").ToString()
                            p.Notes = If(reader.IsDBNull(reader.GetOrdinal("Notes")), "", reader("Notes").ToString())
                            p.CreatedAt = Convert.ToDateTime(reader("CreatedAt"))
                            list.Add(p)
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Function DeletePayroll(payrollId As Integer) As Boolean
            Using conn = DbConnectionFactory.CreateConnection()
                conn.Open()
                Using trans = conn.BeginTransaction()
                    Try
                        ' 1. Unmark advances
                        Using cmdAdv = conn.CreateCommand()
                            cmdAdv.Transaction = trans
                            cmdAdv.CommandText = "UPDATE EmployeeAdvances SET IsDeducted = 0, DeductedPayrollId = NULL WHERE DeductedPayrollId = @payId;"
                            cmdAdv.Parameters.AddWithValue("@payId", payrollId)
                            cmdAdv.ExecuteNonQuery()
                        End Using

                        ' 2. Delete payroll
                        Using cmdPay = conn.CreateCommand()
                            cmdPay.Transaction = trans
                            cmdPay.CommandText = "DELETE FROM EmployeePayrolls WHERE Id = @payId;"
                            cmdPay.Parameters.AddWithValue("@payId", payrollId)
                            cmdPay.ExecuteNonQuery()
                        End Using

                        trans.Commit()
                        Return True
                    Catch
                        trans.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

    End Class

End Namespace
