Public Class FrameSize
    Private Sub NumericUpDown1_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDown1.ValueChanged
        My.Settings.FrameSize = NumericUpDown1.Value
        My.Settings.Save()
    End Sub

    Private Sub FrameSize_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        NumericUpDown1.Value = My.Settings.FrameSize
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        My.Settings.FrameSize = NumericUpDown1.Value
        My.Settings.Save()
        Me.Close()
    End Sub
End Class