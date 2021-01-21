Imports System.IO
Public Class Form1
    Private Extensions As String() = {".aiff", ".ape", ".flac", ".m4a", ".mp3", ".wav"}
    Private SourceFrameMd5List As List(Of String) = New List(Of String)
    Private DestFrameMd5List As List(Of String) = New List(Of String)
    Private SourceFrameMd5MismatchList As List(Of String) = New List(Of String)
    Private DestFrameMd5MismatchList As List(Of String) = New List(Of String)
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
                If Extensions.Contains(IO.Path.GetExtension(path).ToLower) Then listView.Items.Add(path)
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
            SourceFrameMd5List.Clear()
            DestFrameMd5List.Clear()
            SourceFrameMd5MismatchList.Clear()
            DestFrameMd5MismatchList.Clear()
            ListBox1.Items.Clear()
            ListBox2.Items.Clear()
            For Each item In ListView1Items
                SourceFrameMd5List.Add(String.Empty)
                SourceFrameMd5MismatchList.Add(String.Empty)
                DestFrameMd5List.Add(String.Empty)
                DestFrameMd5MismatchList.Add(String.Empty)
                ListBox1.Items.Add(String.Empty)
                ListBox2.Items.Add(String.Empty)
            Next
            Button1.Enabled = False
            Button2.Enabled = False
            Button3.Enabled = False
            Button4.Enabled = False
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
                                Button4.Enabled = True
                            End Sub)
        MsgBox("Done")
    End Sub
    Private Sub CheckThreadFlac(ListView1Items As ListViewItem())
        Dim tasks = New List(Of Action)
        For Each file In ListView1Items
            tasks.Add(Sub() FlacChecker(file, ListView1Items))
        Next
        Parallel.Invoke(New ParallelOptions With {.MaxDegreeOfParallelism = Environment.ProcessorCount}, tasks.ToArray())
        Button1.BeginInvoke(Sub()
                                Button1.Enabled = True
                                Button2.Enabled = True
                                Button3.Enabled = True
                                Button4.Enabled = True
                            End Sub)
        MsgBox("Done")
    End Sub

    Private Sub FileChecker(file As ListViewItem, ListView1Items As ListViewItem(), ListView2Items As ListViewItem())
        Dim hash1 As String = ffmpeg_process(file.Text)
        Dim hash2 As String = ffmpeg_process(ListView2Items(Array.IndexOf(ListView1Items, file)).Text)
        Dim FrameHash1 As String = ffmpeg__framemd5_process(file.Text)
        Dim FrameHash2 As String = ffmpeg__framemd5_process(ListView2Items(Array.IndexOf(ListView1Items, file)).Text)
        If Not hash1 = String.Empty And Not hash2 = String.Empty Then
            Dim FramesMismatches As Integer = 0
            Dim splittedFrameHash1 As String() = FrameHash1.Split(Environment.NewLine)
            Dim splittedFrameHash2 As String() = FrameHash2.Split(Environment.NewLine)
            Dim SourceMismatch As String = String.Empty
            Dim DestMismatch As String = String.Empty
            Dim counter As Integer = 0
            For Each Line In splittedFrameHash1
                If splittedFrameHash2.Count - 1 >= counter And splittedFrameHash1.Count - 1 >= counter Then
                    If Not Line.Contains("#") Then
                        If splittedFrameHash2(counter) <> Line Then
                            FramesMismatches += 1
                            SourceMismatch += Line.Trim() + Environment.NewLine
                            DestMismatch += splittedFrameHash2(counter).Trim() + Environment.NewLine
                        End If
                    End If
                Else
                    Exit For
                End If
                counter += 1
            Next
            If splittedFrameHash1.Count > splittedFrameHash2.Count Then
                For i = splittedFrameHash1.Count - 1 To splittedFrameHash2.Count - 1
                    FramesMismatches += 1
                    SourceMismatch += splittedFrameHash1(i).Trim() + Environment.NewLine
                Next
            ElseIf splittedFrameHash2.Count > splittedFrameHash1.Count Then
                For i = splittedFrameHash2.Count - 1 To splittedFrameHash1.Count - 1
                    FramesMismatches += 1
                    SourceMismatch += splittedFrameHash1(i).Trim() + Environment.NewLine
                Next
            End If
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
            ListBox1.BeginInvoke(Sub()
                                     ListBox1.Items(ListView1.Items.IndexOf(file)) = hash1
                                     ListBox2.Items(ListView1.Items.IndexOf(file)) = FramesMismatches.ToString()
                                     SourceFrameMd5List(ListView1.Items.IndexOf(file)) = FrameHash1
                                     DestFrameMd5List(ListView1.Items.IndexOf(file)) = FrameHash2
                                     SourceFrameMd5MismatchList(ListView1.Items.IndexOf(file)) = SourceMismatch
                                     DestFrameMd5MismatchList(ListView1.Items.IndexOf(file)) = DestMismatch
                                 End Sub)
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
    Private Sub FlacChecker(file As ListViewItem, ListView1Items As ListViewItem())
        Dim Result As String = flac_test(file.Text)
        If Result = "OK" Then
            ListView1.BeginInvoke(Sub()
                                      ListView1.Items(ListView1.Items.IndexOf(file)).BackColor = Color.LimeGreen
                                  End Sub)
        Else
            ListView1.BeginInvoke(Sub()
                                      ListView1.Items(ListView1.Items.IndexOf(file)).BackColor = Color.Red
                                  End Sub)
        End If
        ListBox1.BeginInvoke(Sub()
                                 ListBox1.Items(ListView1.Items.IndexOf(file)) = Result
                                 SourceFrameMd5List(ListView1.Items.IndexOf(file)) = Result
                             End Sub)

    End Sub
    Private Function ffmpeg_process(Input As String) As String
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

    Private Function flac_test(Input As String) As String
        Dim flacProcessInfo As New ProcessStartInfo
        Dim flacProcess As Process
        Dim ok As String = String.Empty
        flacProcessInfo.FileName = "flac.exe"
        flacProcessInfo.Arguments = "-t """ + Input + """"
        flacProcessInfo.CreateNoWindow = True
        flacProcessInfo.RedirectStandardError = True
        flacProcessInfo.UseShellExecute = False
        flacProcess = Process.Start(flacProcessInfo)
        Dim CurrentLine As String = String.Empty
        While Not flacProcess.HasExited
            While Not flacProcess.StandardError.EndOfStream
                CurrentLine = flacProcess.StandardError.ReadLine
                If CurrentLine.Contains("ok") Or CurrentLine.Contains(": ok") Then
                    ok = "OK"
                ElseIf CurrentLine.Contains("ERROR while decoding") Then
                    CurrentLine = flacProcess.StandardError.ReadLine
                    If CurrentLine.Contains("state = ") Then
                        Return CurrentLine.Split("=")(1).Trim()
                    End If
                    Exit While
                ElseIf CurrentLine.Contains("WARNING, ") Then
                    Dim splittedLine As String() = CurrentLine.Split(",")
                    If flacProcess.StandardError.ReadLine.Trim() = "ok" Then
                        Return "OK"
                    End If
                    Return splittedLine(splittedLine.Length - 1).Trim()
                End If
            End While
        End While
        Return ok
    End Function

    Private Function ffmpeg__framemd5_process(Input As String) As String
        Dim ffmpegProcessInfo As New ProcessStartInfo
        Dim ffmpegProcess As Process
        ffmpegProcessInfo.FileName = "ffmpeg.exe"
        ffmpegProcessInfo.Arguments = "-i """ + Input + """ -af asetnsamples=" + Convert.ToInt64(My.Settings.FrameSize).ToString + " -vn -f framemd5 - -y"
        ffmpegProcessInfo.CreateNoWindow = True
        ffmpegProcessInfo.RedirectStandardOutput = True
        ffmpegProcessInfo.UseShellExecute = False
        ffmpegProcess = Process.Start(ffmpegProcessInfo)
        Dim FrameMD5String As String = String.Empty
        While Not ffmpegProcess.HasExited
            While Not ffmpegProcess.StandardOutput.EndOfStream
                Dim CurrentLine As String = ffmpegProcess.StandardOutput.ReadLine.Trim + Environment.NewLine()
                FrameMD5String += CurrentLine
            End While
        End While
        Return FrameMD5String
    End Function
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ListView1.Items.Clear()
        ListView2.Items.Clear()
        ListBox1.Items.Clear()
        ListBox2.Items.Clear()
        SourceFrameMd5List.Clear()
        DestFrameMd5List.Clear()
        SourceFrameMd5MismatchList.Clear()
        DestFrameMd5MismatchList.Clear()
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
                VerifiedResults += """" + listView.Items(listView.Items.IndexOf(item)).Text + """," + HashResult + "," +
                    ListBox1.Items(listView.Items.IndexOf(item)) + ","
                If ListBox2.Items(listView.Items.IndexOf(item)) = "1" Then
                    VerifiedResults += " frame mismatch" + Environment.NewLine
                Else
                    VerifiedResults += " frame mismatches" + Environment.NewLine
                End If
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

    Private Sub ListView1_DoubleClick(sender As Object, e As EventArgs) Handles ListView1.DoubleClick
        If ListView1.SelectedItems.Count > 0 And SourceFrameMd5List.Count > ListView1.SelectedItems.Item(0).Index Then
            FrameMD5Viewer.RichTextBox1.Text = SourceFrameMd5List(ListView1.SelectedItems.Item(0).Index)
            FrameMD5Viewer.RichTextBox2.Text = DestFrameMd5List(ListView1.SelectedItems.Item(0).Index)
            FrameMD5Viewer.ShowDialog()
        End If
    End Sub

    Private Sub ListView2_DoubleClick(sender As Object, e As EventArgs) Handles ListView2.DoubleClick
        If ListView2.SelectedItems.Count > 0 And SourceFrameMd5List.Count > ListView2.SelectedItems.Item(0).Index Then
            FrameMD5Viewer.RichTextBox1.Text = SourceFrameMd5List(ListView2.SelectedItems.Item(0).Index)
            FrameMD5Viewer.RichTextBox2.Text = DestFrameMd5List(ListView2.SelectedItems.Item(0).Index)
            FrameMD5Viewer.ShowDialog()
        End If
    End Sub

    Private Sub ListBox2_DoubleClick(sender As Object, e As EventArgs) Handles ListBox2.DoubleClick
        If ListView1.Items.Count > 0 And ListView2.Items.Count > 0 Then
            If ListBox2.SelectedIndices.Count > 0 Then
                FrameMD5Viewer.RichTextBox1.Text = SourceFrameMd5MismatchList(ListBox2.SelectedIndex)
                FrameMD5Viewer.RichTextBox2.Text = DestFrameMd5MismatchList(ListBox2.SelectedIndex)
                FrameMD5Viewer.ShowDialog()
            End If
        End If
    End Sub

    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        FrameSize.ShowDialog()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        ListView1.SelectedItems.Clear()
        Dim ListView1Items As ListViewItem() = New ListViewItem(ListView1.Items.Count - 1) {}
        ListView1.Items.CopyTo(ListView1Items, 0)
        SourceFrameMd5List.Clear()
        DestFrameMd5List.Clear()
        SourceFrameMd5MismatchList.Clear()
        DestFrameMd5MismatchList.Clear()
        ListBox1.Items.Clear()
        For Each item In ListView1Items
            SourceFrameMd5List.Add(String.Empty)
            SourceFrameMd5MismatchList.Add(String.Empty)
            DestFrameMd5List.Add(String.Empty)
            DestFrameMd5MismatchList.Add(String.Empty)
            ListBox1.Items.Add(String.Empty)
            ListBox2.Items.Add(String.Empty)
        Next
        Button1.Enabled = False
        Button2.Enabled = False
        Button3.Enabled = False
        Button4.Enabled = False
        Dim StartTasks As New Threading.Thread(Sub() CheckThreadFlac(ListView1Items))
        StartTasks.Start()
    End Sub
End Class