Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Data
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Models
Imports SalonPOS.Services

Namespace SalonPOS.Forms

    Public Class FrmExpenses
        Inherits Form

        Private expRepo As New ExpenseRepository()

        Private txtAmount As TextBox
        Private cboCategory As ComboBox
        Private txtNotes As TextBox
        Private btnAddExpense As Button
        Private dgvExpenses As DataGridView

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Text = "إدارة المصروفات اليومية"
            InitializeLayout()
            LoadExpenses()
        End Sub

        Private Sub InitializeLayout()
            Me.Controls.Clear()

            ' 1. Form Title Header
            Dim lblTitle As New Label With {
                .Dock = DockStyle.Top,
                .Height = 42,
                .Text = "تسجيل مصروف جديد",
                .Font = UIStyleHelper.FontSubtitle,
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight,
                .Padding = New Padding(15, 0, 15, 0)
            }

            ' 2. Clean Flow Entry Panel (Right-To-Left Flow)
            Dim pnlEntryCard As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 85,
                .BackColor = Color.White,
                .Padding = New Padding(12)
            }

            Dim flowEntry As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .AutoScroll = False
            }

            ' Group 1: Amount
            Dim pnlA As New Panel With {.Width = 130, .Height = 60}
            Dim lblA As New Label With {.Text = "المبلغ (ج.م):", .Dock = DockStyle.Top, .Height = 22, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy}
            txtAmount = New TextBox With {.Dock = DockStyle.Top, .Height = 30, .Font = UIStyleHelper.FontRegular, .RightToLeft = RightToLeft.Yes}
            pnlA.Controls.Add(txtAmount)
            pnlA.Controls.Add(lblA)

            ' Group 2: Category
            Dim pnlC As New Panel With {.Width = 200, .Height = 60, .Margin = New Padding(10, 0, 10, 0)}
            Dim lblC As New Label With {.Text = "نوع المصروف:", .Dock = DockStyle.Top, .Height = 22, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy}
            cboCategory = New ComboBox With {.Dock = DockStyle.Top, .Height = 30, .Font = UIStyleHelper.FontRegular, .DropDownStyle = ComboBoxStyle.DropDownList, .RightToLeft = RightToLeft.Yes}
            cboCategory.Items.AddRange(New String() {"أدوات ومستلزمات", "مياه وكهرباء", "شاي وقهوة وضيافة", "صيانة ونظافة", "مصروفات إدارية", "أخرى"})
            cboCategory.SelectedIndex = 0
            pnlC.Controls.Add(cboCategory)
            pnlC.Controls.Add(lblC)

            ' Group 3: Notes
            Dim pnlN As New Panel With {.Width = 360, .Height = 60, .Margin = New Padding(10, 0, 10, 0)}
            Dim lblN As New Label With {.Text = "ملاحظات وتفاصيل:", .Dock = DockStyle.Top, .Height = 22, .Font = UIStyleHelper.FontBold, .ForeColor = UIStyleHelper.ZexNavy}
            txtNotes = New TextBox With {.Dock = DockStyle.Top, .Height = 30, .Font = UIStyleHelper.FontRegular, .RightToLeft = RightToLeft.Yes}
            pnlN.Controls.Add(txtNotes)
            pnlN.Controls.Add(lblN)

            ' Group 4: Add Button
            Dim pnlB As New Panel With {.Width = 170, .Height = 60, .Margin = New Padding(10, 0, 10, 0)}
            btnAddExpense = New Button With {
                .Text = "+ إضافة مصروف",
                .Dock = DockStyle.Bottom,
                .Height = 38,
                .Font = UIStyleHelper.FontBold
            }
            UIStyleHelper.ApplyButtonFlat(btnAddExpense, UIStyleHelper.ActionGreen, Color.White)
            AddHandler btnAddExpense.Click, AddressOf OnAddExpenseClicked
            pnlB.Controls.Add(btnAddExpense)

            flowEntry.Controls.Add(pnlA)
            flowEntry.Controls.Add(pnlC)
            flowEntry.Controls.Add(pnlN)
            flowEntry.Controls.Add(pnlB)
            pnlEntryCard.Controls.Add(flowEntry)

            ' 3. Grid Container (100% Full-Width)
            Dim pnlGridContainer As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(12),
                .BackColor = UIStyleHelper.LightBgColor
            }

            dgvExpenses = New DataGridView With {
                .Dock = DockStyle.Fill,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            }
            UIStyleHelper.ApplyDataGridStyle(dgvExpenses)
            pnlGridContainer.Controls.Add(dgvExpenses)

            Me.Controls.Add(pnlGridContainer)
            Me.Controls.Add(pnlEntryCard)
            Me.Controls.Add(lblTitle)
        End Sub

        Private Sub LoadExpenses()
            Using conn = DbConnectionFactory.CreateConnection()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        SELECT Category AS 'نوع المصروف',
                               Amount AS 'المبلغ (ج.م)',
                               Notes AS 'ملاحظات',
                               CreatedAt AS 'التاريخ والوقت'
                        FROM Expenses
                        WHERE date(CreatedAt) = date('now', 'localtime')
                        ORDER BY Id DESC;
                    "
                    Dim dt As New System.Data.DataTable()
                    dt.Load(cmd.ExecuteReader())
                    dgvExpenses.DataSource = dt
                End Using
            End Using
        End Sub

        Private Sub OnAddExpenseClicked(sender As Object, e As EventArgs)
            Dim amt As Double = 0
            If Not Double.TryParse(txtAmount.Text, amt) OrElse amt <= 0 Then
                MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من الصف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim activeShift = AuthService.CurrentShift
            Dim shiftId As Integer = If(activeShift IsNot Nothing, activeShift.Id, 1)
            Dim userId As Integer = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.Id, 1)

            Dim catName As String = cboCategory.SelectedItem.ToString()
            Dim noteStr As String = txtNotes.Text.Trim()
            Dim userName As String = If(AuthService.CurrentUser IsNot Nothing, AuthService.CurrentUser.DisplayName, "مستخدم")

            expRepo.AddExpense(New Expense With {
                .Amount = amt,
                .Category = catName,
                .Notes = noteStr,
                .ShiftId = shiftId,
                .CreatedByUserId = userId
            })

            ActivityLogRepository.Log(userId, userName, "تسجيل مصروف", $"تم إضافة مصروف بقيمة {amt:N2} ج.م (البند: {catName} - ملاحظات: {noteStr})")

            txtAmount.Text = ""
            txtNotes.Text = ""
            MessageBox.Show("تم إضافة المصروف بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information)
            LoadExpenses()
        End Sub
    End Class

End Namespace
