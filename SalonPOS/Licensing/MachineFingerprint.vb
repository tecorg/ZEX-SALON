Imports System.Management
Imports System.Security.Cryptography
Imports System.Text

Namespace SalonPOS.Licensing

    Public Class MachineFingerprint
        Private Shared _cachedFingerprint As String = Nothing

        Public Shared Function GetMachineId() As String
            If Not String.IsNullOrEmpty(_cachedFingerprint) Then
                Return _cachedFingerprint
            End If

            Try
                Dim cpuId As String = GetCpuId()
                Dim mbSerial As String = GetMotherboardSerial()
                Dim driveSerial As String = GetSystemDriveSerial()

                Dim rawHardwareString As String = $"{cpuId}|{mbSerial}|{driveSerial}"
                If String.IsNullOrWhiteSpace(rawHardwareString) OrElse rawHardwareString = "||" Then
                    rawHardwareString = Environment.MachineName & "|" & Environment.UserName
                End If

                Using sha = SHA256.Create()
                    Dim hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawHardwareString))
                    Dim hexString As String = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper()
                    ' Format as SALON-XXXX-XXXX
                    _cachedFingerprint = $"SALON-{hexString.Substring(0, 4)}-{hexString.Substring(4, 4)}"
                End Using
            Catch ex As Exception
                ' Fallback
                _cachedFingerprint = $"SALON-{Environment.MachineName.GetHashCode():X4}-FREE"
            End Try

            Return _cachedFingerprint
        End Function

        Private Shared Function GetCpuId() As String
            Try
                Using searcher As New ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor")
                    For Each obj As ManagementObject In searcher.Get()
                        Return obj("ProcessorId")?.ToString()
                    Next
                End Using
            Catch
            End Try
            Return "CPU-GENERIC"
        End Function

        Private Shared Function GetMotherboardSerial() As String
            Try
                Using searcher As New ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard")
                    For Each obj As ManagementObject In searcher.Get()
                        Return obj("SerialNumber")?.ToString()
                    Next
                End Using
            Catch
            End Try
            Return "MB-GENERIC"
        End Function

        Private Shared Function GetSystemDriveSerial() As String
            Try
                Using searcher As New ManagementObjectSearcher("SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID='C:'")
                    For Each obj As ManagementObject In searcher.Get()
                        Return obj("VolumeSerialNumber")?.ToString()
                    Next
                End Using
            Catch
            End Try
            Return "DISK-GENERIC"
        End Function
    End Class

End Namespace
