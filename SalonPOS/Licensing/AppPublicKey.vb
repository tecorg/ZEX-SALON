Namespace SalonPOS.Licensing

    Public Class AppPublicKey
        ' RSA 2048-bit Public Key used by SalonPOS client app to verify signed licenses
        Public Shared ReadOnly Property XmlPublicKey As String =
            "<RSAKeyValue><Modulus>4suo5VwVQPhlyVVWHHRB5AtGKvCDPykdqP8Mc/MtnpazmF3HHgK92U48SNpiD7XHvHyuLVpX5hqeFOK9pC9cQGmKmnM2OEZ5EO1f6nxAKuJUffKUIXEpqsIUwMbK8or+2gthf7MsqyqLehe2DWLLu4H0NjHOAOtC45v3fqsx+AinV9VuiTEGi9cu49A/o4BhGP0tZSA4DmIauhFA+dTGZercG4xlhSY2rIW2cT9Qm5RDfdC+6qPhbfbANcTd0zKsaMiEWsQsxFQfm8ii5Bp+N3uBXjfr/JrCvfVUDw1C81ENX+t4FDPNoyIa7OKduXGFBaoSfUHDW5fpK8DFYc5ZIQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>"
    End Class

End Namespace
