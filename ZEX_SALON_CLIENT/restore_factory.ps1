Add-Type -Path (Join-Path $PSScriptRoot "System.Data.SQLite.dll")
$emptyDb = Join-Path $PSScriptRoot "salon_pos_EMPTY.db"
$targetDb = Join-Path $PSScriptRoot "salon_pos.db"

if (-not (Test-Path $emptyDb)) {
    Write-Host "خطأ: لم يتم العثور على ملف salon_pos_EMPTY.db" -ForegroundColor Red
    exit 1
}

$licKey = ""
$cust = ""
$mId = ""
$exp = ""
$act = ""

if (Test-Path $targetDb) {
    try {
        $c1 = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$targetDb;")
        $c1.Open()
        $cmd1 = $c1.CreateCommand()
        $cmd1.CommandText = "SELECT LicenseKey, CustomerName, MachineId, ExpiryDate, ActivatedAt FROM LicenseInfo ORDER BY Id DESC LIMIT 1;"
        $r = $cmd1.ExecuteReader()
        if ($r.Read()) {
            $licKey = $r["LicenseKey"].ToString()
            $cust = $r["CustomerName"].ToString()
            $mId = $r["MachineId"].ToString()
            $exp = $r["ExpiryDate"].ToString()
            $act = $r["ActivatedAt"].ToString()
        }
        $r.Close()
        $c1.Close()
    } catch {
        # ignore if cannot read license
    }
}

# Replace target database with empty factory database
Copy-Item $emptyDb $targetDb -Force

# Re-inject license if it was present
if ($licKey -ne "") {
    try {
        $c2 = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$targetDb;")
        $c2.Open()
        $cmd2 = $c2.CreateCommand()
        $cmd2.CommandText = "INSERT INTO LicenseInfo (LicenseKey, CustomerName, MachineId, ExpiryDate, ActivatedAt) VALUES (@k, @c, @m, @e, @a);"
        [void]$cmd2.Parameters.AddWithValue("@k", $licKey)
        [void]$cmd2.Parameters.AddWithValue("@c", $cust)
        [void]$cmd2.Parameters.AddWithValue("@m", $mId)
        [void]$cmd2.Parameters.AddWithValue("@e", $exp)
        [void]$cmd2.Parameters.AddWithValue("@a", $act)
        [void]$cmd2.ExecuteNonQuery()
        $c2.Close()
        Write-Host "   [V] تم الحفاظ على ترخيص البرنامج تلقائياً." -ForegroundColor Green
    } catch {
        Write-Host "   [!] تنبيه أثناء إعادة حقن الترخيص: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host "   [V] تم استرجاع قاعدة البيانات الفارغة من المصنع بنجاح!" -ForegroundColor Green
