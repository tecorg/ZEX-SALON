Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports SalonPOS.Helpers
Imports SalonPOS.Models

Namespace SalonPOS.Controls

    Public Class ServiceCardControl
        Inherits UserControl

        Private _service As ServiceItem
        Private lblName As Label
        Private lblPrice As Label
        Private lblDuration As Label
        Private _isHovered As Boolean = False

        Public Event ServiceClicked(sender As Object, service As ServiceItem)

        Public Sub New()
            InitializeLayout()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
        End Sub

        Public Property ServiceData As ServiceItem
            Get
                Return _service
            End Get
            Set(value As ServiceItem)
                _service = value
                UpdateUI()
            End Set
        End Property

        Private Sub InitializeLayout()
            Me.Size = New Size(225, 140)
            Me.Margin = New Padding(8)
            Me.BackColor = Color.White
            Me.Cursor = Cursors.Hand

            lblName = New Label With {
                .Dock = DockStyle.Top,
                .Height = 52,
                .Font = UIStyleHelper.GetFont(13.5F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(29, 43, 56),
                .TextAlign = ContentAlignment.MiddleCenter,
                .Padding = New Padding(4, 6, 4, 2),
                .BackColor = Color.Transparent
            }

            lblPrice = New Label With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.GetFont(17.0F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.FromArgb(248, 250, 253)
            }

            lblDuration = New Label With {
                .Dock = DockStyle.Bottom,
                .Height = 30,
                .Font = UIStyleHelper.GetFont(10.0F, FontStyle.Regular),
                .ForeColor = Color.FromArgb(100, 116, 139),
                .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.FromArgb(248, 250, 253)
            }

            Me.Controls.Add(lblPrice)
            Me.Controls.Add(lblDuration)
            Me.Controls.Add(lblName)

            AddHandlerClickRecursive(Me)
            AddHandlerHoverRecursive(Me)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias

            Dim rect As Rectangle = Me.ClientRectangle
            rect.Width -= 1
            rect.Height -= 1

            If _isHovered Then
                ' Highlight Border when mouse is over
                Using penHover As New Pen(UIStyleHelper.ZexGold, 2.0F)
                    g.DrawRectangle(penHover, rect)
                End Using
                ' Top Accent Line
                Using brAccent As New SolidBrush(UIStyleHelper.ZexGold)
                    g.FillRectangle(brAccent, 0, 0, Me.Width, 4)
                End Using
            Else
                ' Normal Crisp Border
                Using penNormal As New Pen(Color.FromArgb(226, 232, 240), 1.5F)
                    g.DrawRectangle(penNormal, rect)
                End Using
                ' Top Subtle Navy Accent Line
                Using brAccent As New SolidBrush(UIStyleHelper.ZexNavy)
                    g.FillRectangle(brAccent, 0, 0, Me.Width, 3)
                End Using
            End If
        End Sub

        Private Sub AddHandlerClickRecursive(ctrl As Control)
            AddHandler ctrl.Click, AddressOf OnControlClicked
            For Each child As Control In ctrl.Controls
                AddHandlerClickRecursive(child)
            Next
        End Sub

        Private Sub AddHandlerHoverRecursive(ctrl As Control)
            AddHandler ctrl.MouseEnter, AddressOf OnHoverEnter
            AddHandler ctrl.MouseLeave, AddressOf OnHoverLeave
            For Each child As Control In ctrl.Controls
                AddHandlerHoverRecursive(child)
            Next
        End Sub

        Private Sub OnHoverEnter(sender As Object, e As EventArgs)
            _isHovered = True
            Me.BackColor = Color.FromArgb(254, 254, 255)
            Me.Invalidate()
        End Sub

        Private Sub OnHoverLeave(sender As Object, e As EventArgs)
            Dim p As Point = Me.PointToClient(Cursor.Position)
            If Not Me.ClientRectangle.Contains(p) Then
                _isHovered = False
                Me.BackColor = Color.White
                Me.Invalidate()
            End If
        End Sub

        Private Sub OnControlClicked(sender As Object, e As EventArgs)
            If _service IsNot Nothing Then
                RaiseEvent ServiceClicked(Me, _service)
            End If
        End Sub

        Private Sub UpdateUI()
            If _service Is Nothing Then Return
            lblName.Text = _service.Name
            lblPrice.Text = $"{_service.Price:N0} ج.م"
            lblDuration.Text = $"⏱ {_service.DurationMinutes} دقيقة"
        End Sub
    End Class

End Namespace
