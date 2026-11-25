Imports System.Data.SQLite
Imports SalonPOS.Data
Imports SalonPOS.DTOs

Namespace SalonPOS.Services

    Public Class ReportService

        Public Function GetTodaySummary() As DailySummaryDto
            Dim summary As New DailySummaryDto()

            Using conn = DbConnectionFactory.CreateConnection()
                ' 1. Sales & Customer count today
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        SELECT COUNT(*) AS CustomerCount,
                               IFNULL(SUM(TotalAmount), 0) AS TotalSales,
                               IFNULL(SUM(CASE WHEN PaymentMethod = 'CASH' THEN TotalAmount ELSE 0 END), 0) AS CashSales,
                               IFNULL(SUM(CASE WHEN PaymentMethod = 'CARD' THEN TotalAmount ELSE 0 END), 0) AS CardSales,
                               IFNULL(SUM(CASE WHEN PaymentMethod = 'TRANSFER' THEN TotalAmount ELSE 0 END), 0) AS TransferSales
                        FROM Sales
                        WHERE date(CreatedAt) = date('now', 'localtime');
                    "
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            summary.CustomerCount = Convert.ToInt32(reader("CustomerCount"))
                            summary.TotalSales = Convert.ToDouble(reader("TotalSales"))
                            summary.CashSales = Convert.ToDouble(reader("CashSales"))
                            summary.CardSales = Convert.ToDouble(reader("CardSales"))
                            summary.TransferSales = Convert.ToDouble(reader("TransferSales"))
                        End If
                    End Using
                End Using

                ' 2. Total Services Count today
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        SELECT IFNULL(SUM(si.Quantity), 0)
                        FROM SaleItems si
                        INNER JOIN Sales s ON si.SaleId = s.Id
                        WHERE date(s.CreatedAt) = date('now', 'localtime');
                    "
                    summary.ServiceCount = Convert.ToInt32(cmd.ExecuteScalar())
                End Using

                ' 3. Expenses Today
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        SELECT IFNULL(SUM(Amount), 0)
                        FROM Expenses
                        WHERE date(CreatedAt) = date('now', 'localtime');
                    "
                    summary.TotalExpenses = Convert.ToDouble(cmd.ExecuteScalar())
                End Using
            End Using

            summary.NetProfit = summary.TotalSales - summary.TotalExpenses
            Return summary
        End Function
    End Class

End Namespace
