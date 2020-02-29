Imports System.IO
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
            If Extensions.Contains(file.Extension.ToLower) Then listView.Items.Add(file.FullName)
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
            ListView1.SelectedItems.Clear()
            ListView2.SelectedItems.Clear()
            Dim ListView1Items As ListViewItem() = New ListViewItem(ListView1.Items.Count - 1) {}
            Dim ListView2Items As ListViewItem() = New ListViewItem(ListView2.Items.Count - 1) {}
            ListView1.Items.CopyTo(ListView1Items, 0)
            ListView2.Items.CopyTo(ListView2Items, 0)
            For Each item In ListView1Items
                ListBox1.Items.Add(String.Empty)
            Next
            Button1.Enabled = False
            Button2.Enabled = False
            Button3.Enabled = False
            Dim StartTasks As New Threading.Thread(Sub() CheckThread(ListView1Items, ListView2Items))
            StartTasks.Start()
        Else
            MsgBox("Item size are different. Lists sizes must match.")
        End If
    End Sub
    Private Sub CheckThread(ListView1Items As ListViewItem(), ListView2Items As ListViewItem())
        Dim tasks = New List(Of Action)
        For Each file In ListView1Items
            tasks.Add(Sub() FileChecker(file, ListView1Items, ListView2Items))
        Next
        Parallel.Invoke(New ParallelOptions With {.MaxDegreeOfParallelism = Environment.ProcessorCount}, tasks.ToArray())
        Button1.BeginInvoke(Sub()
                                Button1.Enabled = True
                                Button2.Enabled = True
                                Button3.Enabled = True
                            End Sub)
        MsgBox("Done")
    End Sub

    Private Sub FileChecker(file As ListViewItem, ListView1Items As ListViewItem(), ListView2Items As ListViewItem())
        Dim hash1 As String = ffmpeg_process(file.Text, 1)
        Dim hash2 As String = ffmpeg_process(ListView2Items(Array.IndexOf(ListView1Items, file)).Text, 2)
        If Not hash1 = String.Empty And Not hash2 = String.Empty Then
            ListBox1.BeginInvoke(Sub() ListBox1.Items(ListView1.Items.IndexOf(file)) = hash1)
            If hash1 = hash2 Then
                ListView1.BeginInvoke(Sub()
                                          ListView1.Items(ListView1.Items.IndexOf(file)).BackColor = Color.LimeGreen
                                          ListView2.Items(ListView1.Items.IndexOf(file)).BackColor = Color.LimeGreen
                                      End Sub)
            Else
                ListView1.BeginInvoke(Sub()
                                          ListView1.Items(ListView1.Items.IndexOf(file)).BackColor = Color.Red
                                          ListView2.Items(ListView1.Items.IndexOf(file)).BackColor = Color.Red
                                      End Sub)
            End If
        Else
            ListView1.BeginInvoke(Sub()
                                      ListView1.Items(ListView1.Items.IndexOf(file)).BackColor = Color.Tomato
                                      ListView2.Items(ListView1.Items.IndexOf(file)).BackColor = Color.Tomato
                                  End Sub)
        End If
        ListView1.BeginInvoke(Sub()
                                  ListView1.SelectedItems.Clear()
                                  ListView2.SelectedItems.Clear()
                              End Sub)
    End Sub
    Private Function ffmpeg_process(Input As String, Item As Integer) As String
        Dim ffmpegProcessInfo As New ProcessStartInfo
        Dim ffmpegProcess As Process
        ffmpegProcessInfo.FileName = "ffmpeg.exe"
        ffmpegProcessInfo.Arguments = "-i """ + Input + """ -vn -f md5 - -y"
        ffmpegProcessInfo.CreateNoWindow = True
        ffmpegProcessInfo.RedirectStandardOutput = True
        ffmpegProcessInfo.UseShellExecute = False
        ffmpegProcess = Process.Start(ffmpegProcessInfo)
        Dim CurrentLine As String = String.Empty
        While Not ffmpegProcess.HasExited
            While Not ffmpegProcess.StandardOutput.EndOfStream
                CurrentLine = ffmpegProcess.StandardOutput.ReadLine
                If CurrentLine.Contains("MD5=") Then
                    CurrentLine = CurrentLine.Split("=")(1)
                    Exit While
                End If
            End While
        End While
        Return CurrentLine
    End Function

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ListView1.Items.Clear()
        ListView2.Items.Clear()
        ListBox1.Items.Clear()
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
                VerifiedResults += """" + listView.Items(listView.Items.IndexOf(item)).Text + """," + HashResult + "," + ListBox1.Items(listView.Items.IndexOf(item)) + Environment.NewLine
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