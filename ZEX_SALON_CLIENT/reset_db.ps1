Add-Type -Path (Join-Path $PSScriptRoot "System.Data.SQLite.dll")
$dbPath = Join-Path $PSScriptRoot "salon_pos.db"
if (-not (Test-Path $dbPath)) {
    Write-Host "خطأ: لم يتم العثور على ملف salon_pos.db" -ForegroundColor Red
    exit 1
}

$conn = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$dbPath;")
$conn.Open()
$cmd = $conn.CreateCommand()

$cmd.CommandText = "
DELETE FROM SaleItemExtras;
DELETE FROM SaleItems;
DELETE FROM Sales;
DELETE FROM ChairSessionItemExtras;
DELETE FROM ChairSessionItems;
DELETE FROM ChairSessions;
DELETE FROM Shifts;
DELETE FROM Expenses;
DELETE FROM Customers;
DELETE FROM ActivityLogs;
DELETE FROM EmployeeAttendance;
DELETE FROM EmployeeAdvances;
DELETE FROM EmployeePayrolls;
DELETE FROM sqlite_sequence WHERE name IN ('Sales', 'SaleItems', 'SaleItemExtras', 'Shifts', 'Expenses', 'ChairSessions', 'ChairSessionItems', 'ChairSessionItemExtras', 'Customers', 'ActivityLogs', 'EmployeeAttendance', 'EmployeeAdvances', 'EmployeePayrolls');
VACUUM;
"

$res = $cmd.ExecuteNonQuery()
$conn.Close()
Write-Host "   [V] تم تصفير كافة المعاملات والمبيعات والورديات بنجاح!" -ForegroundColor Green
