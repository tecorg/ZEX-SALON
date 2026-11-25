Imports System.Collections.Generic
Imports System.Data.SQLite
Imports SalonPOS.Models

Namespace SalonPOS.Data.Repositories

    Public Class ServiceRepository

        Public Function GetCategories() As List(Of ServiceCategory)
            Dim list As New List(Of ServiceCategory)()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, Name, IconName, DisplayOrder, IsActive FROM ServiceCategories WHERE IsActive = 1 ORDER BY DisplayOrder ASC, Id ASC;"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            list.Add(New ServiceCategory With {
                                .Id = Convert.ToInt32(reader("Id")),
                                .Name = reader("Name").ToString(),
                                .IconName = If(reader.IsDBNull(reader.GetOrdinal("IconName")), "", reader("IconName").ToString()),
                                .DisplayOrder = Convert.ToInt32(reader("DisplayOrder")),
                                .IsActive = DbConnectionFactory.ToBool(reader("IsActive"))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Function GetServices(Optional categoryId As Integer? = Nothing, Optional search As String = "") As List(Of ServiceItem)
            Dim list As New List(Of ServiceItem)()

            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    Dim sql As String = "
                        SELECT s.Id, s.CategoryId, c.Name AS CategoryName, s.Name, s.Price, s.DurationMinutes, s.IsActive
                        FROM Services s
                        INNER JOIN ServiceCategories c ON s.CategoryId = c.Id
                        WHERE s.IsActive = 1
                    "

                    If categoryId.HasValue AndAlso categoryId.Value > 0 Then
                        sql &= " AND s.CategoryId = @catId"
                        cmd.Parameters.AddWithValue("@catId", categoryId.Value)
                    End If

                    If Not String.IsNullOrWhiteSpace(search) Then
                        sql &= " AND s.Name LIKE @search"
                        cmd.Parameters.AddWithValue("@search", $"%{search.Trim()}%")
                    End If

                    sql &= " ORDER BY s.CategoryId ASC, s.Name ASC;"
                    cmd.CommandText = sql

                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim sItem As New ServiceItem With {
                                .Id = Convert.ToInt32(reader("Id")),
                                .CategoryId = Convert.ToInt32(reader("CategoryId")),
                                .CategoryName = reader("CategoryName").ToString(),
                                .Name = reader("Name").ToString(),
                                .Price = Convert.ToDouble(reader("Price")),
                                .DurationMinutes = Convert.ToInt32(reader("DurationMinutes")),
                                .IsActive = DbConnectionFactory.ToBool(reader("IsActive"))
                            }
                            list.Add(sItem)
                        End While
                    End Using
                End Using

                ' Fetch Extras for services
                For Each s In list
                    s.Extras = GetExtrasForService(conn, s.Id)
                Next
            End Using

            Return list
        End Function

        Private Function GetExtrasForService(conn As SQLiteConnection, serviceId As Integer) As List(Of ServiceExtra)
            Dim extras As New List(Of ServiceExtra)()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "SELECT Id, ServiceId, Name, Price, IsActive FROM ServiceExtras WHERE ServiceId = @sid AND IsActive = 1 ORDER BY Name ASC;"
                cmd.Parameters.AddWithValue("@sid", serviceId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        extras.Add(New ServiceExtra With {
                            .Id = Convert.ToInt32(reader("Id")),
                            .ServiceId = Convert.ToInt32(reader("ServiceId")),
                            .Name = reader("Name").ToString(),
                            .Price = Convert.ToDouble(reader("Price")),
                            .IsActive = Convert.ToBoolean(reader("IsActive"))
                        })
                    End While
                End Using
            End Using
            Return extras
        End Function

        Public Sub SaveCategory(cat As ServiceCategory)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    If cat.Id = 0 Then
                        cmd.CommandText = "INSERT INTO ServiceCategories (Name, IconName, DisplayOrder, IsActive) VALUES (@n, @i, @o, @a);"
                    Else
                        cmd.CommandText = "UPDATE ServiceCategories SET Name = @n, IconName = @i, DisplayOrder = @o, IsActive = @a WHERE Id = @id;"
                        cmd.Parameters.AddWithValue("@id", cat.Id)
                    End If
                    cmd.Parameters.AddWithValue("@n", cat.Name)
                    cmd.Parameters.AddWithValue("@i", If(cat.IconName, ""))
                    cmd.Parameters.AddWithValue("@o", cat.DisplayOrder)
                    cmd.Parameters.AddWithValue("@a", If(cat.IsActive, 1, 0))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub SaveService(service As ServiceItem)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    If service.Id = 0 Then
                        cmd.CommandText = "INSERT INTO Services (CategoryId, Name, Price, DurationMinutes, IsActive) VALUES (@c, @n, @p, @d, @a);"
                    Else
                        cmd.CommandText = "UPDATE Services SET CategoryId = @c, Name = @n, Price = @p, DurationMinutes = @d, IsActive = @a WHERE Id = @id;"
                        cmd.Parameters.AddWithValue("@id", service.Id)
                    End If
                    cmd.Parameters.AddWithValue("@c", service.CategoryId)
                    cmd.Parameters.AddWithValue("@n", service.Name)
                    cmd.Parameters.AddWithValue("@p", service.Price)
                    cmd.Parameters.AddWithValue("@d", service.DurationMinutes)
                    cmd.Parameters.AddWithValue("@a", If(service.IsActive, 1, 0))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub SaveExtra(extra As ServiceExtra)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    If extra.Id = 0 Then
                        cmd.CommandText = "INSERT INTO ServiceExtras (ServiceId, Name, Price, IsActive) VALUES (@s, @n, @p, @a);"
                    Else
                        cmd.CommandText = "UPDATE ServiceExtras SET ServiceId = @s, Name = @n, Price = @p, IsActive = @a WHERE Id = @id;"
                        cmd.Parameters.AddWithValue("@id", extra.Id)
                    End If
                    cmd.Parameters.AddWithValue("@s", extra.ServiceId)
                    cmd.Parameters.AddWithValue("@n", extra.Name)
                    cmd.Parameters.AddWithValue("@p", extra.Price)
                    cmd.Parameters.AddWithValue("@a", If(extra.IsActive, 1, 0))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub DeleteExtra(extraId As Integer)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "DELETE FROM ServiceExtras WHERE Id = @id;"
                    cmd.Parameters.AddWithValue("@id", extraId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Class

End Namespace
