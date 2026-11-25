Imports System.Drawing
Imports System.Windows.Forms
Imports SalonPOS.Helpers
Imports SalonPOS.Models

Namespace SalonPOS.Controls

    Public Class ExtraToggleButton
        Inherits Button

        Private _extra As ServiceExtra
        Private _isSelected As Boolean = False

        Public Event ToggleChanged(sender As Object, extra As ServiceExtra, isSelected As Boolean)

        Public Sub New()
            Me.FlatStyle = FlatStyle.Flat
            Me.FlatAppearance.BorderSize = 1
            Me.FlatAppearance.BorderColor = UIStyleHelper.CardBorderColor
            Me.Height = 42
            Me.Margin = New Padding(4)
            Me.Font = UIStyleHelper.FontBold
            Me.Cursor = Cursors.Hand
            UpdateToggleStyle()
        End Sub

        Public Property ExtraData As ServiceExtra
            Get
                Return _extra
            End Get
            Set(value As ServiceExtra)
                _extra = value
                If _extra IsNot Nothing Then
                    Me.Text = $"[ {_extra.Name} +{_extra.Price:N0} ]"
                End If
            End Set
        End Property

        Public Property IsSelected As Boolean
            Get
                Return _isSelected
            End Get
            Set(value As Boolean)
                _isSelected = value
                UpdateToggleStyle()
            End Set
        End Property

        Protected Overrides Sub OnClick(e As EventArgs)
            MyBase.OnClick(e)
            _isSelected = Not _isSelected
            UpdateToggleStyle()
            If _extra IsNot Nothing Then
                RaiseEvent ToggleChanged(Me, _extra, _isSelected)
            End If
        End Sub

        Private Sub UpdateToggleStyle()
            If _isSelected Then
                Me.BackColor = UIStyleHelper.ActionGreen
                Me.ForeColor = Color.White
                Me.FlatAppearance.BorderColor = UIStyleHelper.ActionGreen
            Else
                Me.BackColor = Color.FromArgb(238, 242, 246)
                Me.ForeColor = Color.FromArgb(44, 62, 80)
                Me.FlatAppearance.BorderColor = UIStyleHelper.CardBorderColor
            End If
        End Sub
    End Class

End Namespace
