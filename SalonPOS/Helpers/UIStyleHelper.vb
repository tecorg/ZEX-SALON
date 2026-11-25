Imports System.Drawing
Imports System.Windows.Forms

Namespace SalonPOS.Helpers

    Public Class UIStyleHelper
        ' Brand Palette
        Public Shared ReadOnly PrimaryColor As Color = Color.FromArgb(41, 128, 185)       ' Rich Blue
        Public Shared ReadOnly DarkHeaderColor As Color = Color.FromArgb(44, 62, 80)       ' Midnight Slate
        Public Shared ReadOnly LightBgColor As Color = Color.FromArgb(245, 247, 250)      ' Clean Light Gray
        Public Shared ReadOnly CardBorderColor As Color = Color.FromArgb(220, 224, 230)

        ' ZEX SALON Palette
        Public Shared ReadOnly ZexNavy As Color = Color.FromArgb(16, 59, 89)           ' Deep Navy #103B59
        Public Shared ReadOnly ZexGold As Color = Color.FromArgb(201, 154, 70)          ' Premium Gold #C99A46
        Public Shared ReadOnly ZexTextDark As Color = Color.FromArgb(29, 43, 56)        ' Dark Text #1D2B38
        Public Shared ReadOnly ZexTextMuted As Color = Color.FromArgb(123, 135, 148)    ' Muted Text #7B8794
        Public Shared ReadOnly ZexBorder As Color = Color.FromArgb(210, 215, 222)       ' Crisp Border #D2D7DE

        ' Status Colors for Chairs
        Public Shared ReadOnly ColorAvailable As Color = Color.FromArgb(34, 168, 102)     ' Emerald Green
        Public Shared ReadOnly ColorInService As Color = Color.FromArgb(217, 144, 32)     ' Amber Orange
        Public Shared ReadOnly ColorWaitingPayment As Color = Color.FromArgb(217, 83, 79)  ' Soft Red

        ' Accent & Action Buttons
        Public Shared ReadOnly ActionGreen As Color = Color.FromArgb(39, 174, 96)
        Public Shared ReadOnly ActionRed As Color = Color.FromArgb(192, 57, 43)

        ' Primary Font Settings
        Public Const FontName As String = "Tajawal"

        Public Shared Function GetFont(size As Single, Optional style As FontStyle = FontStyle.Regular) As Font
            Try
                Return New Font(FontName, size, style)
            Catch
                Return New Font("Segoe UI", size, style)
            End Try
        End Function

        ' Crisp High-DPI Fonts (Tajawal)
        Public Shared ReadOnly FontTitle As Font = GetFont(18.0F, FontStyle.Bold)
        Public Shared ReadOnly FontSubtitle As Font = GetFont(13.0F, FontStyle.Bold)
        Public Shared ReadOnly FontCardTitle As Font = GetFont(14.0F, FontStyle.Bold)
        Public Shared ReadOnly FontRegular As Font = GetFont(11.0F, FontStyle.Regular)
        Public Shared ReadOnly FontBold As Font = GetFont(11.0F, FontStyle.Bold)
        Public Shared ReadOnly FontPrice As Font = GetFont(15.0F, FontStyle.Bold)

        Public Shared Sub ApplyFormDefaults(form As Form)
            form.RightToLeft = RightToLeft.Yes
            form.RightToLeftLayout = False ' Keep standard coordinate system (Right = Visual Right, Left = Visual Left)
            form.Font = FontRegular
            form.BackColor = LightBgColor
            form.StartPosition = FormStartPosition.CenterScreen
            form.AutoScaleMode = AutoScaleMode.Dpi
        End Sub

        Public Shared Sub ApplyButtonFlat(btn As Button, bg As Color, fg As Color)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = bg
            btn.ForeColor = fg
            btn.Font = FontBold
            btn.Cursor = Cursors.Hand
        End Sub

        Public Shared Sub ApplyButtonFlatWithBorder(btn As Button, bg As Color, fg As Color, borderColor As Color, borderWidth As Integer)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = borderWidth
            btn.FlatAppearance.BorderColor = borderColor
            btn.BackColor = bg
            btn.ForeColor = fg
            btn.Font = FontBold
            btn.Cursor = Cursors.Hand
        End Sub

        ''' <summary>
        ''' Applies executive ZEX SALON styling to DataGridView controls across all screens.
        ''' </summary>
        Public Shared Sub ApplyDataGridStyle(dgv As DataGridView)
            dgv.EnableHeadersVisualStyles = False
            dgv.RightToLeft = RightToLeft.Yes
            dgv.Font = FontRegular
            dgv.BackgroundColor = Color.White
            dgv.BorderStyle = BorderStyle.FixedSingle
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            dgv.GridColor = Color.FromArgb(230, 235, 240)
            dgv.RowHeadersVisible = False
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            dgv.MultiSelect = False
            dgv.RowTemplate.Height = 38
            dgv.AllowUserToResizeRows = False

            ' Executive Header Style (ZEX SALON Deep Navy #103B59)
            dgv.ColumnHeadersHeight = 42
            dgv.ColumnHeadersDefaultCellStyle.BackColor = ZexNavy
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            dgv.ColumnHeadersDefaultCellStyle.Font = GetFont(11.0F, FontStyle.Bold)
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

            ' Default Cell Style
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 243, 250)
            dgv.DefaultCellStyle.SelectionForeColor = ZexTextDark
            dgv.DefaultCellStyle.Font = FontRegular
            dgv.DefaultCellStyle.ForeColor = ZexTextDark
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        End Sub
    End Class

End Namespace
