Imports System
Imports System.Data.SQLite
Imports SalonPOS.Models

Namespace SalonPOS.Data.Repositories

    Public Class SettingsRepository

        Public Function GetSettings() As SalonSettings
            Dim s As New SalonSettings()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Key, Value FROM Settings;"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim key = reader("Key").ToString()
                            Dim val = reader("Value").ToString()

                            Select Case key
                                Case "SalonName"
                                    s.SalonName = val
                                Case "LogoPath"
                                    s.LogoPath = val
                                Case "Address"
                                    s.Address = val
                                Case "Phone"
                                    s.Phone = val
                                Case "Currency"
                                    s.Currency = val
                                Case "DefaultTaxRate"
                                    Double.TryParse(val, s.DefaultTaxRate)
                                Case "EnableTax"
                                    Boolean.TryParse(val, s.EnableTax)
                                Case "FooterMessage"
                                    s.FooterMessage = val
                                Case "SelectedPrinterName"
                                    s.SelectedPrinterName = val
                                Case "SecondaryBackupPath"
                                    s.SecondaryBackupPath = val
                                Case "EnableSecondaryBackup"
                                    Boolean.TryParse(val, s.EnableSecondaryBackup)
                            End Select
                        End While
                    End Using
                End Using
            End Using
            Return s
        End Function

        Public Sub SaveSettings(s As SalonSettings)
            If s Is Nothing Then Return
            Using conn = DbConnectionFactory.CreateConnection()
                Using trans = conn.BeginTransaction()
                    Try
                        SaveSingleSetting(conn, trans, "SalonName", If(s.SalonName, "ZEX SALON"))
                        SaveSingleSetting(conn, trans, "LogoPath", If(s.LogoPath, ""))
                        SaveSingleSetting(conn, trans, "Address", If(s.Address, ""))
                        SaveSingleSetting(conn, trans, "Phone", If(s.Phone, ""))
                        SaveSingleSetting(conn, trans, "Currency", If(s.Currency, "ج.م"))
                        SaveSingleSetting(conn, trans, "DefaultTaxRate", s.DefaultTaxRate.ToString())
                        SaveSingleSetting(conn, trans, "EnableTax", s.EnableTax.ToString())
                        SaveSingleSetting(conn, trans, "FooterMessage", If(s.FooterMessage, ""))
                        SaveSingleSetting(conn, trans, "SelectedPrinterName", If(s.SelectedPrinterName, "(طابعة النظام الافتراضية)"))
                        SaveSingleSetting(conn, trans, "SecondaryBackupPath", If(s.SecondaryBackupPath, ""))
                        SaveSingleSetting(conn, trans, "EnableSecondaryBackup", s.EnableSecondaryBackup.ToString())

                        trans.Commit()
                    Catch
                        trans.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Private Sub SaveSingleSetting(conn As SQLiteConnection, trans As SQLiteTransaction, key As String, val As String)
            Using cmd = conn.CreateCommand()
                cmd.Transaction = trans
                cmd.CommandText = "
                    INSERT INTO Settings (Key, Value) VALUES (@k, @v)
                    ON CONFLICT(Key) DO UPDATE SET Value = @v;
                "
                cmd.Parameters.AddWithValue("@k", key)
                cmd.Parameters.AddWithValue("@v", val)
                cmd.ExecuteNonQuery()
            End Using
        End Sub
    End Class

End Namespace
