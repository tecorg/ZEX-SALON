Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Controls
Imports SalonPOS.Data.Repositories
Imports SalonPOS.DTOs
Imports SalonPOS.Helpers
Imports SalonPOS.Models
Imports SalonPOS.Services

Namespace SalonPOS.Forms

    Public Class FrmDashboardChairs
        Inherits Form

        Private mainForm As FrmMain
        Private chairRepo As New ChairRepository()
        Private reportService As New ReportService()

        Private pnlSummary As Panel
        Private flowChairs As FlowLayoutPanel

        Private lblSalesVal As Label
        Private lblCustVal As Label
        Private lblSrvVal As Label
        Private lblExpVal As Label
        Private lblNetVal As Label

        Public Sub New(main As FrmMain)
            Me.mainForm = main
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.BackColor = UIStyleHelper.LightBgColor

            InitializeLayout()
            RefreshDashboard()
        End Sub

        Private Sub InitializeLayout()
            ' 1. Summary Strip Panel
            pnlSummary = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 85,
                .BackColor = Color.White,
                .Padding = New Padding(15, 10, 15, 10)
            }

            Dim flowSummary As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft
            }

            lblSalesVal = CreateMetricCard(flowSummary, "المبيعات اليوم", "0 ج.م", UIStyleHelper.PrimaryColor)
            lblCustVal = CreateMetricCard(flowSummary, "عدد العملاء", "0", Color.FromArgb(142, 68, 173))
            lblSrvVal = CreateMetricCard(flowSummary, "الخدمات المنفذة", "0", Color.FromArgb(39, 174, 96))
            lblExpVal = CreateMetricCard(flowSummary, "المصروفات", "0 ج.م", Color.FromArgb(211, 84, 0))
            lblNetVal = CreateMetricCard(flowSummary, "صافي اليوم", "0 ج.م", Color.FromArgb(44, 62, 80))

            pnlSummary.Controls.Add(flowSummary)

            ' 2. Chairs Grid Flow Panel
            flowChairs = New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .Padding = New Padding(20),
                .FlowDirection = FlowDirection.RightToLeft,
                .BackColor = UIStyleHelper.LightBgColor
            }

            Me.Controls.Add(flowChairs)
            Me.Controls.Add(pnlSummary)
        End Sub

        Private Function CreateMetricCard(parent As FlowLayoutPanel, title As String, initVal As String, accentColor As Color) As Label
            Dim pnl As New Panel With {
                .Size = New Size(210, 65),
                .Margin = New Padding(10, 0, 10, 0),
                .BackColor = Color.FromArgb(248, 250, 252),
                .BorderStyle = BorderStyle.FixedSingle
            }

            Dim lblTitle As New Label With {
                .Dock = DockStyle.Top,
                .Height = 25,
                .Text = title,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.DimGray,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            Dim lblValue As New Label With {
                .Dock = DockStyle.Fill,
                .Text = initVal,
                .Font = UIStyleHelper.FontPrice,
                .ForeColor = accentColor,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            pnl.Controls.Add(lblValue)
            pnl.Controls.Add(lblTitle)
            parent.Controls.Add(pnl)

            Return lblValue
        End Function

        Public Sub RefreshDashboard()
            ' Update Metrics
            Dim summary As DailySummaryDto = reportService.GetTodaySummary()
            lblSalesVal.Text = $"{summary.TotalSales:N0} ج.م"
            lblCustVal.Text = summary.CustomerCount.ToString()
            lblSrvVal.Text = summary.ServiceCount.ToString()
            lblExpVal.Text = $"{summary.TotalExpenses:N0} ج.م"
            lblNetVal.Text = $"{summary.NetProfit:N0} ج.م"

            ' Update Chairs
            flowChairs.Controls.Clear()
            Dim chairs = chairRepo.GetAllChairs()
            For Each ch In chairs
                Dim card As New ChairCardControl With {
                    .ChairData = ch
                }
                AddHandler card.ChairClicked, AddressOf OnChairClicked
                flowChairs.Controls.Add(card)
            Next
        End Sub

        Private Sub OnChairClicked(sender As Object, chair As Chair)
            ' Open POS screen directly for any chair without modals
            mainForm.OpenSubForm(New FrmPOS(mainForm, chair.Id))
        End Sub
    End Class

End Namespace
