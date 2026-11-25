Imports System
Imports System.Data.SQLite
Imports System.IO
Imports SalonPOS.Data
Imports SalonPOS.Data.Repositories

Namespace SalonPOS.Services

    Public Class BackupService

        Public Shared Function CreateBackup(Optional customFolder As String = "") As String
            Try
                Dim backupDir As String = If(String.IsNullOrWhiteSpace(customFolder),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups"),
                    customFolder)

                If Not Directory.Exists(backupDir) Then
                    Directory.CreateDirectory(backupDir)
                End If

                Dim timeStamp As String = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
                Dim backupPath As String = Path.Combine(backupDir, $"salon_pos_backup_{timeStamp}.db")

                ' Clear pools to avoid file locks
                SQLiteConnection.ClearAllPools()
                File.Copy(DbConnectionFactory.DatabasePath, backupPath, overwrite:=True)
                Return backupPath
            Catch ex As Exception
                Throw New Exception($"فشل إنشاء النسخة الاحتياطية: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' Creates immediate backup to local default directory, and also mirrors it to the secondary path (USB/External drive) if configured and connected.
        ''' Returns a tuple of (localBackupPath, secondaryBackupPath).
        ''' </summary>
        Public Shared Function CreateBackupMultiLocation() As Tuple(Of String, String)
            Dim localPath As String = CreateBackup()
            Dim secondaryPath As String = ""

            Try
                Dim repo As New SettingsRepository()
                Dim s = repo.GetSettings()
                If s.EnableSecondaryBackup AndAlso Not String.IsNullOrWhiteSpace(s.SecondaryBackupPath) Then
                    If Directory.Exists(s.SecondaryBackupPath) Then
                        Dim fileName = Path.GetFileName(localPath)
                        Dim destFile = Path.Combine(s.SecondaryBackupPath, fileName)
                        File.Copy(localPath, destFile, overwrite:=True)
                        secondaryPath = destFile
                    End If
                End If
            Catch
                ' Secondary backup fail shouldn't crash main operation
            End Try

            Return Tuple.Create(localPath, secondaryPath)
        End Function

        Public Shared Sub RestoreBackup(backupFilePath As String)
            If Not File.Exists(backupFilePath) Then
                Throw New FileNotFoundException("ملف النسخة الاحتياطية المحدد غير موجود.")
            End If

            Try
                ' Clear active connections
                SQLiteConnection.ClearAllPools()
                GC.Collect()
                GC.WaitForPendingFinalizers()

                File.Copy(backupFilePath, DbConnectionFactory.DatabasePath, overwrite:=True)
            Catch ex As Exception
                Throw New Exception($"فشل استعادة النسخة الاحتياطية: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Resets all operational training data (Sales, Items, Shifts, Expenses, Customers, Chair Sessions)
        ''' while strictly preserving: LicenseInfo, SalonSettings, Services, Categories, Users/Barbers, Roles.
        ''' Automatically creates a safety backup before resetting.
        ''' </summary>
        Public Shared Sub ResetTrainingData()
            ' 1. Safety backup
            Try
                CreateBackup()
            Catch
            End Try

            ' 2. Execute Reset SQL inside transaction
            Using conn = DbConnectionFactory.CreateConnection()
                conn.Open()
                Using trans = conn.BeginTransaction()
                    Try
                        Using cmd = conn.CreateCommand()
                            cmd.Transaction = trans
                            cmd.CommandText = "
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
"
                            cmd.ExecuteNonQuery()
                        End Using
                        trans.Commit()
                    Catch
                        trans.Rollback()
                        Throw
                    End Try
                End Using

                ' 3. Vacuum outside transaction to compact DB file
                Try
                    Using vacCmd = conn.CreateCommand()
                        vacCmd.CommandText = "VACUUM;"
                        vacCmd.ExecuteNonQuery()
                    End Using
                Catch
                End Try
            End Using
        End Sub

        Public Shared Sub OpenBackupsFolder()
            Try
                Dim backupDir As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups")
                If Not Directory.Exists(backupDir) Then
                    Directory.CreateDirectory(backupDir)
                End If
                System.Diagnostics.Process.Start("explorer.exe", backupDir)
            Catch
            End Try
        End Sub

        Public Shared Sub PerformDailyAutoBackup()
            Try
                Dim backupDir As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups", "Daily")
                If Not Directory.Exists(backupDir) Then
                    Directory.CreateDirectory(backupDir)
                End If

                Dim todayFileName As String = $"salon_pos_auto_{DateTime.Now:yyyy-MM-dd}.db"
                Dim todayFile As String = Path.Combine(backupDir, todayFileName)
                If Not File.Exists(todayFile) Then
                    SQLiteConnection.ClearAllPools()
                    File.Copy(DbConnectionFactory.DatabasePath, todayFile, overwrite:=True)
                End If

                ' Mirror to secondary backup location (USB Flash Drive / External Disk) if enabled and drive is plugged in
                Dim repo As New SettingsRepository()
                Dim s = repo.GetSettings()
                If s.EnableSecondaryBackup AndAlso Not String.IsNullOrWhiteSpace(s.SecondaryBackupPath) Then
                    If Directory.Exists(s.SecondaryBackupPath) Then
                        Dim secTodayFile As String = Path.Combine(s.SecondaryBackupPath, todayFileName)
                        If Not File.Exists(secTodayFile) Then
                            File.Copy(todayFile, secTodayFile, overwrite:=True)
                        End If
                    End If
                End If
            Catch
                ' Silent log for auto backup errors
            End Try
        End Sub
    End Class

End Namespace
