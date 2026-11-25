Imports System
Imports System.Security.Cryptography
Imports System.Text

Namespace SalonLicenseGenerator

    Public Class RsaKeyManager
        ' RSA 2048-bit Private Key for signing licenses
        Private Shared ReadOnly XmlPrivateKey As String =
            "<RSAKeyValue><Modulus>4suo5VwVQPhlyVVWHHRB5AtGKvCDPykdqP8Mc/MtnpazmF3HHgK92U48SNpiD7XHvHyuLVpX5hqeFOK9pC9cQGmKmnM2OEZ5EO1f6nxAKuJUffKUIXEpqsIUwMbK8or+2gthf7MsqyqLehe2DWLLu4H0NjHOAOtC45v3fqsx+AinV9VuiTEGi9cu49A/o4BhGP0tZSA4DmIauhFA+dTGZercG4xlhSY2rIW2cT9Qm5RDfdC+6qPhbfbANcTd0zKsaMiEWsQsxFQfm8ii5Bp+N3uBXjfr/JrCvfVUDw1C81ENX+t4FDPNoyIa7OKduXGFBaoSfUHDW5fpK8DFYc5ZIQ==</Modulus><Exponent>AQAB</Exponent><P>+ocyZGhHxGPWH7qLiDrU6cR0AYzOVgzdx6Q+eYyEHJPRfiVYqdwuOYjpvSvSGpt2FC25dr706foQNA33lUBAs7ud+dqGbOMxT+u8WgXt8uWkk2h5ZdLplGZLqmR87z3kVdwntugJAyEPRzAVMhSwXAmUYamYtfOO8sUe6sOTfbM=</P><Q>57/DvTkYbRb6Jdny3fWh70kZLhKFVDTswXhHDFda4CEWXJ/1a/Tb9ktmnInAz4ulpouj+vCMidK78Lbh7wyFBbxTcvHdq/r/1JUf+4nW0pMLgweX/pZR/N0W0Z+1re1eyO3P26Qa0lMIlpuwScyVSKj2VPk8yZwzS6Yql8KIa9s=</Q><DP>GycSCB3hQWDMsU2nl9FAiEGZjR10s5DbbKrZdpULMIm41+I2VNqfS+8R3kXklKuqxY8bHcqegslufiXRYqS/HnvEKHNNxHlvO8lB9cPUwVGwX6WgGrAcTtAzfTk87TUZDVE6ILHPYD5yCHYmpfHmF2OWaGtYOMg4AwnoSut9jc8=</DP><DQ>4zyobLFAXy9JhrXnq3aAG0Y81203qPLgN3B9kf9RzzUCXT5HglytFhqm+dUk5x0SB3UjDBq0YyvsNsLMVP6crJwV4z5/0nOiSMsUeiuVJhMjFY7rnam9qMkM5+zHU0zORQzSZJbL6fGbGTp9qE9fDckHCRj+Xt23enTrjLUl2UM=</DQ><InverseQ>Fhqjc+LYDOAdNtPNWW9z5hAugbjrLnHm52GXsv1FZD6iXD0ZTEuz0tBKfvDRsMWpYQoKZaXYQZNuKTlLg+Yzv/O+YK87vQ9eo72hwXxvDgDBF+HER6zm62HhHsPEyFuWgVdEzwwRGZK3wHALZ7gtWGzio1MAaqRnUytqK6swmMs=</InverseQ><D>Qt18rDKb6Y3SpKtOOhFko9FXHK3QZDzBC+Q2TbEykAEqbg+n9wcj5f+U8n3AT5Vuj/nKeICcTbEnTk0TUGbOMgdr3+F76NeONhlmIAIGAR69fmyYk8uDV17lM3jouN2WCb81PasbzlM60AJRoFyyaFQGJjxE2iyCigyfyW67yr5kN3c+e1Isn1qESuh8zdfPjl+0WC7Y16sQOXzK6cyPbGrQaBby9M0Wj4QcFRDdHBW1bQX5jSe3Xr7rGT2qJWHVtoAT59wXwdAqbGOi4OxtDYN3wUMG0cJNGjBVFc4k9nh1ESzZTWY369Z+81ZddhhpiNhEAn/M9TnHn/sJcTCIiQ==</D></RSAKeyValue>"

        Public Shared Function GenerateLicenseKey(customerName As String, machineId As String, expiryDate As DateTime, isLifetime As Boolean) As String
            Try
                ' Create JSON payload string
                Dim payloadJson As String = "{" &
                    $"""CustomerName"":""{EscapeJson(customerName)}""," &
                    $"""ProductName"":""Simple Salon POS""," &
                    $"""Edition"":""Professional""," &
                    $"""MachineId"":""{EscapeJson(machineId)}""," &
                    $"""ExpiryDate"":""{expiryDate:yyyy-MM-dd}""," &
                    $"""IsLifetime"":{isLifetime.ToString().ToLower()}," &
                    $"""MaxDevices"":1" &
                    "}"

                Dim payloadBytes As Byte() = Encoding.UTF8.GetBytes(payloadJson)
                Dim payloadBase64 As String = Convert.ToBase64String(payloadBytes)

                ' Sign payload using RSA SHA-256 with RSACng / RSA.Create()
                Dim signatureBytes As Byte()
                Using rsa As RSA = RSA.Create()
                    rsa.FromXmlString(XmlPrivateKey)
                    signatureBytes = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                End Using

                Dim signatureBase64 As String = Convert.ToBase64String(signatureBytes)

                ' Combine PayloadBase64 . SignatureBase64
                Return $"{payloadBase64}.{signatureBase64}"

            Catch ex As Exception
                Throw New Exception($"فشل توليد التوقيع الرقمي للترخيص: {ex.Message}")
            End Try
        End Function

        Private Shared Function EscapeJson(str As String) As String
            If String.IsNullOrEmpty(str) Then Return ""
            Return str.Replace("\", "\\").Replace("""", "\""")
        End Function
    End Class

End Namespace
