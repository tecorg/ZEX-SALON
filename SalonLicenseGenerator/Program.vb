Imports System
Imports System.Windows.Forms

Namespace SalonLicenseGenerator

    Public Module Program
        <STAThread>
        Public Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Application.Run(New LicenseGeneratorForm())
        End Sub
    End Module

End Namespace
