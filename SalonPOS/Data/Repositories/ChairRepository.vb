Imports System.Collections.Generic
Imports System.Data.SQLite
Imports SalonPOS.Models

Namespace SalonPOS.Data.Repositories

    Public Class ChairRepository
        Public Function GetAllChairs() As List(Of Chair)
            Dim chairs As New List(Of Chair)()

            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        SELECT c.Id, c.ChairNumber, c.Name, c.DisplayOrder, c.IsActive,
                               s.Id AS ActiveSessionId, s.EmployeeId, e.Name AS EmployeeName,
                               s.CustomerId, cust.Name AS CustomerName, s.Status AS SessionStatus,
                               s.StartedAt, s.TotalAmount
                        FROM Chairs c
                        LEFT JOIN ChairSessions s ON c.Id = s.ChairId AND s.Status IN ('IN_SERVICE', 'WAITING_PAYMENT')
                        LEFT JOIN Employees e ON s.EmployeeId = e.Id
                        LEFT JOIN Customers cust ON s.CustomerId = cust.Id
                        WHERE c.IsActive = 1
                        ORDER BY c.DisplayOrder ASC, c.ChairNumber ASC;
                    "

                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim ch As New Chair()
                            ch.Id = Convert.ToInt32(reader("Id"))
                            ch.ChairNumber = Convert.ToInt32(reader("ChairNumber"))
                            ch.Name = reader("Name").ToString()
                            ch.DisplayOrder = Convert.ToInt32(reader("DisplayOrder"))
                            ch.IsActive = DbConnectionFactory.ToBool(reader("IsActive"))

                            If Not reader.IsDBNull(reader.GetOrdinal("ActiveSessionId")) Then
                                ch.ActiveSessionId = Convert.ToInt32(reader("ActiveSessionId"))
                                ch.CurrentEmployeeName = reader("EmployeeName").ToString()
                                ch.CurrentCustomerName = If(reader.IsDBNull(reader.GetOrdinal("CustomerName")), "عميل نقدي", reader("CustomerName").ToString())
                                ch.ActiveTotalAmount = Convert.ToDouble(reader("TotalAmount"))
                                ch.ServiceStartedAt = Convert.ToDateTime(reader("StartedAt"))

                                Dim statusStr = reader("SessionStatus").ToString()
                                If statusStr = "IN_SERVICE" Then
                                    ch.CurrentStatus = ChairStatus.IN_SERVICE
                                ElseIf statusStr = "WAITING_PAYMENT" Then
                                    ch.CurrentStatus = ChairStatus.WAITING_PAYMENT
                                End If
                            Else
                                ch.CurrentStatus = ChairStatus.AVAILABLE
                            End If

                            chairs.Add(ch)
                        End While
                    End Using
                End Using
            End Using

            Return chairs
        End Function

        Public Sub SaveChair(chair As Chair)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    If chair.Id = 0 Then
                        cmd.CommandText = "INSERT INTO Chairs (ChairNumber, Name, DisplayOrder, IsActive) VALUES (@num, @name, @order, @active);"
                    Else
                        cmd.CommandText = "UPDATE Chairs SET ChairNumber = @num, Name = @name, DisplayOrder = @order, IsActive = @active WHERE Id = @id;"
                        cmd.Parameters.AddWithValue("@id", chair.Id)
                    End If

                    cmd.Parameters.AddWithValue("@num", chair.ChairNumber)
                    cmd.Parameters.AddWithValue("@name", chair.Name)
                    cmd.Parameters.AddWithValue("@order", chair.DisplayOrder)
                    cmd.Parameters.AddWithValue("@active", If(chair.IsActive, 1, 0))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Class

End Namespace
