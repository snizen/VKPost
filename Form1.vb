Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Xml
Imports Newtonsoft.Json

Public Class Form1
    Dim navigateState As Integer = 0
    Dim tmpNavigate As String
    Dim tmpPost As String

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        WebBrowser1.Navigate("https://oauth.vk.com/authorize?client_id=5799717&display=page&redirect_uri=https://oauth.vk.com/blank.html&scope=friends,photos,messages,wall&response_type=token&v=5.60&state=123456")
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
                navigateState = 3
            End If
        End If
        If navigateState = 3 Then
            TextBoxGroupID1.Text = getFromXML("https://api.vk.com/method/groups.get.xml?user_id=" & TextBoxUserID1.Text & "&filter=moder&access_token=" & TextBoxToken1.Text & "&v=5.60", "/response/items", "gid")
            navigateState = 4
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        OpenFileDialog1.InitialDirectory = "c:\"
        OpenFileDialog1.Title = "Открыть изображение"
        OpenFileDialog1.Filter = "Файл изображения|*.jpg"
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then PostToWall(OpenFileDialog1.FileName, False)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If TextBoxGroupID1.Text <> "" Then
            OpenFileDialog1.InitialDirectory = "c:\"
            OpenFileDialog1.Title = "Открыть изображение"
            OpenFileDialog1.Filter = "Файл изображения|*.jpg"
            If OpenFileDialog1.ShowDialog() = DialogResult.OK Then PostToWall(OpenFileDialog1.FileName, True)
        Else
            MsgBox("Не заполнен ID группы")
        End If
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

        TextBoxDescription1.Text = ""

        'Берем описание картинки из общего caption.txt
        Try
            Dim tFilesDir As String = FileIO.FileSystem.GetParentPath(tFileName)
            Dim txtReaderC1 As New System.IO.StreamReader(tFilesDir & "\caption.txt")
            TextBoxDescription1.Text = txtReaderC1.ReadToEnd
        Catch
        End Try

        'Заменяем описание картинки из одноименного txt если он есть
        Try
            Dim txtReaderC2 As New System.IO.StreamReader(Replace(tFileName, ".jpg", "-caption.txt"))
            TextBoxDescription1.Text = txtReaderC2.ReadToEnd
        Catch
        End Try

        'Формируем хэштеги для картинки из имени файла в формате ИМЯПОЛЬЗОВАТЕЛЯ - ИМЯАВТОРА - НАЗВАНИЕФОТО - ИДЕНТИФИКАТОРФОТО - ЧТОНИБУДЬЕЩЕ ,разделенное " - "
        Try
            Dim tFileNameN As String = FileIO.FileSystem.GetName(tFileName)
            tFileNameN = Replace(tFileNameN, ".jpg", "")
            Dim substrings() As String = tFileNameN.Split(" - ")
            For Each substring In substrings
                substring = Replace(substring, "  ", " ")
                substring = Replace(substring, " ", "_")
                'Формируем описание с хэштэгами
                If substring <> "-" And substring <> " " And substring <> "" Then TextBoxDescription1.Text = TextBoxDescription1.Text & " #" & substring
            Next
        Catch
        End Try

        Dim tmpDescription As String = ConvEscape(TextBoxDescription1.Text)

        Dim xmlDoc As New XmlDocument
        xmlDoc.Load("https://api.vk.com/method/photos.saveWallPhoto.xml?server=" & TextBox2.Text & "&photo=" & TextBox3.Text & "&caption=" & tmpDescription & "&hash=" & TextBox4.Text & "&access_token=" & TextBoxToken1.Text & "&v=5.60")
        Dim answers As XmlNodeList = xmlDoc.DocumentElement.SelectNodes("/response/photo")
        For Each element As XmlNode In answers

            'Берем текст поста из message.txt
            Try
                Dim tFilesDir As String = FileIO.FileSystem.GetParentPath(tFileName)
                Dim txtReaderM1 As New System.IO.StreamReader(tFilesDir & "\message.txt")
                TextBoxMessage1.Text = txtReaderM1.ReadToEnd
            Catch
            End Try

            'Заменяем текст поста текстом из одноименного txt если он есть
            Try
                Dim txtReaderM2 As New System.IO.StreamReader(Replace(tFileName, ".jpg", "-message.txt"))
                TextBoxMessage1.Text = txtReaderM2.ReadToEnd
            Catch
            End Try

            Dim tmpMessage As String = ConvEscape(TextBoxMessage1.Text)

            If postToGroupWall = False Then
                'Публикуем пост с картинкой на страницу пользователя
                navigateState = 1
                WebBrowser1.Navigate("https://api.vk.com/method/wall.post.xml?owner_id=" & TextBoxUserID1.Text & "&attachments=photo" & element.SelectSingleNode("owner_id").InnerText & "_" & element.SelectSingleNode("id").InnerText & "&message=" & tmpMessage & "&access_token=" & TextBoxToken1.Text & "&v=5.60")
            Else
                'Публикуем пост с картинкой на страницу группы
                If TextBoxGroupID1.Text <> "" Then
                    navigateState = 2
                    tmpPost = getFromXML("https://api.vk.com/method/wall.post.xml?owner_id=-" & TextBoxGroupID1.Text & "&attachments=photo" & element.SelectSingleNode("owner_id").InnerText & "_" & element.SelectSingleNode("id").InnerText & "&message=" & tmpMessage & "&access_token=" & TextBoxToken1.Text & "&v=5.60", "/response", "post_id")

                    'Лайкаем созданный пост
                    If CheckBox3.Checked = True Then WebBrowser1.Navigate("https://api.vk.com/method/likes.add.xml?owner_id=-" & TextBoxGroupID1.Text & "&type=post&item_id=" & tmpPost & "&access_token=" & TextBoxToken1.Text & "&v=5.60")
                Else
                    MsgBox("Не заполнен ID группы")
                End If
            End If
        Next
    End Sub

    Public Function ConvEscape(ByVal str As String) As String
        Dim utf8 As Encoding = Encoding.GetEncoding("utf-8")
        Dim win1251 As Encoding = Encoding.GetEncoding("windows-1251")
        Dim str1 As Byte() = win1251.GetBytes(str)
        Dim str2 As Byte() = Encoding.Convert(win1251, utf8, str1)
        Dim Result As String = ""
        For i = 0 To str2.Count - 1
            Result = Result & "%" & Hex(str2(i))
        Next
        Return Result
    End Function

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
            For Each dName As String In Directory.GetDirectories(FolderBrowserDialog1.SelectedPath)
                For Each fName As String In Directory.GetFiles(dName, "*.jpg")
                    ListView1.Items.Add(fName)
                Next
            Next
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

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click
        ListView2.Items.Clear()

        Dim xmlDocpost As New XmlDocument
        xmlDocpost.Load("https://api.vk.com/method/fave.getPosts.xml?count=999&access_token=" & TextBoxToken1.Text & "&v=5.60")
        Dim answerspost As XmlNodeList = xmlDocpost.DocumentElement.SelectNodes("/response/items/post")
        For Each element As XmlNode In answerspost
            ListView2.Items.Add("post")
            ListView2.Items.Item(ListView2.Items.Count - 1).SubItems.Add(element.SelectSingleNode("id").InnerText)
            ListView2.Items.Item(ListView2.Items.Count - 1).SubItems.Add(element.SelectSingleNode("owner_id").InnerText)
        Next

        Dim xmlDocphoto As New XmlDocument
        xmlDocphoto.Load("https://api.vk.com/method/fave.getPhotos.xml?count=999&access_token=" & TextBoxToken1.Text & "&v=5.60")
        Dim answersphoto As XmlNodeList = xmlDocphoto.DocumentElement.SelectNodes("/response/items/photo")
        For Each element As XmlNode In answersphoto
            ListView2.Items.Add("photo")
            ListView2.Items.Item(ListView2.Items.Count - 1).SubItems.Add(element.SelectSingleNode("id").InnerText)
            ListView2.Items.Item(ListView2.Items.Count - 1).SubItems.Add(element.SelectSingleNode("owner_id").InnerText)
        Next
        Label15.Text = "Всего лайков: " & ListView2.Items.Count - 1
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        If TextBox5.Text = "" Then
            If MsgBox("Поле 'Owner ID' не заполнено, будут удалены все лайки!", vbOKCancel) = vbOK Then
                VKTimer3.Enabled = True
            End If
        Else
            VKTimer3.Enabled = True
        End If
    End Sub

    Private Sub WebBrowser2_DocumentCompleted(sender As Object, e As WebBrowserDocumentCompletedEventArgs) Handles WebBrowser2.DocumentCompleted
        VKTimer3.Enabled = True
    End Sub

    Private Sub VKTimer3_Tick(sender As Object, e As EventArgs) Handles VKTimer3.Tick
        Dim rnd As New Random
        If ListView2.Items.Count > 0 Then
            If TextBox5.Text = ListView2.Items.Item(0).SubItems(2).Text Or TextBox5.Text = "" Then
                VKTimer3.Enabled = False
                WebBrowser2.Navigate("https://api.vk.com/method/likes.delete.xml?owner_id=" & ListView2.Items.Item(0).SubItems(2).Text & "&type=" & ListView2.Items.Item(0).Text & "&item_id=" & ListView2.Items.Item(0).SubItems(1).Text & "&access_token=" & TextBoxToken1.Text & "&v=5.62")
                VKTimer3.Interval = rnd.Next(3000, 6000)
            Else
                VKTimer3.Interval = 50
            End If
            ListView2.Items.Item(0).Remove()
            Label16.Text = "Осталось удалить: " & ListView2.Items.Count - 1
        End If
    End Sub

End Class