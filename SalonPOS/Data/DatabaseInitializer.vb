Imports System.Data.SQLite
Imports SalonPOS.Helpers

Namespace SalonPOS.Data

    Public Class DatabaseInitializer
        Public Shared Sub InitializeDatabase()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = GetSchemaSql()
                    cmd.ExecuteNonQuery()
                End Using

                EnsureMigrations(conn)
                SeedDefaultData(conn)
            End Using
        End Sub

        Private Shared Sub EnsureMigrations(conn As SQLiteConnection)
            Try
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "ALTER TABLE Users ADD COLUMN PermissionsJson TEXT NULL;"
                    cmd.ExecuteNonQuery()
                End Using
            Catch
            End Try

            ' Migrate Employees table with JobTitle, BaseSalary, HireDate
            Try
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "PRAGMA table_info(Employees);"
                    Dim hasJobTitle As Boolean = False
                    Dim hasBaseSalary As Boolean = False
                    Dim hasHireDate As Boolean = False
                    Using rdr = cmd.ExecuteReader()
                        While rdr.Read()
                            Dim colName = rdr("name").ToString().ToLower()
                            If colName = "jobtitle" Then hasJobTitle = True
                            If colName = "basesalary" Then hasBaseSalary = True
                            If colName = "hiredate" Then hasHireDate = True
                        End While
                    End Using

                    If Not hasJobTitle Then
                        Using alterCmd = conn.CreateCommand()
                            alterCmd.CommandText = "ALTER TABLE Employees ADD COLUMN JobTitle TEXT NULL DEFAULT 'مصفف / حلاق';"
                            alterCmd.ExecuteNonQuery()
                        End Using
                    End If
                    If Not hasBaseSalary Then
                        Using alterCmd = conn.CreateCommand()
                            alterCmd.CommandText = "ALTER TABLE Employees ADD COLUMN BaseSalary REAL NOT NULL DEFAULT 3500.0;"
                            alterCmd.ExecuteNonQuery()
                        End Using
                    End If
                    If Not hasHireDate Then
                        Using alterCmd = conn.CreateCommand()
                            alterCmd.CommandText = "ALTER TABLE Employees ADD COLUMN HireDate TEXT NULL;"
                            alterCmd.ExecuteNonQuery()
                        End Using
                    End If
                End Using
            Catch
            End Try
        End Sub

        Private Shared Function GetSchemaSql() As String
            Return "
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                Role TEXT NOT NULL CHECK(Role IN ('ADMIN', 'CASHIER', 'EMPLOYEE')),
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Employees (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Phone TEXT NULL,
                JobTitle TEXT NULL DEFAULT 'مصفف / حلاق',
                BaseSalary REAL NOT NULL DEFAULT 3500.0,
                CommissionRate REAL NOT NULL DEFAULT 10.0,
                HireDate TEXT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Chairs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ChairNumber INTEGER NOT NULL,
                Name TEXT NOT NULL,
                DisplayOrder INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Customers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Phone TEXT NULL,
                Notes TEXT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ServiceCategories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                IconName TEXT NULL,
                DisplayOrder INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Services (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Price REAL NOT NULL,
                DurationMinutes INTEGER NOT NULL DEFAULT 30,
                IsActive INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY(CategoryId) REFERENCES ServiceCategories(Id)
            );

            CREATE TABLE IF NOT EXISTS ServiceExtras (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ServiceId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Price REAL NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY(ServiceId) REFERENCES Services(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ChairSessions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ChairId INTEGER NOT NULL,
                EmployeeId INTEGER NOT NULL,
                CustomerId INTEGER NULL,
                Status TEXT NOT NULL CHECK(Status IN ('IN_SERVICE', 'WAITING_PAYMENT')),
                StartedAt TEXT NOT NULL,
                TotalAmount REAL NOT NULL DEFAULT 0.0,
                FOREIGN KEY(ChairId) REFERENCES Chairs(Id),
                FOREIGN KEY(EmployeeId) REFERENCES Employees(Id),
                FOREIGN KEY(CustomerId) REFERENCES Customers(Id)
            );

            CREATE TABLE IF NOT EXISTS ChairSessionItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SessionId INTEGER NOT NULL,
                ServiceId INTEGER NOT NULL,
                ServiceName TEXT NOT NULL,
                Price REAL NOT NULL,
                Quantity INTEGER NOT NULL DEFAULT 1,
                EmployeeId INTEGER NULL,
                FOREIGN KEY(SessionId) REFERENCES ChairSessions(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ChairSessionItemExtras (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SessionItemId INTEGER NOT NULL,
                ExtraId INTEGER NOT NULL,
                ExtraName TEXT NOT NULL,
                Price REAL NOT NULL,
                FOREIGN KEY(SessionItemId) REFERENCES ChairSessionItems(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Shifts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                StartedAt TEXT NOT NULL,
                EndedAt TEXT NULL,
                OpeningCash REAL NOT NULL DEFAULT 0.0,
                CashSales REAL NOT NULL DEFAULT 0.0,
                CardSales REAL NOT NULL DEFAULT 0.0,
                TransferSales REAL NOT NULL DEFAULT 0.0,
                TotalExpenses REAL NOT NULL DEFAULT 0.0,
                ExpectedCash REAL NOT NULL DEFAULT 0.0,
                ActualCash REAL NULL,
                Variance REAL NULL,
                Status TEXT NOT NULL CHECK(Status IN ('OPEN', 'CLOSED')),
                FOREIGN KEY(UserId) REFERENCES Users(Id)
            );

            CREATE TABLE IF NOT EXISTS Sales (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceNumber TEXT NOT NULL UNIQUE,
                SessionId INTEGER NULL,
                ChairId INTEGER NOT NULL,
                EmployeeId INTEGER NOT NULL,
                CustomerId INTEGER NULL,
                SubTotal REAL NOT NULL,
                Discount REAL NOT NULL DEFAULT 0.0,
                TotalAmount REAL NOT NULL,
                PaymentMethod TEXT NOT NULL CHECK(PaymentMethod IN ('CASH', 'CARD', 'TRANSFER', 'SPLIT')),
                CashReceived REAL NOT NULL DEFAULT 0.0,
                ChangeAmount REAL NOT NULL DEFAULT 0.0,
                ShiftId INTEGER NOT NULL,
                CreatedByUserId INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY(ShiftId) REFERENCES Shifts(Id),
                FOREIGN KEY(ChairId) REFERENCES Chairs(Id),
                FOREIGN KEY(EmployeeId) REFERENCES Employees(Id)
            );

            CREATE TABLE IF NOT EXISTS SaleItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SaleId INTEGER NOT NULL,
                ServiceId INTEGER NOT NULL,
                ServiceName TEXT NOT NULL,
                Price REAL NOT NULL,
                Quantity INTEGER NOT NULL,
                SubTotal REAL NOT NULL,
                EmployeeId INTEGER NOT NULL,
                EmployeeCommissionAmount REAL NOT NULL DEFAULT 0.0,
                FOREIGN KEY(SaleId) REFERENCES Sales(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS SaleItemExtras (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SaleItemId INTEGER NOT NULL,
                ExtraId INTEGER NOT NULL,
                ExtraName TEXT NOT NULL,
                Price REAL NOT NULL,
                FOREIGN KEY(SaleItemId) REFERENCES SaleItems(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Expenses (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Amount REAL NOT NULL,
                Category TEXT NOT NULL,
                Notes TEXT NULL,
                ShiftId INTEGER NOT NULL,
                CreatedByUserId INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY(ShiftId) REFERENCES Shifts(Id)
            );

            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS LicenseInfo (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseKey TEXT NOT NULL,
                CustomerName TEXT NOT NULL,
                MachineId TEXT NOT NULL,
                ExpiryDate TEXT NOT NULL,
                ActivatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ActivityLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                UserName TEXT NOT NULL,
                ActionType TEXT NOT NULL,
                Details TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS EmployeeAttendance (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EmployeeId INTEGER NOT NULL,
                AttendanceDate TEXT NOT NULL,
                CheckInTime TEXT NULL,
                CheckOutTime TEXT NULL,
                Status TEXT NOT NULL DEFAULT 'حاضر',
                WorkHours REAL NOT NULL DEFAULT 0.0,
                Notes TEXT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY(EmployeeId) REFERENCES Employees(Id)
            );

            CREATE TABLE IF NOT EXISTS EmployeeAdvances (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EmployeeId INTEGER NOT NULL,
                AdvanceDate TEXT NOT NULL,
                Amount REAL NOT NULL,
                Notes TEXT NULL,
                IsDeducted INTEGER NOT NULL DEFAULT 0,
                DeductedPayrollId INTEGER NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY(EmployeeId) REFERENCES Employees(Id)
            );

            CREATE TABLE IF NOT EXISTS EmployeePayrolls (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EmployeeId INTEGER NOT NULL,
                PayrollMonth TEXT NOT NULL,
                BaseSalary REAL NOT NULL DEFAULT 0.0,
                TotalCommission REAL NOT NULL DEFAULT 0.0,
                TotalBonuses REAL NOT NULL DEFAULT 0.0,
                TotalAdvances REAL NOT NULL DEFAULT 0.0,
                TotalDeductions REAL NOT NULL DEFAULT 0.0,
                NetSalary REAL NOT NULL DEFAULT 0.0,
                PaidAmount REAL NOT NULL DEFAULT 0.0,
                PaymentDate TEXT NULL,
                PaymentMethod TEXT NOT NULL DEFAULT 'نقدي',
                Status TEXT NOT NULL DEFAULT 'مصروف',
                Notes TEXT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY(EmployeeId) REFERENCES Employees(Id)
            );
            "
        End Function

        Private Shared Sub SeedDefaultData(conn As SQLiteConnection)
            ' 1. Seed Users (Default admin user: admin / admin123)
            Using cmdCheck = conn.CreateCommand()
                cmdCheck.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = 'admin';"
                Dim adminCount As Long = Convert.ToInt64(cmdCheck.ExecuteScalar())
                If adminCount = 0 Then
                    Using cmdInsert = conn.CreateCommand()
                        cmdInsert.CommandText = "INSERT INTO Users (Username, PasswordHash, DisplayName, Role, IsActive, CreatedAt) VALUES ('admin', @p, 'مدير النظام', 'ADMIN', 1, @c);"
                        cmdInsert.Parameters.AddWithValue("@p", SecurityHelper.HashPassword("admin123"))
                        cmdInsert.Parameters.AddWithValue("@c", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                        cmdInsert.ExecuteNonQuery()
                    End Using
                Else
                    Using cmdUpdate = conn.CreateCommand()
                        cmdUpdate.CommandText = "UPDATE Users SET PasswordHash = @p WHERE Username = 'admin';"
                        cmdUpdate.Parameters.AddWithValue("@p", SecurityHelper.HashPassword("admin123"))
                        cmdUpdate.ExecuteNonQuery()
                    End Using
                End If
            End Using

            ' 2. Seed Chairs (8 Chairs by default)
            Using cmdCheck = conn.CreateCommand()
                cmdCheck.CommandText = "SELECT COUNT(*) FROM Chairs;"
                Dim chairCount As Long = Convert.ToInt64(cmdCheck.ExecuteScalar())
                If chairCount = 0 Then
                    For i As Integer = 1 To 8
                        Using cmdInsert = conn.CreateCommand()
                            cmdInsert.CommandText = "INSERT INTO Chairs (ChairNumber, Name, DisplayOrder, IsActive) VALUES (@num, @name, @order, 1);"
                            cmdInsert.Parameters.AddWithValue("@num", i)
                            cmdInsert.Parameters.AddWithValue("@name", $"كرسي {i}")
                            cmdInsert.Parameters.AddWithValue("@order", i)
                            cmdInsert.ExecuteNonQuery()
                        End Using
                    Next
                End If
            End Using

            ' 3. Seed Sample Employees
            Using cmdCheck = conn.CreateCommand()
                cmdCheck.CommandText = "SELECT COUNT(*) FROM Employees;"
                Dim empCount As Long = Convert.ToInt64(cmdCheck.ExecuteScalar())
                If empCount = 0 Then
                    Dim defaultEmployees = New String() {"محمد", "أحمد", "محمود", "مصطفى"}
                    For Each empName In defaultEmployees
                        Using cmdInsert = conn.CreateCommand()
                            cmdInsert.CommandText = "INSERT INTO Employees (Name, JobTitle, BaseSalary, CommissionRate, IsActive, CreatedAt) VALUES (@n, 'مصفف / حلاق', 3500.0, 10.0, 1, @c);"
                            cmdInsert.Parameters.AddWithValue("@n", empName)
                            cmdInsert.Parameters.AddWithValue("@c", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                            cmdInsert.ExecuteNonQuery()
                        End Using
                    Next
                End If
            End Using

            ' 4. Seed Service Categories & Services with Extras
            Using cmdCheck = conn.CreateCommand()
                cmdCheck.CommandText = "SELECT COUNT(*) FROM ServiceCategories;"
                Dim catCount As Long = Convert.ToInt64(cmdCheck.ExecuteScalar())
                If catCount = 0 Then
                    ' Categories
                    Dim catHairId As Long = InsertCategory(conn, "شعر", 1)
                    Dim catBeardId As Long = InsertCategory(conn, "ذقن", 2)
                    Dim catCareId As Long = InsertCategory(conn, "عناية وبشرة", 3)

                    ' Services for Hair
                    Dim s1 As Long = InsertService(conn, catHairId, "قص شعر", 100, 30)
                    InsertExtra(conn, s1, "غسيل", 20)
                    InsertExtra(conn, s1, "ماسك", 30)
                    InsertExtra(conn, s1, "استشوار", 40)

                    Dim s2 As Long = InsertService(conn, catHairId, "تدريج شعر", 120, 35)
                    InsertExtra(conn, s2, "غسيل", 20)
                    InsertExtra(conn, s2, "استشوار", 40)

                    Dim s3 As Long = InsertService(conn, catHairId, "صبغة شعر", 150, 45)
                    Dim s4 As Long = InsertService(conn, catHairId, "استشوار سادة", 50, 15)

                    ' Services for Beard
                    Dim b1 As Long = InsertService(conn, catBeardId, "حلاقة ذقن", 50, 20)
                    InsertExtra(conn, b1, "فوطة سخنة", 15)
                    InsertExtra(conn, b1, "تدريج ذقن", 20)

                    Dim b2 As Long = InsertService(conn, catBeardId, "تحديد ذقن", 40, 15)

                    ' Services for Care
                    Dim c1 As Long = InsertService(conn, catCareId, "تنظيف بشرة", 80, 25)
                    InsertExtra(conn, c1, "بخار", 25)
                    InsertExtra(conn, c1, "ماسكات إضافية", 35)

                    Dim c2 As Long = InsertService(conn, catCareId, "حمام كريم", 60, 20)
                End If
            End Using

            ' 5. Seed Default Settings
            Using cmdCheck = conn.CreateCommand()
                cmdCheck.CommandText = "SELECT COUNT(*) FROM Settings;"
                Dim settingsCount As Long = Convert.ToInt64(cmdCheck.ExecuteScalar())
                If settingsCount = 0 Then
                    SaveSetting(conn, "SalonName", "صالون الأناقة والجمال")
                    SaveSetting(conn, "SalonPhone", "01000000000")
                    SaveSetting(conn, "SalonAddress", "القاهرة - مصر")
                    SaveSetting(conn, "ReceiptFooter", "شكراً لزيارتكم ونتمنى لكم يوماً سعيداً!")
                    SaveSetting(conn, "CurrencySymbol", "ج.م")
                    SaveSetting(conn, "PrinterName", "")
                    SaveSetting(conn, "AutoPrintReceipt", "True")
                End If
            End Using
        End Sub

        Private Shared Function InsertCategory(conn As SQLiteConnection, name As String, order As Integer) As Long
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "INSERT INTO ServiceCategories (Name, DisplayOrder, IsActive) VALUES (@n, @o, 1); SELECT last_insert_rowid();"
                cmd.Parameters.AddWithValue("@n", name)
                cmd.Parameters.AddWithValue("@o", order)
                Return Convert.ToInt64(cmd.ExecuteScalar())
            End Using
        End Function

        Private Shared Function InsertService(conn As SQLiteConnection, catId As Long, name As String, price As Double, duration As Integer) As Long
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "INSERT INTO Services (CategoryId, Name, Price, DurationMinutes, IsActive) VALUES (@c, @n, @p, @d, 1); SELECT last_insert_rowid();"
                cmd.Parameters.AddWithValue("@c", catId)
                cmd.Parameters.AddWithValue("@n", name)
                cmd.Parameters.AddWithValue("@p", price)
                cmd.Parameters.AddWithValue("@d", duration)
                Return Convert.ToInt64(cmd.ExecuteScalar())
            End Using
        End Function

        Private Shared Sub InsertExtra(conn As SQLiteConnection, serviceId As Long, name As String, price As Double)
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "INSERT INTO ServiceExtras (ServiceId, Name, Price, IsActive) VALUES (@s, @n, @p, 1);"
                cmd.Parameters.AddWithValue("@s", serviceId)
                cmd.Parameters.AddWithValue("@n", name)
                cmd.Parameters.AddWithValue("@p", price)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Shared Sub SaveSetting(conn As SQLiteConnection, key As String, value As String)
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@k, @v);"
                cmd.Parameters.AddWithValue("@k", key)
                cmd.Parameters.AddWithValue("@v", value)
                cmd.ExecuteNonQuery()
            End Using
        End Sub
    End Class

End Namespace
