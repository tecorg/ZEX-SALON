Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Controls
Imports SalonPOS.Data.Repositories
Imports SalonPOS.Helpers
Imports SalonPOS.Models

Namespace SalonPOS.Forms

    Public Class FrmPOS
        Inherits Form

        Private mainForm As FrmMain
        Private srvRepo As New ServiceRepository()
        Private sessionRepo As New SessionRepository()
        Private chairRepo As New ChairRepository()
        Private empRepo As New EmployeeRepository()
        Private custRepo As New CustomerRepository()

        ' Selection State
        Private _selectedChairId As Integer? = Nothing
        Private _selectedEmployeeId As Integer? = Nothing
        Private _selectedCustomer As Customer = Nothing
        Private selectedCategoryFilterId As Integer? = Nothing
        Private selectedCartItemIndex As Integer = -1
        Private currentSession As ChairSession = Nothing

        ' UI Containers
        Private pnlTopSection As Panel
        Private flowChairs As FlowLayoutPanel
        Private flowEmployees As FlowLayoutPanel
        Private pnlCustomerBar As Panel
        Private btnWalkInCustomer As Button
        Private pnlCustomerSearchWrapper As Panel
        Private txtCustomerSearch As TextBox
        Private lstCustomerSearchResults As ListBox
        Private btnAddNewCustomerInline As Button
        Private pnlInlineNewCust As Panel
        Private txtInlineCustName As TextBox
        Private txtInlineCustPhone As TextBox
        Private lblContextBanner As Label
        Private lblInlineWarn As Label

        ' Main Body Split
        Private pnlLeft As Panel
        Private txtSearchService As TextBox
        Private flowCategories As FlowLayoutPanel
        Private flowServices As FlowLayoutPanel

        ' Right Cart Panel
        Private pnlRight As Panel
        Private lblCartTitle As Label
        Private lblCartSubtitle As Label
        Private dgvCart As DataGridView
        Private pnlEmptyCartPlaceHolder As Panel
        Private flowQtyBar As FlowLayoutPanel
        Private btnQtyPlus As Button
        Private btnQtyMinus As Button
        Private btnDeleteItem As Button
        Private btnChangeItemEmp As Button
        Private pnlExtrasBox As GroupBox
        Private flowExtras As FlowLayoutPanel
        Private lblTotalAmount As Label
        Private pnlActionButtons As Panel
        Private btnHoldSession As Button
        Private btnCheckoutPay As Button

        ' Debounce Timer for Customer Search
        Private searchTimer As Timer

        Public Sub New(main As FrmMain, chairId As Integer?)
            Me.mainForm = main
            Me._selectedChairId = chairId
            UIStyleHelper.ApplyFormDefaults(Me)
            Me.BackColor = UIStyleHelper.LightBgColor

            InitializeSearchTimer()
            InitializeLayout()

            ' Default customer to Walk-in
            _selectedCustomer = New Customer With {.Id = 0, .Name = "عميل نقدي", .Phone = ""}

            ' Default select first active employee if none selected
            Dim allEmps = empRepo.GetActiveEmployees()
            If allEmps.Count > 0 Then
                _selectedEmployeeId = allEmps(0).Id
            End If

            LoadChairsBar()
            LoadEmployeesBar()
            LoadCategoryTabs()
            LoadServicesCatalog()

            If _selectedChairId.HasValue Then
                SelectChair(_selectedChairId.Value)
            Else
                ' Default select Chair 1 if available
                Dim chairs = chairRepo.GetAllChairs()
                If chairs.Count > 0 Then
                    SelectChair(chairs(0).Id)
                End If
            End If
        End Sub

        Private Sub InitializeSearchTimer()
            searchTimer = New Timer With {.Interval = 200}
            AddHandler searchTimer.Tick, AddressOf OnSearchTimerTick
        End Sub

        Private Sub InitializeLayout()
            Me.Controls.Clear()

            ' 1. TOP SECTION (Chairs, Employees, Customer Search)
            pnlTopSection = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 178,
                .BackColor = Color.White,
                .Padding = New Padding(10, 4, 10, 4)
            }

            ' 1A. Chairs Bar (FlowDirection.LeftToRight + RightToLeft = Yes starts at Visual Right)
            flowChairs = New FlowLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 68,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .AutoScroll = True,
                .BackColor = Color.FromArgb(248, 250, 252),
                .Padding = New Padding(4)
            }

            ' 1B. Employees Bar
            flowEmployees = New FlowLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 52,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .AutoScroll = True,
                .BackColor = Color.White,
                .Padding = New Padding(4, 6, 4, 4)
            }

            ' 1C. Customer Bar
            ' Search box container matched to exact 38px height of adjacent buttons
            pnlCustomerBar = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 52,
                .RightToLeft = RightToLeft.Yes,
                .BackColor = Color.FromArgb(241, 245, 249),
                .Padding = New Padding(8, 7, 8, 7)
            }

            lblContextBanner = New Label With {
                .Dock = DockStyle.Right,
                .AutoSize = False,
                .Width = 340,
                .Font = UIStyleHelper.GetFont(11.5F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight,
                .Text = "اختر كرسياً للبدء"
            }

            btnWalkInCustomer = New Button With {
                .Text = "عميل نقدي ✓",
                .Dock = DockStyle.Right,
                .Width = 120,
                .Height = 38,
                .Margin = New Padding(4, 0, 4, 0),
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold)
            }
            UIStyleHelper.ApplyButtonFlat(btnWalkInCustomer, UIStyleHelper.ZexNavy, Color.White)
            AddHandler btnWalkInCustomer.Click, Sub()
                                                    _selectedCustomer = New Customer With {.Id = 0, .Name = "عميل نقدي", .Phone = ""}
                                                    btnWalkInCustomer.Text = "عميل نقدي ✓"
                                                    txtCustomerSearch.Text = "ابحث بالاسم أو رقم الهاتف..."
                                                    UpdateContextBanner()
                                                End Sub

            pnlCustomerSearchWrapper = New Panel With {
                .Dock = DockStyle.Right,
                .Width = 280,
                .Height = 38,
                .BackColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle,
                .Padding = New Padding(6, 6, 6, 4),
                .Margin = New Padding(4, 0, 4, 0)
            }

            txtCustomerSearch = New TextBox With {
                .Dock = DockStyle.Fill,
                .BorderStyle = BorderStyle.None,
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Regular),
                .RightToLeft = RightToLeft.Yes,
                .TextAlign = HorizontalAlignment.Right,
                .Text = "ابحث بالاسم أو رقم الهاتف..."
            }
            AddHandler txtCustomerSearch.GotFocus, Sub()
                                                       If txtCustomerSearch.Text = "ابحث بالاسم أو رقم الهاتف..." Then txtCustomerSearch.Text = ""
                                                   End Sub
            AddHandler txtCustomerSearch.LostFocus, Sub()
                                                        If String.IsNullOrWhiteSpace(txtCustomerSearch.Text) Then
                                                            txtCustomerSearch.Text = "ابحث بالاسم أو رقم الهاتف..."
                                                            lstCustomerSearchResults.Visible = False
                                                        End If
                                                    End Sub
            AddHandler txtCustomerSearch.TextChanged, Sub()
                                                          If txtCustomerSearch.Text <> "ابحث بالاسم أو رقم الهاتف..." Then
                                                              searchTimer.Stop()
                                                              searchTimer.Start()
                                                          End If
                                                      End Sub
            AddHandler txtCustomerSearch.KeyDown, AddressOf OnCustomerSearchKeyDown

            pnlCustomerSearchWrapper.Controls.Add(txtCustomerSearch)

            btnAddNewCustomerInline = New Button With {
                .Text = "+ عميل جديد",
                .Dock = DockStyle.Right,
                .Width = 115,
                .Height = 38,
                .Margin = New Padding(4, 0, 4, 0),
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold)
            }
            UIStyleHelper.ApplyButtonFlat(btnAddNewCustomerInline, Color.FromArgb(5, 150, 105), Color.White)
            AddHandler btnAddNewCustomerInline.Click, AddressOf ToggleInlineNewCustomerPanel

            lblInlineWarn = New Label With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.FontBold,
                .ForeColor = Color.FromArgb(192, 57, 43),
                .TextAlign = ContentAlignment.MiddleCenter,
                .Visible = False
            }

            ' Floating Customer Search Dropdown
            lstCustomerSearchResults = New ListBox With {
                .Width = 260,
                .Height = 150,
                .Visible = False,
                .Font = UIStyleHelper.FontRegular,
                .RightToLeft = RightToLeft.Yes
            }
            AddHandler lstCustomerSearchResults.Click, AddressOf OnCustomerSearchResultSelected

            ' Order of docking: Rightmost items added first
            pnlCustomerBar.Controls.Add(lblInlineWarn)
            pnlCustomerBar.Controls.Add(btnAddNewCustomerInline)
            pnlCustomerBar.Controls.Add(pnlCustomerSearchWrapper)
            pnlCustomerBar.Controls.Add(btnWalkInCustomer)
            pnlCustomerBar.Controls.Add(lblContextBanner)

            pnlTopSection.Controls.Add(pnlCustomerBar)
            pnlTopSection.Controls.Add(flowEmployees)
            pnlTopSection.Controls.Add(flowChairs)

            ' Inline Add New Customer Panel
            pnlInlineNewCust = New Panel With {
                .Size = New Size(320, 115),
                .BackColor = Color.FromArgb(236, 240, 241),
                .BorderStyle = BorderStyle.FixedSingle,
                .Visible = False
            }
            Dim lblN As New Label With {.Text = "الاسم:", .Location = New Point(250, 12), .Size = New Size(50, 25), .Font = UIStyleHelper.FontBold}
            txtInlineCustName = New TextBox With {.Location = New Point(60, 10), .Size = New Size(185, 25)}
            Dim lblP As New Label With {.Text = "الهاتف:", .Location = New Point(250, 45), .Size = New Size(50, 25), .Font = UIStyleHelper.FontBold}
            txtInlineCustPhone = New TextBox With {.Location = New Point(60, 43), .Size = New Size(185, 25)}
            Dim btnSaveInlineCust As New Button With {.Text = "حفظ واختيار", .Location = New Point(10, 78), .Size = New Size(100, 28)}
            UIStyleHelper.ApplyButtonFlat(btnSaveInlineCust, UIStyleHelper.ActionGreen, Color.White)
            AddHandler btnSaveInlineCust.Click, AddressOf OnSaveInlineCustomerClicked
            Dim btnCancelInlineCust As New Button With {.Text = "إلغاء", .Location = New Point(120, 78), .Size = New Size(70, 28)}
            UIStyleHelper.ApplyButtonFlat(btnCancelInlineCust, Color.Gray, Color.White)
            AddHandler btnCancelInlineCust.Click, Sub() pnlInlineNewCust.Visible = False

            pnlInlineNewCust.Controls.Add(lblN)
            pnlInlineNewCust.Controls.Add(txtInlineCustName)
            pnlInlineNewCust.Controls.Add(lblP)
            pnlInlineNewCust.Controls.Add(txtInlineCustPhone)
            pnlInlineNewCust.Controls.Add(btnSaveInlineCust)
            pnlInlineNewCust.Controls.Add(btnCancelInlineCust)

            ' 2. MAIN SPLIT CONTAINER (Left = Services Catalog, Right = Cart Panel 450px)
            pnlRight = New Panel With {
                .Dock = DockStyle.Right,
                .Width = 450,
                .BackColor = Color.White,
                .Padding = New Padding(12)
            }

            pnlLeft = New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.FromArgb(246, 248, 251),
                .Padding = New Padding(10)
            }

            ' Build Left Side (Services Catalog & Search)
            Dim pnlSearchServiceBox As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 52,
                .BackColor = Color.White,
                .Padding = New Padding(10, 6, 10, 6)
            }

            Dim pnlSearchBorder As New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.FromArgb(248, 250, 252),
                .BorderStyle = BorderStyle.FixedSingle,
                .Padding = New Padding(8, 4, 8, 4)
            }

            Dim lblSearchIcon As New Label With {
                .Dock = DockStyle.Right,
                .Width = 36,
                .Text = "🔍",
                .Font = UIStyleHelper.GetFont(12.0F),
                .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.Transparent
            }

            txtSearchService = New TextBox With {
                .Dock = DockStyle.Fill,
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.FromArgb(248, 250, 252),
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Regular),
                .RightToLeft = RightToLeft.Yes,
                .TextAlign = HorizontalAlignment.Right,
                .Text = "ابحث عن خدمة..."
            }
            AddHandler txtSearchService.GotFocus, Sub() If txtSearchService.Text = "ابحث عن خدمة..." Then txtSearchService.Text = ""
            AddHandler txtSearchService.LostFocus, Sub() If String.IsNullOrWhiteSpace(txtSearchService.Text) Then txtSearchService.Text = "ابحث عن خدمة..."
            AddHandler txtSearchService.TextChanged, Sub() If txtSearchService.Text <> "ابحث عن خدمة..." Then LoadServicesCatalog()

            pnlSearchBorder.Controls.Add(txtSearchService)
            pnlSearchBorder.Controls.Add(lblSearchIcon)
            pnlSearchServiceBox.Controls.Add(pnlSearchBorder)

            flowCategories = New FlowLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 54,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .BackColor = Color.White,
                .Padding = New Padding(8, 6, 8, 6)
            }

            flowServices = New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(12)
            }

            pnlLeft.Controls.Add(flowServices)
            pnlLeft.Controls.Add(flowCategories)
            pnlLeft.Controls.Add(pnlSearchServiceBox)

            ' =========================================================================
            ' BUILD RIGHT SIDE: INVOICE & CART PANEL (Zero-Gap Connected Architecture)
            ' =========================================================================

            ' A. Cart Header
            Dim pnlCartHeader As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 64,
                .BackColor = Color.White,
                .Padding = New Padding(0, 0, 0, 8)
            }

            lblCartTitle = New Label With {
                .Dock = DockStyle.Top,
                .Height = 32,
                .Text = "🧾 الفاتورة الحالية",
                .Font = UIStyleHelper.GetFont(14.5F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleRight
            }

            lblCartSubtitle = New Label With {
                .Dock = DockStyle.Fill,
                .Text = "اختر كرسياً للبدء",
                .Font = UIStyleHelper.GetFont(10.5F, FontStyle.Regular),
                .ForeColor = Color.FromArgb(100, 116, 139),
                .TextAlign = ContentAlignment.MiddleRight
            }

            pnlCartHeader.Controls.Add(lblCartSubtitle)
            pnlCartHeader.Controls.Add(lblCartTitle)

            ' B. Cart Bottom Controls (Connected Stack)
            Dim pnlCartBottom As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 265,
                .BackColor = Color.White
            }

            ' Flow Qty Bar (Item Actions - Directly Under Table)
            flowQtyBar = New FlowLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 44,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(0, 2, 0, 4),
                .WrapContents = False
            }

            btnQtyPlus = New Button With {.Text = "+ زيادة", .Width = 80, .Height = 36, .Margin = New Padding(2), .Font = UIStyleHelper.GetFont(10.5F, FontStyle.Bold)}
            btnQtyMinus = New Button With {.Text = "- إنقاص", .Width = 80, .Height = 36, .Margin = New Padding(2), .Font = UIStyleHelper.GetFont(10.5F, FontStyle.Bold)}
            btnDeleteItem = New Button With {.Text = "🗑 حذف", .Width = 95, .Height = 36, .Margin = New Padding(2), .Font = UIStyleHelper.GetFont(10.5F, FontStyle.Bold)}
            btnChangeItemEmp = New Button With {.Text = "👤 المشغل", .Width = 140, .Height = 36, .Margin = New Padding(2), .Font = UIStyleHelper.GetFont(10.5F, FontStyle.Bold)}

            UIStyleHelper.ApplyButtonFlatWithBorder(btnQtyPlus, Color.FromArgb(240, 253, 244), Color.FromArgb(22, 101, 52), Color.FromArgb(187, 247, 208), 1)
            UIStyleHelper.ApplyButtonFlatWithBorder(btnQtyMinus, Color.FromArgb(254, 242, 242), Color.FromArgb(185, 28, 28), Color.FromArgb(254, 202, 202), 1)
            UIStyleHelper.ApplyButtonFlatWithBorder(btnDeleteItem, Color.FromArgb(220, 38, 38), Color.White, Color.FromArgb(220, 38, 38), 1)
            UIStyleHelper.ApplyButtonFlatWithBorder(btnChangeItemEmp, UIStyleHelper.ZexNavy, Color.White, UIStyleHelper.ZexNavy, 1)

            AddHandler btnQtyPlus.Click, Sub() ModifyCartItemQty(1)
            AddHandler btnQtyMinus.Click, Sub() ModifyCartItemQty(-1)
            AddHandler btnDeleteItem.Click, Sub() RemoveSelectedCartItem()
            AddHandler btnChangeItemEmp.Click, AddressOf OnChangeCartItemEmpClicked

            flowQtyBar.Controls.Add(btnQtyPlus)
            flowQtyBar.Controls.Add(btnQtyMinus)
            flowQtyBar.Controls.Add(btnDeleteItem)
            flowQtyBar.Controls.Add(btnChangeItemEmp)

            ' Extras Box (Under Qty Bar)
            pnlExtrasBox = New GroupBox With {
                .Dock = DockStyle.Top,
                .Height = 95,
                .Text = "الإضافات المتاحة للخدمة المحددة",
                .Font = UIStyleHelper.GetFont(10.5F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .Padding = New Padding(4)
            }

            flowExtras = New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .RightToLeft = RightToLeft.Yes,
                .FlowDirection = FlowDirection.LeftToRight
            }
            pnlExtrasBox.Controls.Add(flowExtras)

            ' Action Buttons (Checkout & Hold - Anchored at the Absolute Bottom)
            pnlActionButtons = New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 60,
                .RightToLeft = RightToLeft.Yes,
                .Padding = New Padding(0, 4, 0, 0)
            }

            btnHoldSession = New Button With {
                .Text = "⏸ تعليق (F8)",
                .Dock = DockStyle.Left,
                .Width = 125,
                .Height = 54,
                .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold),
                .Margin = New Padding(0, 0, 4, 0)
            }
            UIStyleHelper.ApplyButtonFlat(btnHoldSession, Color.FromArgb(100, 116, 139), Color.White)
            AddHandler btnHoldSession.Click, AddressOf OnHoldSessionClicked

            btnCheckoutPay = New Button With {
                .Text = "💳 دفع 0 ج.م (F9)",
                .Dock = DockStyle.Fill,
                .Height = 54,
                .Font = UIStyleHelper.GetFont(15.0F, FontStyle.Bold)
            }
            UIStyleHelper.ApplyButtonFlatWithBorder(btnCheckoutPay, UIStyleHelper.ZexNavy, Color.White, UIStyleHelper.ZexGold, 2)
            AddHandler btnCheckoutPay.Click, AddressOf OnCheckoutPayClicked

            pnlActionButtons.Controls.Add(btnCheckoutPay)
            pnlActionButtons.Controls.Add(btnHoldSession)

            ' Total Amount Banner (Above Action Buttons)
            Dim pnlTotalWrapper As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 56,
                .Padding = New Padding(0, 4, 0, 4)
            }

            lblTotalAmount = New Label With {
                .Dock = DockStyle.Fill,
                .Font = UIStyleHelper.GetFont(18.0F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(22, 101, 52),
                .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.FromArgb(240, 253, 244),
                .BorderStyle = BorderStyle.FixedSingle,
                .Text = "الإجمالي: 0 ج.م"
            }
            pnlTotalWrapper.Controls.Add(lblTotalAmount)

            pnlCartBottom.Controls.Add(pnlActionButtons)
            pnlCartBottom.Controls.Add(pnlTotalWrapper)
            pnlCartBottom.Controls.Add(pnlExtrasBox)
            pnlCartBottom.Controls.Add(flowQtyBar)

            flowQtyBar.BringToFront()
            pnlExtrasBox.BringToFront()

            ' C. Cart Middle Container (Fills Available Space Cleanly)
            Dim pnlCartMiddle As New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White
            }

            pnlEmptyCartPlaceHolder = New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.FromArgb(250, 251, 254),
                .Visible = True,
                .Padding = New Padding(16)
            }

            Dim lblEmptyMsg As New Label With {
                .Dock = DockStyle.Fill,
                .Text = "🛒 السلة فارغة حالياً" & vbCrLf & vbCrLf & "اختر خدمة من القائمة لإضافتها مباشرة للفاتورة",
                .Font = UIStyleHelper.GetFont(12.0F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            pnlEmptyCartPlaceHolder.Controls.Add(lblEmptyMsg)

            dgvCart = New DataGridView With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AllowUserToResizeRows = False,
                .RowHeadersVisible = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle,
                .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                .GridColor = Color.FromArgb(230, 235, 240),
                .RightToLeft = RightToLeft.Yes,
                .Font = UIStyleHelper.FontRegular,
                .Visible = False
            }
            dgvCart.EnableHeadersVisualStyles = False
            dgvCart.ColumnHeadersDefaultCellStyle.BackColor = UIStyleHelper.ZexNavy
            dgvCart.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            dgvCart.ColumnHeadersDefaultCellStyle.Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold)
            dgvCart.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            dgvCart.ColumnHeadersHeight = 36

            Dim colName As New DataGridViewTextBoxColumn With {
                .Name = "ServiceName",
                .HeaderText = "الخدمة / الإضافة",
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft, .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold), .ForeColor = UIStyleHelper.ZexTextDark}
            }

            Dim colPrice As New DataGridViewTextBoxColumn With {
                .Name = "Price",
                .HeaderText = "السعر",
                .Width = 70,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontRegular}
            }

            Dim colQty As New DataGridViewTextBoxColumn With {
                .Name = "Quantity",
                .HeaderText = "الكمية",
                .Width = 55,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.FontRegular}
            }

            Dim colTotal As New DataGridViewTextBoxColumn With {
                .Name = "Total",
                .HeaderText = "الإجمالي",
                .Width = 80,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold), .ForeColor = UIStyleHelper.ZexNavy}
            }

            dgvCart.Columns.Add(colName)
            dgvCart.Columns.Add(colPrice)
            dgvCart.Columns.Add(colQty)
            dgvCart.Columns.Add(colTotal)

            AddHandler dgvCart.SelectionChanged, AddressOf OnCartItemSelectedIndexChanged

            pnlCartMiddle.Controls.Add(pnlEmptyCartPlaceHolder)
            pnlCartMiddle.Controls.Add(dgvCart)

            ' Assemble pnlRight (Top, Bottom, then Fill in middle)
            pnlRight.Controls.Add(pnlCartMiddle)
            pnlRight.Controls.Add(pnlCartBottom)
            pnlRight.Controls.Add(pnlCartHeader)
            pnlCartMiddle.BringToFront()

            Me.Controls.Add(pnlLeft)
            Me.Controls.Add(pnlRight)
            Me.Controls.Add(pnlTopSection)

            Me.Controls.Add(lstCustomerSearchResults)
            Me.Controls.Add(pnlInlineNewCust)
            lstCustomerSearchResults.BringToFront()
            pnlInlineNewCust.BringToFront()
        End Sub

        ' =========================================================================
        ' 1. CHAIRS & EMPLOYEES LOADING & SELECTION
        ' =========================================================================

        Private Sub LoadChairsBar()
            flowChairs.Controls.Clear()
            Dim chairs = chairRepo.GetAllChairs()

            For Each ch As Chair In chairs
                Dim btnChair As New Button With {
                    .Width = 155,
                    .Height = 56,
                    .Margin = New Padding(4),
                    .Tag = ch.Id,
                    .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold)
                }

                If ch.CurrentStatus = ChairStatus.AVAILABLE Then
                    btnChair.Text = $"{ch.Name}{vbCrLf}متاح"
                    UIStyleHelper.ApplyButtonFlatWithBorder(btnChair, Color.FromArgb(240, 253, 244), Color.FromArgb(22, 101, 52), Color.FromArgb(187, 247, 208), 1)
                ElseIf ch.CurrentStatus = ChairStatus.IN_SERVICE Then
                    Dim empName As String = If(Not String.IsNullOrEmpty(ch.CurrentEmployeeName), ch.CurrentEmployeeName, "مشغول")
                    btnChair.Text = $"{ch.Name}{vbCrLf}{empName} - {ch.ActiveTotalAmount:N0}ج"
                    UIStyleHelper.ApplyButtonFlatWithBorder(btnChair, Color.FromArgb(254, 252, 232), Color.FromArgb(180, 83, 9), Color.FromArgb(254, 240, 138), 1)
                Else
                    btnChair.Text = $"{ch.Name}{vbCrLf}بانتظار الدفع"
                    UIStyleHelper.ApplyButtonFlatWithBorder(btnChair, Color.FromArgb(254, 242, 242), Color.FromArgb(185, 28, 28), Color.FromArgb(254, 202, 202), 1)
                End If

                If _selectedChairId.HasValue AndAlso _selectedChairId.Value = ch.Id Then
                    btnChair.Text = $"✓ {btnChair.Text}"
                    UIStyleHelper.ApplyButtonFlatWithBorder(btnChair, UIStyleHelper.ZexNavy, Color.White, UIStyleHelper.ZexGold, 2)
                End If

                AddHandler btnChair.Click, Sub(sender As Object, e As EventArgs)
                                               Dim cId As Integer = CType(CType(sender, Button).Tag, Integer)
                                               SelectChair(cId)
                                           End Sub
                flowChairs.Controls.Add(btnChair)
            Next
        End Sub

        Private Sub SelectChair(chairId As Integer)
            _selectedChairId = chairId

            ' Check if chair is already IN_SERVICE or WAITING_PAYMENT
            Dim activeSession = sessionRepo.GetSessionByChairId(chairId)
            If activeSession IsNot Nothing Then
                currentSession = activeSession
                _selectedEmployeeId = activeSession.EmployeeId
                If activeSession.CustomerId.HasValue Then
                    _selectedCustomer = New Customer With {.Id = activeSession.CustomerId.Value, .Name = activeSession.CustomerName, .Phone = ""}
                Else
                    _selectedCustomer = New Customer With {.Id = 0, .Name = "عميل نقدي", .Phone = ""}
                End If
            Else
                ' New draft session for available chair
                Dim allChairs = chairRepo.GetAllChairs()
                Dim chObj = allChairs.Find(Function(c) c.Id = chairId)
                Dim chName As String = If(chObj IsNot Nothing, chObj.Name, $"كرسي {chairId}")

                ' Get current selected employee name
                Dim empName As String = "محمد"
                If _selectedEmployeeId.HasValue Then
                    Dim emps = empRepo.GetActiveEmployees()
                    Dim empObj = emps.Find(Function(e) e.Id = _selectedEmployeeId.Value)
                    If empObj IsNot Nothing Then empName = empObj.Name
                End If

                currentSession = New ChairSession With {
                    .Id = 0,
                    .ChairId = chairId,
                    .ChairName = chName,
                    .EmployeeId = If(_selectedEmployeeId.HasValue, _selectedEmployeeId.Value, 1),
                    .EmployeeName = empName,
                    .CustomerName = If(_selectedCustomer IsNot Nothing, _selectedCustomer.Name, "عميل نقدي")
                }
            End If

            LoadChairsBar()
            LoadEmployeesBar()
            UpdateContextBanner()
            RefreshCartUI()
        End Sub

        Private Sub LoadEmployeesBar()
            flowEmployees.Controls.Clear()
            Dim emps = empRepo.GetActiveEmployees()

            Dim lblTitle As New Label With {
                .Text = "المشغل:",
                .Width = 70,
                .Height = 40,
                .Font = UIStyleHelper.GetFont(11.5F, FontStyle.Bold),
                .ForeColor = UIStyleHelper.ZexNavy,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            flowEmployees.Controls.Add(lblTitle)

            For Each emp As Employee In emps
                Dim btnEmp As New Button With {
                    .Text = emp.Name,
                    .Width = 120,
                    .Height = 40,
                    .Margin = New Padding(4),
                    .Tag = emp.Id,
                    .Font = UIStyleHelper.GetFont(11.0F, FontStyle.Bold)
                }

                If _selectedEmployeeId.HasValue AndAlso _selectedEmployeeId.Value = emp.Id Then
                    btnEmp.Text = $"✓ {emp.Name}"
                    UIStyleHelper.ApplyButtonFlatWithBorder(btnEmp, UIStyleHelper.ZexNavy, Color.White, UIStyleHelper.ZexGold, 1)
                Else
                    UIStyleHelper.ApplyButtonFlatWithBorder(btnEmp, Color.FromArgb(248, 250, 252), Color.FromArgb(30, 41, 59), Color.FromArgb(226, 232, 240), 1)
                End If

                AddHandler btnEmp.Click, Sub(sender As Object, e As EventArgs)
                                             Dim empId As Integer = CType(CType(sender, Button).Tag, Integer)
                                             SelectEmployee(empId)
                                         End Sub
                flowEmployees.Controls.Add(btnEmp)
            Next
        End Sub

        Private Sub SelectEmployee(employeeId As Integer)
            _selectedEmployeeId = employeeId
            HideInlineWarning()

            ' Update current session default employee
            If currentSession IsNot Nothing Then
                currentSession.EmployeeId = employeeId
                Dim emps = empRepo.GetActiveEmployees()
                Dim empObj = emps.Find(Function(e) e.Id = employeeId)
                If empObj IsNot Nothing Then currentSession.EmployeeName = empObj.Name
            End If

            LoadEmployeesBar()
            UpdateContextBanner()
        End Sub

        Private Sub UpdateContextBanner()
            If _selectedChairId.HasValue Then
                Dim chairName As String = $"كرسي {_selectedChairId.Value}"
                Dim empName As String = "لم يحدد"
                If _selectedEmployeeId.HasValue Then
                    Dim emps = empRepo.GetActiveEmployees()
                    Dim empObj = emps.Find(Function(e) e.Id = _selectedEmployeeId.Value)
                    If empObj IsNot Nothing Then empName = empObj.Name
                End If
                Dim custName As String = If(_selectedCustomer IsNot Nothing, _selectedCustomer.Name, "عميل نقدي")

                lblContextBanner.Text = $"{chairName}  •  المشغل: {empName}  •  العميل: {custName}"
                lblCartSubtitle.Text = $"{chairName} • {empName} • {custName}"
            Else
                lblContextBanner.Text = "اختر كرسياً ومشغلاً للبدء"
                lblCartSubtitle.Text = "لم يتم تحديد كرسي"
            End If
        End Sub

        Private Sub ShowInlineWarning(msg As String)
            lblInlineWarn.Text = msg
            lblInlineWarn.Visible = True
        End Sub

        Private Sub HideInlineWarning()
            lblInlineWarn.Visible = False
        End Sub

        ' =========================================================================
        ' 2. CUSTOMER SEARCH & INLINE CREATION
        ' =========================================================================

        Private Sub OnSearchTimerTick(sender As Object, e As EventArgs)
            searchTimer.Stop()
            Dim kw As String = txtCustomerSearch.Text.Trim()
            If kw.Length >= 1 AndAlso kw <> "ابحث بالاسم أو رقم الهاتف..." Then
                Dim results = custRepo.SearchCustomers(kw)
                lstCustomerSearchResults.Items.Clear()

                ' Add option to quick-add as new customer
                Dim promptItem As New Customer With {
                    .Id = -1,
                    .Name = $"+ إضافة '{kw}' كعميل جديد",
                    .Phone = ""
                }
                lstCustomerSearchResults.Items.Add(promptItem)

                For Each cust As Customer In results
                    lstCustomerSearchResults.Items.Add(cust)
                Next

                lstCustomerSearchResults.DisplayMember = "Name"
                lstCustomerSearchResults.Location = New Point(pnlCustomerSearchWrapper.Left + pnlCustomerBar.Left, pnlCustomerBar.Bottom + pnlTopSection.Top)
                lstCustomerSearchResults.Visible = True
                lstCustomerSearchResults.BringToFront()
            Else
                lstCustomerSearchResults.Visible = False
            End If
        End Sub

        Private Sub OnCustomerSearchKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then
                e.SuppressKeyPress = True
                e.Handled = True
                ConfirmCustomerSelectionOrAdd()
            End If
        End Sub

        Private Sub ConfirmCustomerSelectionOrAdd()
            Dim kw As String = txtCustomerSearch.Text.Trim()
            If String.IsNullOrWhiteSpace(kw) OrElse kw = "ابحث بالاسم أو رقم الهاتف..." Then Return

            ' Check existing customers in DB
            Dim existing = custRepo.SearchCustomers(kw)
            Dim match = existing.Find(Function(c) c.Name.Equals(kw, StringComparison.OrdinalIgnoreCase))

            If match IsNot Nothing Then
                _selectedCustomer = match
            Else
                ' Quick add new customer
                Dim newId As Integer = custRepo.QuickAddCustomer(kw, "")
                _selectedCustomer = New Customer With {.Id = newId, .Name = kw, .Phone = ""}
            End If

            If currentSession IsNot Nothing Then
                currentSession.CustomerId = _selectedCustomer.Id
                currentSession.CustomerName = _selectedCustomer.Name
            End If

            lstCustomerSearchResults.Visible = False
            btnWalkInCustomer.Text = "عميل نقدي"
            txtCustomerSearch.Text = _selectedCustomer.Name
            UpdateContextBanner()
        End Sub

        Private Sub OnCustomerSearchResultSelected(sender As Object, e As EventArgs)
            If lstCustomerSearchResults.SelectedItem IsNot Nothing Then
                Dim cust = CType(lstCustomerSearchResults.SelectedItem, Customer)

                If cust.Id = -1 Then
                    ' User clicked "+ إضافة 'اسم' كعميل جديد"
                    Dim realName As String = txtCustomerSearch.Text.Trim()
                    If String.IsNullOrEmpty(realName) OrElse realName = "ابحث بالاسم أو رقم الهاتف..." Then Return
                    Dim newId As Integer = custRepo.QuickAddCustomer(realName, "")
                    _selectedCustomer = New Customer With {.Id = newId, .Name = realName, .Phone = ""}
                Else
                    _selectedCustomer = cust
                End If

                txtCustomerSearch.Text = _selectedCustomer.Name
                lstCustomerSearchResults.Visible = False
                btnWalkInCustomer.Text = "عميل نقدي"

                If currentSession IsNot Nothing Then
                    currentSession.CustomerId = _selectedCustomer.Id
                    currentSession.CustomerName = _selectedCustomer.Name
                End If

                UpdateContextBanner()
            End If
        End Sub

        Private Sub ToggleInlineNewCustomerPanel(sender As Object, e As EventArgs)
            txtInlineCustName.Text = ""
            txtInlineCustPhone.Text = ""
            pnlInlineNewCust.Location = New Point(btnAddNewCustomerInline.Left + pnlCustomerBar.Left - 100, pnlCustomerBar.Bottom + pnlTopSection.Top)
            pnlInlineNewCust.Visible = Not pnlInlineNewCust.Visible
            pnlInlineNewCust.BringToFront()
        End Sub

        Private Sub OnSaveInlineCustomerClicked(sender As Object, e As EventArgs)
            Dim cName As String = txtInlineCustName.Text.Trim()
            Dim cPhone As String = txtInlineCustPhone.Text.Trim()

            If String.IsNullOrEmpty(cName) Then
                MessageBox.Show("يرجى إدخال اسم العميل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim newCustId As Integer = custRepo.QuickAddCustomer(cName, cPhone)
            _selectedCustomer = New Customer With {.Id = newCustId, .Name = cName, .Phone = cPhone}

            If currentSession IsNot Nothing Then
                currentSession.CustomerId = newCustId
                currentSession.CustomerName = cName
            End If

            pnlInlineNewCust.Visible = False
            btnWalkInCustomer.Text = "عميل نقدي"
            txtCustomerSearch.Text = cName
            UpdateContextBanner()
            MessageBox.Show($"تم حفظ واختيار العميل '{cName}' بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        ' =========================================================================
        ' 3. SERVICES CATALOG & 1-CLICK AUTO-SESSION START
        ' =========================================================================

        Private Sub LoadCategoryTabs()
            flowCategories.Controls.Clear()

            Dim btnAll As New Button With {
                .Text = "الكل",
                .Width = 105,
                .Height = 40,
                .Margin = New Padding(4),
                .Font = UIStyleHelper.GetFont(11.5F, FontStyle.Bold),
                .Tag = Nothing
            }
            UIStyleHelper.ApplyButtonFlatWithBorder(btnAll, UIStyleHelper.ZexNavy, Color.White, UIStyleHelper.ZexNavy, 1)
            AddHandler btnAll.Click, AddressOf OnCategoryTabClicked
            flowCategories.Controls.Add(btnAll)

            Dim categories = srvRepo.GetCategories()
            For Each cat As ServiceCategory In categories
                Dim btn As New Button With {
                    .Text = cat.Name,
                    .Width = 125,
                    .Height = 40,
                    .Margin = New Padding(4),
                    .Font = UIStyleHelper.GetFont(11.5F, FontStyle.Bold),
                    .Tag = cat.Id
                }
                UIStyleHelper.ApplyButtonFlatWithBorder(btn, Color.FromArgb(248, 250, 252), Color.FromArgb(30, 41, 59), Color.FromArgb(226, 232, 240), 1)
                AddHandler btn.Click, AddressOf OnCategoryTabClicked
                flowCategories.Controls.Add(btn)
            Next
        End Sub

        Private Sub OnCategoryTabClicked(sender As Object, e As EventArgs)
            For Each btn As Button In flowCategories.Controls
                UIStyleHelper.ApplyButtonFlatWithBorder(btn, Color.FromArgb(248, 250, 252), Color.FromArgb(30, 41, 59), Color.FromArgb(226, 232, 240), 1)
            Next

            Dim clickedBtn As Button = CType(sender, Button)
            selectedCategoryFilterId = CType(clickedBtn.Tag, Integer?)
            UIStyleHelper.ApplyButtonFlatWithBorder(clickedBtn, UIStyleHelper.ZexNavy, Color.White, UIStyleHelper.ZexNavy, 1)

            LoadServicesCatalog()
        End Sub

        Private Sub LoadServicesCatalog()
            flowServices.Controls.Clear()
            Dim searchKw As String = If(txtSearchService.Text = "ابحث عن خدمة...", "", txtSearchService.Text)
            Dim services = srvRepo.GetServices(selectedCategoryFilterId, searchKw)

            For Each srv As ServiceItem In services
                Dim card As New ServiceCardControl With {
                    .ServiceData = srv
                }
                AddHandler card.ServiceClicked, AddressOf OnServiceCardClicked
                flowServices.Controls.Add(card)
            Next
        End Sub

        Private Sub OnServiceCardClicked(sender As Object, service As ServiceItem)
            ' 1-Click Auto Session Start Check
            If Not _selectedChairId.HasValue Then
                ShowInlineWarning("اختر الكرسي أولاً!")
                Return
            End If

            If Not _selectedEmployeeId.HasValue Then
                ShowInlineWarning("اختر المشغل أولاً!")
                Return
            End If

            HideInlineWarning()

            ' Ensure Session is created in DB (1-Click Auto Start)
            If currentSession Is Nothing OrElse currentSession.Id <= 0 Then
                Dim custId As Integer? = If(_selectedCustomer IsNot Nothing AndAlso _selectedCustomer.Id > 0, _selectedCustomer.Id, CType(Nothing, Integer?))
                Dim newSessId As Integer = sessionRepo.CreateSession(_selectedChairId.Value, _selectedEmployeeId.Value, custId)

                currentSession = sessionRepo.GetSessionById(newSessId)
            End If

            ' Add service to session cart
            Dim existingItem = currentSession.Items.Find(Function(i) i.ServiceId = service.Id)
            If existingItem IsNot Nothing Then
                existingItem.Quantity += 1
            Else
                Dim empName As String = "محمد"
                If _selectedEmployeeId.HasValue Then
                    Dim emps = empRepo.GetActiveEmployees()
                    Dim empObj = emps.Find(Function(e) e.Id = _selectedEmployeeId.Value)
                    If empObj IsNot Nothing Then empName = empObj.Name
                End If

                Dim newItem As New ChairSessionItem With {
                    .SessionId = currentSession.Id,
                    .ServiceId = service.Id,
                    .ServiceName = service.Name,
                    .Price = service.Price,
                    .Quantity = 1,
                    .EmployeeId = _selectedEmployeeId,
                    .EmployeeName = empName
                }
                currentSession.Items.Add(newItem)
            End If

            ' Save cart changes to DB immediately
            sessionRepo.SaveSessionCart(currentSession)

            RefreshCartUI()
            LoadChairsBar()
        End Sub

        ' =========================================================================
        ' 4. CART & EXTRAS DISPLAY
        ' =========================================================================

        Private Sub RefreshCartUI()
            dgvCart.Rows.Clear()
            Dim grandTotal As Double = 0.0

            If currentSession IsNot Nothing AndAlso currentSession.Items IsNot Nothing AndAlso currentSession.Items.Count > 0 Then
                pnlEmptyCartPlaceHolder.Visible = False
                dgvCart.Visible = True

                For Each item As ChairSessionItem In currentSession.Items
                    Dim rowIdx = dgvCart.Rows.Add(item.ServiceName, $"{item.Price:N0}", item.Quantity.ToString(), $"{item.LineTotal:N0}")
                    Dim mainRow = dgvCart.Rows(rowIdx)
                    mainRow.Tag = item
                    mainRow.Cells("ServiceName").Style.Alignment = DataGridViewContentAlignment.MiddleLeft

                    ' Extras sub-items
                    For Each ex As ChairSessionItemExtra In item.Extras
                        Dim exRowIdx = dgvCart.Rows.Add($"   └ {ex.ExtraName}", $"+{ex.Price:N0}", "-", $"+{ex.Price * item.Quantity:N0}")
                        Dim exRow = dgvCart.Rows(exRowIdx)
                        exRow.DefaultCellStyle.ForeColor = Color.DimGray
                        exRow.DefaultCellStyle.Font = UIStyleHelper.GetFont(9.5F, FontStyle.Italic)
                        exRow.Cells("ServiceName").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
                        exRow.Tag = Nothing
                    Next

                    grandTotal += item.LineTotal
                Next

                currentSession.TotalAmount = grandTotal
            Else
                pnlEmptyCartPlaceHolder.Visible = True
                dgvCart.Visible = False
            End If

            lblTotalAmount.Text = $"الإجمالي: {grandTotal:N0} ج.م"
            btnCheckoutPay.Text = $"💳 دفع {grandTotal:N0} ج.م (F9)"
        End Sub

        Private Sub OnCartItemSelectedIndexChanged(sender As Object, e As EventArgs)
            flowExtras.Controls.Clear()

            If dgvCart.SelectedRows.Count > 0 Then
                Dim selectedRow = dgvCart.SelectedRows(0)
                If selectedRow.Tag IsNot Nothing AndAlso TypeOf selectedRow.Tag Is ChairSessionItem Then
                    Dim cartItem As ChairSessionItem = CType(selectedRow.Tag, ChairSessionItem)
                    LoadExtrasForCartItem(cartItem)
                End If
            End If
        End Sub

        Private Sub LoadExtrasForCartItem(cartItem As ChairSessionItem)
            flowExtras.Controls.Clear()
            Dim allServices = srvRepo.GetServices()
            Dim srvObj = allServices.Find(Function(s) s.Id = cartItem.ServiceId)

            If srvObj IsNot Nothing AndAlso srvObj.Extras.Count > 0 Then
                For Each ex As ServiceExtra In srvObj.Extras
                    Dim toggleBtn As New ExtraToggleButton With {
                        .ExtraData = ex,
                        .IsSelected = cartItem.Extras.Exists(Function(x) x.ExtraId = ex.Id)
                    }
                    AddHandler toggleBtn.ToggleChanged, Sub(senderObj, extraData, isChecked)
                                                            If isChecked Then
                                                                cartItem.Extras.Add(New ChairSessionItemExtra With {
                                                                    .SessionItemId = cartItem.Id,
                                                                    .ExtraId = ex.Id,
                                                                    .ExtraName = ex.Name,
                                                                    .Price = ex.Price
                                                                })
                                                            Else
                                                                cartItem.Extras.RemoveAll(Function(x) x.ExtraId = ex.Id)
                                                            End If
                                                            sessionRepo.SaveSessionCart(currentSession)
                                                            RefreshCartUI()
                                                        End Sub
                    flowExtras.Controls.Add(toggleBtn)
                Next
            End If
        End Sub

        Private Sub ModifyCartItemQty(delta As Integer)
            If dgvCart.SelectedRows.Count > 0 Then
                Dim selectedRow = dgvCart.SelectedRows(0)
                If selectedRow.Tag IsNot Nothing AndAlso TypeOf selectedRow.Tag Is ChairSessionItem Then
                    Dim cartItem As ChairSessionItem = CType(selectedRow.Tag, ChairSessionItem)
                    cartItem.Quantity += delta

                    If cartItem.Quantity <= 0 Then
                        currentSession.Items.Remove(cartItem)
                    End If

                    sessionRepo.SaveSessionCart(currentSession)
                    RefreshCartUI()
                End If
            End If
        End Sub

        Private Sub RemoveSelectedCartItem()
            If dgvCart.SelectedRows.Count > 0 Then
                Dim selectedRow = dgvCart.SelectedRows(0)
                If selectedRow.Tag IsNot Nothing AndAlso TypeOf selectedRow.Tag Is ChairSessionItem Then
                    Dim cartItem As ChairSessionItem = CType(selectedRow.Tag, ChairSessionItem)
                    currentSession.Items.Remove(cartItem)
                    sessionRepo.SaveSessionCart(currentSession)
                    RefreshCartUI()
                    flowExtras.Controls.Clear()
                End If
            End If
        End Sub

        Private Sub OnChangeCartItemEmpClicked(sender As Object, e As EventArgs)
            If dgvCart.SelectedRows.Count > 0 Then
                Dim selectedRow = dgvCart.SelectedRows(0)
                If selectedRow.Tag IsNot Nothing AndAlso TypeOf selectedRow.Tag Is ChairSessionItem Then
                    Dim cartItem As ChairSessionItem = CType(selectedRow.Tag, ChairSessionItem)

                    Dim emps = empRepo.GetActiveEmployees()
                    Dim empNames = emps.ConvertAll(Function(emp) emp.Name).ToArray()

                    Dim selectedName = InputBox("اختر اسم المشغل للخدمة المحددة:" & vbCrLf & String.Join(" - ", empNames), "تغيير مشغل الخدمة", cartItem.EmployeeName)
                    If Not String.IsNullOrEmpty(selectedName) Then
                        Dim matchEmp = emps.Find(Function(emp) emp.Name.Trim() = selectedName.Trim())
                        If matchEmp IsNot Nothing Then
                            cartItem.EmployeeId = matchEmp.Id
                            cartItem.EmployeeName = matchEmp.Name
                            sessionRepo.SaveSessionCart(currentSession)
                            RefreshCartUI()
                        End If
                    End If
                End If
            End If
        End Sub

        Private Sub OnHoldSessionClicked(sender As Object, e As EventArgs)
            If currentSession IsNot Nothing AndAlso currentSession.Items.Count > 0 Then
                sessionRepo.SaveSessionCart(currentSession)
                MessageBox.Show("تم حفظ وتعليق الجلسة بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information)
                LoadChairsBar()
            End If
        End Sub

        Private Sub OnCheckoutPayClicked(sender As Object, e As EventArgs)
            If currentSession Is Nothing OrElse currentSession.Items.Count = 0 Then
                MessageBox.Show("السلة فارغة! يرجى إضافة خدمة واحدة على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            sessionRepo.SaveSessionCart(currentSession)

            Dim payModal As New FrmPaymentModal(currentSession)
            If payModal.ShowDialog(Me) = DialogResult.OK Then
                _selectedChairId = Nothing
                currentSession = Nothing
                LoadChairsBar()
                RefreshCartUI()
                flowExtras.Controls.Clear()
                UpdateContextBanner()
            End If
        End Sub

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = Keys.F8 Then
                OnHoldSessionClicked(Nothing, Nothing)
                Return True
            ElseIf keyData = Keys.F9 Then
                OnCheckoutPayClicked(Nothing, Nothing)
                Return True
            End If
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function
    End Class

End Namespace
