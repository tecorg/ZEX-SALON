Imports System

Namespace SalonPOS.Models

    Public Class EmployeeAttendance
        Public Property Id As Integer
        Public Property EmployeeId As Integer
        Public Property EmployeeName As String
        Public Property AttendanceDate As String ' yyyy-MM-dd
        Public Property CheckInTime As String   ' HH:mm:ss or hh:mm tt
        Public Property CheckOutTime As String  ' HH:mm:ss or hh:mm tt
        Public Property Status As String = "حاضر" ' حاضر, غائب, متأخر, إجازة
        Public Property WorkHours As Double = 0.0
        Public Property Notes As String = ""
        Public Property CreatedAt As DateTime = DateTime.Now
    End Class

    Public Class EmployeeAdvance
        Public Property Id As Integer
        Public Property EmployeeId As Integer
        Public Property EmployeeName As String
        Public Property AdvanceDate As String ' yyyy-MM-dd
        Public Property Amount As Double = 0.0
        Public Property Notes As String = ""
        Public Property IsDeducted As Boolean = False
        Public Property DeductedPayrollId As Integer?
        Public Property CreatedAt As DateTime = DateTime.Now
    End Class

    Public Class EmployeePayroll
        Public Property Id As Integer
        Public Property EmployeeId As Integer
        Public Property EmployeeName As String
        Public Property JobTitle As String = ""
        Public Property PayrollMonth As String ' yyyy-MM e.g. "2026-11"
        Public Property BaseSalary As Double = 0.0
        Public Property TotalCommission As Double = 0.0
        Public Property TotalBonuses As Double = 0.0
        Public Property TotalAdvances As Double = 0.0
        Public Property TotalDeductions As Double = 0.0
        Public Property NetSalary As Double = 0.0
        Public Property PaidAmount As Double = 0.0
        Public Property PaymentDate As String = ""
        Public Property PaymentMethod As String = "نقدي"
        Public Property Status As String = "مصروف" ' مسودة, معتمد, مصروف
        Public Property Notes As String = ""
        Public Property CreatedAt As DateTime = DateTime.Now
    End Class

    Public Class EmployeePayrollSummaryDto
        Public Property EmployeeId As Integer
        Public Property EmployeeName As String
        Public Property JobTitle As String
        Public Property BaseSalary As Double
        Public Property CommissionRate As Double
        Public Property TotalSalesAmount As Double
        Public Property TotalCommissions As Double
        Public Property TotalPendingAdvances As Double
        Public Property DaysPresent As Integer
        Public Property DaysAbsent As Integer
        Public Property DaysLate As Integer
        Public Property SuggestedNetSalary As Double
    End Class

End Namespace
