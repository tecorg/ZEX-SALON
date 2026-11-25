Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Services

Namespace SalonPOS.Forms

    Public Class FrmLogin
        Inherits Form

        ' Repositories
        Private userRepo As New UserRepository()
        Private shiftRepo As New ShiftRepository()

        ' Split Panels
        Private pnlLeftHero As Panel
        Private pnlRightContainer As Panel
        Private pnlCard As Panel

        ' Form Controls
        Private txtUsername As TextBox
        Private txtPassword As TextBox
        Private btnEyeToggle As Button
        Private chkRememberMe As CheckBox
        Private btnLogin As Button
        Private pnlErrorAlert As Panel
        Private lblErrorAlert As Label

        ' Input Container Panels for Focus Bordering
        Private pnlUserContainer As Panel
        Private pnlPassContainer As Panel

        Public Sub New()
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.Text = "ZEX SALON — تسجيل الدخول"
            Me.Size = New Size(1280, 768)
            Me.MinimumSize = New Size(1024, 700)
            Me.WindowState = FormWindowState.Maximized
            Me.BackColor = Color.FromArgb(247, 248, 250)

            InitializeLayout()
        End Sub

        Private Sub InitializeLayout()
            Me.Controls.Clear()

            ' 1. Left Visual Hero Area (55% Width) - Custom Painted Luxury Salon Ambient
            pnlLeftHero = New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = UIStyleHelper.ZexNavy
            }
            AddHandler pnlLeftHero.Paint, AddressOf OnPaintLeftHero

            ' 2. Right Login Container (45% Width ~540px)
            pnlRightContainer = New Panel With {
                .Dock = DockStyle.Right,
                .Width = 540,
                .BackColor = Color.FromArgb(247, 248, 250),
                .Padding = New Padding(20)
            }
            AddHandler pnlRightContainer.Resize, Sub() CenterLoginCard()

            ' 3. Centered White Login Card
            pnlCard = New Panel With {
                .Width = 450,
                .Height = 620,
                .BackColor = Color.White,
                .Padding = New Padding(30, 25, 30, 25)
            }
            AddHandler pnlCard.Paint, AddressOf OnPaintLoginCard

            BuildLoginCardContent()

            pnlRightContainer.Controls.Add(pnlCard)
            Me.Controls.Add(pnlLeftHero)
            Me.Controls.Add(pnlRightContainer)

            CenterLoginCard()
            Me.AcceptButton = btnLogin
        End Sub

        Private Sub CenterLoginCard()
            If pnlRightContainer IsNot Nothing AndAlso pnlCard IsNot Nothing Then
                Dim topPos As Integer = Math.Max(20, (pnlRightContainer.Height - pnlCard.Height) \ 2)
                Dim leftPos As Integer = Math.Max(20, (pnlRightContainer.Width - pnlCard.Width) \ 2)
                pnlCard.Location = New Point(leftPos, topPos)
            End If
        End Sub

        Private Sub BuildLoginCardContent()
            pnlCard.Controls.Clear()

            ' A. Brand Title Header
            Dim lblBrand As New Label With {
                .Dock = DockStyle.Top,
                .Height = 42,
                .Text = "ZEX SALON",
                .Font = UIStyleHelper.GetFont(24.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            Dim lblSubBrand As New Label With {
                .Dock = DockStyle.Top,
                .Height = 25,
                .Text = "نظام إدارة وتشغيل الصالونات",
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexGold,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            ' Divider
            Dim pnlDivider As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 2,
                .BackColor = UIStyleHelper.ZexGold,
                .Margin = New Padding(0, 5, 0, 15)
            }

            ' B. Login Greeting & Subtitle
            Dim lblHeading As New Label With {
                .Dock = DockStyle.Top,
                .Height = 35,
                .Text = "تسجيل الدخول",
                .Font = UIStyleHelper.GetFont(15.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexTextDark,
                .TextAlign = ContentAlignment.MiddleRight
            }

            Dim lblSubHeading As New Label With {
                .Dock = DockStyle.Top,
                .Height = 25,
                .Text = "أدخل بيانات حسابك للمتابعة",
                .Font = UIStyleHelper.FontRegular,
                .ForeColor = UIStyleHelper.ZexTextMuted,
                .TextAlign = ContentAlignment.MiddleRight
            }

            ' Space
            Dim pnlSpace1 As New Panel With {.Dock = DockStyle.Top, .Height = 15}

            ' C. Username Field Group
            Dim lblUsernameLabel As New Label With {
                .Dock = DockStyle.Top,
                .Height = 25,
                .Text = "اسم المستخدم",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexTextDark,
                .TextAlign = ContentAlignment.MiddleRight
            }

            pnlUserContainer = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 46,
                .BackColor = Color.FromArgb(250, 251, 253),
                .Padding = New Padding(5)
            }
            AddHandler pnlUserContainer.Paint, Sub(s, pe) DrawInputContainerBorder(pnlUserContainer, pe.Graphics, txtUsername.Focused)

            ' User Icon Picture/Paint
            Dim pnlUserIcon As New Panel With {.Dock = DockStyle.Right, .Width = 40}
            AddHandler pnlUserIcon.Paint, AddressOf OnPaintUserIcon

            txtUsername = New TextBox With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.GetFont(11.0F),
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.FromArgb(250, 251, 253),
                .Text = "admin"
            }
            AddHandler txtUsername.GotFocus, Sub() pnlUserContainer.Invalidate()
            AddHandler txtUsername.LostFocus, Sub() pnlUserContainer.Invalidate()

            pnlUserContainer.Controls.Add(txtUsername)
            pnlUserContainer.Controls.Add(pnlUserIcon)

            ' Space
            Dim pnlSpace2 As New Panel With {.Dock = DockStyle.Top, .Height = 12}

            ' D. Password Field Group
            Dim lblPasswordLabel As New Label With {
                .Dock = DockStyle.Top,
                .Height = 25,
                .Text = "كلمة المرور",
                .Font = UIStyleHelper.FontBold,
                .ForeColor = UIStyleHelper.ZexTextDark,
                .TextAlign = ContentAlignment.MiddleRight
            }

            pnlPassContainer = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 46,
                .BackColor = Color.FromArgb(250, 251, 253),
                .Padding = New Padding(5)
            }
            AddHandler pnlPassContainer.Paint, Sub(s, pe) DrawInputContainerBorder(pnlPassContainer, pe.Graphics, txtPassword.Focused)

            ' Lock Icon
            Dim pnlLockIcon As New Panel With {.Dock = DockStyle.Right, .Width = 40}
            AddHandler pnlLockIcon.Paint, AddressOf OnPaintLockIcon

            ' Eye Toggle Button
            btnEyeToggle = New Button With {
                .Dock = DockStyle.Left,
                .Width = 40,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            btnEyeToggle.FlatAppearance.BorderSize = 0
            btnEyeToggle.FlatAppearance.MouseDownBackColor = Color.Transparent
            btnEyeToggle.FlatAppearance.MouseOverBackColor = Color.Transparent
            AddHandler btnEyeToggle.Paint, AddressOf OnPaintEyeIcon
            AddHandler btnEyeToggle.Click, AddressOf OnEyeToggleClicked

            txtPassword = New TextBox With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.GetFont(11.0F),
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.FromArgb(250, 251, 253),
                .UseSystemPasswordChar = True,
                .Text = "admin123"
            }
            AddHandler txtPassword.GotFocus, Sub() pnlPassContainer.Invalidate()
            AddHandler txtPassword.LostFocus, Sub() pnlPassContainer.Invalidate()

            pnlPassContainer.Controls.Add(txtPassword)
            pnlPassContainer.Controls.Add(btnEyeToggle)
            pnlPassContainer.Controls.Add(pnlLockIcon)

            ' Space
            Dim pnlSpace3 As New Panel With {.Dock = DockStyle.Top, .Height = 10}

            ' E. Remember Me CheckBox
            chkRememberMe = New CheckBox With {
                .Dock = DockStyle.Top,
                .Height = 25,
                .Text = "تذكرني على هذا الجهاز",
                .Font = UIStyleHelper.FontRegular,
                .ForeColor = UIStyleHelper.ZexTextDark,
                .Checked = True
            }

            ' Space
            Dim pnlSpace4 As New Panel With {.Dock = DockStyle.Top, .Height = 10}

            ' F. Inline Error Alert Banner
            pnlErrorAlert = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 36,
                .BackColor = Color.FromArgb(250, 219, 216),
                .Visible = False,
                .Padding = New Padding(5)
            }
            lblErrorAlert = New Label With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.FromArgb(201, 75, 75),
                .TextAlign = ContentAlignment.MiddleCenter,
                .Text = "⚠ اسم المستخدم أو كلمة المرور غير صحيحة"
            }
            pnlErrorAlert.Controls.Add(lblErrorAlert)

            ' Space
            Dim pnlSpace5 As New Panel With {.Dock = DockStyle.Top, .Height = 15}

            ' G. Login Button
            btnLogin = New Button With {
                .Dock = DockStyle.Top,
                .Height = 52,
                .Text = "تسجيل الدخول",
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                .ForeColor = Color.White,
                .BackColor = UIStyleHelper.ZexNavy,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            btnLogin.FlatAppearance.BorderSize = 0
            AddHandler btnLogin.MouseEnter, Sub() btnLogin.BackColor = Color.FromArgb(26, 82, 118)
            AddHandler btnLogin.MouseLeave, Sub() btnLogin.BackColor = UIStyleHelper.ZexNavy
            AddHandler btnLogin.Click, AddressOf OnLoginClicked

            ' H. Bottom Support Footer Info Inside Card
            Dim pnlCardFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 65,
                .BackColor = Color.Transparent
            }

            Dim lblDefaultHint As New Label With {
                .Dock = DockStyle.Top,
                .Height = 22,
                .Text = "🔑 الحساب الافتراضي: admin | كلمة المرور: admin123",
                .Font = UIStyleHelper.GetFont(9.0F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(41, 128, 185),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            Dim lblSupport As New Label With {
                .Dock = DockStyle.Top,
                .Height = 20,
                .Text = "الدعم الفني: 01022422010 | 01096060003",
                .Font = UIStyleHelper.GetFont(8.5F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            Dim currentYear As Integer = DateTime.Now.Year
            Dim lblCopyright As New Label With {
                .Dock = DockStyle.Bottom,
                .Height = 20,
                .Text = $"Powered by ZEX  •  Nowzex.com  •  © {currentYear}",
                .Font = UIStyleHelper.GetFont(8.0F, FontStyle.Regular),
                .ForeColor = UIStyleHelper.ZexTextMuted,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            pnlCardFooter.Controls.Add(lblSupport)
            pnlCardFooter.Controls.Add(lblDefaultHint)
            pnlCardFooter.Controls.Add(lblCopyright)

            ' Add controls in Top-to-Bottom order
            pnlCard.Controls.Add(pnlCardFooter)
            pnlCard.Controls.Add(btnLogin)
            pnlCard.Controls.Add(pnlSpace5)
            pnlCard.Controls.Add(pnlErrorAlert)
            pnlCard.Controls.Add(pnlSpace4)
            pnlCard.Controls.Add(chkRememberMe)
            pnlCard.Controls.Add(pnlSpace3)
            pnlCard.Controls.Add(pnlPassContainer)
            pnlCard.Controls.Add(lblPasswordLabel)
            pnlCard.Controls.Add(pnlSpace2)
            pnlCard.Controls.Add(pnlUserContainer)
            pnlCard.Controls.Add(lblUsernameLabel)
            pnlCard.Controls.Add(pnlSpace1)
            pnlCard.Controls.Add(lblSubHeading)
            pnlCard.Controls.Add(lblHeading)
            pnlCard.Controls.Add(pnlDivider)
            pnlCard.Controls.Add(lblSubBrand)
            pnlCard.Controls.Add(lblBrand)
        End Sub

        ' =========================================================================
        ' DRAWING & GDI+ HELPERS FOR HIGH-END COMMERCIAL LOOK
        ' =========================================================================

        Private Sub OnPaintLeftHero(sender As Object, e As PaintEventArgs)
            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias

            Dim rect As Rectangle = pnlLeftHero.ClientRectangle
            If rect.Width <= 0 OrElse rect.Height <= 0 Then Return

            ' Deep Navy Gradient Background
            Using br As New LinearGradientBrush(rect, UIStyleHelper.ZexNavy, Color.FromArgb(10, 35, 55), LinearGradientMode.ForwardDiagonal)
                g.FillRectangle(br, rect)
            End Using

            ' Subtle Warm Gold Ambient Light Rings
            Using penGold As New Pen(Color.FromArgb(35, UIStyleHelper.ZexGold), 2.5F)
                g.DrawEllipse(penGold, rect.Width - 300, -100, 500, 500)
                g.DrawEllipse(penGold, rect.Width - 250, -50, 400, 400)
                g.DrawEllipse(penGold, -100, rect.Height - 350, 450, 450)
            End Using

            ' Watermark & Brand Showcase Text
            Dim drawFontTitle As Font = UIStyleHelper.GetFont(36.0F, FontStyle.Bold)
            Dim drawFontSub As Font = UIStyleHelper.GetFont(16.0F, FontStyle.Regular)
            Dim drawFontDesc As Font = UIStyleHelper.GetFont(12.0F, FontStyle.Italic)

            Dim formatRtl As New StringFormat With {
                .Alignment = StringAlignment.Center,
                .LineAlignment = StringAlignment.Center
            }

            Dim centerY As Integer = rect.Height \ 2 - 40
            Dim centerX As Integer = rect.Width \ 2

            ' Draw Large Brand Title
            Using brGold As New SolidBrush(UIStyleHelper.ZexGold)
                g.DrawString("ZEX SALON", drawFontTitle, brGold, New PointF(centerX, centerY - 40), formatRtl)
            End Using

            ' Draw Subtitle
            Using brWhite As New SolidBrush(Color.White)
                g.DrawString("النظام الذكي لإدارة الصالونات ومراكز التجميل", drawFontSub, brWhite, New PointF(centerX, centerY + 30), formatRtl)
            End Using

            ' Draw Commercial Tagline
            Using brMuted As New SolidBrush(Color.FromArgb(180, 205, 225))
                g.DrawString("Smart, Fast & Premium Salon Operations", drawFontDesc, brMuted, New PointF(centerX, centerY + 75), formatRtl)
            End Using
        End Sub

        Private Sub OnPaintLoginCard(sender As Object, e As PaintEventArgs)
            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias

            Dim rect As Rectangle = pnlCard.ClientRectangle
            rect.Width -= 1
            rect.Height -= 1

            ' Subtle Card Border
            Using pen As New Pen(UIStyleHelper.ZexBorder, 1.5F)
                g.DrawRectangle(pen, rect)
            End Using

            ' Top Gold Accent Border Line
            Using penGold As New Pen(UIStyleHelper.ZexGold, 4.0F)
                g.DrawLine(penGold, 0, 2, rect.Width, 2)
            End Using
        End Sub

        Private Sub DrawInputContainerBorder(pnl As Panel, g As Graphics, isFocused As Boolean)
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim rect As Rectangle = pnl.ClientRectangle
            rect.Width -= 1
            rect.Height -= 1

            Dim borderClr As Color = If(isFocused, UIStyleHelper.ZexNavy, UIStyleHelper.ZexBorder)
            Dim borderWidth As Single = If(isFocused, 2.0F, 1.0F)

            Using pen As New Pen(borderClr, borderWidth)
                g.DrawRectangle(pen, rect)
            End Using
        End Sub

        Private Sub OnPaintUserIcon(sender As Object, e As PaintEventArgs)
            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Using pen As New Pen(UIStyleHelper.ZexTextMuted, 2.0F)
                ' Head circle
                g.DrawEllipse(pen, 12, 10, 14, 14)
                ' Body arc
                g.DrawArc(pen, 8, 24, 22, 16, 180, 180)
            End Using
        End Sub

        Private Sub OnPaintLockIcon(sender As Object, e As PaintEventArgs)
            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Using pen As New Pen(UIStyleHelper.ZexTextMuted, 2.0F)
                ' Lock shackle
                g.DrawArc(pen, 13, 10, 12, 14, 180, 180)
                ' Lock body
                g.DrawRectangle(pen, 10, 18, 18, 14)
            End Using
        End Sub

        Private Sub OnPaintEyeIcon(sender As Object, e As PaintEventArgs)
            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim eyeClr As Color = If(txtPassword.UseSystemPasswordChar, UIStyleHelper.ZexTextMuted, UIStyleHelper.ZexNavy)
            Using pen As New Pen(eyeClr, 2.0F)
                ' Eye outline
                g.DrawArc(pen, 8, 14, 22, 16, 30, 120)
                g.DrawArc(pen, 8, 8, 22, 16, 210, 120)
                ' Pupil
                Using br As New SolidBrush(eyeClr)
                    g.FillEllipse(br, 16, 17, 6, 6)
                End Using
            End Using
        End Sub

        Private Sub OnEyeToggleClicked(sender As Object, e As EventArgs)
            txtPassword.UseSystemPasswordChar = Not txtPassword.UseSystemPasswordChar
            btnEyeToggle.Invalidate()
        End Sub

        ' =========================================================================
        ' AUTHENTICATION & LOGIN LOGIC (PRESERVED EXACTLY AS IS)
        ' =========================================================================

        Private Sub ShowError(msg As String)
            lblErrorAlert.Text = msg
            pnlErrorAlert.Visible = True
        End Sub

        Private Sub HideError()
            pnlErrorAlert.Visible = False
        End Sub

        Private Sub OnLoginClicked(sender As Object, e As EventArgs)
            HideError()

            Dim username As String = txtUsername.Text.Trim()
            Dim password As String = txtPassword.Text

            If String.IsNullOrEmpty(username) Then
                ShowError("⚠ يرجى إدخال اسم المستخدم")
                txtUsername.Focus()
                Return
            End If

            If String.IsNullOrEmpty(password) Then
                ShowError("⚠ يرجى إدخال كلمة المرور")
                txtPassword.Focus()
                Return
            End If

            ' UI Feedback: Disabling button during authentication
            btnLogin.Enabled = False
            btnLogin.Text = "جاري تسجيل الدخول..."
            Application.DoEvents()

            Try
                Dim user = userRepo.Authenticate(username, password)
                If user IsNot Nothing Then
                    AuthService.CurrentUser = user
                    ActivityLogRepository.Log(user.Id, user.DisplayName, "تسجيل دخول", "تم تسجيل الدخول إلى النظام بنجاح")

                    Dim activeShift = shiftRepo.GetActiveShift(user.Id)
                    If activeShift Is Nothing Then
                        activeShift = shiftRepo.OpenShift(user.Id, 0.0)
                    End If
                    AuthService.CurrentShift = activeShift

                    Me.Hide()
                    Dim mainForm As New FrmMain()
                    mainForm.ShowDialog()
                    Me.Close()
                Else
                    ShowError("⚠ اسم المستخدم أو كلمة المرور غير صحيحة (الافتراضي: admin / admin123)")
                    btnLogin.Enabled = True
                    btnLogin.Text = "تسجيل الدخول"
                End If
            Catch ex As Exception
                ShowError($"⚠ تعذر تسجيل الدخول: {ex.Message}")
                btnLogin.Enabled = True
                btnLogin.Text = "تسجيل الدخول"
            End Try
        End Sub
    End Class

End Namespace
