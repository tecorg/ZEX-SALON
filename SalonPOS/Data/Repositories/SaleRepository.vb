Imports System
Imports System.Collections.Generic
Imports System.Data.SQLite
Imports SalonPOS.Models

Namespace SalonPOS.Data.Repositories

    Public Class SaleRepository

        Public Function SaveSale(sale As Sale) As String
            Using conn = DbConnectionFactory.CreateConnection()
                Using trans = conn.BeginTransaction()
                    Try
                        ' Generate Invoice Number (e.g. INV-20260819-0001)
                        Dim invoiceNum As String = GenerateInvoiceNumber(conn, trans)
                        sale.InvoiceNumber = invoiceNum

                        Dim saleId As Integer
                        Using cmd = conn.CreateCommand()
                            cmd.Transaction = trans
                            cmd.CommandText = "
                                INSERT INTO Sales (InvoiceNumber, SessionId, ChairId, EmployeeId, CustomerId, SubTotal, Discount, TotalAmount, PaymentMethod, CashReceived, ChangeAmount, ShiftId, CreatedByUserId, CreatedAt)
                                VALUES (@inv, @sess, @chair, @emp, @cust, @sub, @disc, @tot, @pm, @rec, @chg, @shift, @user, @created);
                                SELECT last_insert_rowid();
                            "
                            cmd.Parameters.AddWithValue("@inv", sale.InvoiceNumber)
                            cmd.Parameters.AddWithValue("@sess", If(sale.SessionId.HasValue AndAlso sale.SessionId.Value > 0, sale.SessionId.Value, DBNull.Value))
                            cmd.Parameters.AddWithValue("@chair", sale.ChairId)
                            cmd.Parameters.AddWithValue("@emp", sale.EmployeeId)
                            cmd.Parameters.AddWithValue("@cust", If(sale.CustomerId.HasValue AndAlso sale.CustomerId.Value > 0, sale.CustomerId.Value, DBNull.Value))
                            cmd.Parameters.AddWithValue("@sub", sale.SubTotal)
                            cmd.Parameters.AddWithValue("@disc", sale.Discount)
                            cmd.Parameters.AddWithValue("@tot", sale.TotalAmount)
                            cmd.Parameters.AddWithValue("@pm", sale.PaymentMethod.ToString())
                            cmd.Parameters.AddWithValue("@rec", sale.CashReceived)
                            cmd.Parameters.AddWithValue("@chg", sale.ChangeAmount)
                            cmd.Parameters.AddWithValue("@shift", sale.ShiftId)
                            cmd.Parameters.AddWithValue("@user", sale.CreatedByUserId)
                            cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                            saleId = Convert.ToInt32(cmd.ExecuteScalar())
                        End Using

                        ' Insert items
                        For Each item In sale.Items
                            Dim saleItemId As Integer
                            Using cmdItem = conn.CreateCommand()
                                cmdItem.Transaction = trans
                                cmdItem.CommandText = "
                                    INSERT INTO SaleItems (SaleId, ServiceId, ServiceName, Price, Quantity, SubTotal, EmployeeId, EmployeeCommissionAmount)
                                    VALUES (@saleId, @srvId, @srvName, @price, @qty, @sub, @empId, @comm);
                                    SELECT last_insert_rowid();
                                "
                                cmdItem.Parameters.AddWithValue("@saleId", saleId)
                                cmdItem.Parameters.AddWithValue("@srvId", item.ServiceId)
                                cmdItem.Parameters.AddWithValue("@srvName", item.ServiceName)
                                cmdItem.Parameters.AddWithValue("@price", item.Price)
                                cmdItem.Parameters.AddWithValue("@qty", item.Quantity)
                                cmdItem.Parameters.AddWithValue("@sub", item.SubTotal)
                                cmdItem.Parameters.AddWithValue("@empId", item.EmployeeId)
                                cmdItem.Parameters.AddWithValue("@comm", item.EmployeeCommissionAmount)
                                saleItemId = Convert.ToInt32(cmdItem.ExecuteScalar())
                            End Using

                            For Each ex In item.Extras
                                Using cmdEx = conn.CreateCommand()
                                    cmdEx.Transaction = trans
                                    cmdEx.CommandText = "INSERT INTO SaleItemExtras (SaleItemId, ExtraId, ExtraName, Price) VALUES (@siId, @exId, @exName, @price);"
                                    cmdEx.Parameters.AddWithValue("@siId", saleItemId)
                                    cmdEx.Parameters.AddWithValue("@exId", ex.ExtraId)
                                    cmdEx.Parameters.AddWithValue("@exName", ex.ExtraName)
                                    cmdEx.Parameters.AddWithValue("@price", ex.Price)
                                    cmdEx.ExecuteNonQuery()
                                End Using
                            Next
                        Next

                        ' If session exists, clear session
                        If sale.SessionId.HasValue AndAlso sale.SessionId.Value > 0 Then
                            Using cmdClear = conn.CreateCommand()
                                cmdClear.Transaction = trans
                                cmdClear.CommandText = "
                                    DELETE FROM ChairSessionItemExtras WHERE SessionItemId IN (SELECT Id FROM ChairSessionItems WHERE SessionId = @sid);
                                    DELETE FROM ChairSessionItems WHERE SessionId = @sid;
                                    DELETE FROM ChairSessions WHERE Id = @sid;
                                "
                                cmdClear.Parameters.AddWithValue("@sid", sale.SessionId.Value)
                                cmdClear.ExecuteNonQuery()
                            End Using
                        End If

                        ' Update Shift sales totals
                        Using cmdShift = conn.CreateCommand()
                            cmdShift.Transaction = trans
                            If sale.PaymentMethod = PaymentMethod.CASH Then
                                cmdShift.CommandText = "UPDATE Shifts SET CashSales = CashSales + @tot WHERE Id = @shiftId;"
                            ElseIf sale.PaymentMethod = PaymentMethod.CARD Then
                                cmdShift.CommandText = "UPDATE Shifts SET CardSales = CardSales + @tot WHERE Id = @shiftId;"
                            Else
                                cmdShift.CommandText = "UPDATE Shifts SET TransferSales = TransferSales + @tot WHERE Id = @shiftId;"
                            End If
                            cmdShift.Parameters.AddWithValue("@tot", sale.TotalAmount)
                            cmdShift.Parameters.AddWithValue("@shiftId", sale.ShiftId)
                            cmdShift.ExecuteNonQuery()
                        End Using

                        trans.Commit()
                        Return invoiceNum
                    Catch
                        trans.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Private Function GenerateInvoiceNumber(conn As SQLiteConnection, trans As SQLiteTransaction) As String
            Using cmd = conn.CreateCommand()
                cmd.Transaction = trans
                cmd.CommandText = "SELECT COUNT(*) FROM Sales;"
                Dim count As Long = Convert.ToInt64(cmd.ExecuteScalar()) + 1
                Return $"INV-{DateTime.Now:yyyyMMdd}-{count:D4}"
            End Using
        End Function
    End Class

End Namespace
