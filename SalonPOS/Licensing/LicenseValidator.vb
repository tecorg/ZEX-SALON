Imports System.Data.SQLite
Imports System.Security.Cryptography
Imports System.Text
Imports SalonPOS.Data
Imports SalonPOS.Models

Namespace SalonPOS.Licensing

    Public Enum LicenseStatus
        ACTIVE
        EXPIRED
        INVALID
        DEVICE_MISMATCH
        NOT_ACTIVATED
    End Enum

    Public Class LicenseValidationResult
        Public Property Status As LicenseStatus
        Public Property Message As String
        Public Property License As LicenseData
    End Class

    Public Class LicenseValidator

        Public Shared Function ValidateCurrentLicense() As LicenseValidationResult
            Dim result As New LicenseValidationResult()
            Dim currentMachineId As String = MachineFingerprint.GetMachineId()

            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT LicenseKey, CustomerName, MachineId, ExpiryDate FROM LicenseInfo ORDER BY Id DESC LIMIT 1;"
                    Using reader = cmd.ExecuteReader()
                        If Not reader.Read() Then
                            result.Status = LicenseStatus.NOT_ACTIVATED
                            result.Message = "لم يتم تفعيل البرنامج بعد. يرجى إدخال كود التفعيل."
                            Return result
                        End If

                        Dim licenseKey As String = reader("LicenseKey").ToString()
                        Return ValidateKey(licenseKey, currentMachineId)
                    End Using
                End Using
            End Using
        End Function

        Public Shared Function ValidateKey(licenseKey As String, currentMachineId As String) As LicenseValidationResult
            Dim result As New LicenseValidationResult()

            If String.IsNullOrWhiteSpace(licenseKey) Then
                result.Status = LicenseStatus.NOT_ACTIVATED
                result.Message = "كود التفعيل فارغ."
                Return result
            End If

            Try
                ' LicenseKey format: Base64(PayloadJson) . Base64(Signature)
                Dim parts = licenseKey.Split("."c)
                If parts.Length <> 2 Then
                    result.Status = LicenseStatus.INVALID
                    result.Message = "رمز التفعيل غير صالح (تنسيق خاطئ)."
                    Return result
                End If

                Dim payloadJsonBytes = Convert.FromBase64String(parts(0))
                Dim signatureBytes = Convert.FromBase64String(parts(1))
                Dim payloadJson As String = Encoding.UTF8.GetString(payloadJsonBytes)

                ' Verify RSA Signature
                If Not VerifySignature(payloadJsonBytes, signatureBytes) Then
                    result.Status = LicenseStatus.INVALID
                    result.Message = "توقيع التفعيل غير مجاز أو تم التلاعب به."
                    Return result
                End If

                ' Parse payload fields manually or via regex/json
                Dim licenseObj = ParsePayload(payloadJson)
                licenseObj.LicenseKey = licenseKey
                result.License = licenseObj

                ' Check Machine ID
                If Not String.Equals(licenseObj.MachineId, currentMachineId, StringComparison.OrdinalIgnoreCase) Then
                    result.Status = LicenseStatus.DEVICE_MISMATCH
                    result.Message = $"رمز التفعيل مخصص لجهاز آخر ({licenseObj.MachineId}) وليس لهذا الجهاز ({currentMachineId})."
                    Return result
                End If

                ' Check Expiry Date
                If Not licenseObj.IsLifetime AndAlso DateTime.Now.Date > licenseObj.ExpiryDate.Date Then
                    result.Status = LicenseStatus.EXPIRED
                    result.Message = $"انتهت صلاحية الترخيص في ({licenseObj.ExpiryDate:yyyy-MM-dd}). يرجى التواصل للتجديد."
                    Return result
                End If

                result.Status = LicenseStatus.ACTIVE
                result.Message = "الترخيص سارٍ ونشط."
                Return result

            Catch ex As Exception
                result.Status = LicenseStatus.INVALID
                result.Message = "حدث خطأ أثناء فحص الترخيص."
                Return result
            End Try
        End Function

        Private Shared Function VerifySignature(dataBytes As Byte(), signatureBytes As Byte()) As Boolean
            Try
                Using rsa As RSA = RSA.Create()
                    rsa.FromXmlString(AppPublicKey.XmlPublicKey)
                    Return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                End Using
            Catch
                Return False
            End Try
        End Function

        Private Shared Function ParsePayload(json As String) As LicenseData
            Dim lic As New LicenseData()
            ' Lightweight manual JSON key-value extraction
            lic.CustomerName = ExtractJsonValue(json, "CustomerName")
            lic.ProductName = ExtractJsonValue(json, "ProductName")
            lic.Edition = ExtractJsonValue(json, "Edition")
            lic.MachineId = ExtractJsonValue(json, "MachineId")

            Dim expStr As String = ExtractJsonValue(json, "ExpiryDate")
            If Not String.IsNullOrEmpty(expStr) AndAlso DateTime.TryParse(expStr, lic.ExpiryDate) Then
            Else
                lic.ExpiryDate = DateTime.MaxValue
            End If

            Dim lifetimeStr As String = ExtractJsonValue(json, "IsLifetime")
            Boolean.TryParse(lifetimeStr, lic.IsLifetime)

            Return lic
        End Function

        Private Shared Function ExtractJsonValue(json As String, key As String) As String
            Dim pattern As String = String.Format("""{0}""\s*:\s*""?([^"",}}]+)""?", key)
            Dim match = System.Text.RegularExpressions.Regex.Match(json, pattern)
            If match.Success Then
                Return match.Groups(1).Value.Trim()
            End If
            Return ""
        End Function

        Public Shared Sub SaveLicense(licenseKey As String, customerName As String, machineId As String, expiryDate As DateTime)
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "INSERT INTO LicenseInfo (LicenseKey, CustomerName, MachineId, ExpiryDate, ActivatedAt) VALUES (@key, @cust, @mid, @exp, @act);"
                    cmd.Parameters.AddWithValue("@key", licenseKey)
                    cmd.Parameters.AddWithValue("@cust", customerName)
                    cmd.Parameters.AddWithValue("@mid", machineId)
                    cmd.Parameters.AddWithValue("@exp", expiryDate.ToString("yyyy-MM-dd"))
                    cmd.Parameters.AddWithValue("@act", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Class

End Namespace
