Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Helpers
Imports SalonPOS.Models

Namespace SalonPOS.Controls

    Public Class ChairCardControl
        Inherits UserControl

        Private _chair As Chair
        Private pnlHeader As Panel
        Private lblChairName As Label
        Private lblStatusBadge As Label
        Private lblEmployee As Label
        Private lblCustomer As Label
        Private lblTime As Label
        Private lblTotal As Label

        Public Event ChairClicked(sender As Object, chair As Chair)

        Public Sub New()
            InitializeComponentLayout()
        End Sub

        Public Property ChairData As Chair
            Get
                Return _chair
            End Get
            Set(value As Chair)
                _chair = value
                UpdateCardUI()
            End Set
        End Property

        Private Sub InitializeComponentLayout()
            Me.Size = New Size(220, 180)
            Me.Margin = New Padding(12)
            Me.BackColor = Color.White
            Me.BorderStyle = BorderStyle.FixedSingle
            Me.Cursor = Cursors.Hand

            ' Header Panel
            pnlHeader = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 45,
                .Padding = New Padding(8, 0, 8, 0)
            }

            lblChairName = New Label With {
                .Dock = DockStyle.Right,
                .AutoSize = False,
                .Width = 110,
                .Font = UIStyleHelper.FontCardTitle,
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleRight
            }

            lblStatusBadge = New Label With {
                .Dock = DockStyle.Left,
                .AutoSize = False,
                .Width = 90,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleLeft
            }

            pnlHeader.Controls.Add(lblChairName)
            pnlHeader.Controls.Add(lblStatusBadge)

            ' Content Labels
            lblEmployee = New Label With {
                .Location = New Point(10, 52),
                .Size = New Size(200, 24),
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.FromArgb(44, 62, 80),
                .TextAlign = ContentAlignment.MiddleRight
            }

            lblCustomer = New Label With {
                .Location = New Point(10, 78),
                .Size = New Size(200, 22),
                .Font = UIStyleHelper.FontRegular,
                .ForeColor = Color.DimGray,
                .TextAlign = ContentAlignment.MiddleRight
            }

            lblTime = New Label With {
                .Location = New Point(10, 102),
                .Size = New Size(200, 22),
                .Font = UIStyleHelper.FontRegular,
                .ForeColor = Color.DimGray,
                .TextAlign = ContentAlignment.MiddleRight
            }

            lblTotal = New Label With {
                .Location = New Point(10, 130),
                .Size = New Size(200, 38),
                .Font = UIStyleHelper.FontPrice,
                .ForeColor = Color.FromArgb(41, 128, 185),
                .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.FromArgb(240, 244, 248)
            }

            Me.Controls.Add(pnlHeader)
            Me.Controls.Add(lblEmployee)
            Me.Controls.Add(lblCustomer)
            Me.Controls.Add(lblTime)
            Me.Controls.Add(lblTotal)

            ' Attach click handlers to all child controls so clicking anywhere triggers ChairClicked
            AddHandlerClickRecursive(Me)
        End Sub

        Private Sub AddHandlerClickRecursive(ctrl As Control)
            AddHandler ctrl.Click, AddressOf OnControlClicked
            For Each child As Control In ctrl.Controls
                AddHandlerClickRecursive(child)
            Next
        End Sub

        Private Sub OnControlClicked(sender As Object, e As EventArgs)
            If _chair IsNot Nothing Then
                RaiseEvent ChairClicked(Me, _chair)
            End If
        End Sub

        Private Sub UpdateCardUI()
            If _chair Is Nothing Then Return

            lblChairName.Text = _chair.Name

            Select Case _chair.CurrentStatus
                Case ChairStatus.AVAILABLE
                    pnlHeader.BackColor = UIStyleHelper.ColorAvailable
                    lblStatusBadge.Text = "متاح"
                    lblEmployee.Text = "اضغط لبدء خدمة"
                    lblCustomer.Text = ""
                    lblTime.Text = ""
                    lblTotal.Text = ""
                    lblTotal.Visible = False

                Case ChairStatus.IN_SERVICE
                    pnlHeader.BackColor = UIStyleHelper.ColorInService
                    lblStatusBadge.Text = "مشغول"
                    lblEmployee.Text = $"الموظف: {_chair.CurrentEmployeeName}"
                    lblCustomer.Text = $"العميل: {_chair.CurrentCustomerName}"

                    If _chair.ServiceStartedAt.HasValue Then
                        Dim elapsed As TimeSpan = DateTime.Now - _chair.ServiceStartedAt.Value
                        lblTime.Text = $"المدة: {elapsed.Minutes} دقيقة"
                    Else
                        lblTime.Text = ""
                    End If

                    lblTotal.Visible = True
                    lblTotal.Text = $"الإجمالي: {_chair.ActiveTotalAmount:N0} ج.م"

                Case ChairStatus.WAITING_PAYMENT
                    pnlHeader.BackColor = UIStyleHelper.ColorWaitingPayment
                    lblStatusBadge.Text = "ينتظر الدفع"
                    lblEmployee.Text = $"الموظف: {_chair.CurrentEmployeeName}"
                    lblCustomer.Text = $"العميل: {_chair.CurrentCustomerName}"
                    lblTime.Text = "جاهز للحساب"
                    lblTotal.Visible = True
                    lblTotal.Text = $"الإجمالي: {_chair.ActiveTotalAmount:N0} ج.م"
            End Select
        End Sub
    End Class

End Namespace
