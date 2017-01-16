﻿Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Xml
Imports Newtonsoft.Json

Public Class Form1
    Dim navigateState As Integer = 0
    Dim tmpNavigate As String
    Dim tmpPost As String

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ListView1.Columns.Item(0).Width = ListView1.Width - 24
        WebBrowser1.Navigate("https://oauth.vk.com/authorize?client_id=5799717&display=page&redirect_uri=https://oauth.vk.com/blank.html&scope=photos,messages,wall&response_type=token&v=5.60&state=123456")
    End Sub

    Private Sub WebBrowser1_DocumentCompleted(sender As Object, e As WebBrowserDocumentCompletedEventArgs) Handles WebBrowser1.DocumentCompleted
        If navigateState = 0 Then
            If e.Url.ToString().IndexOf("access_token") <> -1 Then
                Dim adressTK() As String = e.Url.ToString().Split("=")
                Dim accessTK() As String = adressTK(1).Split("&")

                TextBoxToken1.Text = accessTK(0)
                Dim tmpUserID() As String = adressTK(3).Split("&")
                TextBoxUserID1.Text = tmpUserID(0)

                Dim xmlDoc As New XmlDocument
                xmlDoc.Load("https://api.vk.com/method/photos.getWallUploadServer.xml?access_token=" & TextBoxToken1.Text & "&v=5.60")
                Dim answers As XmlNodeList = xmlDoc.DocumentElement.SelectNodes("/response")
                For Each element As XmlNode In answers
                    TextBox1.Text = element.SelectSingleNode("upload_url").InnerText
                Next
                TabControl1.SelectedIndex = 1
            End If
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        OpenFileDialog1.InitialDirectory = "c:\"
        OpenFileDialog1.Title = "Открыть изображение"
        OpenFileDialog1.Filter = "Файл изображения|*.jpg"
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then PostToWall(OpenFileDialog1.FileName, False)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        OpenFileDialog1.InitialDirectory = "c:\"
        OpenFileDialog1.Title = "Открыть изображение"
        OpenFileDialog1.Filter = "Файл изображения|*.jpg"
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then PostToWall(OpenFileDialog1.FileName, True)
    End Sub

    Public Sub PostToWall(tFileName As String, postToGroupWall As Boolean)
        Dim form As New MultipartForm(TextBox1.Text)
        form.setField("name", "photo") 'имя поля для запроса
        form.sendFile(tFileName)
        TextBoxJSON1.Text = form.ResponseText.ToString

        Using sr As StringReader = New StringReader(TextBoxJSON1.Text)
            Using jr As JsonReader = New JsonTextReader(sr)
                jr.Read()

                jr.Read()
                jr.Read()
                TextBox2.Text = jr.Value 'server

                jr.Read()
                jr.Read()
                TextBox3.Text = jr.Value 'photos_list

                TextBox3.Text = Replace(TextBox3.Text, "\""", """")

                Using ssr As StringReader = New StringReader(TextBox3.Text)
                    Using sjr As JsonReader = New JsonTextReader(ssr)

                        sjr.Read()
                        sjr.Read()
                        sjr.Read()
                        sjr.Read()
                        TextBoxPhotoId1.Text = sjr.Value 'photo_id

                    End Using
                End Using

                jr.Read()
                jr.Read()
                TextBox4.Text = jr.Value 'hash
            End Using
        End Using

        'Берем описание картинки из caption.txt
        Try
            Dim tFilesDir As String = FileIO.FileSystem.GetParentPath(tFileName)
            Dim txtReaderC1 As New System.IO.StreamReader(tFilesDir & "\caption.txt")
            TextBoxDescription1.Text = txtReaderC1.ReadToEnd
        Catch
        End Try

        'Добавляем к описанию картинки текст из одноименного txt
        Try
            Dim txtReaderC2 As New System.IO.StreamReader(Replace(tFileName, ".jpg", "-caption.txt"))
            TextBoxDescription1.Text = TextBoxDescription1.Text & " " & txtReaderC2.ReadToEnd
        Catch
        End Try

        TextBoxDescription1.Text = Replace(TextBoxDescription1.Text, "#", "%23") ' %23 - номер символа # в unicode

        Dim xmlDoc As New XmlDocument
        xmlDoc.Load("https://api.vk.com/method/photos.saveWallPhoto.xml?server=" & TextBox2.Text & "&photo=" & TextBox3.Text & "&caption=" & TextBoxDescription1.Text & "&hash=" & TextBox4.Text & "&access_token=" & TextBoxToken1.Text & "&v=5.60")
        Dim answers As XmlNodeList = xmlDoc.DocumentElement.SelectNodes("/response/photo")
        For Each element As XmlNode In answers

            'Берем текст поста из message.txt
            Try
                Dim tFilesDir As String = FileIO.FileSystem.GetParentPath(tFileName)
                Dim txtReaderM1 As New System.IO.StreamReader(tFilesDir & "\message.txt")
                TextBoxMessage1.Text = txtReaderM1.ReadToEnd
            Catch
            End Try

            'Добавляем к тексту поста текст из одноименного txt
            Try
                Dim txtReaderM2 As New System.IO.StreamReader(Replace(tFileName, ".jpg", "-message.txt"))
                TextBoxMessage1.Text = TextBoxMessage1.Text & " " & txtReaderM2.ReadToEnd
            Catch
            End Try

            TextBoxMessage1.Text = Replace(TextBoxMessage1.Text, "#", "%23")

            If postToGroupWall = False Then
                navigateState = 1
                'Публикуем пост с картинкой на страницу пользователя
                WebBrowser1.Navigate("https://api.vk.com/method/wall.post.xml?owner_id=" & TextBoxUserID1.Text & "&attachments=photo" & element.SelectSingleNode("owner_id").InnerText & "_" & element.SelectSingleNode("id").InnerText & "&message=" & TextBoxMessage1.Text & "&access_token=" & TextBoxToken1.Text & "&v=5.60")
            Else
                navigateState = 2
                'Публикуем пост с картинкой на страницу группы
                tmpPost = getFromXML("https://api.vk.com/method/wall.post.xml?owner_id=-" & TextBoxGroupID1.Text & "&attachments=photo" & element.SelectSingleNode("owner_id").InnerText & "_" & element.SelectSingleNode("id").InnerText & "&message=" & TextBoxMessage1.Text & "&access_token=" & TextBoxToken1.Text & "&v=5.60", "/response", "post_id")
                'Лайкаем созданный пост
                If CheckBox3.Checked = True Then WebBrowser1.Navigate("https://api.vk.com/method/likes.add.xml?owner_id=-" & TextBoxGroupID1.Text & "&type=post&item_id=" & tmpPost & "&access_token=" & TextBoxToken1.Text & "&v=5.60")
            End If
        Next
    End Sub

    Public Function getFromXML(URL As String, selectNode As String, returnValue As String) As String
        Dim xmlDoc As New XmlDocument
        xmlDoc.Load(URL)
        Dim answers As XmlNodeList = xmlDoc.DocumentElement.SelectNodes(selectNode)
        For Each element As XmlNode In answers
            returnValue = element.SelectSingleNode(returnValue).InnerText
            Return returnValue
        Next
    End Function

    Public Function toUnicode(srcString As String) As String
        Dim asciiString As String = srcString
        Dim ascii As Encoding = Encoding.ASCII
        Dim unicode As Encoding = Encoding.Unicode
        Dim asciiBytes As Byte() = ascii.GetBytes(asciiString)

        Dim unicodeBytes As Byte() = Encoding.Convert(ascii, unicode, asciiBytes)
        Dim unicodeChars(unicode.GetCharCount(unicodeBytes, 0, unicodeBytes.Length) - 1) As Char
        unicode.GetChars(unicodeBytes, 0, unicodeBytes.Length, unicodeChars, 0)
        Dim decodedString As New String(unicodeChars)

        Return decodedString
    End Function

    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs) Handles TrackBar1.Scroll
        Label12.Text = "Интервал, " & TrackBar1.Value & " мин."
        VKTimer1.Interval = TrackBar1.Value * 60000
        ToolTip1.SetToolTip(TrackBar1, "Текущий интервал автоматического постинга: " & VKTimer1.Interval & " мин.")
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If CheckBox1.Checked = True Or CheckBox2.Checked = True Then
            VKTimer1.Enabled = True
        Else
            VKTimer1.Enabled = False
        End If
    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged
        If CheckBox1.Checked = True Or CheckBox2.Checked = True Then
            VKTimer1.Enabled = True
        Else
            VKTimer1.Enabled = False
        End If
    End Sub

    Private Sub VKTimer1_Tick(sender As Object, e As EventArgs) Handles VKTimer1.Tick
        If ListView1.Items.Count > 0 Then
            If CheckBox1.Checked = True Then PostToWall(ListView1.Items.Item(0).Text, False)
            If CheckBox2.Checked = True Then PostToWall(ListView1.Items.Item(0).Text, True)
            ListView1.Items.Item(0).Remove()
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        If FolderBrowserDialog1.ShowDialog() = DialogResult.OK Then
            For Each fName As String In Directory.GetFiles(FolderBrowserDialog1.SelectedPath, "*.jpg")
                ListView1.Items.Add(fName)
            Next
        End If
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        ListView1.Items.Clear()
    End Sub

    Private Sub NotifyIcon1_MouseClick(sender As Object, e As MouseEventArgs) Handles NotifyIcon1.MouseClick
        If e.Button = MouseButtons.Left Then
            Me.ShowInTaskbar = True
            Me.Show()
            Me.WindowState = FormWindowState.Normal
            Me.Activate()
        End If
    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If Me.WindowState = FormWindowState.Minimized Then
            Me.Hide()
            Me.ShowInTaskbar = False
        End If
    End Sub

    Private Sub TextBoxMessage1_TextChanged(sender As Object, e As EventArgs) Handles TextBoxMessage1.TextChanged
        '
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        Dim Random As New System.Random
        Dim ArrayList As New System.Collections.ArrayList(ListView1.Items)
        ListView1.Items.Clear()
        While ArrayList.Count > 0
            Dim Index As System.Int32 = Random.Next(0, ArrayList.Count)
            ListView1.Items.Add(ArrayList(Index))
            ArrayList.RemoveAt(Index)
        End While
    End Sub
End Class

'escape последовательность слова «привет» в кодировке UTF-8: 
'%D0%BF%D1%80%D0%B8%D0%B2%D0%B5%D1%82