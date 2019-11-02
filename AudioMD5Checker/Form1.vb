﻿Imports System.IO
Public Class Form1
    Private Extensions As String() = {".mp3", ".flac", ".wav"}
    Private Sub ListView1_DragEnter(sender As Object, e As DragEventArgs) Handles ListView1.DragEnter
        DragEnterEvent(sender, e)
    End Sub

    Private Sub ListView1_DragDrop(sender As Object, e As DragEventArgs) Handles ListView1.DragDrop
        ListBoxDragDrop(sender, e, ListView1)
    End Sub
    Private Sub GetDirectoriesAndFiles(ByVal BaseFolder As DirectoryInfo, listView As ListView)
        For Each file As FileInfo In BaseFolder.GetFiles
            If Extensions.Contains(file.Extension) Then listView.Items.Add(file.FullName)
        Next
        For Each subF As DirectoryInfo In BaseFolder.GetDirectories()
            Application.DoEvents()
            GetDirectoriesAndFiles(subF, listView)
        Next
    End Sub

    Private Sub ListView2_DragDrop(sender As Object, e As DragEventArgs) Handles ListView2.DragDrop
        ListBoxDragDrop(sender, e, ListView2)
    End Sub

    Private Sub DragEnterEvent(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        End If
    End Sub

    Private Sub ListBoxDragDrop(sender As Object, e As DragEventArgs, listView As ListView)
        Dim filepath() As String = CType(e.Data.GetData(DataFormats.FileDrop), String())
        For Each path In filepath
            If Directory.Exists(path) Then
                GetDirectoriesAndFiles(New DirectoryInfo(path), listView)
            Else
                listView.Items.Add(path)
            End If
        Next
    End Sub

    Private Sub ListView2_DragEnter(sender As Object, e As DragEventArgs) Handles ListView2.DragEnter
        DragEnterEvent(sender, e)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If ListView1.Items.Count = ListView2.Items.Count Then
            For Each file In ListView1.Items
                ListView1.SelectedItems.Clear()
                ListView1.Items(ListView1.Items.IndexOf(file)).Selected = True
                ListView1.Items(ListView1.Items.IndexOf(file)).Focused = True
                ListView1.Items(ListView1.Items.IndexOf(file)).EnsureVisible()
                ListView1.Select()
                ListView2.SelectedItems.Clear()
                ListView2.Items(ListView1.Items.IndexOf(file)).Selected = True
                ListView2.Items(ListView1.Items.IndexOf(file)).Focused = True
                ListView2.Items(ListView1.Items.IndexOf(file)).EnsureVisible()
                ListView2.Select()
                ffmpeg_process(file.Text, 1)
                ffmpeg_process(ListView2.Items.Item(ListView1.Items.IndexOf(file)).Text, 2)
                If My.Computer.FileSystem.ReadAllText("test1.md5") = My.Computer.FileSystem.ReadAllText("test2.md5") Then
                    ListView1.Items(ListView1.Items.IndexOf(file)).BackColor = Color.LimeGreen
                    ListView2.Items(ListView1.Items.IndexOf(file)).BackColor = Color.LimeGreen
                Else
                    ListView1.Items(ListView1.Items.IndexOf(file)).BackColor = Color.Red
                    ListView2.Items(ListView1.Items.IndexOf(file)).BackColor = Color.Red
                End If
                My.Computer.FileSystem.DeleteFile("test1.md5")
                My.Computer.FileSystem.DeleteFile("test2.md5")
            Next
            MsgBox("Done")
        Else
            MsgBox("Item size are different. Lists sizes must match.")
        End If
    End Sub
    Private Sub ffmpeg_process(Input As String, Item As Integer)
        Dim ffmpegProcessInfo As New ProcessStartInfo
        Dim ffmpegProcess As Process
        ffmpegProcessInfo.FileName = "ffmpeg.exe"
        ffmpegProcessInfo.Arguments = "-i """ + Input + """ -vn -f md5 test" + Item.ToString() + ".md5 -y"
        ffmpegProcessInfo.CreateNoWindow = True
        ffmpegProcessInfo.RedirectStandardOutput = False
        ffmpegProcessInfo.UseShellExecute = False
        ffmpegProcess = Process.Start(ffmpegProcessInfo)
        ffmpegProcess.WaitForExit()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ListView1.Items.Clear()
        ListView2.Items.Clear()
    End Sub

    Private Sub SaveResults(listView As ListView)
        Dim saveFileDialog As New SaveFileDialog With {
            .Title = "Save the verified files list.",
            .Filter = "CSV File|*.csv"}
        Dim result As DialogResult = saveFileDialog.ShowDialog
        If result = DialogResult.OK Then
            Dim VerifiedResults As String = ""
            For Each item In listView.Items
                Dim HashResult As String = "NOT CHECKED"
                If listView.Items(listView.Items.IndexOf(item)).BackColor = Color.LimeGreen Then
                    HashResult = "MATCH"
                ElseIf listView.Items(listView.Items.IndexOf(item)).BackColor = Color.Red Then
                    HashResult = "MISMATCH"
                End If
                VerifiedResults += """" + listView.Items(listView.Items.IndexOf(item)).Text + """," + HashResult + Environment.NewLine
            Next
            IO.File.WriteAllText(saveFileDialog.FileName, VerifiedResults)
            MessageBox.Show("File list saved")
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        SaveResults(ListView2)
    End Sub

    Private Sub CheckFfmpeg()
        Try
            Dim ffmpegProcessInfo As New ProcessStartInfo
            Dim ffmpegProcess As Process
            ffmpegProcessInfo.FileName = "ffmpeg.exe"
            ffmpegProcessInfo.CreateNoWindow = True
            ffmpegProcessInfo.RedirectStandardError = True
            ffmpegProcessInfo.UseShellExecute = False
            ffmpegProcess = Process.Start(ffmpegProcessInfo)
            ffmpegProcess.WaitForExit()
        Catch ex As Exception
            MessageBox.Show("ffmpeg.exe was not found. Exiting")
            Me.Close()
        End Try
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CheckFfmpeg()
    End Sub
End Class