Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports SalonPOS.Data
Imports SalonPOS.Forms
Imports SalonPOS.Licensing
Imports SalonPOS.Services

Namespace SalonPOS

    Public Module Program

        <DllImport("user32.dll")>
        Private Function SetProcessDPIAware() As Boolean
        End Function

        <STAThread>
        Public Sub Main()
            Try
                SetProcessDPIAware()
            Catch
            End Try

            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)

            Try
                ' 1. Initialize SQLite Database Schema & Default Seed Data
                DatabaseInitializer.InitializeDatabase()

                ' 2. Perform daily automatic database backup
                BackupService.PerformDailyAutoBackup()

                ' 3. Validate License
                Dim licResult = LicenseValidator.ValidateCurrentLicense()
                If licResult.Status <> LicenseStatus.ACTIVE Then
                    Dim activationForm As New FrmLicenseActivation()
                    If activationForm.ShowDialog() <> DialogResult.OK Then
                        Return
                    End If
                End If

                ' 4. Launch Login Form
                Application.Run(New FrmLogin())

            Catch ex As Exception
                MessageBox.Show($"تعذر تشغيل النظام: {ex.Message}", "خطأ تشغيلي", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

    End Module

End Namespace
