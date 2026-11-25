Imports System.Collections.Generic
Imports System.Data.SQLite
Imports SalonPOS.Models

Namespace SalonPOS.Data.Repositories

    Public Class EmployeeRepository

        Public Function GetActiveEmployees() As List(Of Employee)
            Dim list As New List(Of Employee)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, Name, Phone, CommissionRate, IsActive, CreatedAt FROM Employees WHERE IsActive = 1 ORDER BY Name ASC;"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            list.Add(New Employee With {
                                .Id = Convert.ToInt32(reader("Id")),
                                .Name = reader("Name").ToString(),
                                .Phone = If(reader.IsDBNull(reader.GetOrdinal("Phone")), "", reader("Phone").ToString()),
                                .CommissionRate = Convert.ToDouble(reader("CommissionRate")),
                                .IsActive = DbConnectionFactory.ToBool(reader("IsActive")),
                                .CreatedAt = Convert.ToDateTime(reader("CreatedAt"))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Sub SaveEmployee(emp As Employee)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    If emp.Id = 0 Then
                        cmd.CommandText = "INSERT INTO Employees (Name, Phone, CommissionRate, IsActive, CreatedAt) VALUES (@n, @p, @c, @a, @created);"
                    Else
                        cmd.CommandText = "UPDATE Employees SET Name = @n, Phone = @p, CommissionRate = @c, IsActive = @a WHERE Id = @id;"
                        cmd.Parameters.AddWithValue("@id", emp.Id)
                    End If
                    cmd.Parameters.AddWithValue("@n", emp.Name)
                    cmd.Parameters.AddWithValue("@p", If(emp.Phone, ""))
                    cmd.Parameters.AddWithValue("@c", emp.CommissionRate)
                    cmd.Parameters.AddWithValue("@a", If(emp.IsActive, 1, 0))
                    cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Class

    Public Class CustomerRepository

        Public Function SearchCustomers(query As String) As List(Of Customer)
            Dim list As New List(Of Customer)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    Dim sql As String = "SELECT Id, Name, Phone, Notes, CreatedAt FROM Customers"
                    If Not String.IsNullOrWhiteSpace(query) Then
                        sql &= " WHERE Name LIKE @q OR Phone LIKE @q"
                        cmd.Parameters.AddWithValue("@q", $"%{query.Trim()}%")
                    End If
                    sql &= " ORDER BY Name ASC LIMIT 50;"
                    cmd.CommandText = sql

                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            list.Add(New Customer With {
                                .Id = Convert.ToInt32(reader("Id")),
                                .Name = reader("Name").ToString(),
                                .Phone = If(reader.IsDBNull(reader.GetOrdinal("Phone")), "", reader("Phone").ToString()),
                                .Notes = If(reader.IsDBNull(reader.GetOrdinal("Notes")), "", reader("Notes").ToString()),
                                .CreatedAt = Convert.ToDateTime(reader("CreatedAt"))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Function QuickAddCustomer(name As String, phone As String) As Integer
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "INSERT INTO Customers (Name, Phone, CreatedAt) VALUES (@n, @p, @c); SELECT last_insert_rowid();"
                    cmd.Parameters.AddWithValue("@n", name)
                    cmd.Parameters.AddWithValue("@p", If(phone, ""))
                    cmd.Parameters.AddWithValue("@c", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    Return Convert.ToInt32(cmd.ExecuteScalar())
                End Using
            End Using
        End Function
    End Class

End Namespace
